namespace DbManager.Entities;

public class Table
{
    public string Name { get; set; }
    public List<Column> Columns { get; set; }
    public List<Dictionary<string, object>> Data { get; set; }
    public string PrimaryKey { get; set; }

    public Table(string name, List<Column> columns, string primaryKey)
    {
        if (columns.All(c => c.Name != primaryKey))
        {
            throw new ArgumentException($"Не найден первичный ключ '{primaryKey}' среди колонок.");
        }

        if (columns.GroupBy(x => x.Name).Any(g => g.Count() > 1))
            throw new ApplicationException("Названия колонок должны быть уникальными");

        Name = name;
        Columns = columns;
        Data = new List<Dictionary<string, object>>();
        PrimaryKey = primaryKey;
    }

    // Добавление записи в таблицу
    public void AddRecord(Dictionary<string, object> record)
    {
        if (!record.TryGetValue(PrimaryKey, out var primaryKeyValue))
            throw new ApplicationException("Не задано значение для PrimaryKey.");

        if (Data.Any(r => ConvertValue(r[PrimaryKey], GetColumnType(PrimaryKey)).Equals(primaryKeyValue)))
        {
            throw new ApplicationException($"Запись с таким значением PrimaryKey ({primaryKeyValue}) уже существует.");
        }

        foreach (var column in Columns)
        {
            if (!record.ContainsKey(column.Name))
                throw new ApplicationException($"Колонка {column.Name} отсутствует в записи.");

            record[column.Name] = ConvertValue(record[column.Name], column.Type);
        }
        Data.Add(record);
    }

    // Удаление строки по значению Primary Key
    public void DeleteRecord(object pkValue)
    {
        var recordToDelete = Data.FirstOrDefault(record =>
            record.ContainsKey(PrimaryKey) && record[PrimaryKey].Equals(pkValue));

        if (recordToDelete != null)
        {
            Data.Remove(recordToDelete);
        }
        else
        {
            throw new ApplicationException($"Не удалось получить запись с ключом {pkValue}");
        }
    }

    // Редактирование строки по значению Primary Key
    public void EditRecord(object primaryKeyValue, string columnName, object newValue)
    {
        var recordToEdit = Data
            .FirstOrDefault(record => record.ContainsKey(PrimaryKey)
                && record[PrimaryKey].Equals(primaryKeyValue));

        if (recordToEdit == null)
            throw new ApplicationException($"Не удалось получить запись с ключом {primaryKeyValue}");

        if (recordToEdit.ContainsKey(columnName))
        {
            var value = ConvertValue(newValue, GetColumnType(columnName));

            if (columnName == PrimaryKey)
            {
                var currentKey = ConvertValue(recordToEdit[columnName], GetColumnType(PrimaryKey));

                if (!currentKey.Equals(value) && Data.Any(r => ConvertValue(r[PrimaryKey], GetColumnType(PrimaryKey)).Equals(value)))
                    throw new ApplicationException($"Запись с таким значением PrimaryKey ({value}) уже существует.");
            }

            recordToEdit[columnName] = value;
        }
        else
        {
            throw new ApplicationException($"Колонка {columnName} не найдена в таблице.");
        }
    }

    // Конвертация значений в нужный тип
    private object ConvertValue(object value, string type)
    {
        return type switch
        {
            "int" => Convert.ToInt32(value),
            "decimal" => Convert.ToDecimal(value),
            "date" => DateTime.Parse(value.ToString()),
            "bool" => Convert.ToBoolean(value),
            "string" => value.ToString(),
            _ => throw new Exception($"Неподдерживаемый тип: {type}")
        };
    }

    // Получение типа колонки по имени
    private string GetColumnType(string columnName)
    {
        return Columns.FirstOrDefault(c => c.Name == columnName)?.Type
            ?? throw new Exception($"Колонка {columnName} не найдена.");
    }
}