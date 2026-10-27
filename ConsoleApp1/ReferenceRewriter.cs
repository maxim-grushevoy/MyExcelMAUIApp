using System.Text;
using Antlr4.Runtime;
using SpreadsheetExpressions.Grammar;

namespace SpreadsheetExpressions;

/// <summary>
/// Переписує посилання на клітинки у виразі (потрібно при вставці/видаленні рядків і стовпчиків).
/// </summary>
internal static class ReferenceRewriter
{
    /// <param name="map">(рядок, стовпчик) -> нова позиція, або null, якщо клітинку видалено.</param>
    public static string Rewrite(string expression, Func<int, int, (int Row, int Column)?> map)
    {
        if (string.IsNullOrEmpty(expression)) return expression;

        var lexer = new SpreadsheetExpressionLexer(new AntlrInputStream(expression));
        lexer.RemoveErrorListeners();

        var result = new StringBuilder();
        int position = 0;

        foreach (var token in lexer.GetAllTokens())
        {
            if (token.Type != SpreadsheetExpressionLexer.CELL) continue;

            result.Append(expression, position, token.StartIndex - position);

            string replacement = token.Text;
            if (CellAddress.TryParse(token.Text, out int row, out int column))
            {
                var moved = map(row, column);
                replacement = moved is { } m ? CellAddress.Format(m.Row, m.Column) : "#REF!";
            }
            result.Append(replacement);
            position = token.StopIndex + 1;
        }

        result.Append(expression, position, expression.Length - position);
        return result.ToString();
    }
}
