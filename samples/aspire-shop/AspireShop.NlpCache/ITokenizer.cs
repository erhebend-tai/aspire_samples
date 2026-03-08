// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// Produces a sequence of <see cref="TokenKey"/> instances with position and gap measurements
/// from a source string.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Tokenises <paramref name="text"/> and returns an ordered list of <see cref="TokenKey"/>
    /// instances.  Each key carries <see cref="TokenKey.GapBefore"/> and
    /// <see cref="TokenKey.GapAfter"/> measurements derived from the surrounding whitespace or
    /// non-token characters.
    /// </summary>
    /// <param name="text">The source text to tokenise.</param>
    /// <returns>
    /// An ordered list of tokens.  Returns an empty list when <paramref name="text"/> is
    /// <see langword="null"/>, empty or consists entirely of whitespace.
    /// </returns>
    IReadOnlyList<TokenKey> Tokenize(string? text);
}
