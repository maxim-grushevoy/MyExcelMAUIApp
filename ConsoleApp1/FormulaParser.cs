using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SpreadsheetExpressions.Grammar;

namespace SpreadsheetExpressions;

public sealed record ParseResult(IParseTree Tree, IReadOnlyList<FormulaSyntaxError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed class FormulaParser
{
    public ParseResult Parse(string expression)
    {
        expression ??= string.Empty;
        var errors = new SyntaxErrorCollector(expression);

        var lexer = new SpreadsheetExpressionLexer(new AntlrInputStream(expression));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errors);

        var parser = new SpreadsheetExpressionParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errors);

        IParseTree tree = parser.compileUnit();
        return new ParseResult(tree, errors.Errors);
    }

    public IReadOnlyList<FormulaSyntaxError> Validate(string expression) => Parse(expression).Errors;

    public string? ValidateSyntax(string expression)
    {
        var errors = Validate(expression);
        return errors.Count == 0 ? null : errors[0].ToString();
    }
}
