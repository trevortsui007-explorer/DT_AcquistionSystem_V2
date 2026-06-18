namespace DT.DAS.Application.PostProcessing;

public interface IPostProcessorResolver
{
    IPostProcessor Resolve(string processorName);
}
