using SpreadsheetExpressions;
using Xunit;

namespace ConsoleApp1.Tests;

/// <summary>Контролер: посередник між формою та моделлю, прапорець незбережених змін.</summary>
public class SpreadsheetControllerTests
{
    [Fact]
    public void Valid_expression_returns_no_error_and_marks_table_modified()
    {
        var controller = new SpreadsheetController();

        string? error = controller.UpdateCellExpression(0, 0, "=2 + 3");

        Assert.Null(error);
        Assert.True(controller.IsModified);
        Assert.Equal(5, (int)controller.Table.GetCell(0, 0).Value!.Value);
    }

    [Fact]
    public void Invalid_expression_returns_localized_error()
    {
        var controller = new SpreadsheetController();

        string? error = controller.UpdateCellExpression(0, 0, "3 + * 4");

        Assert.NotNull(error);
        Assert.StartsWith("Позиція 5:", error);
    }

    [Fact]
    public void Dependent_cells_are_recalculated_through_controller()
    {
        var controller = new SpreadsheetController();
        controller.UpdateCellExpression(0, 0, "10");
        controller.UpdateCellExpression(0, 1, "=A1 + 1");

        controller.UpdateCellExpression(0, 0, "20");

        Assert.Equal(21, (int)controller.Table.GetCell(0, 1).Value!.Value);
    }

    [Fact]
    public void Save_remembers_path_and_clears_modified_flag()
    {
        using var file = new TempFile();
        var controller = new SpreadsheetController();
        controller.UpdateCellExpression(0, 0, "5");

        controller.SaveTable(file.FilePath);

        Assert.Equal(file.FilePath, controller.FilePath);
        Assert.False(controller.IsModified);
        Assert.True(File.Exists(file.FilePath));
    }

    [Fact]
    public void Load_replaces_table_and_clears_modified_flag()
    {
        using var file = new TempFile();
        var source = new SpreadsheetController();
        source.UpdateCellExpression(0, 0, "42");
        source.InsertRow(0);
        source.SaveTable(file.FilePath);

        var controller = new SpreadsheetController();
        controller.UpdateCellExpression(5, 5, "1");   // є незбережені зміни
        controller.LoadTable(file.FilePath);

        Assert.Equal(SpreadsheetController.InitialRows + 1, controller.Table.RowCount);
        Assert.Equal("42", controller.Table.GetCell(1, 0).Expression);
        Assert.False(controller.IsModified);
    }

    [Fact]
    public void Structural_changes_mark_table_modified()
    {
        var controller = new SpreadsheetController();

        controller.InsertRow(0);

        Assert.True(controller.IsModified);
    }
}
