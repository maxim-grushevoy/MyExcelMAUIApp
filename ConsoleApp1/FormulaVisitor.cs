using System.Globalization;
using System.Numerics;
using SpreadsheetExpressions.Grammar;

namespace SpreadsheetExpressions;

internal sealed class FormulaVisitor : SpreadsheetExpressionBaseVisitor<BigInteger>
{
    private readonly ICellValueResolver _cells;

    public FormulaVisitor(ICellValueResolver cells) => _cells = cells;

    public override BigInteger VisitCompileUnit(SpreadsheetExpressionParser.CompileUnitContext context)
        => Visit(context.expression());

    public override BigInteger VisitNumberExpr(SpreadsheetExpressionParser.NumberExprContext context)
        => BigInteger.Parse(context.NUMBER().GetText(), CultureInfo.InvariantCulture);

    public override BigInteger VisitCellExpr(SpreadsheetExpressionParser.CellExprContext context)
        => _cells.GetCellValue(context.CELL().GetText().ToUpperInvariant());

    public override BigInteger VisitParenExpr(SpreadsheetExpressionParser.ParenExprContext context)
        => Visit(context.expression());

    public override BigInteger VisitUnaryExpr(SpreadsheetExpressionParser.UnaryExprContext context)
    {
        var value = Visit(context.expression());
        return context.op.Type == SpreadsheetExpressionLexer.MINUS ? -value : value;
    }

    public override BigInteger VisitAddExpr(SpreadsheetExpressionParser.AddExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        return context.op.Type == SpreadsheetExpressionLexer.PLUS ? left + right : left - right;
    }

    public override BigInteger VisitMulExpr(SpreadsheetExpressionParser.MulExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        switch (context.op.Type)
        {
            case SpreadsheetExpressionLexer.MUL:
                return left * right;

            case SpreadsheetExpressionLexer.DIV:
            case SpreadsheetExpressionLexer.IDIV:
                EnsureNotZero(right, "Ділення на нуль.");
                return BigInteger.Divide(left, right);   

            default: 
                EnsureNotZero(right, "Остача від ділення на нуль.");
                return BigInteger.Remainder(left, right);
        }
    }

    public override BigInteger VisitFuncExpr(SpreadsheetExpressionParser.FuncExprContext context)
    {
        var args = context.expression().Select(Visit);
        return context.func.Type == SpreadsheetExpressionLexer.MMAX
            ? args.Aggregate(BigInteger.Max)
            : args.Aggregate(BigInteger.Min);
    }

    private static void EnsureNotZero(BigInteger divisor, string message)
    {
        if (divisor.IsZero)
            throw new FormulaEvaluationException(message);
    }
}
