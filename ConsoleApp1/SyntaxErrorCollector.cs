using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace SpreadsheetExpressions;

internal sealed class SyntaxErrorCollector : BaseErrorListener, IAntlrErrorListener<int>
{
    private readonly string _expression;
    private readonly List<FormulaSyntaxError> _errors = new();

    public SyntaxErrorCollector(string expression) => _expression = expression;

    public IReadOnlyList<FormulaSyntaxError> Errors => _errors;

    // помилки парсера
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        bool atEnd = offendingSymbol.Type == TokenConstants.EOF;
        int start = atEnd ? _expression.Length : offendingSymbol.StartIndex;
        int length = atEnd ? 0 : offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1;

        string text = atEnd
            ? "Несподіваний кінець виразу."
            : $"Неочікувана лексема '{offendingSymbol.Text}'.";

        string expected = DescribeExpected(recognizer as Parser);
        if (expected.Length > 0)
            text += $" Очікувалось: {expected}.";

        _errors.Add(new FormulaSyntaxError(start, length, text));
    }

    // помилки лексера(невідомий символ)
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        int index = e is LexerNoViableAltException lex ? lex.StartIndex : Math.Max(0, charPositionInLine);
        string ch = index < _expression.Length ? _expression[index].ToString() : "";
        _errors.Add(new FormulaSyntaxError(index, ch.Length, $"Недопустимий символ '{ch}'."));
    }

    private static string DescribeExpected(Parser? parser)
    {
        if (parser is null) return string.Empty;
        try
        {
            var names = parser.GetExpectedTokens().ToList()
                .Where(t => t > 0)
                .Select(t => TokenName(parser.Vocabulary, t))
                .Distinct();
            return string.Join(", ", names);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string TokenName(IVocabulary vocabulary, int type) => vocabulary.GetSymbolicName(type) switch
    {
        "NUMBER" => "число",
        "CELL" => "посилання на клітинку",
        "MMAX" => "mmax",
        "MMIN" => "mmin",
        "IDIV" => "div",
        "MOD" => "mod",
        _ => vocabulary.GetDisplayName(type)
    };
}
