using System.Globalization;
using System.Text;

namespace Homework2Variant26;

/// <summary>
/// Построитель отчётов с использованием Fluent Interface.
/// </summary>
public class ReportBuilder
{
    private readonly DatabaseManager _db;
    private string _sql = string.Empty;
    private string _title = string.Empty;
    private string[] _headers = Array.Empty<string>();
    private int[] _widths = Array.Empty<int>();
    private bool _numbered;
    private string _footer = string.Empty;

    /// <summary>
    /// Создаёт построитель отчётов.
    /// </summary>
    public ReportBuilder(DatabaseManager db)
    {
        _db = db;
    }

    /// <summary>
    /// Устанавливает SQL-запрос.
    /// </summary>
    public ReportBuilder Query(string sql)
    {
        _sql = sql;
        return this;
    }

    /// <summary>
    /// Устанавливает заголовок отчёта.
    /// </summary>
    public ReportBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Устанавливает названия колонок.
    /// </summary>
    public ReportBuilder Header(params string[] columns)
    {
        _headers = columns;
        return this;
    }

    /// <summary>
    /// Устанавливает ширины колонок.
    /// </summary>
    public ReportBuilder ColumnWidths(params int[] widths)
    {
        _widths = widths;
        return this;
    }

    /// <summary>
    /// Включает нумерацию строк.
    /// </summary>
    public ReportBuilder Numbered()
    {
        _numbered = true;
        return this;
    }

    /// <summary>
    /// Добавляет итоговую строку в конце отчёта.
    /// </summary>
    public ReportBuilder Footer(string label)
    {
        _footer = label;
        return this;
    }

    /// <summary>
    /// Строит текст отчёта.
    /// </summary>
    public string Build()
    {
        if (string.IsNullOrWhiteSpace(_sql))
        {
            throw new InvalidOperationException("Для отчёта не задан SQL-запрос.");
        }

        List<string[]> rows = _db.ExecuteQuery(_sql);
        int[] widths = ResolveWidths(rows);
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_title))
        {
            sb.AppendLine($"=== {_title} ===");
        }

        if (_headers.Length > 0)
        {
            AppendHeader(sb, widths);
            sb.AppendLine(new string('─', Sum(widths)));
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            AppendRow(sb, rows[rowIndex], rowIndex + 1, widths);
        }

        if (!string.IsNullOrWhiteSpace(_footer))
        {
            if (rows.Count > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(BuildFooterText(rows.Count));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Выводит отчёт в консоль.
    /// </summary>
    public void Print()
    {
        Console.WriteLine(Build());
    }

    /// <summary>
    /// Сохраняет отчёт в текстовый файл.
    /// </summary>
    public void SaveToFile(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, Build(), Encoding.UTF8);
    }

    private void AppendHeader(StringBuilder sb, int[] widths)
    {
        int headerIndex = 0;

        if (_numbered)
        {
            AppendCell(sb, "№", widths[0]);
            headerIndex = 1;
        }

        for (int i = 0; i < _headers.Length; i++)
        {
            AppendCell(sb, _headers[i], widths[i + headerIndex]);
        }

        sb.AppendLine();
    }

    private void AppendRow(StringBuilder sb, string[] row, int rowNumber, int[] widths)
    {
        int rowIndex = 0;

        if (_numbered)
        {
            AppendCell(sb, rowNumber.ToString(CultureInfo.InvariantCulture), widths[0]);
            rowIndex = 1;
        }

        for (int i = 0; i < row.Length; i++)
        {
            AppendCell(sb, row[i], widths[i + rowIndex]);
        }

        sb.AppendLine();
    }

    private static void AppendCell(StringBuilder sb, string value, int width)
    {
        sb.Append(value.PadRight(width));
    }

    private int[] ResolveWidths(List<string[]> rows)
    {
        int dataColumnCount = 0;

        if (_headers.Length > 0)
        {
            dataColumnCount = _headers.Length;
        }
        else if (rows.Count > 0)
        {
            dataColumnCount = rows[0].Length;
        }

        int totalColumnCount = dataColumnCount + (_numbered ? 1 : 0);

        if (_widths.Length == totalColumnCount)
        {
            return CloneArray(_widths);
        }

        if (_numbered && _widths.Length == dataColumnCount)
        {
            int[] adjusted = new int[totalColumnCount];
            adjusted[0] = 5;

            for (int i = 0; i < _widths.Length; i++)
            {
                adjusted[i + 1] = _widths[i];
            }

            return adjusted;
        }

        if (_widths.Length > 0 && _widths.Length != totalColumnCount)
        {
            throw new InvalidOperationException("Количество ширин колонок не совпадает с количеством столбцов.");
        }

        return BuildAutomaticWidths(rows, dataColumnCount, totalColumnCount);
    }

    private int[] BuildAutomaticWidths(List<string[]> rows, int dataColumnCount, int totalColumnCount)
    {
        int[] widths = new int[totalColumnCount];
        int offset = 0;

        if (_numbered)
        {
            widths[0] = 5;
            offset = 1;
        }

        for (int i = 0; i < dataColumnCount; i++)
        {
            int width = 12;

            if (_headers.Length > i)
            {
                width = Math.Max(width, _headers[i].Length + 2);
            }

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rows[rowIndex].Length > i)
                {
                    width = Math.Max(width, rows[rowIndex][i].Length + 2);
                }
            }

            widths[i + offset] = width;
        }

        return widths;
    }

    private string BuildFooterText(int rowCount)
    {
        if (_footer.Contains("{count}", StringComparison.OrdinalIgnoreCase))
        {
            return _footer.Replace(
                "{count}",
                rowCount.ToString(CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        return $"{_footer}: {rowCount.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int Sum(int[] values)
    {
        int result = 0;

        for (int i = 0; i < values.Length; i++)
        {
            result += values[i];
        }

        return result;
    }

    private static int[] CloneArray(int[] source)
    {
        int[] clone = new int[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            clone[i] = source[i];
        }

        return clone;
    }
}
