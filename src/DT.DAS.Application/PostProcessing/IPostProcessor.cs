using DT.DAS.Application.PostProcessing.Contracts;

namespace DT.DAS.Application.PostProcessing;

public interface IPostProcessor
{
    string ProcessorName { get; }
    Task ExecuteAsync(PostProcessingContext context, CancellationToken ct = default);
}

