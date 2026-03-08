// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// Configuration options for <see cref="NlpCache"/>.
/// </summary>
public sealed class NlpCacheOptions
{
    /// <summary>
    /// Default time-to-live applied when a caller does not supply an explicit TTL.
    /// Set to <see langword="null"/> (the default) for entries that never expire.
    /// </summary>
    public TimeSpan? DefaultTtl { get; init; } = null;

    /// <summary>
    /// Maximum number of entries the cache will hold.  When the limit is reached the
    /// oldest entries are evicted to make room.  Set to <c>0</c> (default) for unlimited.
    /// </summary>
    public int MaxEntries { get; set; } = 0;

    /// <summary>
    /// When <see langword="true"/> (default) a background pass evicts expired entries
    /// whenever <see cref="INlpCache.Set{TValue}"/> is called and the cache is over
    /// half-full.
    /// </summary>
    public bool AutoPurge { get; set; } = true;
}
