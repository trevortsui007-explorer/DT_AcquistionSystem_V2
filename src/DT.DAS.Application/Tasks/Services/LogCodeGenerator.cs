using DT.DAS.Application.Tasks;
namespace DT.DAS.Application.Tasks.Services;

public sealed class LogCodeGenerator : ILogCodeGenerator
{
    public Task<string> GenerateTaskCodeAsync(string triggerType, CancellationToken ct = default)
    {
        var safeType = string.IsNullOrWhiteSpace(triggerType) ? TaskTriggerTypes.Manual : triggerType.Trim().ToUpperInvariant();
        return Task.FromResult($"{safeType}{DateTime.Now:yyyyMMddHHmmssfff}");
    }
}



