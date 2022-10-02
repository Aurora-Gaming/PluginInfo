namespace PluginInfo;

/// <summary>
/// Source: http://stackoverflow.com/questions/856845/how-to-best-way-to-draw-table-in-console-app-c
/// </summary>
public static class TableHelper
{
	private static int tableWidth = 77;

	public static void PrintLine()
	{
		Console.WriteLine(new string('-', tableWidth));
	}

	public static void PrintRow(params string[] columns)
	{
		int width = (tableWidth - columns.Length) / columns.Length;
		string row = "|";

		foreach (string column in columns)
		{
			row += AlignCentre(column, width) + "|";
		}

		Console.WriteLine(row);
	}

	public static string AlignCentre(string text, int width)
	{
		text = text.Length > width ? text[..(width - 3)] + "..." : text;

		if (string.IsNullOrEmpty(text))
		{
			return new string(' ', width);
		}
		else
		{
			return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
		}
	}
}
