// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspireShop.NlpCache;

namespace AspireShop.NlpCache.Tests;

public class TokenSpanTests
{
    [Fact]
    public void Constructor_SetsStartAndEnd()
    {
        var span = new TokenSpan(3, 10);
        Assert.Equal(3, span.Start);
        Assert.Equal(10, span.End);
    }

    [Fact]
    public void Length_IsEndMinusStart()
    {
        var span = new TokenSpan(2, 9);
        Assert.Equal(7, span.Length);
    }

    [Fact]
    public void IsEmpty_WhenLengthIsZero()
    {
        var span = new TokenSpan(5, 5);
        Assert.True(span.IsEmpty);
    }

    [Fact]
    public void IsEmpty_False_WhenLengthIsPositive()
    {
        var span = new TokenSpan(0, 1);
        Assert.False(span.IsEmpty);
    }

    [Fact]
    public void GapTo_ReturnsCharsBetweenSpans()
    {
        var a = new TokenSpan(0, 3);   // "int"
        var b = new TokenSpan(4, 8);   // " foo"  (gap = 1 space at index 3)
        Assert.Equal(1, a.GapTo(b));
    }

    [Fact]
    public void GapTo_Negative_WhenSpansOverlap()
    {
        var a = new TokenSpan(0, 5);
        var b = new TokenSpan(3, 8);
        Assert.True(a.GapTo(b) < 0);
    }

    [Fact]
    public void Contains_ReturnsTrueForPositionInsideSpan()
    {
        var span = new TokenSpan(10, 20);
        Assert.True(span.Contains(10));
        Assert.True(span.Contains(15));
        Assert.False(span.Contains(20)); // exclusive end
        Assert.False(span.Contains(9));
    }

    [Fact]
    public void Overlaps_TrueWhenSpansIntersect()
    {
        var a = new TokenSpan(0, 5);
        var b = new TokenSpan(3, 8);
        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_FalseWhenSpansAreAdjacent()
    {
        var a = new TokenSpan(0, 5);
        var b = new TokenSpan(5, 10);
        Assert.False(a.Overlaps(b));
    }

    [Theory]
    [InlineData(0, 5, 0, 5, true)]
    [InlineData(0, 5, 0, 6, false)]
    [InlineData(1, 5, 0, 5, false)]
    public void Equality(int s1, int e1, int s2, int e2, bool expected)
    {
        var a = new TokenSpan(s1, e1);
        var b = new TokenSpan(s2, e2);
        Assert.Equal(expected, a == b);
        Assert.Equal(expected, a.Equals(b));
        if (expected)
        {
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenSpan(-1, 5));
    }

    [Fact]
    public void Constructor_ThrowsWhenEndLessThanStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenSpan(5, 3));
    }

    [Fact]
    public void ToString_ShowsHalfOpenInterval()
    {
        var span = new TokenSpan(2, 7);
        Assert.Equal("[2..7)", span.ToString());
    }

    [Fact]
    public void CompareTo_OrdersByStartThenEnd()
    {
        var a = new TokenSpan(0, 5);
        var b = new TokenSpan(0, 10);
        var c = new TokenSpan(1, 5);

        Assert.True(a < b);
        Assert.True(b < c);
    }
}
