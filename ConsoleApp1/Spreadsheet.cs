using System.Numerics;

namespace SpreadsheetExpressions;

/// <summary>Електронна таблиця: клітинки, розміри рядків/стовпчиків, обчислення з урахуванням залежностей.</summary>
public sealed class Spreadsheet : ICellValueResolver
{
    public const double DefaultRowHeight = 40;
    public const double DefaultColumnWidth = 100;

    private readonly List<List<Cell>> _rows = new();
    private readonly List<double> _rowHeights = new();
    private readonly List<double> _columnWidths = new();
    private readonly HashSet<Cell> _evaluating = new();   // для виявлення циклічних посилань
    private readonly FormulaParser _parser = new();
    private readonly FormulaEvaluator _evaluator;

    public Spreadsheet(int rowCount, int columnCount)
    {
        if (rowCount < 1) throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (columnCount < 1) throw new ArgumentOutOfRangeException(nameof(columnCount));

        _evaluator = new FormulaEvaluator(_parser);

        for (int c = 0; c < columnCount; c++) _columnWidths.Add(DefaultColumnWidth);
        for (int r = 0; r < rowCount; r++)
        {
            _rows.Add(NewRow(columnCount));
            _rowHeights.Add(DefaultRowHeight);
        }
    }

    public int RowCount => _rows.Count;
    public int ColumnCount => _columnWidths.Count;

    public int ErrorCount => _rows.SelectMany(row => row).Count(cell => cell.Error is not null);

    // ---------- доступ до клітинок ----------

    public Cell GetCell(int row, int column) => _rows[row][column];

    public double GetRowHeight(int row) => _rowHeights[row];
    public double GetColumnWidth(int column) => _columnWidths[column];

    public void SetRowHeight(int row, double height) => _rowHeights[row] = height;
    public void SetColumnWidth(int column, double width) => _columnWidths[column] = width;

    public void SetExpression(int row, int column, string expression)
        => _rows[row][column].Expression = expression ?? string.Empty;

    // ---------- зміна кількості рядків і стовпчиків ----------

    /// <summary>Вставляє порожній рядок так, що він стає рядком з індексом <paramref name="index"/> (0..RowCount).</summary>
    public void InsertRow(int index)
    {
        if (index < 0 || index > RowCount) throw new ArgumentOutOfRangeException(nameof(index));

        RewriteReferences((r, c) => (r >= index ? r + 1 : r, c));
        _rows.Insert(index, NewRow(ColumnCount));
        _rowHeights.Insert(index, DefaultRowHeight);
        Recalculate();
    }

    public void InsertColumn(int index)
    {
        if (index < 0 || index > ColumnCount) throw new ArgumentOutOfRangeException(nameof(index));

        RewriteReferences((r, c) => (r, c >= index ? c + 1 : c));
        foreach (var row in _rows) row.Insert(index, new Cell());
        _columnWidths.Insert(index, DefaultColumnWidth);
        Recalculate();
    }

    public void DeleteRow(int index)
    {
        if (index < 0 || index >= RowCount) throw new ArgumentOutOfRangeException(nameof(index));
        if (RowCount == 1) throw new InvalidOperationException("Не можна видалити останній рядок.");

        RewriteReferences((r, c) => ShiftAfterDelete(r, index) is { } nr ? (nr, c) : null);
        _rows.RemoveAt(index);
        _rowHeights.RemoveAt(index);
        Recalculate();
    }

    public void DeleteColumn(int index)
    {
        if (index < 0 || index >= ColumnCount) throw new ArgumentOutOfRangeException(nameof(index));
        if (ColumnCount == 1) throw new InvalidOperationException("Не можна видалити останній стовпчик.");

        RewriteReferences((r, c) => ShiftAfterDelete(c, index) is { } nc ? (r, nc) : null);
        foreach (var row in _rows) row.RemoveAt(index);
        _columnWidths.RemoveAt(index);
        Recalculate();
    }

    // ---------- обчислення ----------

    /// <summary>Перераховує всі клітинки (етапи: синтаксична перевірка, обчислення).</summary>
    public void Recalculate()
    {
        foreach (var row in _rows)
            foreach (var cell in row)
                cell.IsEvaluated = false;

        foreach (var row in _rows)
            foreach (var cell in row)
                Evaluate(cell);
    }

    private void Evaluate(Cell cell)
    {
        if (cell.IsEvaluated) return;

        if (cell.IsEmpty)
        {
            cell.Value = BigInteger.Zero;
            cell.Error = null;
            cell.IsEvaluated = true;
            return;
        }

        _evaluating.Add(cell);
        try
        {
            // етап 1: синтаксична перевірка з локалізацією помилок
            var syntaxErrors = _parser.Validate(cell.Expression);
            if (syntaxErrors.Count > 0)
            {
                cell.Value = null;
                cell.Error = syntaxErrors[0].ToString();
            }
            else
            {
                // етап 2: обчислення значення
                cell.Value = _evaluator.ComputeValue(cell.Expression, this);
                cell.Error = null;
            }
        }
        catch (FormulaEvaluationException ex)
        {
            cell.Value = null;
            cell.Error = ex.Message;
        }
        finally
        {
            _evaluating.Remove(cell);
            cell.IsEvaluated = true;
        }
    }

    /// <summary>Значення клітинки для обчислювача виразів.</summary>
    public BigInteger GetCellValue(string address)
    {
        if (!CellAddress.TryParse(address, out int row, out int column)
            || row >= RowCount || column >= ColumnCount)
            throw new FormulaEvaluationException($"Клітинки {address} не існує.");

        var cell = _rows[row][column];

        if (_evaluating.Contains(cell))
            throw new FormulaEvaluationException($"Циклічне посилання на клітинку {address}.");

        Evaluate(cell);

        if (cell.Error is not null)
            throw new FormulaEvaluationException($"Помилка в клітинці {address}.");

        return cell.Value!.Value;
    }

    // ---------- допоміжні ----------

    private static List<Cell> NewRow(int columnCount)
    {
        var row = new List<Cell>(columnCount);
        for (int c = 0; c < columnCount; c++) row.Add(new Cell());
        return row;
    }

    private static int? ShiftAfterDelete(int position, int deleted)
    {
        if (position == deleted) return null;
        return position > deleted ? position - 1 : position;
    }

    private void RewriteReferences(Func<int, int, (int Row, int Column)?> map)
    {
        foreach (var row in _rows)
            foreach (var cell in row)
                cell.Expression = ReferenceRewriter.Rewrite(cell.Expression, map);
    }
}
