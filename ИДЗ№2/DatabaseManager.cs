using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Homework2Variant26;

/// <summary>
/// Одна строка CSV-файла.
/// </summary>
internal record CsvRow(string[] Fields);

/// <summary>
/// CSV-таблица: заголовки и список строк.
/// </summary>
internal record CsvTable(string[] Headers, List<CsvRow> Rows);

/// <summary>
/// Инкапсулирует работу с SQLite для школ и учеников.
/// </summary>
public class DatabaseManager
{
    private readonly string _connectionString;

    /// <summary>
    /// Создаёт менеджер базы данных и таблицы при необходимости.
    /// </summary>
    public DatabaseManager(string databasePath)
    {
        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath}";
        CreateTables();
    }

    /// <summary>
    /// Возвращает признак пустой базы данных.
    /// </summary>
    public bool IsDatabaseEmpty()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM school";

        long schoolsCount = (long)(command.ExecuteScalar() ?? 0L);
        return schoolsCount == 0;
    }

    /// <summary>
    /// Загружает таблицы из CSV-файлов.
    /// </summary>
    public void ImportFromCsv(string schoolsCsvPath, string studentsCsvPath)
    {
        CsvTable schoolsTable = LoadCsv(schoolsCsvPath);
        CsvTable studentsTable = LoadCsv(studentsCsvPath);

        using SqliteConnection connection = OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        using (SqliteCommand clearStudents = connection.CreateCommand())
        {
            clearStudents.Transaction = transaction;
            clearStudents.CommandText = "DELETE FROM student";
            clearStudents.ExecuteNonQuery();
        }

        using (SqliteCommand clearSchools = connection.CreateCommand())
        {
            clearSchools.Transaction = transaction;
            clearSchools.CommandText = "DELETE FROM school";
            clearSchools.ExecuteNonQuery();
        }

        foreach (CsvRow row in schoolsTable.Rows)
        {
            using SqliteCommand insertSchool = connection.CreateCommand();
            insertSchool.Transaction = transaction;
            insertSchool.CommandText =
                """
                INSERT INTO school (school_id, school_name)
                VALUES (@id, @name)
                """;
            insertSchool.Parameters.AddWithValue("@id", ParseInt(row.Fields[0], "school_id"));
            insertSchool.Parameters.AddWithValue("@name", row.Fields[1]);
            insertSchool.ExecuteNonQuery();
        }

        foreach (CsvRow row in studentsTable.Rows)
        {
            using SqliteCommand insertStudent = connection.CreateCommand();
            insertStudent.Transaction = transaction;
            insertStudent.CommandText =
                """
                INSERT INTO student (student_id, school_id, student_name, avg_grade)
                VALUES (@id, @schoolId, @name, @avgGrade)
                """;
            insertStudent.Parameters.AddWithValue("@id", ParseInt(row.Fields[0], "student_id"));
            insertStudent.Parameters.AddWithValue("@schoolId", ParseInt(row.Fields[1], "school_id"));
            insertStudent.Parameters.AddWithValue("@name", row.Fields[2]);
            insertStudent.Parameters.AddWithValue("@avgGrade", ParseDouble(row.Fields[3], "avg_grade"));
            insertStudent.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Возвращает все школы.
    /// </summary>
    public List<School> GetAllSchools()
    {
        var result = new List<School>();

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT school_id, school_name
            FROM school
            ORDER BY school_name
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new School(
                reader.GetInt32(0),
                reader.GetString(1)));
        }

        return result;
    }

    /// <summary>
    /// Возвращает все записи таблицы учеников.
    /// </summary>
    public List<Student> GetAllStudents()
    {
        var result = new List<Student>();

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT student_id, school_id, student_name, avg_grade
            FROM student
            ORDER BY student_name
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Student(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDouble(3)));
        }

        return result;
    }

    /// <summary>
    /// Возвращает всех учеников вместе с названиями школ.
    /// </summary>
    public List<StudentView> GetAllStudentsWithSchoolNames()
    {
        return GetStudentsBySchoolWithNames(null);
    }

    /// <summary>
    /// Возвращает учеников указанной школы вместе с названием школы.
    /// </summary>
    public List<StudentView> GetStudentsBySchoolWithNames(int? schoolId)
    {
        var result = new List<StudentView>();

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();

        if (schoolId.HasValue)
        {
            command.CommandText =
                """
                SELECT st.student_id, st.school_id, st.student_name, sc.school_name, st.avg_grade
                FROM student st
                JOIN school sc ON st.school_id = sc.school_id
                WHERE st.school_id = @schoolId
                ORDER BY st.student_name
                """;
            command.Parameters.AddWithValue("@schoolId", schoolId.Value);
        }
        else
        {
            command.CommandText =
                """
                SELECT st.student_id, st.school_id, st.student_name, sc.school_name, st.avg_grade
                FROM student st
                JOIN school sc ON st.school_id = sc.school_id
                ORDER BY st.student_name
                """;
        }

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new StudentView(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDouble(4)));
        }

        return result;
    }

    /// <summary>
    /// Возвращает школу по идентификатору или null.
    /// </summary>
    public School? GetSchoolById(int id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT school_id, school_name
            FROM school
            WHERE school_id = @id
            """;
        command.Parameters.AddWithValue("@id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new School(
                reader.GetInt32(0),
                reader.GetString(1));
        }

        return null;
    }

    /// <summary>
    /// Возвращает ученика по идентификатору или null.
    /// </summary>
    public Student? GetStudentById(int id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT student_id, school_id, student_name, avg_grade
            FROM student
            WHERE student_id = @id
            """;
        command.Parameters.AddWithValue("@id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Student(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDouble(3));
        }

        return null;
    }

    /// <summary>
    /// Добавляет нового ученика.
    /// </summary>
    public void AddStudent(Student student)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO student (school_id, student_name, avg_grade)
            VALUES (@schoolId, @name, @avgGrade);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@schoolId", student.SchoolId);
        command.Parameters.AddWithValue("@name", student.Name);
        command.Parameters.AddWithValue("@avgGrade", student.AvgGrade);

        long newId = (long)(command.ExecuteScalar() ?? 0L);
        student.Id = (int)newId;
    }

    /// <summary>
    /// Обновляет существующего ученика по идентификатору.
    /// </summary>
    public void UpdateStudent(Student student)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE student
            SET school_id = @schoolId,
                student_name = @name,
                avg_grade = @avgGrade
            WHERE student_id = @id
            """;
        command.Parameters.AddWithValue("@id", student.Id);
        command.Parameters.AddWithValue("@schoolId", student.SchoolId);
        command.Parameters.AddWithValue("@name", student.Name);
        command.Parameters.AddWithValue("@avgGrade", student.AvgGrade);

        int affectedRows = command.ExecuteNonQuery();
        if (affectedRows == 0)
        {
            throw new InvalidOperationException("Не удалось обновить ученика: запись не найдена.");
        }
    }

    /// <summary>
    /// Удаляет ученика по идентификатору.
    /// </summary>
    public bool DeleteStudent(int id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM student
            WHERE student_id = @id
            """;
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Выполняет SQL-запрос и возвращает набор строк для отчёта.
    /// </summary>
    public List<string[]> ExecuteQuery(string sql)
    {
        var result = new List<string[]>();

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string[] fields = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                fields[i] = ConvertValueToString(reader.GetValue(i));
            }

            result.Add(fields);
        }

        return result;
    }

    private void CreateTables()
    {
        using SqliteConnection connection = OpenConnection();

        using (SqliteCommand createSchools = connection.CreateCommand())
        {
            createSchools.CommandText =
                """
                CREATE TABLE IF NOT EXISTS school
                (
                    school_id   INTEGER PRIMARY KEY,
                    school_name TEXT NOT NULL UNIQUE
                )
                """;
            createSchools.ExecuteNonQuery();
        }

        using (SqliteCommand createStudents = connection.CreateCommand())
        {
            createStudents.CommandText =
                """
                CREATE TABLE IF NOT EXISTS student
                (
                    student_id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    school_id    INTEGER NOT NULL,
                    student_name TEXT NOT NULL,
                    avg_grade    REAL NOT NULL CHECK(avg_grade >= 0),
                    FOREIGN KEY (school_id) REFERENCES school(school_id)
                )
                """;
            createStudents.ExecuteNonQuery();
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static CsvTable LoadCsv(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"CSV-файл не найден: {path}");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidOperationException($"CSV-файл пуст: {path}");
        }

        string[] headers = SplitCsvLine(lines[0]);
        var rows = new List<CsvRow>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] fields = SplitCsvLine(lines[i]);
            rows.Add(new CsvRow(fields));
        }

        return new CsvTable(headers, rows);
    }

    private static string[] SplitCsvLine(string line)
    {
        string[] fields = line.Split(';');

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].Trim();
        }

        return fields;
    }

    private static int ParseInt(string value, string fieldName)
    {
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        throw new FormatException($"Поле {fieldName} должно содержать целое число.");
    }

    private static double ParseDouble(string value, string fieldName)
    {
        string normalized = value.Replace(',', '.');

        if (double.TryParse(
                normalized,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out double result))
        {
            return result;
        }

        throw new FormatException($"Поле {fieldName} должно содержать число.");
    }

    private static string ConvertValueToString(object value)
    {
        if (value == DBNull.Value)
        {
            return string.Empty;
        }

        if (value is double doubleValue)
        {
            return doubleValue.ToString("0.00", CultureInfo.InvariantCulture);
        }

        if (value is float floatValue)
        {
            return floatValue.ToString("0.00", CultureInfo.InvariantCulture);
        }

        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("0.00", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
