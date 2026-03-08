// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// An immutable cache key that represents a single token in source text together with its
/// position (<see cref="Span"/>) and the gap measurements to its neighbours
/// (<see cref="GapBefore"/> and <see cref="GapAfter"/>).
/// </summary>
/// <remarks>
/// <para>
/// Equality and hash-code are computed from <see cref="Text"/>, <see cref="Span"/> and
/// <see cref="Type"/> only.  Gap measurements are deliberately excluded so that the same
/// logical token found at the same position always resolves to the same cache entry
/// regardless of how much whitespace surrounds it in a particular source revision.
/// </para>
/// <para>
/// The record is sealed and uses the compiler-generated equality for its participating
/// properties, which gives value-based equality with no heap allocations on the hot path.
/// </para>
/// </remarks>
public sealed record TokenKey
{
    /// <summary>The raw text of the token.</summary>
    public required string Text { get; init; }

    /// <summary>Position of this token within the source string.</summary>
    public required TokenSpan Span { get; init; }

    /// <summary>Coarse classification of the token.</summary>
    public required TokenType Type { get; init; }

    /// <summary>
    /// Number of characters between the previous token's end and this token's start.
    /// <c>-1</c> when this is the first token in the sequence.
    /// </summary>
    public int GapBefore { get; init; } = -1;

    /// <summary>
    /// Number of characters between this token's end and the next token's start.
    /// <c>-1</c> when this is the last token in the sequence.
    /// </summary>
    public int GapAfter { get; init; } = -1;

    // Override equality so that gap measurements are NOT part of the key identity.
    /// <inheritdoc/>
    public bool Equals(TokenKey? other) =>
        other is not null
        && Text == other.Text
        && Span == other.Span
        && Type == other.Type;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Text, Span, Type);

    /// <inheritdoc/>
    public override string ToString() =>
        $"TokenKey({Type}: \"{Text}\" {Span}, gap_before={GapBefore}, gap_after={GapAfter})";
}
