using System.Numerics;
using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

/// <summary>Етап 2: обчислення виразів варіанта 4 (+ - * /, mod, div, унарні + -, mmax, mmin).</summary>
public class FormulaEvaluatorTests
{
    private static readonly DictionaryCellResolver Cells =
        new DictionaryCellResolver().Add("A1", 10).Add("B2", 3);

    private static BigInteger Compute(string expression)
        => new FormulaEvaluator().ComputeValue(expression, Cells);

    [Theory]
    [InlineData("10 - 3 - 2", "5")]          // ліва асоціативність
    [InlineData("2 + 3 * 4", "14")]          // * сильніше за +
    [InlineData("(2 + 3) * 4", "20")]        // дужки
    [InlineData("2 * (3 + 4) - 1", "13")]
    public void Binary_operations_respect_precedence(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("10 / 3", "3")]
    [InlineData("10 div 3", "3")]
    [InlineData("-7 div 2", "-3")]           // відкидання в бік нуля
    [InlineData("10 mod 3", "1")]
    [InlineData("-7 mod 3", "-1")]           // знак діленого
    [InlineData("7 mod -3", "1")]
    public void Div_and_mod_follow_integer_semantics(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("-5", "-5")]
    [InlineData("2 - -3", "5")]              // бінарний і унарний мінус разом
    [InlineData("-2 * 3", "-6")]             // унарний сильніший за *
    [InlineData("-(2 + 3) * 2", "-10")]
    public void Unary_operators_work(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("mmax(1)", "1")]             // N = 1
    [InlineData("mmax(1, 5, 3)", "5")]
    [InlineData("mmin(4, 2, 9)", "2")]
    [InlineData("mmax(1, mmin(9, 6), 5)", "6")]      // вкладені виклики
    public void Mmax_and_mmin_work(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("A1 + B2", "13")]
    [InlineData("a1 - b2", "7")]             // регістр не важливий
    public void Cell_references_are_resolved(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("123456789012345678901234567890 * 987654321098765432109876543210",
                "121932631137021795226185032733622923332237463801111263526900")]
    [InlineData("100000000000000000000 div 3", "33333333333333333333")]
    public void Arbitrary_length_integers_are_exact(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("=1 + 1", "2")]              // необов'язковий '=' на початку
    [InlineData("  1   +   2  ", "3")]       // пропуски
    [InlineData("MMAX(1, 2)", "2")]          // регістр ключових слів
    public void Formatting_and_case_do_not_matter(string expression, string expected)
        => Assert.Equal(BigInteger.Parse(expected), Compute(expression));

    [Theory]
    [InlineData("10 / 0")]
    [InlineData("10 div 0")]
    [InlineData("10 / (2 - 2)")]
    public void Division_by_zero_throws(string expression)
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Compute(expression));
        Assert.Equal("Ділення на нуль.", ex.Message);
    }

    [Fact]
    public void Mod_by_zero_throws()
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Compute("5 mod 0"));
        Assert.Equal("Остача від ділення на нуль.", ex.Message);
    }

    [Fact]
    public void Unknown_cell_error_from_resolver_is_propagated()
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Compute("Z9 + 1"));
        Assert.Equal("Клітинки Z9 не існує.", ex.Message);
    }

    [Theory]
    [InlineData("3 + * 4")]
    [InlineData("mmax()")]
    [InlineData("(1 + 2")]
    [InlineData("1 + 2)")]
    [InlineData("1.5")]
    public void Invalid_syntax_throws_syntax_exception(string expression)
    {
        var ex = Assert.Throws<FormulaSyntaxException>(() => Compute(expression));
        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public void Syntax_exception_reports_position_of_first_error()
    {
        var ex = Assert.Throws<FormulaSyntaxException>(() => Compute("3 + * 4"));
        Assert.Equal(5, ex.Errors[0].Position);
    }
}
