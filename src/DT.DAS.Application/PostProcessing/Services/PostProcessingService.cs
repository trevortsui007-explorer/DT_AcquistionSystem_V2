using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Contracts;
using DT.DAS.Domain.Enums;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.PostProcessing.Services;

public sealed class PostProcessingService : IPostProcessingService
{
    private readonly IDataService _dataService;
    private readonly IPostProcessorResolver _processorResolver;

    public PostProcessingService(IDataService dataService, IPostProcessorResolver processorResolver)
    {
        _dataService = dataService;
        _processorResolver = processorResolver;
    }

    public async Task ProcessAsync(PostProcessingContext context, CancellationToken ct = default)
    {
        if (context.Config.PostProcessingType == PostProcessingType.None)
        {
            return;
        }

        if (context.Config.PostProcessingType == PostProcessingType.Procedure && !string.IsNullOrWhiteSpace(context.Config.ProcedureName))
        {
            await _dataService.ExecuteStoredProcedureAsync(context.Config.Flag, context.Config.ProcedureName, ct).ConfigureAwait(false);
            return;
        }

        if (context.Config.PostProcessingType == PostProcessingType.Service && !string.IsNullOrWhiteSpace(context.Config.ServiceName))
        {
            var processor = _processorResolver.Resolve(context.Config.ServiceName);
            await processor.ExecuteAsync(context, ct).ConfigureAwait(false);
        }
    }
}
