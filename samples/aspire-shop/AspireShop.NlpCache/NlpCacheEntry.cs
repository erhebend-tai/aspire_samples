// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// Non-generic interface that exposes expiry metadata for all cache entries.
/// </summary>
internal interface INlpCacheEntry
{
    bool IsExpired { get; }
}

/// <summary>
/// A single entry held inside <see cref="INlpCache"/>.  Associates a <see cref="TokenKey"/>
/// with an arbitrary value and tracks when the entry was created and when it expires.
/// </summary>
/// <typeparam name="TValue">The type of the cached value (e.g. <c>float[]</c> for embeddings).</typeparam>
public sealed class NlpCacheEntry<TValue> : INlpCacheEntry
{
    /// <summary>The immutable key used to look up this entry.</summary>
    public TokenKey Key { get; }

    /// <summary>The cached value.</summary>
    public TValue Value { get; }

    /// <summary>UTC time at which this entry was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>UTC time at which this entry expires, or <see langword="null"/> for no expiry.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>Returns <see langword="true"/> when the entry has passed its expiry time.</summary>
    public bool IsExpired =>
        ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;

    internal NlpCacheEntry(TokenKey key, TValue value, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }
}
