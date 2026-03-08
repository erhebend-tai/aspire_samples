// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspireShop.NlpCache;

namespace AspireShop.NlpCache.Tests;

public class CodeTokenizerTests
{
    private readonly CodeTokenizer _tokenizer = new();

    // ── Null / empty input ────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Tokenize_EmptyOrWhitespace_ReturnsEmptyList(string? input)
    {
        var tokens = _tokenizer.Tokenize(input);
        Assert.Empty(tokens);
    }

    // ── Basic keyword / identifier recognition ────────────────────────────────

    [Fact]
    public void Tokenize_SingleKeyword()
    {
        var tokens = _tokenizer.Tokenize("if");
        var token = Assert.Single(tokens);
        Assert.Equal("if", token.Text);
        Assert.Equal(TokenType.Keyword, token.Type);
        Assert.Equal(new TokenSpan(0, 2), token.Span);
    }

    [Fact]
    public void Tokenize_SingleIdentifier()
    {
        var tokens = _tokenizer.Tokenize("myVar");
        var token = Assert.Single(tokens);
        Assert.Equal("myVar", token.Text);
        Assert.Equal(TokenType.Identifier, token.Type);
    }

    // ── Gap measurements ──────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_MeasuresGapBetweenTokens()
    {
        // "int  x" – 2 spaces between 'int' and 'x'
        var tokens = _tokenizer.Tokenize("int  x");

        Assert.Equal(2, tokens.Count);

        var intToken = tokens[0];
        var xToken = tokens[1];

        // int: [0..3), x: [5..6)  →  gap = 5 - 3 = 2
        Assert.Equal(2, intToken.GapAfter);
        Assert.Equal(2, xToken.GapBefore);
    }

    [Fact]
    public void Tokenize_FirstToken_GapBeforeIsMinusOne()
    {
        var tokens = _tokenizer.Tokenize("var x = 1;");
        Assert.Equal(-1, tokens[0].GapBefore);
    }

    [Fact]
    public void Tokenize_LastToken_GapAfterIsMinusOne()
    {
        var tokens = _tokenizer.Tokenize("var x = 1;");
        Assert.Equal(-1, tokens[^1].GapAfter);
    }

    [Fact]
    public void Tokenize_AdjacentTokens_GapIsZero()
    {
        // "()" – two operators with no gap
        var tokens = _tokenizer.Tokenize("()");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(0, tokens[0].GapAfter);
        Assert.Equal(0, tokens[1].GapBefore);
    }

    // ── Numeric literals ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("42")]
    [InlineData("3.14")]
    public void Tokenize_NumericLiteral(string input)
    {
        var tokens = _tokenizer.Tokenize(input);
        var tok = Assert.Single(tokens);
        Assert.Equal(TokenType.NumericLiteral, tok.Type);
    }

    // ── String literals ───────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_StringLiteral()
    {
        var tokens = _tokenizer.Tokenize("\"hello world\"");
        var tok = Assert.Single(tokens);
        Assert.Equal(TokenType.StringLiteral, tok.Type);
    }

    [Fact]
    public void Tokenize_VerbatimString()
    {
        var tokens = _tokenizer.Tokenize(@"@""C:\path""");
        var tok = Assert.Single(tokens);
        Assert.Equal(TokenType.StringLiteral, tok.Type);
    }

    // ── Comments ──────────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_CommentsSkippedByDefault()
    {
        var tokens = _tokenizer.Tokenize("// this is a comment\nint x;");
        Assert.DoesNotContain(tokens, t => t.Type == TokenType.Comment);
    }

    [Fact]
    public void Tokenize_CommentsIncludedWhenConfigured()
    {
        var tokenizer = new CodeTokenizer(new CodeTokenizerOptions { IncludeComments = true });
        var tokens = tokenizer.Tokenize("// comment\nint x;");
        Assert.Contains(tokens, t => t.Type == TokenType.Comment);
    }

    [Fact]
    public void Tokenize_BlockComment_SkippedByDefault()
    {
        var tokens = _tokenizer.Tokenize("/* block */ int x;");
        Assert.DoesNotContain(tokens, t => t.Type == TokenType.Comment);
        Assert.Contains(tokens, t => t.Text == "int");
    }

    // ── Whitespace inclusion ──────────────────────────────────────────────────

    [Fact]
    public void Tokenize_WhitespaceSkippedByDefault()
    {
        var tokens = _tokenizer.Tokenize("int x = 0;");
        Assert.DoesNotContain(tokens, t => t.Type == TokenType.Whitespace);
    }

    [Fact]
    public void Tokenize_WhitespaceIncludedWhenConfigured()
    {
        var tokenizer = new CodeTokenizer(new CodeTokenizerOptions { IncludeWhitespace = true });
        var tokens = tokenizer.Tokenize("int x;");
        Assert.Contains(tokens, t => t.Type == TokenType.Whitespace);
    }

    // ── Realistic code snippet ────────────────────────────────────────────────

    [Fact]
    public void Tokenize_SimpleAssignment_ProducesCorrectSequence()
    {
        // "int x = 42;"
        var tokens = _tokenizer.Tokenize("int x = 42;");

        var texts = tokens.Select(t => t.Text).ToArray();
        Assert.Equal(["int", "x", "=", "42", ";"], texts);

        var types = tokens.Select(t => t.Type).ToArray();
        Assert.Equal(
            [TokenType.Keyword, TokenType.Identifier, TokenType.Operator, TokenType.NumericLiteral, TokenType.Operator],
            types);
    }

    [Fact]
    public void Tokenize_Spans_AreNonOverlappingAndOrdered()
    {
        var tokens = _tokenizer.Tokenize("public class Foo { }");
        for (int i = 1; i < tokens.Count; i++)
        {
            Assert.True(tokens[i - 1].Span.End <= tokens[i].Span.Start,
                $"Token {i - 1} ({tokens[i - 1].Text}) and token {i} ({tokens[i].Text}) overlap.");
        }
    }

    [Fact]
    public void Tokenize_ContextualKeyword_var_IsKeyword()
    {
        var tokens = _tokenizer.Tokenize("var x = 1;");
        Assert.Equal(TokenType.Keyword, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_SumOfSpanLengthsPlusGaps_CoverFullNonWhitespaceRange()
    {
        const string src = "int x=1;";
        var tokens = _tokenizer.Tokenize(src);

        // Reconstruct every character index covered by a token or a gap.
        var covered = new HashSet<int>();
        foreach (var t in tokens)
        {
            for (int i = t.Span.Start; i < t.Span.End; i++)
            {
                covered.Add(i);
            }
        }

        // Every non-whitespace character should be inside at least one token span.
        for (int i = 0; i < src.Length; i++)
        {
            if (!char.IsWhiteSpace(src[i]))
            {
                Assert.Contains(i, covered);
            }
        }
    }
}
