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
      CreateGrid();
    }

    // ---------- створення таблиці ----------

    private void CreateGrid()
    {
      grid.Children.Clear();
      grid.RowDefinitions.Clear();
      grid.ColumnDefinitions.Clear();

      AddColumnsAndColumnLabels();
      AddRowsAndCellEntries();
      RefreshCells();
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
    private void RefreshCells()
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

    private void ShowCellStatus(int row, int col)
    {
      var cell = controller.Table.GetCell(row, col);
      string address = CellAddress.Format(row, col);

      statusLabel.Text = cell.Error is not null
        ? $"{address}: {cell.Error}"
        : cell.IsEmpty ? $"{address}: порожня клітинка" : $"{address}: {cell.Expression}";
    }

    // ---------- події клітинок ----------

    private void Entry_Focused(object sender, FocusEventArgs e)
    {
      var entry = (Entry)sender;
      ShowCellStatus(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
    }

    // викликається, коли користувач вийде зі зміненої клітинки (втратить фокус)
    private void Entry_Unfocused(object sender, FocusEventArgs e)
    {
      if (!isFormulaMode) return;

      var entry = (Entry)sender;
      int row = Grid.GetRow(entry) - 1;
      int col = Grid.GetColumn(entry) - 1;

      controller.UpdateCellExpression(row, col, entry.Text ?? "");
      RefreshCells();
      ShowCellStatus(row, col);
    }

    // ---------- кнопки ----------

    private void CalculateButton_Clicked(object sender, EventArgs e)
    {
      controller.Recalculate();
      RefreshCells();

      int errors = controller.Table.ErrorCount;
      statusLabel.Text = errors == 0
        ? "Обчислено без помилок."
        : $"Обчислено. Клітинок з помилками: {errors} (виділені червоним).";
    }

    private void ModeButton_Clicked(object sender, EventArgs e)
    {
      isFormulaMode = !isFormulaMode;
      modeButton.Text = isFormulaMode ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
      RefreshCells();
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
      if (controller.FilePath is null)
        await SaveAsAsync();
      else
        await TrySaveAsync(controller.FilePath);
    }

    private async void SaveAsButton_Clicked(object sender, EventArgs e) => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
      string? name = await DisplayPromptAsync("Зберегти як", "Назва файлу (без розширення):",
        "OK", "Скасувати", initialValue: "table");
      if (string.IsNullOrWhiteSpace(name)) return;

      name = name.Trim();
      if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
      {
        await DisplayAlert("Помилка", "Назва файлу містить недопустимі символи.", "OK");
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
        await DisplayAlert("Помилка збереження", ex.Message, "OK");
      }
    }

    private async void ReadButton_Clicked(object sender, EventArgs e)
    {
      var file = await FilePicker.Default.PickAsync(new PickOptions
      {
        PickerTitle = "Виберіть файл таблиці",
        FileTypes = JsonFileType
      });
      if (file is null) return;

      try
      {
        controller.LoadTable(file.FullPath);
        CreateGrid();
        statusLabel.Text = $"Прочитано: {file.FullPath}";
      }
      catch (Exception ex)
      {
        await DisplayAlert("Помилка читання", ex.Message, "OK");
      }
    }

    private async void ExitButton_Clicked(object sender, EventArgs e)
    {
      bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
      if (answer)
      {
        System.Environment.Exit(0);
      }
    }

    private async void HelpButton_Clicked(object sender, EventArgs e)
    {
      await DisplayAlert("Довідка",
        "Лабораторна робота 1. Електронна таблиця, варіант 4.\n" +
        "Клітинка містить вираз (необов'язково з «=» на початку): цілі числа довільної довжини, " +
        "посилання на клітинки (A1), дужки, операції + - * /, mod, div, унарні + -, " +
        "функції mmax(...) та mmin(...).\n" +
        "Кнопка «Режим» перемикає відображення ВИРАЗ / ЗНАЧЕННЯ. " +
        "Клітинки з помилками підсвічуються червоним, текст помилки видно в рядку стану.\n" +
        "Кнопки «＋ Рядок у кінець» і «＋ Стовпчик у кінець» додають рядок або стовпчик унизу чи праворуч. " +
        "Щоб додати в довільне місце, видалити або змінити розмір, натисніть на номер рядка " +
        "або на літеру стовпчика — з'явиться меню.",
        "OK");
    }

    // ---------- рядки і стовпчики ----------

    private void AddRowButton_Clicked(object sender, EventArgs e)
    {
      controller.InsertRow(controller.Table.RowCount);
      CreateGrid();
      statusLabel.Text = $"Додано рядок {controller.Table.RowCount} у кінець таблиці.";
    }

    private void AddColumnButton_Clicked(object sender, EventArgs e)
    {
      controller.InsertColumn(controller.Table.ColumnCount);
      CreateGrid();
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

      string action = await DisplayActionSheet($"Рядок {row + 1}", "Скасувати", delete, above, below, resize);

      switch (action)
      {
        case above:
          controller.InsertRow(row);
          CreateGrid();
          statusLabel.Text = $"Додано рядок {row + 1} (вище попереднього {row + 1}-го).";
          break;

        case below:
          controller.InsertRow(row + 1);
          CreateGrid();
          statusLabel.Text = $"Додано рядок {row + 2} (нижче рядка {row + 1}).";
          break;

        case delete:
          if (controller.Table.RowCount == 1)
          {
            await DisplayAlert("Помилка", "Не можна видалити останній рядок.", "OK");
            break;
          }
          controller.DeleteRow(row);
          CreateGrid();
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

      string action = await DisplayActionSheet($"Стовпчик {name}", "Скасувати", delete, left, right, resize);

      switch (action)
      {
        case left:
          controller.InsertColumn(col);
          CreateGrid();
          statusLabel.Text = $"Додано стовпчик ліворуч від {name}.";
          break;

        case right:
          controller.InsertColumn(col + 1);
          CreateGrid();
          statusLabel.Text = $"Додано стовпчик праворуч від {name}.";
          break;

        case delete:
          if (controller.Table.ColumnCount == 1)
          {
            await DisplayAlert("Помилка", "Не можна видалити останній стовпчик.", "OK");
            break;
          }
          controller.DeleteColumn(col);
          CreateGrid();
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

      await DisplayAlert("Помилка", "Розмір має бути числом від 20 до 500.", "OK");
      return null;
    }
  }
}
