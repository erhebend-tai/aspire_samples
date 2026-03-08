// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspireShop.NlpCache;

/// <summary>
/// Coarse classification of a token produced by <see cref="ITokenizer"/>.
/// </summary>
public enum TokenType
{
    /// <summary>A language keyword (e.g. <c>if</c>, <c>class</c>, <c>var</c>).</summary>
    Keyword,

    /// <summary>A user-defined or language identifier (e.g. <c>myVariable</c>).</summary>
    Identifier,

    /// <summary>A numeric literal (e.g. <c>42</c>, <c>3.14</c>).</summary>
    NumericLiteral,

    /// <summary>A string or character literal (e.g. <c>"hello"</c>, <c>'a'</c>).</summary>
    StringLiteral,

    /// <summary>A punctuation or operator token (e.g. <c>{</c>, <c>=></c>, <c>+=</c>).</summary>
    Operator,

    /// <summary>Whitespace (spaces, tabs, newlines) — included when the tokenizer is
    /// configured to preserve whitespace.</summary>
    Whitespace,

    /// <summary>A single-line or block comment.</summary>
    Comment,

    /// <summary>A token that did not match any other category.</summary>
    Unknown,
}
