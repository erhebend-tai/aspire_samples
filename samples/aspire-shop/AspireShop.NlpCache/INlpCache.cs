// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// A cache keyed by <see cref="TokenKey"/> that can store arbitrary typed values as well as
/// dense float vectors suitable for NLP / ML embeddings.
/// </summary>
public interface INlpCache
{
    // ── Generic value API ────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to retrieve a cached value for <paramref name="key"/>.
    /// </summary>
    /// <returns><see langword="true"/> when a non-expired entry was found.</returns>
    bool TryGet<TValue>(TokenKey key, out TValue? value);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    /// <param name="ttl">
    /// Optional time-to-live.  When <see langword="null"/> the cache-level default from
    /// <see cref="NlpCacheOptions.DefaultTtl"/> is used; if that is also <see langword="null"/>
    /// the entry never expires.
    /// </param>
    void Set<TValue>(TokenKey key, TValue value, TimeSpan? ttl = null);

    /// <summary>Removes the entry associated with <paramref name="key"/>, if any.</summary>
    void Remove(TokenKey key);

    // ── Vector API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to retrieve a stored embedding vector for <paramref name="key"/>.
    /// </summary>
    /// <returns><see langword="true"/> when a non-expired vector was found.</returns>
    bool TryGetVector(TokenKey key, out float[]? vector);

    /// <summary>Stores a dense float vector (e.g. an ML embedding) for <paramref name="key"/>.</summary>
    void SetVector(TokenKey key, float[] vector, TimeSpan? ttl = null);

    // ── Inspection ────────────────────────────────────────────────────────────

    /// <summary>All keys currently held in the cache (including expired entries).</summary>
    IEnumerable<TokenKey> Keys { get; }

    /// <summary>Number of entries currently in the cache (including expired entries).</summary>
    int Count { get; }

    /// <summary>Removes all entries from the cache.</summary>
    void Clear();

    /// <summary>Evicts all entries whose TTL has elapsed.</summary>
    void Purge();
}
