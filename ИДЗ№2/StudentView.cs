namespace Homework2Variant26;

/// <summary>
/// Представление ученика вместе с названием школы для вывода.
/// </summary>
public class StudentView
{
    /// <summary>
    /// Идентификатор ученика.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор школы.
    /// </summary>
    public int SchoolId { get; set; }

    /// <summary>
    /// Имя ученика.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Название школы.
    /// </summary>
    public string SchoolName { get; set; }

    /// <summary>
    /// Средний балл.
    /// </summary>
    public double AvgGrade { get; set; }

    /// <summary>
    /// Конструктор с параметрами.
    /// </summary>
    public StudentView(int id, int schoolId, string name, string schoolName, double avgGrade)
    {
        Id = id;
        SchoolId = schoolId;
        Name = name;
        SchoolName = schoolName;
        AvgGrade = avgGrade;
    }

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    public StudentView()
        : this(0, 0, string.Empty, string.Empty, 0)
    {
    }
}
