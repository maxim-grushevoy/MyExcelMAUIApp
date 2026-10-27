namespace SpreadsheetExpressions;

/// <summary>Посередник між формою та моделлю таблиці (не залежить від MAUI).</summary>
public sealed class SpreadsheetController
{
    public const int InitialRows = 20;
    public const int InitialColumns = 10;

    public Spreadsheet Table { get; private set; } = new(InitialRows, InitialColumns);

    /// <summary>Файл, з яким пов'язана таблиця (null — ще не збережена).</summary>
    public string? FilePath { get; private set; }

    /// <summary>Є незбережені зміни.</summary>
    public bool IsModified { get; private set; }

    /// <summary>
    /// Записує вираз у клітинку, перевіряє синтаксис, обчислює значення
    /// та перераховує залежні клітинки.
    /// </summary>
    /// <returns>Локалізоване повідомлення про помилку або null, якщо все гаразд.</returns>
    public string? UpdateCellExpression(int row, int column, string expression)
    {
        var cell = Table.GetCell(row, column);
        if (cell.Expression == expression) return cell.Error;

        Table.UpdateCellAndRecalculateDependents(row, column, expression);
        IsModified = true;
        return cell.Error;
    }

    public void Recalculate() => Table.Recalculate();

    public void InsertRow(int index) { Table.InsertRow(index); IsModified = true; }
    public void InsertColumn(int index) { Table.InsertColumn(index); IsModified = true; }
    public void DeleteRow(int index) { Table.DeleteRow(index); IsModified = true; }
    public void DeleteColumn(int index) { Table.DeleteColumn(index); IsModified = true; }

    public void SetRowHeight(int row, double height) { Table.SetRowHeight(row, height); IsModified = true; }
    public void SetColumnWidth(int column, double width) { Table.SetColumnWidth(column, width); IsModified = true; }

    public void SaveTable(string path)
    {
        SpreadsheetStorage.Save(Table, path);
        FilePath = path;
        IsModified = false;
    }

    public void LoadTable(string path)
    {
        Table = SpreadsheetStorage.Load(path);
        FilePath = path;
        IsModified = false;
    }
}
