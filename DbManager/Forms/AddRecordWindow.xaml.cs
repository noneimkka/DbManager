using System.IO;
using System.Windows;
using System.Windows.Controls;
using DbManager.Entities;

namespace DbManager.Forms
{
    public partial class AddRecordWindow
    {
        private readonly List<Column> _columns;
        public Dictionary<string, object> Record { get; private set; }
        private readonly TextWriter _errorWriter;

        public AddRecordWindow(List<Column> columns, TextWriter errorWriter)
        {
            InitializeComponent();
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            GenerateFields();
            _errorWriter = errorWriter;
            Record = new Dictionary<string, object>();
        }

        private void GenerateFields()
        {
            foreach (var column in _columns)
            {
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

                var label = new TextBlock
                {
                    Text = $"{column.Name} ({column.Type})",
                    Width = 150,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var textBox = new TextBox { Name = $"Field_{column.Name}", Width = 200 };
                textBox.Tag = column.Name;

                stackPanel.Children.Add(label);
                stackPanel.Children.Add(textBox);

                FieldsPanel.Children.Add(stackPanel);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Record = new Dictionary<string, object>();

                foreach (var child in FieldsPanel.Children)
                {
                    if (child is StackPanel stackPanel)
                    {
                        var textBox = stackPanel.Children[1] as TextBox;
                        if (textBox != null)
                        {
                            var columnName = textBox.Tag.ToString();
                            var column = _columns.Find(c => c.Name == columnName);
                            if (column == null)
                                throw new Exception($"Колонка с именем '{columnName}' не найдена.");

                            var value = ConvertValue(textBox.Text, column.Type);
                            Record.Add(column.Name, value);
                        }
                    }
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка добавления записи: {ex.Message}", ex);
            }
        }

        private object ConvertValue(string input, string type)
        {
            try
            {
                return type.ToLower() switch
                {
                    "int" => int.Parse(input),
                    "decimal" => decimal.Parse(input),
                    "date" => DateTime.Parse(input),
                    "bool" => bool.Parse(input),
                    "string" => input,
                    _ => throw new Exception($"Неподдерживаемый тип данных: {type}")
                };
            }
            catch (Exception)
            {
                throw new Exception($"Некорректное значение для типа '{type}': {input}");
            }
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            _errorWriter.WriteLine(ex);
        }
    }
}
