using System.Text.Json;

namespace SpreadsheetExpressions;

/// <summary>Збереження і читання таблиці у власному форматі (JSON).</summary>
public static class SpreadsheetStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void Save(Spreadsheet table, string path)
    {
        var dto = new SpreadsheetDto
        {
            Rows = table.RowCount,
            Columns = table.ColumnCount,
            Expressions = new string[table.RowCount][],
            RowHeights = Enumerable.Range(0, table.RowCount).Select(table.GetRowHeight).ToArray(),
            ColumnWidths = Enumerable.Range(0, table.ColumnCount).Select(table.GetColumnWidth).ToArray()
        };

        for (int r = 0; r < table.RowCount; r++)
        {
            dto.Expressions[r] = new string[table.ColumnCount];
            for (int c = 0; c < table.ColumnCount; c++)
                dto.Expressions[r][c] = table.GetCell(r, c).Expression;
        }

        File.WriteAllText(path, JsonSerializer.Serialize(dto, Options));
    }

    /// <exception cref="InvalidDataException">Файл має неправильний формат.</exception>
    public static Spreadsheet Load(string path)
    {
        SpreadsheetDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SpreadsheetDto>(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Файл не є коректною таблицею.", ex);
        }

        if (dto is null || dto.Rows < 1 || dto.Columns < 1
            || dto.Expressions is null || dto.Expressions.Length != dto.Rows
            || dto.Expressions.Any(row => row is null || row.Length != dto.Columns))
            throw new InvalidDataException("Файл не є коректною таблицею.");

        var table = new Spreadsheet(dto.Rows, dto.Columns);

        for (int r = 0; r < dto.Rows; r++)
            for (int c = 0; c < dto.Columns; c++)
                table.SetExpression(r, c, dto.Expressions[r][c]);

        if (dto.RowHeights?.Length == dto.Rows)
            for (int r = 0; r < dto.Rows; r++) table.SetRowHeight(r, dto.RowHeights[r]);

        if (dto.ColumnWidths?.Length == dto.Columns)
            for (int c = 0; c < dto.Columns; c++) table.SetColumnWidth(c, dto.ColumnWidths[c]);

        table.Recalculate();
        return table;
    }
}

public sealed class SpreadsheetDto
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public string[][]? Expressions { get; set; }
    public double[]? RowHeights { get; set; }
    public double[]? ColumnWidths { get; set; }
}
