namespace DT.DAS.WebApi.Modules.Configs;

internal static class ConfigQueryHelpers
{
    public static string[] SplitValues(IEnumerable<string>? values)
    {
        return values?
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    public static int[] SplitIntValues(IEnumerable<string>? values)
    {
        return SplitValues(values)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToArray();
    }
}
