using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Acquisition.Utilities;

public static class FileDateTimeUtil
{
    public static string GetProcessedFilePath(AcquisitionConfig config, DateTime processDate)
    {
        var root = ApplyDateTokens(config.FilePathPattern ?? string.Empty, processDate);
        var fileName = GetProcessedFileName(config, processDate);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return root;
        }

        if (root.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root.TrimEnd('/')}/{fileName}";
        }

        return Path.Combine(root, fileName);
    }

    public static string GetProcessedFileName(AcquisitionConfig config, DateTime processDate)
    {
        return ApplyDateTokens(config.FileNamePattern ?? string.Empty, processDate);
    }

    private static string ApplyDateTokens(string template, DateTime value)
    {
        return template
            .Replace("{yyyy}", value.ToString("yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{MM}", value.ToString("MM"), StringComparison.OrdinalIgnoreCase)
            .Replace("{M}", value.ToString("%M"), StringComparison.OrdinalIgnoreCase)
            .Replace("{dd}", value.ToString("dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{d}", value.ToString("%d"), StringComparison.OrdinalIgnoreCase);
    }
}


