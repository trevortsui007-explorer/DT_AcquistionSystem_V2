namespace DT.DAS.Application.Data.Contracts;

/// <summary>
/// Request for high-throughput bulk import.
/// </summary>
public sealed class BulkImportRequest
{
    /// <summary>Destination table name.</summary>
    public string? TableName { get; set; }

    /// <summary>Rows to import.</summary>
    public IEnumerable<IDictionary<string, object?>>? Data { get; set; }

    /// <summary>Optional business flag passed to post-processing procedure.</summary>
    public string? Flag { get; set; }

    /// <summary>Optional stored procedure executed after import.</summary>
    public string? PostProcessSproc { get; set; }
}

/// <summary>
/// Request for standalone post-processing stored procedure execution.
/// </summary>
public sealed class PostProcessRequest
{
    /// <summary>Optional business flag.</summary>
    public string? Flag { get; set; }

    /// <summary>Stored procedure name.</summary>
    public string? Sproc { get; set; }
}

/// <summary>
/// Request for creating a destination table when missing.
/// </summary>
public sealed class CreateTableRequest
{
    /// <summary>Table name to create.</summary>
    public string? TableName { get; set; }

    /// <summary>Column definitions.</summary>
    public List<ApiColumnDefinition>? Columns { get; set; }
}

/// <summary>
/// API-facing column definition.
/// </summary>
public sealed class ApiColumnDefinition
{
    /// <summary>Column name.</summary>
    public string? ColumnName { get; set; }

    /// <summary>Type name such as string, int, datetime, bool, or guid.</summary>
    public string? DataType { get; set; }

    /// <summary>Whether the column is an identity primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Whether the column allows null values.</summary>
    public bool AllowNull { get; set; } = true;

    /// <summary>Optional string max length.</summary>
    public int? MaxLength { get; set; }
}

/// <summary>
/// Table column metadata returned by the schema endpoint.
/// </summary>
public sealed class TableColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool AllowDBNull { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int MaxLength { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Result returned after bulk import.
/// </summary>
public sealed class BulkImportResult
{
    public int RowCount { get; set; }
    public string? TableName { get; set; }
}
