using System.Globalization;

namespace Homework2Variant26;

/// <summary>
/// Ученик (основная таблица, сторона «много»).
/// </summary>
public class Student
{
    private double _avgGrade;

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
    /// Средний балл успеваемости.
    /// </summary>
    public double AvgGrade
    {
        get => _avgGrade;
        set
        {
            if (value < 0)
            {
                throw new ArgumentException("Средний балл не может быть отрицательным.");
            }

            _avgGrade = value;
        }
    }

    /// <summary>
    /// Конструктор с параметрами.
    /// </summary>
    public Student(int id, int schoolId, string name, double avgGrade)
    {
        Id = id;
        SchoolId = schoolId;
        Name = name;
        AvgGrade = avgGrade;
    }

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    public Student()
        : this(0, 0, string.Empty, 0)
    {
    }

    /// <summary>
    /// Возвращает строковое представление ученика.
    /// </summary>
    public override string ToString()
    {
        return $"[{Id}] {Name}, школа #{SchoolId}, средний балл: {AvgGrade.ToString("0.00", CultureInfo.InvariantCulture)}";
    }
}
