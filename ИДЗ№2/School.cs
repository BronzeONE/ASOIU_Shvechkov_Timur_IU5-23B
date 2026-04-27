namespace Homework2Variant26;

/// <summary>
/// Школа (справочная таблица, сторона «один»).
/// </summary>
public class School
{
    /// <summary>
    /// Идентификатор школы.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название школы.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Конструктор с параметрами.
    /// </summary>
    public School(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    public School()
        : this(0, string.Empty)
    {
    }

    /// <summary>
    /// Возвращает строковое представление школы.
    /// </summary>
    public override string ToString()
    {
        return $"[{Id}] {Name}";
    }
}
