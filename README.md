# Електронна таблиця (лабораторна робота № 1, варіант 4)

Застосунок для роботи з електронними таблицями. Клітинки містять арифметичні вирази; вирази
перевіряються (синтаксичний аналіз з локалізацією помилок) і обчислюються.

**Варіант 4** — набір операцій 1, 2, 3, 7:
бінарні `+ - * /`, `mod`, `div`, унарні `+ -`, функції `mmax(x1,…,xN)` та `mmin(x1,…,xN)` (N ≥ 1).
У виразах: цілі числа довільної довжини, круглі дужки, імена клітинок (`A3`).

## Структура

| Проєкт | Призначення |
|---|---|
| `ConsoleApp1` | бібліотека (Class Library): граматика ANTLR, парсер, обчислювач, модель таблиці, контролер, збереження |
| `MyExcelMAUIApp` | користувацький інтерфейс (.NET MAUI, Windows) |

## Збірка і запуск

Потрібно: Visual Studio з навантаженням **.NET Multi-platform App UI development**, **Java** у `PATH`
(для генерації ANTLR-коду), доступ до NuGet.

1. Відкрити `MyExcelMAUIApp.slnx`.
2. Стартовий проєкт — **MyExcelMAUIApp**, платформа — **Windows Machine**.
3. Build → Rebuild Solution, потім F5.

## Відповідність діаграмам

| Діаграма | У коді |
|---|---|
| `SpreadsheetForm` | `MainPage` (`UpdateGridDimensions`, `OnCellEdit`, `ToggleDisplayMode`, `ShowErrorInUI`, `UpdateUI`) |
| `SpreadsheetController` | `SpreadsheetController` (`LoadTable`, `SaveTable`, `UpdateCellExpression`, `InsertRow`, `InsertColumn`) |
| `Spreadsheet` | `Spreadsheet` (`GetCell`, `InsertRow`, `InsertColumn`, `UpdateCellAndRecalculateDependents`) |
| `Cell` | `Cell` (значення — `BigInteger`, бо числа довільної довжини) |
| `Parser` | `FormulaParser` (`ValidateSyntax`), граматика `Grammar/SpreadsheetExpression.g4` |
| `ExpressionEvaluator` | `FormulaEvaluator` (`ComputeValue`) + `FormulaVisitor` |
