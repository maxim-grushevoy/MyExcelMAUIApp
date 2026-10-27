using System.Numerics;

namespace SpreadsheetExpressions;

/// <summary>Етап 2: обчислення значення синтаксично правильного виразу.</summary>
public sealed class FormulaEvaluator
{
    private readonly FormulaParser _parser;

    public FormulaEvaluator() : this(new FormulaParser()) { }

    public FormulaEvaluator(FormulaParser parser) => _parser = parser;

    /// <exception cref="FormulaSyntaxException">Вираз містить синтаксичні помилки.</exception>
    /// <exception cref="FormulaEvaluationException">Помилка обчислення (ділення на нуль, помилка клітинки тощо).</exception>
    public BigInteger ComputeValue(string expression, ICellValueResolver cells)
    {
        var parsed = _parser.Parse(expression);
        if (!parsed.IsValid)
            throw new FormulaSyntaxException(parsed.Errors);

        return new FormulaVisitor(cells).Visit(parsed.Tree);
    }
}
