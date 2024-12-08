using System.IO;
using System.Windows;
using System.Windows.Controls;
using DbManager.Entities;
using Microsoft.Win32;

namespace DbManager.Forms
{
    public partial class MainWindow
    {
        private Database _database = new();
        private Table? _currentTable;
        private bool _cellEdit;
        private readonly TextWriter _errorWriter;

        public MainWindow()
        {
            InitializeComponent();
            RefreshTablesList();
            _errorWriter = Console.Error;
        }

        // Обновление списка таблиц
        private void RefreshTablesList()
        {
            TablesListBox.ItemsSource = null;
            TablesListBox.ItemsSource = _database.Tables.Select(t => t.Name).ToList();
        }

        // Обработчик выбора таблицы
        private void TablesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesListBox.SelectedItem is string selectedTableName)
            {
                _currentTable = _database.GetTable(selectedTableName);

                RefreshTable();
            }
        }

        private void RefreshTable(bool updateOnlyValues = false)
        {
            if (_currentTable != null)
            {
                if (!updateOnlyValues)
                {
                    // Очищаем предыдущие столбцы DataGrid
                    DataGrid.Columns.Clear();

                    // Генерируем столбцы на основе атрибутов таблицы
                    foreach (var column in _currentTable.Columns)
                    {
                        DataGrid.Columns.Add(new DataGridTextColumn
                        {
                            Header = column.Name,
                            Binding = new System.Windows.Data.Binding($"[{column.Name}]"),
                            CanUserSort = true,
                            CanUserResize = true,
                            CanUserReorder = true,
                        });
                    }
                }

                // Устанавливаем данные для отображения
                DataGrid.ItemsSource = _currentTable.Data
                    .Select(record => record.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                    .ToList();
            }
            else
            {
                DataGrid.ItemsSource = null; // Если таблица не найдена, очищаем DataGrid
            }
        }

        // Добавление таблицы
        private void AddTableButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddTableWindow();
            if (dialog.ShowDialog() == true)
            {
                var newTable = new Table(dialog.TableName, dialog.Columns, dialog.PrimaryKey);
                _database.Tables.Add(newTable);
                RefreshTablesList();
            }
        }

        // Удаление таблицы
        private void DeleteTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTable != null)
            {
                _database.Tables.Remove(_currentTable);
                _currentTable = null;
                DataGrid.ItemsSource = null;
                RefreshTablesList();
            }
        }

        // Добавление записи
        private void AddRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTable != null)
            {
                var dialog = new AddRecordWindow(_currentTable.Columns, _errorWriter);
                if (dialog.ShowDialog() == true)
                {
                    _currentTable.AddRecord(dialog.Record);
                    DataGrid.Items.Refresh();
                    RefreshTable(updateOnlyValues: true);
                }
            }
        }

        // Удаление записи
        private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTable != null && DataGrid.SelectedItem is Dictionary<string, object> record)
            {
                var pkValue = record[_currentTable.PrimaryKey];
                _currentTable.DeleteRecord(pkValue);
                DataGrid.Items.Refresh();
                RefreshTable(updateOnlyValues: true);
            }
        }

        // Сохранение базы данных
        private void SaveDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Открытие диалога для сохранения файла
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить базу данных",
                    Filter = "JSON файлы (*.json)|*.json|XML файлы (*.xml)|*.xml|Excel файлы (*.xlsx)|*.xlsx",
                    DefaultExt = ".json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string fileExtension = Path.GetExtension(filePath);

                    // Сохраняем данные в зависимости от выбранного формата
                    switch (fileExtension.ToLower())
                    {
                        case ".json":
                            _database.SaveToJson(filePath);
                            break;
                        case ".xml":
                            _database.SaveToXml(filePath);
                            break;
                        case ".xlsx":
                            _database.SaveToXlsx(filePath);
                            break;
                        default:
                            throw new Exception("Неподдерживаемый формат файла.");
                    }

                    MessageBox.Show("База данных успешно сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
               ShowError($"Ошибка сохранения базы данных: {ex.Message}", ex);
            }
        }

        // Загрузка базы данных
        private void LoadDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentTable = null;
                _cellEdit = false;

                // Открытие диалога для выбора файла
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Загрузить базу данных",
                    Filter = "JSON файлы (*.json)|*.json|XML файлы (*.xml)|*.xml|Excel файлы (*.xlsx)|*.xlsx",
                    DefaultExt = ".json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileExtension = Path.GetExtension(filePath);

                    // Загружаем данные в зависимости от выбранного формата
                    switch (fileExtension.ToLower())
                    {
                        case ".json":
                            _database = Database.LoadFromJson(filePath);
                            break;
                        case ".xml":
                            _database = Database.LoadFromXml(filePath);
                            break;
                        case ".xlsx":
                            _database = Database.LoadFromXlsx(filePath);
                            break;
                        default:
                            throw new Exception("Неподдерживаемый формат файла.");
                    }

                    // Обновляем список таблиц в интерфейсе
                    RefreshTablesList();
                    RefreshTable();
                    MessageBox.Show("База данных успешно загружена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки базы данных: {ex.Message}", ex);
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_currentTable == null)
                return;

            if (_cellEdit)
                return;

            try
            {
                var editedColumn = e.Column as DataGridTextColumn;
                if (editedColumn == null) return;

                // Получаем имя колонки
                var bindingPath = ((System.Windows.Data.Binding)editedColumn.Binding).Path.Path.Trim('[', ']');

                // Получаем изменённое значение
                var editedElement = e.EditingElement as TextBox;
                if (editedElement == null) return;

                var newValue = editedElement.Text;

                // Получаем запись, соответствующую строке
                var record = (Dictionary<string, object>)e.Row.Item;
                var primaryKeyValue = record[_currentTable.PrimaryKey];

                // Вызываем метод редактирования записи
                _currentTable.EditRecord(primaryKeyValue, bindingPath, newValue);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка редактирования: {ex.Message}", ex);
            }
            finally
            {
                _cellEdit = true;
                DataGrid.CancelEdit();
                DataGrid.CancelEdit();
                _cellEdit = false;
                DataGrid.Items.Refresh();
                RefreshTable(updateOnlyValues: true);
            }
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            _errorWriter.WriteLine(ex);
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentTable != null)
            {
                DataGrid.Items.Refresh();
                RefreshTable(updateOnlyValues: true);
            }
        }
    }
}
