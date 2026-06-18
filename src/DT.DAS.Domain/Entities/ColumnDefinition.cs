namespace DT.DAS.Domain.Entities;

public sealed class ColumnDefinition
{
    public string ColumnName { get; set; } = string.Empty;
    public Type DataType { get; set; } = typeof(string);
    public int? MaxLength { get; set; }
    public bool AllowNull { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
}

