using DT.DAS.Application.PostProcessing;

namespace DT.DAS.Application.PostProcessing.Services;

public sealed class PostProcessorResolver : IPostProcessorResolver
{
    private readonly IReadOnlyDictionary<string, IPostProcessor> _processors;

    public PostProcessorResolver(IEnumerable<IPostProcessor> processors)
    {
        _processors = processors.ToDictionary(x => x.ProcessorName, StringComparer.OrdinalIgnoreCase);
    }

    public IPostProcessor Resolve(string processorName)
    {
        if (_processors.TryGetValue(processorName, out var processor))
        {
            return processor;
        }

        throw new InvalidOperationException($"Post processor is not registered: {processorName}");
    }
}
