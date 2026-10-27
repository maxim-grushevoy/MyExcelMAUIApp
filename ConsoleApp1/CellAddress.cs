using System.Globalization;

namespace SpreadsheetExpressions;

/// <summary>Перетворення між адресами клітинок ("B12") та індексами (з 0).</summary>
public static class CellAddress
{
    /// <summary>0 -> "A", 25 -> "Z", 26 -> "AA" ...</summary>
    public static string ColumnName(int column)
    {
        string name = string.Empty;
        int n = column + 1;
        while (n > 0)
        {
            int modulo = (n - 1) % 26;
            name = (char)('A' + modulo) + name;
            n = (n - modulo) / 26;
        }
        return name;
    }

    public static string Format(int row, int column) => ColumnName(column) + (row + 1);

    /// <summary>"A" -> 0, "AA" -> 26. Регістр не важливий.</summary>
    public static bool TryParseColumn(string text, out int column)
    {
        column = -1;
        if (string.IsNullOrEmpty(text) || text.Length > 7) return false;

        int value = 0;
        foreach (char ch in text.ToUpperInvariant())
        {
            if (ch < 'A' || ch > 'Z') return false;
            value = value * 26 + (ch - 'A' + 1);
        }
        column = value - 1;
        return true;
    }

    /// <summary>"B12" -> (row 11, column 1).</summary>
    public static bool TryParse(string address, out int row, out int column)
    {
        row = column = -1;
        if (string.IsNullOrEmpty(address)) return false;

        int i = 0;
        while (i < address.Length && char.IsAsciiLetter(address[i])) i++;
        if (i == 0 || i == address.Length) return false;

        if (!TryParseColumn(address[..i], out int col)) return false;
        if (!int.TryParse(address[i..], NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            || number < 1) return false;

        row = number - 1;
        column = col;
        return true;
    }
}
