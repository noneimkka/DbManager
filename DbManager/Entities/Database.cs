using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using ClosedXML.Excel;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DbManager.Entities;

public class Database
{
    public List<Table> Tables { get; set; }

    public Database()
    {
        Tables = new List<Table>();
    }

    // Добавление таблицы
    public void AddTable(string name, List<Column> columns, string primaryKey)
    {
        Tables.Add(new Table(name, columns, primaryKey));
    }

    // Удаление таблицы по имени
    public void RemoveTable(string name)
    {
        var table = Tables.FirstOrDefault(t => t.Name == name);
        if (table != null)
        {
            Tables.Remove(table);
        }
        else
        {
            throw new Exception($"Table {name} not found.");
        }
    }

    // Получение таблицы по имени
    public Table GetTable(string name)
    {
        return Tables.FirstOrDefault(t => t.Name == name)
            ?? throw new ApplicationException($"Не удалось получить таблицу {name}");
    }

    // Сохранение базы данных в JSON
    public void SaveToJson(string filePath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    // Загрузка базы данных из JSON
    public static Database LoadFromJson(string filePath)
    {
        if (!File.Exists(filePath)) return new Database();
        var json = File.ReadAllText(filePath)
            ?? throw new ApplicationException("Не удалось получить json");

        return JsonConvert.DeserializeObject<Database>(json)
            ?? throw new ApplicationException("Не удалось сформировать базу данных по json");
    }

    // Сохранение базы данных в XML
    public void SaveToXml(string filePath)
    {
        var document = new XDocument(new XElement("Database",
            Tables.Select(t => new XElement("Table",
                new XAttribute("name", t.Name),
                new XAttribute("primaryKey", t.PrimaryKey),
                new XElement("Columns", t.Columns.Select(c =>
                    new XElement("Column", new XAttribute("name", c.Name), new XAttribute("type", c.Type)))),
                new XElement("Data", t.Data.Select(row =>
                    new XElement("Row", row.Select(cell =>
                        new XElement(cell.Key, cell.Value)))))))));

        document.Save(filePath);
    }

    // Загрузка базы данных из XML
    public static Database LoadFromXml(string filePath)
    {
        if (!File.Exists(filePath)) return new Database();

        var document = XDocument.Load(filePath);
        var database = new Database();

        if (document.Root != null)
            foreach (var tableElement in document.Root.Elements("Table"))
            {
                var tableName = tableElement.Attribute("name")?.Value
                    ?? throw new ApplicationException("Не удалось получить наименование таблицы");

                var tablePrimaryKey = tableElement.Attribute("primaryKey")?.Value
                    ?? throw new ApplicationException($"Не удалось получить первичный ключ для таблицы {tableName}");

                var columns = tableElement.Element("Columns")?
                    .Elements("Column")
                    .Select(c => new Column(
                        c.Attribute("name")?.Value
                            ?? throw new ApplicationException("Не найдено имя колонки"),
                        c.Attribute("type")?.Value
                            ?? throw new ApplicationException("Не найден тип колонки")))
                    .ToList()
                    ?? throw new ApplicationException($"Не удалось получить колонки для таблицы {tableName}");

                var table = new Table(tableName, columns, tablePrimaryKey);

                var records = tableElement.Element("Data")
                    ?? throw new ApplicationException($"Не удалось получить данные для таблицы {tableName}");

                foreach (var rowElement in records.Elements("Row"))
                {
                    var record = rowElement.Elements().ToDictionary(
                        cell => cell.Name.LocalName,
                        cell => (object)cell.Value
                    );

                    table.AddRecord(record);
                }

                database.Tables.Add(table);
            }

        return database;
    }

    // Сохранение базы данных в XLSX
    public void SaveToXlsx(string filePath)
    {
        using var workbook = new XLWorkbook();

        foreach (var table in Tables)
        {
            var worksheet = workbook.Worksheets.Add(table.Name);

            var sortedColumns = table.Columns
                .OrderBy(x => x.Name == table.PrimaryKey)
                .ToList();

            // Добавляем заголовки
            for (int i = 0; i < sortedColumns.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = sortedColumns[i].Name;
            }

            // Добавляем данные
            for (int rowIndex = 0; rowIndex < table.Data.Count; rowIndex++)
            {
                var record = table.Data[rowIndex];
                for (int colIndex = 0; colIndex < sortedColumns.Count; colIndex++)
                {
                    var column = sortedColumns[colIndex];
                    var cellValue = record[column.Name];

                    // Преобразуем значение к подходящему типу для Excel
                    worksheet.Cell(rowIndex + 2, colIndex + 1).Value = XLCellValue.FromObject(cellValue);
                }
            }
        }

        workbook.SaveAs(filePath);
    }


    public static Database LoadFromXlsx(string filePath)
    {
        var database = new Database();
        using var workbook = new XLWorkbook(filePath);

        foreach (var worksheet in workbook.Worksheets)
        {
            var columns = worksheet.Row(1).Cells().Select(c => new Column(c.GetValue<string>(), "string")).ToList();
            var table = new Table(worksheet.Name, columns, columns.First().Name); // Первый столбец - PrimaryKey

            foreach (var row in worksheet.RowsUsed().Skip(1)) // Пропускаем заголовок
            {
                var record = new Dictionary<string, object>();
                for (int i = 0; i < columns.Count; i++)
                {
                    record[columns[i].Name] = row.Cell(i + 1).Value;
                }
                table.AddRecord(record);
            }

            database.Tables.Add(table);
        }

        return database;
    }
}