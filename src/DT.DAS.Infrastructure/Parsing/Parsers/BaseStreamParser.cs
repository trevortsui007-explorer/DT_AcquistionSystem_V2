using System.Collections.Concurrent;
using System.Reflection;

namespace DT.DAS.Infrastructure.Parsing.Parsers;

public abstract class BaseStreamParser
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();

    protected static T MapToEntity<T>(string[] headers, object?[] values, int rowIndex, bool hasExt, string? filePath) where T : class, new()
    {
        if (typeof(T) == typeof(Dictionary<string, object?>))
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                dict[headers[i]] = values[i];
            }

            if (hasExt)
            {
                dict["fullFilePath"] = filePath;
                dict["row"] = rowIndex;
            }

            return (dict as T)!;
        }

        var entity = new T();
        var properties = PropertyCache.GetOrAdd(typeof(T), type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase));

        for (var i = 0; i < Math.Min(headers.Length, values.Length); i++)
        {
            if (properties.TryGetValue(headers[i], out var property))
            {
                property.SetValue(entity, ConvertValue(values[i], property.PropertyType));
            }
        }

        if (hasExt)
        {
            if (properties.TryGetValue("fullFilePath", out var fullFilePathProperty))
            {
                fullFilePathProperty.SetValue(entity, filePath);
            }

            if (properties.TryGetValue("row", out var rowProperty))
            {
                rowProperty.SetValue(entity, rowIndex);
            }
        }

        return entity;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actualType == typeof(Guid))
        {
            return Guid.Parse(Convert.ToString(value)!);
        }

        if (actualType.IsEnum)
        {
            return Enum.Parse(actualType, Convert.ToString(value)!, ignoreCase: true);
        }

        return actualType == typeof(string) ? Convert.ToString(value) : Convert.ChangeType(value, actualType);
    }
}

