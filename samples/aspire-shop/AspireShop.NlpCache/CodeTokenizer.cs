// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace AspireShop.NlpCache;

/// <summary>
/// A lightweight, regex-based tokenizer for C# and general code.
///
/// <para>
/// Tokens are produced in source order.  After tokenisation each key is stamped with
/// <see cref="TokenKey.GapBefore"/> / <see cref="TokenKey.GapAfter"/> values that measure
/// the number of characters between adjacent tokens, giving downstream models a sense of
/// how "far apart" two concepts are in the source.
/// </para>
///
/// <para>
/// The following token types are recognised (in priority order):
/// <list type="bullet">
///   <item>Single-line comments (<c>//…</c>)</item>
///   <item>Block comments (<c>/* … */</c>)</item>
///   <item>String literals (<c>"…"</c> and verbatim <c>@"…"</c>)</item>
///   <item>Character literals (<c>'a'</c>)</item>
///   <item>Numeric literals (integers and decimals)</item>
///   <item>C# keywords</item>
///   <item>Identifiers</item>
///   <item>Operators and punctuation</item>
///   <item>Whitespace (skipped by default; kept when
///     <see cref="CodeTokenizerOptions.IncludeWhitespace"/> is <see langword="true"/>)</item>
/// </list>
/// </para>
/// </summary>
public sealed partial class CodeTokenizer : ITokenizer
{
    // C# reserved keywords
    private static readonly HashSet<string> s_csharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
        "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
        "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "virtual", "void", "volatile", "while",
        // contextual keywords
        "add", "alias", "and", "async", "await", "by", "descending", "dynamic", "equals",
        "from", "get", "global", "group", "init", "into", "join", "let", "managed", "nameof",
        "nint", "not", "notnull", "nuint", "on", "or", "orderby", "partial", "record",
        "remove", "required", "scoped", "select", "set", "unmanaged", "value", "var",
        "when", "where", "with", "yield",
    };

    // Master pattern – order matters: earlier alternatives take priority.
    [GeneratedRegex(
        @"(?<SingleLineComment>//[^\r\n]*)"
        + @"|(?<BlockComment>/\*.*?\*/)"
        + @"|(?<VerbatimString>@""(?:[^""]|"""")*"")"
        + @"|(?<InterpolatedVerbatim>\$@""(?:[^""]|"""")*"")"
        + @"|(?<StringLiteral>""(?:[^""\\]|\\.)*"")"
        + @"|(?<CharLiteral>'(?:[^'\\]|\\.)*')"
        + @"|(?<NumericLiteral>\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?[uUlLfFdDmM]?\b)"
        + @"|(?<Identifier>[A-Za-z_]\w*)"
        + @"|(?<Operator>[{}()\[\];,.<>!?:=+\-*/%&|^~@#])"
        + @"|(?<Whitespace>\s+)",
        RegexOptions.Compiled | RegexOptions.Singleline,
        matchTimeoutMilliseconds: 5_000)]
    private static partial Regex TokenPattern();

    private readonly CodeTokenizerOptions _options;

    /// <summary>Initialises a new <see cref="CodeTokenizer"/> with default options.</summary>
    public CodeTokenizer() : this(new CodeTokenizerOptions()) { }

    /// <summary>Initialises a new <see cref="CodeTokenizer"/> with custom options.</summary>
    public CodeTokenizer(CodeTokenizerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public IReadOnlyList<TokenKey> Tokenize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<TokenKey>();
        }

        var rawTokens = new List<(string Text, TokenSpan Span, TokenType Type)>();

        foreach (Match m in TokenPattern().Matches(text))
        {
            var span = new TokenSpan(m.Index, m.Index + m.Length);

            TokenType type;
            string tokenText = m.Value;

            if (m.Groups["Whitespace"].Success)
            {
                if (!_options.IncludeWhitespace)
                {
                    continue;
                }
                type = TokenType.Whitespace;
            }
            else if (m.Groups["SingleLineComment"].Success || m.Groups["BlockComment"].Success)
            {
                if (!_options.IncludeComments)
                {
                    continue;
                }
                type = TokenType.Comment;
            }
            else if (m.Groups["StringLiteral"].Success
                     || m.Groups["VerbatimString"].Success
                     || m.Groups["InterpolatedVerbatim"].Success
                     || m.Groups["CharLiteral"].Success)
            {
                type = TokenType.StringLiteral;
            }
            else if (m.Groups["NumericLiteral"].Success)
            {
                type = TokenType.NumericLiteral;
            }
            else if (m.Groups["Identifier"].Success)
            {
                type = s_csharpKeywords.Contains(tokenText) ? TokenType.Keyword : TokenType.Identifier;
            }
            else if (m.Groups["Operator"].Success)
            {
                type = TokenType.Operator;
            }
            else
            {
                type = TokenType.Unknown;
            }

            rawTokens.Add((tokenText, span, type));
        }

        // Stamp gap measurements
        var result = new List<TokenKey>(rawTokens.Count);
        for (int i = 0; i < rawTokens.Count; i++)
        {
            var (txt, span, type) = rawTokens[i];

            int gapBefore = i == 0 ? -1 : rawTokens[i - 1].Span.GapTo(span);
            int gapAfter = i == rawTokens.Count - 1 ? -1 : span.GapTo(rawTokens[i + 1].Span);

            result.Add(new TokenKey
            {
                Text = txt,
                Span = span,
                Type = type,
                GapBefore = gapBefore,
                GapAfter = gapAfter,
            });
        }

        return result;
    }
}

/// <summary>Configuration for <see cref="CodeTokenizer"/>.</summary>
public sealed class CodeTokenizerOptions
{
    /// <summary>
    /// When <see langword="true"/>, whitespace tokens are included in the output.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool IncludeWhitespace { get; init; } = false;

    /// <summary>
    /// When <see langword="true"/>, comment tokens are included in the output.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool IncludeComments { get; init; } = false;
}
