using System.Globalization;
using SpreadsheetExpressions;

namespace MyExcelMAUIApp
{
  public partial class MainPage : ContentPage
  {
    private static readonly Color ErrorColor = Color.FromArgb("#FFB3B3");
    private static readonly Color HeaderColor = Color.FromArgb("#C9D3E6");

    private static readonly FilePickerFileType JsonFileType = new(
      new Dictionary<DevicePlatform, IEnumerable<string>>
      {
        { DevicePlatform.WinUI, new[] { ".json" } },
        { DevicePlatform.Android, new[] { "application/json" } },
        { DevicePlatform.iOS, new[] { "public.json" } },
        { DevicePlatform.MacCatalyst, new[] { "public.json" } }
      });

    private readonly SpreadsheetController controller = new();
    private bool isFormulaMode = true;   // true — режим ВИРАЗ, false — режим ЗНАЧЕННЯ

    public MainPage()
    {
      InitializeComponent();
      UpdateGridDimensions();
    }

    // ---------- створення таблиці ----------

    // (пере)будує сітку за поточними розмірами таблиці
    private void UpdateGridDimensions()
    {
      grid.Children.Clear();
      grid.RowDefinitions.Clear();
      grid.ColumnDefinitions.Clear();

      AddColumnsAndColumnLabels();
      AddRowsAndCellEntries();
      UpdateUI();
    }

    // заголовок, на який можна натиснути (номер рядка або літера стовпчика)
    private static Label CreateHeaderLabel(string text, string hint, EventHandler<TappedEventArgs> onTapped)
    {
      var label = new Label
      {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.Black,
        BackgroundColor = HeaderColor,
        Padding = new Thickness(0, 6),
        Margin = new Thickness(1),
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill
      };
      ToolTipProperties.SetText(label, hint);

      var tap = new TapGestureRecognizer();
      tap.Tapped += onTapped;
      label.GestureRecognizers.Add(tap);
      return label;
    }

    private void AddColumnsAndColumnLabels()
    {
      var table = controller.Table;

      // рядок із підписами стовпчиків і стовпчик із номерами рядків
      grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
      grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(50)));

      for (int col = 0; col < table.ColumnCount; col++)
      {
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(table.GetColumnWidth(col))));

        var label = CreateHeaderLabel(CellAddress.ColumnName(col),
          "Натисніть: додати, видалити або змінити ширину стовпчика", ColumnHeader_Tapped);
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, col + 1);
        grid.Children.Add(label);
      }
    }

    private void AddRowsAndCellEntries()
    {
      var table = controller.Table;

      for (int row = 0; row < table.RowCount; row++)
      {
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(table.GetRowHeight(row))));

        var label = CreateHeaderLabel((row + 1).ToString(),
          "Натисніть: додати, видалити або змінити висоту рядка", RowHeader_Tapped);
        Grid.SetRow(label, row + 1);
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        for (int col = 0; col < table.ColumnCount; col++)
        {
          var entry = new Entry
          {
            Text = "",
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
          };
          entry.Focused += Entry_Focused;
          entry.Unfocused += Entry_Unfocused;
          Grid.SetRow(entry, row + 1);
          Grid.SetColumn(entry, col + 1);
          grid.Children.Add(entry);
        }
      }
    }

    // показує у клітинках вирази або значення, підсвічує клітинки з помилками
    private void UpdateUI()
    {
      var table = controller.Table;

      foreach (var entry in grid.Children.OfType<Entry>())
      {
        int row = Grid.GetRow(entry) - 1;
        int col = Grid.GetColumn(entry) - 1;
        var cell = table.GetCell(row, col);

        entry.Text = isFormulaMode ? cell.Expression : cell.DisplayValue;
        entry.IsReadOnly = !isFormulaMode;

        if (cell.Error is not null)
        {
          entry.BackgroundColor = ErrorColor;
          entry.TextColor = Colors.Black;
        }
        else
        {
          entry.ClearValue(VisualElement.BackgroundColorProperty);
          entry.ClearValue(Entry.TextColorProperty);
        }
      }
    }

    // повідомлення про помилку (або вміст клітинки) у рядку стану
    private void ShowErrorInUI(string message) => statusLabel.Text = message;

    private void ShowCellStatus(int row, int col)
    {
      var cell = controller.Table.GetCell(row, col);
      string address = CellAddress.Format(row, col);

      if (cell.Error is not null)
        ShowErrorInUI($"{address}: {cell.Error}");
      else
        statusLabel.Text = cell.IsEmpty ? $"{address}: порожня клітинка" : $"{address}: {cell.Expression}";
    }

    // ---------- події клітинок ----------

    private void Entry_Focused(object? sender, FocusEventArgs e)
    {
      var entry = (Entry)sender!;
      int row = Grid.GetRow(entry) - 1;
      int col = Grid.GetColumn(entry) - 1;
      ShowCellStatus(row, col);

      // локалізація помилки: виділяємо у виразі фрагмент, де виявлено синтаксичну помилку
      var cell = controller.Table.GetCell(row, col);
      if (isFormulaMode && cell.ErrorStart is int start)
      {
        int length = cell.ErrorLength;
        Dispatcher.Dispatch(() =>
        {
          int textLength = entry.Text?.Length ?? 0;
          int position = Math.Min(start, textLength);
          entry.CursorPosition = position;
          entry.SelectionLength = Math.Min(length, textLength - position);
        });
      }
    }

    // викликається, коли користувач вийде зі зміненої клітинки (втратить фокус)
    private void Entry_Unfocused(object? sender, FocusEventArgs e)
    {
      if (!isFormulaMode) return;

      var entry = (Entry)sender!;
      OnCellEdit(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1, entry.Text ?? "");
    }

    private void OnCellEdit(int row, int col, string expression)
    {
      controller.UpdateCellExpression(row, col, expression);
      UpdateUI();
      ShowCellStatus(row, col);
    }

    // ---------- кнопки ----------

    private void CalculateButton_Clicked(object? sender, EventArgs e)
    {
      controller.Recalculate();
      UpdateUI();

      int errors = controller.Table.ErrorCount;
      statusLabel.Text = errors == 0
        ? "Обчислено без помилок."
        : $"Обчислено. Клітинок з помилками: {errors} (виділені червоним).";
    }

    private void ModeButton_Clicked(object? sender, EventArgs e) => ToggleDisplayMode();

    private void ToggleDisplayMode()
    {
      isFormulaMode = !isFormulaMode;
      modeButton.Text = isFormulaMode ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
      UpdateUI();
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
      if (controller.FilePath is null)
        await SaveAsAsync();
      else
        await TrySaveAsync(controller.FilePath);
    }

    private async void SaveAsButton_Clicked(object? sender, EventArgs e) => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
      string? name = await DisplayPromptAsync("Зберегти як", "Назва файлу (без розширення):",
        "OK", "Скасувати", initialValue: "table");
      if (string.IsNullOrWhiteSpace(name)) return;

      name = name.Trim();
      if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
      {
        await DisplayAlertAsync("Помилка", "Назва файлу містить недопустимі символи.", "OK");
        return;
      }

      string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      await TrySaveAsync(Path.Combine(folder, name + ".json"));
    }

    private async Task TrySaveAsync(string path)
    {
      try
      {
        controller.SaveTable(path);
        statusLabel.Text = $"Збережено: {path}";
      }
      catch (Exception ex)
      {
        await DisplayAlertAsync("Помилка збереження", ex.Message, "OK");
      }
    }

    private async void ReadButton_Clicked(object? sender, EventArgs e)
    {
      if (controller.IsModified)
      {
        bool proceed = await DisplayAlertAsync("Підтвердження",
          "Є незбережені зміни. Відкрити інший файл без збереження?", "Так", "Ні");
        if (!proceed) return;
      }

      var file = await FilePicker.Default.PickAsync(new PickOptions
      {
        PickerTitle = "Виберіть файл таблиці",
        FileTypes = JsonFileType
      });
      if (file is null) return;

      try
      {
        controller.LoadTable(file.FullPath);
        UpdateGridDimensions();
        statusLabel.Text = $"Прочитано: {file.FullPath}";
      }
      catch (Exception ex)
      {
        await DisplayAlertAsync("Помилка читання", ex.Message, "OK");
      }
    }

    private async void ExitButton_Clicked(object? sender, EventArgs e)
    {
      string message = controller.IsModified
        ? "Є незбережені зміни. Ви дійсно хочете вийти без збереження?"
        : "Ви дійсно хочете вийти?";

      bool answer = await DisplayAlertAsync("Підтвердження", message, "Так", "Ні");
      if (answer)
      {
        System.Environment.Exit(0);
      }
    }

    private async void HelpButton_Clicked(object? sender, EventArgs e)
    {
      await DisplayAlertAsync("Довідка",
        "Клітинка містить вираз (необов'язково з «=» на початку).\n" +
        "У виразі можна використовувати: цілі числа довільної довжини, посилання на клітинки (A1, B12), " +
        "круглі дужки, операції + - * /, mod, div, унарні + та -, функції mmax(...) і mmin(...) " +
        "з одним або більше аргументами. Пропуски ігноруються.\n\n" +
        "Кнопка «Режим» перемикає відображення ВИРАЗ / ЗНАЧЕННЯ. У режимі ЗНАЧЕННЯ клітинки лише для читання.\n\n" +
        "Помилки: клітинка з помилкою підсвічується червоним, текст помилки показано в рядку стану; " +
        "якщо помилка синтаксична, при вході в клітинку виділяється фрагмент виразу з помилкою.\n\n" +
        "Кнопки «＋ Рядок у кінець» і «＋ Стовпчик у кінець» додають рядок або стовпчик унизу чи праворуч. " +
        "Щоб додати в довільне місце, видалити або змінити розмір, натисніть на номер рядка " +
        "або на літеру стовпчика — з'явиться меню.\n\n" +
        "Таблицю можна зберегти («Зберегти», «Зберегти як») у файл у папці «Документи» та прочитати знову.",
        "OK");
    }

    private async void AboutButton_Clicked(object? sender, EventArgs e)
    {
      await DisplayAlertAsync("Про програму",
        "Електронна таблиця\n" +
        "Лабораторна робота № 1, варіант 4\n" +
        "(операції: + - * /, mod, div, унарні + -, функції mmax, mmin).\n\n" +
        "Аналіз виразів: ANTLR 4. Інтерфейс: .NET MAUI.",
        "OK");
    }

    // ---------- рядки і стовпчики ----------

    private void AddRowButton_Clicked(object? sender, EventArgs e)
    {
      controller.InsertRow(controller.Table.RowCount);
      UpdateGridDimensions();
      statusLabel.Text = $"Додано рядок {controller.Table.RowCount} у кінець таблиці.";
    }

    private void AddColumnButton_Clicked(object? sender, EventArgs e)
    {
      controller.InsertColumn(controller.Table.ColumnCount);
      UpdateGridDimensions();
      statusLabel.Text = $"Додано стовпчик {CellAddress.ColumnName(controller.Table.ColumnCount - 1)} у кінець таблиці.";
    }

    // натиснули на номер рядка -> меню дій
    private async void RowHeader_Tapped(object? sender, TappedEventArgs e)
    {
      if (sender is not Label label) return;
      int row = Grid.GetRow(label) - 1;

      const string above = "Додати рядок вище";
      const string below = "Додати рядок нижче";
      const string delete = "Видалити рядок";
      const string resize = "Змінити висоту";

      string action = await DisplayActionSheetAsync($"Рядок {row + 1}", "Скасувати", delete, above, below, resize);

      switch (action)
      {
        case above:
          controller.InsertRow(row);
          UpdateGridDimensions();
          statusLabel.Text = $"Додано новий рядок {row + 1}.";
          break;

        case below:
          controller.InsertRow(row + 1);
          UpdateGridDimensions();
          statusLabel.Text = $"Додано новий рядок {row + 2} (нижче рядка {row + 1}).";
          break;

        case delete:
          if (controller.Table.RowCount == 1)
          {
            await DisplayAlertAsync("Помилка", "Не можна видалити останній рядок.", "OK");
            break;
          }
          controller.DeleteRow(row);
          UpdateGridDimensions();
          statusLabel.Text = $"Видалено рядок {row + 1}.";
          break;

        case resize:
          double? height = await AskSizeAsync("Висота рядка", controller.Table.GetRowHeight(row));
          if (height is null) break;
          controller.SetRowHeight(row, height.Value);
          grid.RowDefinitions[row + 1].Height = new GridLength(height.Value);
          break;
      }
    }

    // натиснули на літеру стовпчика -> меню дій
    private async void ColumnHeader_Tapped(object? sender, TappedEventArgs e)
    {
      if (sender is not Label label) return;
      int col = Grid.GetColumn(label) - 1;
      string name = CellAddress.ColumnName(col);

      const string left = "Додати стовпчик ліворуч";
      const string right = "Додати стовпчик праворуч";
      const string delete = "Видалити стовпчик";
      const string resize = "Змінити ширину";

      string action = await DisplayActionSheetAsync($"Стовпчик {name}", "Скасувати", delete, left, right, resize);

      switch (action)
      {
        case left:
          controller.InsertColumn(col);
          UpdateGridDimensions();
          statusLabel.Text = $"Додано стовпчик ліворуч від {name}.";
          break;

        case right:
          controller.InsertColumn(col + 1);
          UpdateGridDimensions();
          statusLabel.Text = $"Додано стовпчик праворуч від {name}.";
          break;

        case delete:
          if (controller.Table.ColumnCount == 1)
          {
            await DisplayAlertAsync("Помилка", "Не можна видалити останній стовпчик.", "OK");
            break;
          }
          controller.DeleteColumn(col);
          UpdateGridDimensions();
          statusLabel.Text = $"Видалено стовпчик {name}.";
          break;

        case resize:
          double? width = await AskSizeAsync("Ширина стовпчика", controller.Table.GetColumnWidth(col));
          if (width is null) break;
          controller.SetColumnWidth(col, width.Value);
          grid.ColumnDefinitions[col + 1].Width = new GridLength(width.Value);
          break;
      }
    }

    private async Task<double?> AskSizeAsync(string title, double current)
    {
      string? text = await DisplayPromptAsync(title, "Розмір у пікселях (від 20 до 500):",
        "OK", "Скасувати", initialValue: current.ToString(CultureInfo.CurrentCulture),
        keyboard: Keyboard.Numeric);
      if (text is null) return null;   // натиснули «Скасувати»

      if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double size)
          && size >= 20 && size <= 500)
        return size;

      await DisplayAlertAsync("Помилка", "Розмір має бути числом від 20 до 500.", "OK");
      return null;
    }
  }
}
