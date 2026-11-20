using System.Numerics;
using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

/// <summary>Модель таблиці: обчислення, залежності, помилки, вставка і видалення.</summary>
public class SpreadsheetTests
{
    private static Spreadsheet NewTable(int rows = 5, int columns = 5) => new(rows, columns);

    private static void Set(Spreadsheet table, string address, string expression)
    {
        Assert.True(CellAddress.TryParse(address, out int row, out int column));
        table.UpdateCellAndRecalculateDependents(row, column, expression);
    }

    private static BigInteger ValueOf(Spreadsheet table, string address)
    {
        var cell = table.GetCell(address);
        Assert.Null(cell.Error);
        return cell.Value!.Value;
    }

    // ---------- обчислення і залежності ----------

    [Fact]
    public void Formula_uses_values_of_other_cells()
    {
        var table = NewTable();
        Set(table, "A1", "10");
        Set(table, "A2", "3");
        Set(table, "A3", "=A1 + A2 * 2");

        Assert.Equal(16, ValueOf(table, "A3"));
    }

    [Fact]
    public void Changing_a_cell_recalculates_dependent_cells()
    {
        var table = NewTable();
        Set(table, "A1", "10");
        Set(table, "B1", "=A1 + 1");
        Set(table, "C1", "=B1 * 2");

        Set(table, "A1", "20");

        Assert.Equal(21, ValueOf(table, "B1"));
        Assert.Equal(42, ValueOf(table, "C1"));
    }

    [Fact]
    public void Dependencies_are_resolved_regardless_of_cell_order()
    {
        var table = NewTable();
        Set(table, "A1", "=B1 + 1");   // залежить від клітинок, що йдуть пізніше
        Set(table, "B1", "=C1 * 2");
        Set(table, "C1", "5");

        Assert.Equal(10, ValueOf(table, "B1"));
        Assert.Equal(11, ValueOf(table, "A1"));
    }

    [Fact]
    public void Empty_cell_has_value_zero_in_formulas()
    {
        var table = NewTable();
        Set(table, "A1", "=B1 + 5");

        Assert.Equal(5, ValueOf(table, "A1"));
    }

    // ---------- помилки ----------

    [Fact]
    public void Syntax_error_is_stored_with_location()
    {
        var table = NewTable();
        Set(table, "A1", "3 + * 4");

        var cell = table.GetCell("A1");
        Assert.Null(cell.Value);
        Assert.StartsWith("Позиція 5:", cell.Error);
        Assert.Equal(4, cell.ErrorStart);
        Assert.Equal(1, cell.ErrorLength);
    }

    [Fact]
    public void Division_by_zero_is_reported_and_propagates_to_dependent_cells()
    {
        var table = NewTable();
        Set(table, "A1", "=1 / 0");
        Set(table, "B1", "=A1 + 1");

        Assert.Equal("Ділення на нуль.", table.GetCell("A1").Error);
        Assert.Equal("Помилка в клітинці A1.", table.GetCell("B1").Error);
    }

    [Fact]
    public void Reference_to_nonexistent_cell_is_an_error()
    {
        var table = NewTable();
        Set(table, "A1", "=Z99 + 1");

        Assert.Equal("Клітинки Z99 не існує.", table.GetCell("A1").Error);
    }

    [Fact]
    public void Self_reference_is_detected_as_cycle()
    {
        var table = NewTable();
        Set(table, "A1", "=A1 + 1");

        Assert.Equal("Циклічне посилання на клітинку A1.", table.GetCell("A1").Error);
    }

    [Fact]
    public void Cycle_between_cells_is_detected_in_both_cells()
    {
        var table = NewTable();
        Set(table, "A1", "=B1");
        Set(table, "B1", "=A1");

        Assert.NotNull(table.GetCell("A1").Error);
        Assert.NotNull(table.GetCell("B1").Error);
    }

    // ---------- вставка і видалення ----------

    [Fact]
    public void InsertRow_shifts_cells_and_updates_references()
    {
        var table = NewTable();
        Set(table, "A1", "10");
        Set(table, "B1", "=A1 + 1");

        table.InsertRow(0);

        Assert.Equal(6, table.RowCount);
        Assert.Equal("=A2 + 1", table.GetCell("B2").Expression);
        Assert.Equal(11, ValueOf(table, "B2"));
    }

    [Fact]
    public void InsertColumn_shifts_cells_and_updates_references()
    {
        var table = NewTable();
        Set(table, "A1", "10");
        Set(table, "B1", "=A1 + 1");

        table.InsertColumn(0);

        Assert.Equal(6, table.ColumnCount);
        Assert.Equal("=B1 + 1", table.GetCell("C1").Expression);
        Assert.Equal(11, ValueOf(table, "C1"));
    }

    [Fact]
    public void DeleteRow_shifts_cells_up_and_updates_references()
    {
        var table = NewTable();
        Set(table, "A1", "5");
        Set(table, "A2", "7");
        Set(table, "A3", "=A2 * 2");

        table.DeleteRow(0);

        Assert.Equal(4, table.RowCount);
        Assert.Equal("=A1 * 2", table.GetCell("A2").Expression);
        Assert.Equal(14, ValueOf(table, "A2"));
    }

    [Fact]
    public void Reference_to_deleted_row_becomes_error()
    {
        var table = NewTable();
        Set(table, "A1", "5");
        Set(table, "A2", "=A1 + 1");

        table.DeleteRow(0);

        var cell = table.GetCell("A1");
        Assert.Contains("#REF!", cell.Expression);
        Assert.NotNull(cell.Error);
    }

    [Fact]
    public void Last_row_and_last_column_cannot_be_deleted()
    {
        var table = NewTable(1, 1);

        Assert.Throws<InvalidOperationException>(() => table.DeleteRow(0));
        Assert.Throws<InvalidOperationException>(() => table.DeleteColumn(0));
    }

    [Fact]
    public void Sizes_follow_their_rows_and_columns_after_insertion()
    {
        var table = NewTable();
        table.SetRowHeight(1, 77);
        table.SetColumnWidth(1, 150);

        table.InsertRow(0);
        table.InsertColumn(0);

        Assert.Equal(77, table.GetRowHeight(2));
        Assert.Equal(150, table.GetColumnWidth(2));
    }
}
