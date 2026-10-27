using System.Numerics;

namespace SpreadsheetExpressions;

/// <summary>Клітинка таблиці: вираз, обчислене значення або текст помилки.</summary>
public sealed class Cell
{
    public string Expression { get; set; } = string.Empty;

    /// <summary>Значення виразу (null, якщо є помилка).</summary>
    public BigInteger? Value { get; internal set; }

    /// <summary>Повідомлення про помилку (null, якщо помилки немає).</summary>
    public string? Error { get; internal set; }

    /// <summary>Початок фрагмента виразу з синтаксичною помилкою (для підсвічування), інакше null.</summary>
    public int? ErrorStart { get; internal set; }

    /// <summary>Довжина фрагмента з помилкою (0 — помилка в кінці виразу).</summary>
    public int ErrorLength { get; internal set; }

    internal bool IsEvaluated { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Expression);

    /// <summary>Текст для режиму ЗНАЧЕННЯ.</summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return string.Empty;
            return Error is not null ? "#ПОМИЛКА" : Value?.ToString() ?? string.Empty;
        }
    }
}
