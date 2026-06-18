using System.Text.Json;
using DT.DAS.Domain.Enums;

namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionConfig
{
    public int Id { get; set; }
    public string? EqName { get; set; }
    public string? TableName { get; set; }
    public string? FilePathPattern { get; set; }
    public string? FileNamePattern { get; set; }
    public string? FileType { get; set; }
    public int HeaderRow { get; set; } = 1;
    public int StartRow { get; set; } = 2;
    public string? FieldMappings { get; set; }
    public string? ExtFields { get; set; }
    public bool IsEnabled { get; set; } = true;
    public PostProcessingType PostProcessingType { get; set; }
    public string? PostTableName { get; set; }
    public string? ProcedureName { get; set; }
    public string? ServiceName { get; set; }
    public string? Flag { get; set; }
    public string? FlagName { get; set; }
    public DateTime CreateTime { get; set; }

    public Dictionary<string, string> ParseFieldMappings()
    {
        if (string.IsNullOrWhiteSpace(FieldMappings))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(FieldMappings)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}

