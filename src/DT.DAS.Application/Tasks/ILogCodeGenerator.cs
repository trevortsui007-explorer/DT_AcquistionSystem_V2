namespace DT.DAS.Application.Tasks;

public interface ILogCodeGenerator
{
    Task<string> GenerateTaskCodeAsync(string triggerType, CancellationToken ct = default);
}

