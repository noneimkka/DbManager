namespace DbManager.Entities;

public class Column
{
	public string Name { get; set; }
	public string Type { get; set; }

	public Column(string name, string type)
	{
		Name = name;
		Type = type;
	}
}