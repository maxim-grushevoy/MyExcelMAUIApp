namespace SpreadsheetExpressions;

/// <summary>Вираз синтаксично некоректний.</summary>
public sealed class FormulaSyntaxException : Exception
{
    public IReadOnlyList<FormulaSyntaxError> Errors { get; }

    public FormulaSyntaxException(IReadOnlyList<FormulaSyntaxError> errors)
        : base(errors.Count > 0 ? errors[0].ToString() : "Синтаксична помилка.")
    {
        Errors = errors;
    }
}

/// <summary>Помилка під час обчислення синтаксично правильного виразу.</summary>
public sealed class FormulaEvaluationException : Exception
{
    public FormulaEvaluationException(string message) : base(message) { }
}
