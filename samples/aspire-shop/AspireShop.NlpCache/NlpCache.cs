// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspireShop.NlpCache;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="INlpCache"/>.
///
/// <para>
/// Values of different types (and float vectors) are stored in separate internal buckets so
/// that a <c>Set&lt;string&gt;</c> followed by <c>TryGet&lt;float[]&gt;</c> for the same key
/// returns <see langword="false"/> instead of throwing an <see cref="InvalidCastException"/>.
/// </para>
/// </summary>
public sealed class NlpCache : INlpCache
{
    // Each unique System.Type gets its own concurrent dictionary of raw boxed entries.
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<TokenKey, object>> _buckets = new();

    // Vectors live in a dedicated bucket for fast typed access.
    private readonly ConcurrentDictionary<TokenKey, NlpCacheEntry<float[]>> _vectors = new();

    private readonly NlpCacheOptions _options;
    private readonly ILogger<NlpCache> _logger;

    /// <summary>
    /// Initialises a new <see cref="NlpCache"/> with the supplied options and logger.
    /// </summary>
    public NlpCache(IOptions<NlpCacheOptions> options, ILogger<NlpCache> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── INlpCache : Generic value API ─────────────────────────────────────────

    /// <inheritdoc/>
    public bool TryGet<TValue>(TokenKey key, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        var bucket = GetBucket<TValue>();
        if (bucket.TryGetValue(key, out var raw) && raw is NlpCacheEntry<TValue> entry)
        {
            if (entry.IsExpired)
            {
                bucket.TryRemove(key, out _);
                _logger.LogDebug("Cache miss (expired): {Key}", key);
                value = default;
                return false;
            }

            _logger.LogDebug("Cache hit: {Key}", key);
            value = entry.Value;
            return true;
        }

        _logger.LogDebug("Cache miss: {Key}", key);
        value = default;
        return false;
    }

    /// <inheritdoc/>
    public void Set<TValue>(TokenKey key, TValue value, TimeSpan? ttl = null)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        var effectiveTtl = ttl ?? _options.DefaultTtl;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = effectiveTtl.HasValue ? now.Add(effectiveTtl.Value) : (DateTimeOffset?)null;

        var entry = new NlpCacheEntry<TValue>(key, value, now, expiresAt);
        var bucket = GetBucket<TValue>();

        EnforceCapacity(bucket);
        bucket[key] = entry;

        _logger.LogDebug("Cache set: {Key} (expires: {ExpiresAt})", key, expiresAt?.ToString("u") ?? "never");
    }

    /// <inheritdoc/>
    public void Remove(TokenKey key)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        foreach (var bucket in _buckets.Values)
        {
            bucket.TryRemove(key, out _);
        }
        _vectors.TryRemove(key, out _);
    }

    // ── INlpCache : Vector API ────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool TryGetVector(TokenKey key, out float[]? vector)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        if (_vectors.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _vectors.TryRemove(key, out _);
                vector = null;
                return false;
            }
            vector = entry.Value;
            return true;
        }

        vector = null;
        return false;
    }

    /// <inheritdoc/>
    public void SetVector(TokenKey key, float[] vector, TimeSpan? ttl = null)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        ArgumentNullException.ThrowIfNull(vector, nameof(vector));

        var effectiveTtl = ttl ?? _options.DefaultTtl;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = effectiveTtl.HasValue ? now.Add(effectiveTtl.Value) : (DateTimeOffset?)null;
        _vectors[key] = new NlpCacheEntry<float[]>(key, vector, now, expiresAt);
    }

    // ── INlpCache : Inspection ────────────────────────────────────────────────

    /// <inheritdoc/>
    public IEnumerable<TokenKey> Keys
    {
        get
        {
            var seen = new HashSet<TokenKey>();
            foreach (var bucket in _buckets.Values)
            {
                foreach (var k in bucket.Keys)
                {
                    seen.Add(k);
                }
            }
            foreach (var k in _vectors.Keys)
            {
                seen.Add(k);
            }
            return seen;
        }
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            var seen = new HashSet<TokenKey>();
            foreach (var bucket in _buckets.Values)
            {
                foreach (var k in bucket.Keys)
                {
                    seen.Add(k);
                }
            }
            foreach (var k in _vectors.Keys)
            {
                seen.Add(k);
            }
            return seen.Count;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
        }
        _vectors.Clear();
        _logger.LogDebug("Cache cleared.");
    }

    /// <inheritdoc/>
    public void Purge()
    {
        int removed = 0;
        foreach (var bucket in _buckets.Values)
        {
            foreach (var kvp in bucket)
            {
                if (kvp.Value is INlpCacheEntry entry && entry.IsExpired)
                {
                    if (bucket.TryRemove(kvp.Key, out _))
                    {
                        removed++;
                    }
                }
            }
        }

        foreach (var kvp in _vectors)
        {
            if (kvp.Value.IsExpired && _vectors.TryRemove(kvp.Key, out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Purged {Count} expired cache entries.", removed);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private ConcurrentDictionary<TokenKey, object> GetBucket<TValue>() =>
        _buckets.GetOrAdd(typeof(TValue), _ => new ConcurrentDictionary<TokenKey, object>());

    private void EnforceCapacity<TBucketValue>(ConcurrentDictionary<TokenKey, TBucketValue> bucket)
    {
        if (_options.MaxEntries <= 0)
        {
            return;
        }

        // Auto-purge when over half-full to avoid thrashing.
        if (_options.AutoPurge && bucket.Count >= _options.MaxEntries / 2)
        {
            Purge();
        }

        // Evict the first (oldest insertion order approximation) entries until under the cap.
        while (bucket.Count >= _options.MaxEntries)
        {
            var firstKey = bucket.Keys.FirstOrDefault();
            if (firstKey is null)
            {
                break;
            }
            bucket.TryRemove(firstKey, out _);
        }
    }
}
