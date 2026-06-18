using System.Text.Json;

namespace DT.DAS.Application.Acquisition.Utilities;

public static class DataMapperUtil
{
    public static Dictionary<string, object?> MapRow(IDictionary<string, object?> row, string? fieldMappings)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var mappings = ParseMappings(fieldMappings);

        foreach (var item in row)
        {
            var targetName = mappings.TryGetValue(item.Key, out var mappedName) && !string.IsNullOrWhiteSpace(mappedName)
                ? mappedName
                : item.Key;

            result[targetName] = item.Value;
        }

        return result;
    }

    private static Dictionary<string, string> ParseMappings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();

        return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }
}


