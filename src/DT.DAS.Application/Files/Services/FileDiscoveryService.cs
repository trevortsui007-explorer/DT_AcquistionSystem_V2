using DT.DAS.Application.Configs;
using DT.DAS.Application.Files.Contracts;
using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Files.Services;

public sealed class FileDiscoveryService : IFileDiscoveryService
{
    private readonly IFileProviderFactory _factory;
    private readonly IFileConfigService _configService;
    private readonly IAcquisitionFileStateService _fileStateService;

    public FileDiscoveryService(IFileProviderFactory factory, IFileConfigService configService, IAcquisitionFileStateService fileStateService)
    {
        _factory = factory;
        _configService = configService;
        _fileStateService = fileStateService;
    }

    public async Task<List<FileDiscoveryDto>> GetDetailedDiscoveryAsync(AcquisitionConfig config, DateTime startDate, DateTime endDate, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (endDate.Date < startDate.Date)
        {
            throw new InvalidOperationException("结束日期不能早于开始日期");
        }

        var result = new List<FileDiscoveryDto>();
        var fileStates = await _fileStateService.GetByConfigAndDateRangeAsync(config.Id, startDate.Date, endDate.Date, ct).ConfigureAwait(false);
        var stateMap = fileStates
            .GroupBy(x => BuildStateKey(x.BusinessDate, x.FileName))
            .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.UpdateTime).First());
        var folderFilesCache = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        for (var monthDate = new DateTime(startDate.Year, startDate.Month, 1); monthDate <= endDate.Date; monthDate = monthDate.AddMonths(1))
        {
            var month = new FileDiscoveryDto
            {
                MonthName = monthDate.ToString("yyyy-MM")
            };

            var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var currentDay = new DateTime(monthDate.Year, monthDate.Month, day);
                if (currentDay < startDate.Date || currentDay > endDate.Date)
                {
                    continue;
                }

                var folderPath = ApplyDateTokens(config.FilePathPattern ?? string.Empty, currentDay);
                month.FolderPath ??= folderPath;

                if (!folderFilesCache.TryGetValue(folderPath, out var existingFiles))
                {
                    existingFiles = await GetExistingFilesSafelyAsync(folderPath, user, pass, ct).ConfigureAwait(false);
                    folderFilesCache[folderPath] = existingFiles;
                }

                var extension = NormalizeExtension(config.FileType);
                var expectedName = NormalizeExpectedFileName(ApplyDateTokens(config.FileNamePattern ?? string.Empty, currentDay), extension);
                var matches = existingFiles
                    .Select(Path.GetFileName)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Where(x => HasExpectedExtension(x!, extension))
                    .Where(x => string.Equals(x, expectedName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matches.Length == 0)
                {
                    var entry = new FileEntryDto
                    {
                        FileName = expectedName,
                        FullPath = CombinePath(folderPath, expectedName),
                        DetectedDate = currentDay,
                        IsMissing = true
                    };
                    AttachFileState(entry, stateMap);
                    month.Files.Add(entry);
                    continue;
                }

                foreach (var fileName in matches)
                {
                    var entry = new FileEntryDto
                    {
                        FileName = fileName,
                        FullPath = CombinePath(folderPath, fileName!),
                        DetectedDate = currentDay,
                        IsMissing = false
                    };
                    AttachFileState(entry, stateMap);
                    month.Files.Add(entry);
                }
            }

            if (month.Files.Count > 0)
            {
                result.Add(month);
            }
        }

        return result;
    }

    public async Task<GroupFileDiscoveryDto> GetGroupDiscoveryAsync(int groupId, DateTime date, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        if (groupId <= 0)
        {
            throw new InvalidOperationException("配置组ID不能为空");
        }

        var configs = _configService.GetConfigsByGroupIds(new[] { groupId.ToString() }).ToArray();
        var tasks = configs.Select(async config =>
        {
            var discovery = await GetDetailedDiscoveryAsync(config, date.Date, date.Date, user, pass, ct).ConfigureAwait(false);
            var file = discovery.SelectMany(x => x.Files).FirstOrDefault(x => x.DetectedDate.Date == date.Date);
            return new GroupFileDiscoveryItemDto
            {
                ConfigId = config.Id,
                EqName = config.EqName,
                IsMissing = file?.IsMissing ?? true,
                FullFilePath = file?.FullPath ?? string.Empty
            };
        });

        return new GroupFileDiscoveryDto
        {
            GroupId = groupId,
            Date = date.Date,
            Items = (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList()
        };
    }

    private async Task<IEnumerable<string>> GetExistingFilesSafelyAsync(string folderPath, string? user, string? pass, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var provider = _factory.Create(folderPath, user, pass);
            return await provider.GetFileNamesAsync(folderPath, "*", false, ct).ConfigureAwait(false);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
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

    private static string CombinePath(string? folder, string? file)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return file ?? string.Empty;
        }

        return folder.TrimEnd('/', '\\') + "/" + (file ?? string.Empty).TrimStart('/', '\\');
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        extension = extension.Trim();
        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
    }

    private static string NormalizeExpectedFileName(string expectedFileName, string extension)
    {
        var expectedName = Path.GetFileName((expectedFileName ?? string.Empty).Replace("*", string.Empty, StringComparison.Ordinal));
        return !string.IsNullOrEmpty(extension) && !expectedName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? expectedName + extension
            : expectedName;
    }

    private static bool HasExpectedExtension(string fileName, string extension)
    {
        return string.IsNullOrEmpty(extension) || fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    private static void AttachFileState(FileEntryDto entry, IReadOnlyDictionary<string, AcquisitionFileState> stateMap)
    {
        if (!stateMap.TryGetValue(BuildStateKey(entry.DetectedDate, entry.FileName), out var state))
        {
            return;
        }

        entry.DataRowCount = state.DataRowCount;
        entry.LastStartRow = state.LastStartRow;
        entry.LastProcessedRows = state.LastProcessedRows;
        entry.LastStatus = state.LastStatus;
        entry.LastUpdateSource = state.LastUpdateSource;
        entry.IsSealed = state.IsSealed;
        entry.LastScanTime = state.LastScanTime;
        entry.FileStateUpdateTime = state.UpdateTime;
    }

    private static string BuildStateKey(DateTime businessDate, string? fileName)
    {
        return $"{businessDate:yyyyMMdd}|{(fileName ?? string.Empty).Trim().ToUpperInvariant()}";
    }
}

