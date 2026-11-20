using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

/// <summary>Етап 1: синтаксична перевірка та локалізація помилок.</summary>
public class FormulaParserTests
{
    private readonly FormulaParser _parser = new();

    [Theory]
    [InlineData("1 + 2")]
    [InlineData("=mmax(A1, B1, 5) + 2")]
    [InlineData("-(A1 - 25) div 3 mod 2")]
    public void Valid_expressions_have_no_errors(string expression)
    {
        Assert.Empty(_parser.Validate(expression));
        Assert.Null(_parser.ValidateSyntax(expression));
    }

    [Fact]
    public void Unexpected_token_is_located_and_described_in_ukrainian()
    {
        var error = _parser.Validate("3 + * 4")[0];

        Assert.Equal(5, error.Position);
        Assert.Equal(1, error.Length);
        Assert.Contains("Неочікувана лексема", error.Message);
        Assert.Contains("'*'", error.Message);
    }

    [Fact]
    public void Empty_function_call_is_reported_at_closing_parenthesis()
    {
        var error = _parser.Validate("mmax()")[0];

        Assert.Equal(6, error.Position);
        Assert.Contains("')'", error.Message);
    }

    [Fact]
    public void Unexpected_end_of_expression_has_zero_length()
    {
        var error = _parser.Validate("(1 + 2")[0];

        Assert.Equal(0, error.Length);
        Assert.Contains("кінець", error.Message);
    }

    [Fact]
    public void ValidateSyntax_returns_localized_message_with_position()
    {
        string? message = _parser.ValidateSyntax("3 + * 4");

        Assert.NotNull(message);
        Assert.StartsWith("Позиція 5:", message);
    }
}
