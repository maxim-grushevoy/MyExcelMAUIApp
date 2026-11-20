using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

public class CellAddressTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ColumnName_converts_index_to_letters(int column, string expected)
        => Assert.Equal(expected, CellAddress.ColumnName(column));

    [Fact]
    public void ColumnName_and_TryParseColumn_are_inverse()
    {
        for (int column = 0; column < 2000; column++)
        {
            Assert.True(CellAddress.TryParseColumn(CellAddress.ColumnName(column), out int parsed));
            Assert.Equal(column, parsed);
        }
    }

    [Theory]
    [InlineData(0, 0, "A1")]
    [InlineData(11, 1, "B12")]
    [InlineData(9, 26, "AA10")]
    public void Format_builds_address(int row, int column, string expected)
        => Assert.Equal(expected, CellAddress.Format(row, column));

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("B12", 11, 1)]
    [InlineData("a1", 0, 0)]                 // регістр не важливий
    [InlineData("AA10", 9, 26)]
    public void TryParse_reads_valid_address(string address, int row, int column)
    {
        Assert.True(CellAddress.TryParse(address, out int r, out int c));
        Assert.Equal(row, r);
        Assert.Equal(column, c);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("A0")]
    [InlineData("A-1")]
    [InlineData("1A")]
    [InlineData("A 1")]
    [InlineData("A1B")]
    [InlineData("AAAAAAAA1")]                // надто довга назва стовпчика
    [InlineData("A99999999999")]             // переповнення номера рядка
    public void TryParse_rejects_invalid_address(string address)
        => Assert.False(CellAddress.TryParse(address, out _, out _));

    [Fact]
    public void TryParse_rejects_null()
        => Assert.False(CellAddress.TryParse(null!, out _, out _));

    [Theory]
    [InlineData("A", 0)]
    [InlineData("a", 0)]
    [InlineData("Z", 25)]
    [InlineData("AA", 26)]
    public void TryParseColumn_reads_letters(string text, int expected)
    {
        Assert.True(CellAddress.TryParseColumn(text, out int column));
        Assert.Equal(expected, column);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A1")]
    [InlineData("1")]
    public void TryParseColumn_rejects_non_letters(string text)
        => Assert.False(CellAddress.TryParseColumn(text, out _));
}
