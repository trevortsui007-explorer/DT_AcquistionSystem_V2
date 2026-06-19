using System.Text.RegularExpressions;

namespace DT.DAS.Infrastructure.Persistence;

public static partial class SqlIdentifier
{
    public static string Table(string? value, string fallback)
    {
        return QuoteMultipart(string.IsNullOrWhiteSpace(value) ? fallback : value);
    }

    public static string Column(string? value, string fallback)
    {
        return QuoteSingle(string.IsNullOrWhiteSpace(value) ? fallback : value);
    }

    public static string RawName(string? value, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        EnsureSafe(raw);
        return raw;
    }

    private static string QuoteMultipart(string value)
    {
        var raw = RawName(value, value);
        return string.Join('.', raw.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(QuoteSingle));
    }

    private static string QuoteSingle(string value)
    {
        var raw = value.Trim();
        EnsureSafe(raw);
        if (raw.Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException($"SQL column name cannot contain '.': {value}", nameof(value));
        }

        return $"[{raw}]";
    }

    private static void EnsureSafe(string value)
    {
        if (!SafeIdentifierRegex().IsMatch(value))
        {
            throw new ArgumentException($"Unsafe SQL identifier: {value}", nameof(value));
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_.]+$")]
    private static partial Regex SafeIdentifierRegex();
}
