using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DbManager.Entities;

namespace DbManager.Forms
{
    public partial class AddTableWindow : Window
    {
        public string TableName { get; private set; }
        public List<Column> Columns { get; private set; }
        public string PrimaryKey { get; private set; }

        private List<ColumnEntry> _columnEntries = new();

        public AddTableWindow()
        {
            InitializeComponent();
        }

        // Класс для хранения информации о колонке
        private class ColumnEntry
        {
            public TextBox NameTextBox { get; set; }
            public ComboBox TypeComboBox { get; set; }
            public CheckBox PrimaryKeyCheckBox { get; set; }
            public Button DeleteButton { get; set; }
        }

        private StackPanel CreatePlaceholderTextBox(out TextBox textBox, string placeholder)
        {
            var container = new StackPanel { Orientation = Orientation.Vertical };
            var textBlock = new TextBlock
            {
                Text = placeholder,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 0, 0, -20),
                IsHitTestVisible = false
            };

            var newTextBox = new TextBox();
            newTextBox.TextChanged += (s, e) =>
            {
                textBlock.Visibility = string.IsNullOrEmpty(newTextBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            container.Children.Add(textBlock);
            container.Children.Add(newTextBox);

            textBox = newTextBox;
            return container;
        }

        // Обработчик для добавления колонки
        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

            var nameContainer = CreatePlaceholderTextBox(out var nameTextBox, "Имя колонки");
            nameContainer.Margin = new Thickness(0, 0, 5, 0);
            nameTextBox.Width = 150;

            var typeComboBox = new ComboBox { Width = 100, Margin = new Thickness(0, 0, 5, 0) };
            typeComboBox.ItemsSource = new[] { "int", "decimal", "string", "date", "bool" };
            typeComboBox.SelectedIndex = 0;

            var primaryKeyCheckBox = new CheckBox { Content = "PK", Margin = new Thickness(0, 0, 5, 0) };
            primaryKeyCheckBox.Checked += PrimaryKeyCheckBox_Checked;

            var columnEntry = new ColumnEntry(); // Объявляем columnEntry здесь, до удаления
            var deleteButton = new Button { Content = "-", Width = 30, Margin = new Thickness(0, 0, 5, 0) };
            deleteButton.Click += (s, ev) =>
            {
                _columnEntries.Remove(columnEntry);
                ColumnsPanel.Children.Remove(stackPanel);
            };

            stackPanel.Children.Add(nameContainer);
            stackPanel.Children.Add(typeComboBox);
            stackPanel.Children.Add(primaryKeyCheckBox);
            stackPanel.Children.Add(deleteButton);

            ColumnsPanel.Children.Add(stackPanel);

            columnEntry = new ColumnEntry
            {
                NameTextBox = nameTextBox,
                TypeComboBox = typeComboBox,
                PrimaryKeyCheckBox = primaryKeyCheckBox,
                DeleteButton = deleteButton
            };

            _columnEntries.Add(columnEntry);
        }


        // Обработчик для чекбоксов PrimaryKey
        private void PrimaryKeyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var entry in _columnEntries)
            {
                if (entry.PrimaryKeyCheckBox != sender)
                    entry.PrimaryKeyCheckBox.IsChecked = false;
            }
        }

        // Обработчик для создания таблицы
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TableNameTextBox.Text))
            {
                MessageBox.Show("Введите имя таблицы.");
                return;
            }

            TableName = TableNameTextBox.Text;
            Columns = new List<Column>();
            PrimaryKey = null;

            foreach (var entry in _columnEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.NameTextBox.Text))
                {
                    MessageBox.Show("Укажите имя для всех колонок.");
                    return;
                }

                var columnName = entry.NameTextBox.Text;
                var columnType = entry.TypeComboBox.SelectedItem.ToString();
                Columns.Add(new Column(columnName, columnType));

                if (entry.PrimaryKeyCheckBox.IsChecked == true)
                {
                    if (PrimaryKey != null)
                    {
                        MessageBox.Show("Только одна колонка может быть Primary Key.");
                        return;
                    }
                    PrimaryKey = columnName;
                }
            }

            if (PrimaryKey == null)
            {
                MessageBox.Show("Выберите колонку для Primary Key.");
                return;
            }

            DialogResult = true;
        }
    }
}
