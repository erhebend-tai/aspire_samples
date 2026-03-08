// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspireShop.NlpCache;

namespace AspireShop.NlpCache.Tests;

public class TokenKeyTests
{
    private static TokenKey Make(string text, int start, int end, TokenType type,
        int gapBefore = -1, int gapAfter = -1) =>
        new()
        {
            Text = text,
            Span = new TokenSpan(start, end),
            Type = type,
            GapBefore = gapBefore,
            GapAfter = gapAfter,
        };

    [Fact]
    public void Equality_BasedOnTextSpanAndType_NotGaps()
    {
        var a = Make("int", 0, 3, TokenType.Keyword, gapBefore: 0, gapAfter: 1);
        var b = Make("int", 0, 3, TokenType.Keyword, gapBefore: 5, gapAfter: 10);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Inequality_WhenTextDiffers()
    {
        var a = Make("int", 0, 3, TokenType.Keyword);
        var b = Make("var", 0, 3, TokenType.Keyword);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Inequality_WhenSpanDiffers()
    {
        var a = Make("x", 0, 1, TokenType.Identifier);
        var b = Make("x", 5, 6, TokenType.Identifier);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Inequality_WhenTypeDiffers()
    {
        var a = Make("value", 0, 5, TokenType.Keyword);
        var b = Make("value", 0, 5, TokenType.Identifier);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GapBefore_DefaultIsMinusOne()
    {
        var key = new TokenKey
        {
            Text = "x",
            Span = new TokenSpan(0, 1),
            Type = TokenType.Identifier,
        };
        Assert.Equal(-1, key.GapBefore);
        Assert.Equal(-1, key.GapAfter);
    }

    [Fact]
    public void ToString_ContainsUsefulInfo()
    {
        var key = Make("if", 4, 6, TokenType.Keyword, gapBefore: 1, gapAfter: 2);
        var str = key.ToString();
        Assert.Contains("if", str);
        Assert.Contains("Keyword", str);
        Assert.Contains("gap_before=1", str);
        Assert.Contains("gap_after=2", str);
    }

    [Fact]
    public void UsableAsDictionaryKey()
    {
        var dict = new Dictionary<TokenKey, string>();
        var key = Make("foo", 0, 3, TokenType.Identifier);
        dict[key] = "bar";
        Assert.Equal("bar", dict[key]);
    }
}
