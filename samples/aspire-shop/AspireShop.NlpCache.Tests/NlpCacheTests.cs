// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspireShop.NlpCache;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AspireShop.NlpCache.Tests;

public class NlpCacheTests
{
    private static NlpCache MakeCache(NlpCacheOptions? opts = null)
    {
        var options = Options.Create(opts ?? new NlpCacheOptions());
        return new NlpCache(options, NullLogger<NlpCache>.Instance);
    }

    private static TokenKey Key(string text, int start, int end, TokenType type = TokenType.Identifier) =>
        new()
        {
            Text = text,
            Span = new TokenSpan(start, end),
            Type = type,
        };

    // ── Basic set / get ───────────────────────────────────────────────────────

    [Fact]
    public void Set_ThenTryGet_ReturnsValue()
    {
        var cache = MakeCache();
        var key = Key("foo", 0, 3);
        cache.Set(key, "bar");

        var found = cache.TryGet<string>(key, out var value);
        Assert.True(found);
        Assert.Equal("bar", value);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var cache = MakeCache();
        var key = Key("missing", 0, 7);
        var found = cache.TryGet<string>(key, out var value);
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void Set_Overwrites_ExistingEntry()
    {
        var cache = MakeCache();
        var key = Key("x", 0, 1);
        cache.Set(key, "first");
        cache.Set(key, "second");

        cache.TryGet<string>(key, out var value);
        Assert.Equal("second", value);
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_DeletesEntry()
    {
        var cache = MakeCache();
        var key = Key("y", 0, 1);
        cache.Set(key, 42);
        cache.Remove(key);

        Assert.False(cache.TryGet<int>(key, out _));
    }

    // ── TTL / expiry ──────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_ReturnsFalse_AfterTtlExpires()
    {
        var cache = MakeCache();
        var key = Key("z", 0, 1);
        cache.Set(key, "data", ttl: TimeSpan.FromMilliseconds(1));

        System.Threading.Thread.Sleep(50); // wait for expiry

        var found = cache.TryGet<string>(key, out _);
        Assert.False(found);
    }

    [Fact]
    public void DefaultTtl_AppliesToEntriesWithNoExplicitTtl()
    {
        var opts = new NlpCacheOptions { DefaultTtl = TimeSpan.FromMilliseconds(1) };
        var cache = MakeCache(opts);
        var key = Key("q", 0, 1);
        cache.Set(key, "hello");

        System.Threading.Thread.Sleep(50);

        Assert.False(cache.TryGet<string>(key, out _));
    }

    // ── Vector API ────────────────────────────────────────────────────────────

    [Fact]
    public void SetVector_ThenTryGetVector_ReturnsVector()
    {
        var cache = MakeCache();
        var key = Key("embed", 0, 5);
        float[] vec = [0.1f, 0.2f, 0.3f];
        cache.SetVector(key, vec);

        var found = cache.TryGetVector(key, out var result);
        Assert.True(found);
        Assert.Equal(vec, result);
    }

    [Fact]
    public void TryGetVector_MissingKey_ReturnsFalse()
    {
        var cache = MakeCache();
        var key = Key("absent", 0, 6);
        Assert.False(cache.TryGetVector(key, out _));
    }

    [Fact]
    public void SetVector_TryGet_ForDifferentType_ReturnsFalse()
    {
        var cache = MakeCache();
        var key = Key("tok", 0, 3);
        cache.SetVector(key, [1f, 2f]);

        // vector is stored separately from generic string entries
        Assert.False(cache.TryGet<string>(key, out _));
    }

    // ── Count / Keys / Clear ──────────────────────────────────────────────────

    [Fact]
    public void Count_ReflectsNumberOfDistinctKeys()
    {
        var cache = MakeCache();
        cache.Set(Key("a", 0, 1), "v1");
        cache.Set(Key("b", 1, 2), "v2");
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Keys_ContainsAllStoredKeys()
    {
        var cache = MakeCache();
        var k1 = Key("a", 0, 1);
        var k2 = Key("b", 1, 2);
        cache.Set(k1, "v1");
        cache.Set(k2, "v2");

        var keys = cache.Keys.ToList();
        Assert.Contains(k1, keys);
        Assert.Contains(k2, keys);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = MakeCache();
        cache.Set(Key("a", 0, 1), "v1");
        cache.SetVector(Key("b", 1, 2), [1f]);
        cache.Clear();
        Assert.Equal(0, cache.Count);
    }

    // ── Purge ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Purge_RemovesExpiredEntries_LeavesValid()
    {
        var cache = MakeCache();
        var expired = Key("exp", 0, 3);
        var valid = Key("val", 3, 6);

        cache.Set(expired, "gone", ttl: TimeSpan.FromMilliseconds(1));
        cache.Set(valid, "here");

        System.Threading.Thread.Sleep(50);
        cache.Purge();

        Assert.False(cache.TryGet<string>(expired, out _));
        Assert.True(cache.TryGet<string>(valid, out _));
    }

    // ── MaxEntries ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxEntries_EvictsOldestWhenFull()
    {
        var opts = new NlpCacheOptions { MaxEntries = 2, AutoPurge = false };
        var cache = MakeCache(opts);

        cache.Set(Key("a", 0, 1), "v0");
        cache.Set(Key("b", 1, 2), "v1");
        cache.Set(Key("c", 2, 3), "v2"); // should evict "a"

        Assert.Equal(2, cache.Count);
    }

    // ── Null argument guards ──────────────────────────────────────────────────

    [Fact]
    public void Set_NullKey_Throws()
    {
        var cache = MakeCache();
        Assert.Throws<ArgumentNullException>(() => cache.Set<string>(null!, "v"));
    }

    [Fact]
    public void TryGet_NullKey_Throws()
    {
        var cache = MakeCache();
        Assert.Throws<ArgumentNullException>(() => cache.TryGet<string>(null!, out _));
    }

    [Fact]
    public void SetVector_NullVector_Throws()
    {
        var cache = MakeCache();
        Assert.Throws<ArgumentNullException>(() => cache.SetVector(Key("k", 0, 1), null!));
    }
}
