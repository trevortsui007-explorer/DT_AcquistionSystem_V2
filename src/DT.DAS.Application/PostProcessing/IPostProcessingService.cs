using DT.DAS.Application.PostProcessing.Contracts;

namespace DT.DAS.Application.PostProcessing;

public interface IPostProcessingService
{
    Task ProcessAsync(PostProcessingContext context, CancellationToken ct = default);
}

