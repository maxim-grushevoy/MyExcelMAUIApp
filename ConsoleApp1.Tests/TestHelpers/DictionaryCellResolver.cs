using System.Numerics;
using SpreadsheetExpressions;

namespace ConsoleApp1.Tests;

/// <summary>Проста заглушка таблиці: значення клітинок беруться зі словника.</summary>
public sealed class DictionaryCellResolver : ICellValueResolver
{
    private readonly Dictionary<string, BigInteger> _values = new();

    public DictionaryCellResolver Add(string address, BigInteger value)
    {
        _values[address] = value;
        return this;
    }

    public BigInteger GetCellValue(string address)
    {
        if (_values.TryGetValue(address, out var value))
            return value;

        throw new FormulaEvaluationException($"Клітинки {address} не існує.");
    }
}
