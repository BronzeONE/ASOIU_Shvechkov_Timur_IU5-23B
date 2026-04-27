using System.Globalization;
using System.Text;

namespace Homework2Variant26;

/// <summary>
/// Точка входа в консольное приложение для варианта 26.
/// </summary>
internal static class Program
{
    private const string DataDirectoryName = "data";
    private const string ReportsDirectoryName = "reports";

    /// <summary>
    /// Запускает программу.
    /// </summary>
    private static void Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        string projectDirectory = ResolveProjectDirectory();
        string dataDirectory = Path.Combine(projectDirectory, DataDirectoryName);
        string reportsDirectory = Path.Combine(projectDirectory, ReportsDirectoryName);
        string schoolsCsvPath = Path.Combine(dataDirectory, "schools.csv");
        string studentsCsvPath = Path.Combine(dataDirectory, "students.csv");
        string databasePath = Path.Combine(dataDirectory, "schools_students.db");

        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(reportsDirectory);

        var db = new DatabaseManager(databasePath);

        if (db.IsDatabaseEmpty())
        {
            db.ImportFromCsv(schoolsCsvPath, studentsCsvPath);
        }

        RunMenu(db, reportsDirectory, schoolsCsvPath, studentsCsvPath);
    }

    private static void RunMenu(
        DatabaseManager db,
        string reportsDirectory,
        string schoolsCsvPath,
        string studentsCsvPath)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== Управление школами и учениками ===");
            Console.WriteLine("1. Показать школы");
            Console.WriteLine("2. Показать учеников");
            Console.WriteLine("3. Добавить ученика");
            Console.WriteLine("4. Изменить ученика");
            Console.WriteLine("5. Удалить ученика");
            Console.WriteLine("6. Фильтр по школе");
            Console.WriteLine("7. Отчёты");
            Console.WriteLine("8. Повторно импортировать данные из CSV");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите пункт: ");

            string? choice = Console.ReadLine();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        PrintSchools(db);
                        break;
                    case "2":
                        PrintStudents(db.GetAllStudentsWithSchoolNames());
                        break;
                    case "3":
                        AddStudent(db);
                        break;
                    case "4":
                        EditStudent(db);
                        break;
                    case "5":
                        DeleteStudent(db);
                        break;
                    case "6":
                        FilterBySchool(db);
                        break;
                    case "7":
                        ShowReportsMenu(db, reportsDirectory);
                        break;
                    case "8":
                        db.ImportFromCsv(schoolsCsvPath, studentsCsvPath);
                        Console.WriteLine("Данные заново загружены из CSV.");
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Неизвестный пункт меню.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    private static void PrintSchools(DatabaseManager db)
    {
        List<School> schools = db.GetAllSchools();

        if (schools.Count == 0)
        {
            Console.WriteLine("Список школ пуст.");
            return;
        }

        Console.WriteLine("=== Школы ===");
        Console.WriteLine("ID   Название");
        Console.WriteLine(new string('─', 60));

        foreach (School school in schools)
        {
            Console.WriteLine($"{school.Id,-4} {school.Name}");
        }
    }

    private static void PrintStudents(List<StudentView> students)
    {
        if (students.Count == 0)
        {
            Console.WriteLine("Список учеников пуст.");
            return;
        }

        Console.WriteLine("=== Ученики ===");
        Console.WriteLine("ID   Имя                      Школа                          Средний балл");
        Console.WriteLine(new string('─', 92));

        foreach (StudentView student in students)
        {
            Console.WriteLine(
                $"{student.Id,-4} {student.Name,-24} {student.SchoolName,-30} {student.AvgGrade.ToString("0.00", CultureInfo.InvariantCulture)}");
        }
    }

    private static void AddStudent(DatabaseManager db)
    {
        Console.WriteLine("Добавление ученика");
        PrintSchools(db);

        int schoolId = ReadInt("ID школы: ");
        EnsureSchoolExists(db, schoolId);

        string name = ReadRequiredString("Имя ученика: ");
        double avgGrade = ReadDouble("Средний балл: ");

        var student = new Student(0, schoolId, name, avgGrade);
        db.AddStudent(student);

        Console.WriteLine($"Ученик добавлен с ID = {student.Id}.");
    }

    private static void EditStudent(DatabaseManager db)
    {
        PrintStudents(db.GetAllStudentsWithSchoolNames());

        int studentId = ReadInt("ID ученика для изменения: ");
        Student? existingStudent = db.GetStudentById(studentId);

        if (existingStudent is null)
        {
            Console.WriteLine("Ученик с таким ID не найден.");
            return;
        }

        Console.WriteLine("Нажмите Enter, чтобы оставить значение без изменений.");
        Console.WriteLine($"Текущее имя: {existingStudent.Name}");
        string? nameInput = ReadOptionalString("Новое имя: ");

        PrintSchools(db);
        Console.WriteLine($"Текущая школа ID: {existingStudent.SchoolId}");
        string? schoolIdInput = ReadOptionalString("Новый ID школы: ");

        Console.WriteLine($"Текущий средний балл: {existingStudent.AvgGrade.ToString("0.00", CultureInfo.InvariantCulture)}");
        string? avgGradeInput = ReadOptionalString("Новый средний балл: ");

        if (!string.IsNullOrWhiteSpace(nameInput))
        {
            existingStudent.Name = nameInput.Trim();
        }

        if (!string.IsNullOrWhiteSpace(schoolIdInput))
        {
            if (!int.TryParse(schoolIdInput, out int schoolId))
            {
                throw new ArgumentException("ID школы должен быть целым числом.");
            }

            EnsureSchoolExists(db, schoolId);
            existingStudent.SchoolId = schoolId;
        }

        if (!string.IsNullOrWhiteSpace(avgGradeInput))
        {
            existingStudent.AvgGrade = ParseDouble(avgGradeInput);
        }

        db.UpdateStudent(existingStudent);
        Console.WriteLine("Данные ученика обновлены.");
    }

    private static void DeleteStudent(DatabaseManager db)
    {
        PrintStudents(db.GetAllStudentsWithSchoolNames());

        int studentId = ReadInt("ID ученика для удаления: ");

        if (db.DeleteStudent(studentId))
        {
            Console.WriteLine("Ученик удалён.");
        }
        else
        {
            Console.WriteLine("Ученик с таким ID не найден.");
        }
    }

    private static void FilterBySchool(DatabaseManager db)
    {
        PrintSchools(db);
        int schoolId = ReadInt("ID школы для фильтрации: ");
        EnsureSchoolExists(db, schoolId);

        List<StudentView> students = db.GetStudentsBySchoolWithNames(schoolId);
        PrintStudents(students);
    }

    private static void ShowReportsMenu(DatabaseManager db, string reportsDirectory)
    {
        while (true)
        {
            Console.WriteLine("=== Отчёты ===");
            Console.WriteLine("1. Полный список учеников");
            Console.WriteLine("2. Количество учеников по школам");
            Console.WriteLine("3. Средний балл по школам");
            Console.WriteLine("4. Сохранить отчёт 1 в файл");
            Console.WriteLine("5. Сохранить отчёт 2 в файл");
            Console.WriteLine("6. Сохранить отчёт 3 в файл");
            Console.WriteLine("0. Назад");
            Console.Write("Выберите пункт: ");

            string? choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    CreateReport(db, 1).Print();
                    break;
                case "2":
                    CreateReport(db, 2).Print();
                    break;
                case "3":
                    CreateReport(db, 3).Print();
                    break;
                case "4":
                    SaveReport(db, 1, reportsDirectory, "report_full_list.txt");
                    break;
                case "5":
                    SaveReport(db, 2, reportsDirectory, "report_counts.txt");
                    break;
                case "6":
                    SaveReport(db, 3, reportsDirectory, "report_average_grade.txt");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Неизвестный пункт меню.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void SaveReport(
        DatabaseManager db,
        int reportNumber,
        string reportsDirectory,
        string fileName)
    {
        string reportPath = Path.Combine(reportsDirectory, fileName);
        CreateReport(db, reportNumber).SaveToFile(reportPath);
        Console.WriteLine($"Отчёт сохранён в файл: {reportPath}");
    }

    private static ReportBuilder CreateReport(DatabaseManager db, int reportNumber)
    {
        switch (reportNumber)
        {
            case 1:
                return new ReportBuilder(db)
                    .Query(
                        """
                        SELECT st.student_name, sc.school_name, st.avg_grade
                        FROM student st
                        JOIN school sc ON st.school_id = sc.school_id
                        ORDER BY st.student_name
                        """)
                    .Title("Полный список учеников")
                    .Header("Имя ученика", "Школа", "Средний балл")
                    .ColumnWidths(5, 26, 30, 16)
                    .Numbered()
                    .Footer("Всего записей");
            case 2:
                return new ReportBuilder(db)
                    .Query(
                        """
                        SELECT sc.school_name, COUNT(st.student_id) AS student_count
                        FROM school sc
                        LEFT JOIN student st ON st.school_id = sc.school_id
                        GROUP BY sc.school_id, sc.school_name
                        ORDER BY student_count DESC, sc.school_name
                        """)
                    .Title("Количество учеников по школам")
                    .Header("Школа", "Количество учеников")
                    .ColumnWidths(30, 22)
                    .Footer("Всего строк");
            case 3:
                return new ReportBuilder(db)
                    .Query(
                        """
                        SELECT sc.school_name, AVG(st.avg_grade) AS avg_grade
                        FROM school sc
                        LEFT JOIN student st ON st.school_id = sc.school_id
                        GROUP BY sc.school_id, sc.school_name
                        HAVING COUNT(st.student_id) > 0
                        ORDER BY avg_grade DESC, sc.school_name
                        """)
                    .Title("Средний балл по школам")
                    .Header("Школа", "Средний балл")
                    .ColumnWidths(30, 18)
                    .Footer("Всего строк");
            default:
                throw new ArgumentException("Неизвестный номер отчёта.");
        }
    }

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int value))
            {
                return value;
            }

            Console.WriteLine("Введите целое число.");
        }
    }

    private static double ReadDouble(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            try
            {
                return ParseDouble(input);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private static string ReadRequiredString(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine("Строка не должна быть пустой.");
        }
    }

    private static string? ReadOptionalString(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }

    private static double ParseDouble(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Введите число.");
        }

        string normalized = input.Trim().Replace(',', '.');

        if (double.TryParse(
                normalized,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out double value))
        {
            return value;
        }

        throw new ArgumentException("Введите корректное число.");
    }

    private static void EnsureSchoolExists(DatabaseManager db, int schoolId)
    {
        if (db.GetSchoolById(schoolId) is null)
        {
            throw new ArgumentException("Школа с таким ID не найдена.");
        }
    }

    private static string ResolveProjectDirectory()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string? projectDirectory = TryFindProjectDirectory(currentDirectory);

        if (projectDirectory is not null)
        {
            return projectDirectory;
        }

        string baseDirectory = AppContext.BaseDirectory;
        projectDirectory = TryFindProjectDirectory(baseDirectory);

        return projectDirectory ?? currentDirectory;
    }

    private static string? TryFindProjectDirectory(string startDirectory)
    {
        DirectoryInfo? current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            string[] projects = Directory.GetFiles(current.FullName, "*.csproj");
            if (projects.Length > 0)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
