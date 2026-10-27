namespace SpreadsheetExpressions;

/// <summary>Синтаксична помилка з локалізацією у тексті виразу.</summary>
/// <param name="StartIndex">Індекс (з 0) першого символу, де виявлено помилку.</param>
/// <param name="Length">Довжина фрагмента, що спричинив помилку (0 — кінець виразу).</param>
/// <param name="Message">Повідомлення українською.</param>
public sealed record FormulaSyntaxError(int StartIndex, int Length, string Message)
{
    public int Position => StartIndex + 1;

    public override string ToString() => $"Позиція {Position}: {Message}";
}
