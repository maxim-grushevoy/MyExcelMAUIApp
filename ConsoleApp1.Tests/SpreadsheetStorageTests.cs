using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

public class SpreadsheetStorageTests
{
    [Fact]
    public void Saved_table_is_loaded_back_with_same_content_and_sizes()
    {
        using var file = new TempFile();
        var table = new Spreadsheet(4, 3);
        table.UpdateCellAndRecalculateDependents(0, 0, "10");
        table.UpdateCellAndRecalculateDependents(0, 1, "=A1 * 2");
        table.SetRowHeight(2, 55);
        table.SetColumnWidth(1, 120);

        SpreadsheetStorage.Save(table, file.FilePath);
        var loaded = SpreadsheetStorage.Load(file.FilePath);

        Assert.Equal(4, loaded.RowCount);
        Assert.Equal(3, loaded.ColumnCount);
        Assert.Equal("=A1 * 2", loaded.GetCell("B1").Expression);
        Assert.Equal(55, loaded.GetRowHeight(2));
        Assert.Equal(120, loaded.GetColumnWidth(1));
    }

    [Fact]
    public void Values_are_recalculated_after_loading()
    {
        using var file = new TempFile();
        var table = new Spreadsheet(3, 3);
        table.UpdateCellAndRecalculateDependents(0, 0, "10");
        table.UpdateCellAndRecalculateDependents(0, 1, "=A1 * 2");

        SpreadsheetStorage.Save(table, file.FilePath);
        var loaded = SpreadsheetStorage.Load(file.FilePath);

        Assert.Equal(20, (int)loaded.GetCell("B1").Value!.Value);
    }

    [Fact]
    public void File_that_is_not_json_is_rejected()
    {
        using var file = new TempFile();
        File.WriteAllText(file.FilePath, "це не json");

        Assert.Throws<InvalidDataException>(() => SpreadsheetStorage.Load(file.FilePath));
    }

    [Fact]
    public void Json_with_wrong_structure_is_rejected()
    {
        using var file = new TempFile();
        File.WriteAllText(file.FilePath, "{}");

        Assert.Throws<InvalidDataException>(() => SpreadsheetStorage.Load(file.FilePath));
    }
}
