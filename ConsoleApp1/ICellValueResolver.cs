using System.Numerics;

namespace SpreadsheetExpressions;

/// <summary>
/// Надає обчислювачу значення клітинок таблиці за їхньою адресою.
/// Реалізується таблицею (Spreadsheet) — бібліотека від неї не залежить.
/// </summary>
public interface ICellValueResolver
{
    /// <param name="address">Адреса клітинки у верхньому регістрі, напр. "B12".</param>
    /// <returns>Значення клітинки (для порожньої клітинки — 0).</returns>
    /// <exception cref="FormulaEvaluationException">
    /// Якщо клітинка не існує, містить помилку або утворює циклічну залежність.
    /// </exception>
    BigInteger GetCellValue(string address);
}
