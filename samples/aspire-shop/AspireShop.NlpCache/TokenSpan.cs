// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// An immutable value type representing a span (position range) within source text, used as
/// a measurement unit between tokens. All position values are zero-based and use half-open
/// intervals [Start, End).
/// </summary>
public readonly struct TokenSpan : IEquatable<TokenSpan>, IComparable<TokenSpan>
{
    /// <summary>Zero-based start position (inclusive).</summary>
    public int Start { get; }

    /// <summary>Zero-based end position (exclusive).</summary>
    public int End { get; }

    /// <summary>Number of characters covered by this span.</summary>
    public int Length => End - Start;

    /// <summary>Returns <see langword="true"/> when the span covers no characters.</summary>
    public bool IsEmpty => Length == 0;

    /// <summary>A span that covers no text.</summary>
    public static TokenSpan Empty => new(0, 0);

    /// <summary>Initialises a new <see cref="TokenSpan"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start"/> is negative or when <paramref name="end"/> is
    /// less than <paramref name="start"/>.
    /// </exception>
    public TokenSpan(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start, nameof(start));
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start, nameof(end));
        Start = start;
        End = end;
    }

    /// <summary>
    /// Returns the number of characters between the end of this span and the start of
    /// <paramref name="next"/>.  A negative value indicates that the spans overlap.
    /// </summary>
    public int GapTo(TokenSpan next) => next.Start - End;

    /// <summary>Returns <see langword="true"/> when this span contains <paramref name="position"/>.</summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>Returns <see langword="true"/> when this span overlaps with <paramref name="other"/>.</summary>
    public bool Overlaps(TokenSpan other) => Start < other.End && other.Start < End;

    /// <inheritdoc/>
    public bool Equals(TokenSpan other) => Start == other.Start && End == other.End;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TokenSpan s && Equals(s);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc/>
    public int CompareTo(TokenSpan other)
    {
        var cmp = Start.CompareTo(other.Start);
        return cmp != 0 ? cmp : End.CompareTo(other.End);
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{Start}..{End})";

    public static bool operator ==(TokenSpan left, TokenSpan right) => left.Equals(right);
    public static bool operator !=(TokenSpan left, TokenSpan right) => !left.Equals(right);
    public static bool operator <(TokenSpan left, TokenSpan right) => left.CompareTo(right) < 0;
    public static bool operator >(TokenSpan left, TokenSpan right) => left.CompareTo(right) > 0;
    public static bool operator <=(TokenSpan left, TokenSpan right) => left.CompareTo(right) <= 0;
    public static bool operator >=(TokenSpan left, TokenSpan right) => left.CompareTo(right) >= 0;
}
