using System;
using System.Threading.Tasks;

namespace Flo;

public class PipelineBuilder<T> : Builder<T, T, PipelineBuilder<T>> // : Builder<T, Task<T>>
{
    public PipelineBuilder(Func<Type, object> serviceProvider = null)
        : base(input => ValueTask.FromResult(input), serviceProvider)
    {
        InnerHandler = input => ValueTask.FromResult(input);
    }

    public PipelineBuilder<T> When(
        Func<T, bool> predicate,
        Action<PipelineBuilder<T>> configurePipeline)
    {
        return When(predicate, async (input, innerPipeline, next) =>
            {
                input = await innerPipeline.Invoke(input);
                return await next.Invoke(input);
            },
            configurePipeline);
    }

    public PipelineBuilder<T> When(
        Func<T, ValueTask<bool>> predicate,
        Action<PipelineBuilder<T>> configurePipeline)
    {
        return When(predicate, async (input, innerPipeline, next) =>
            {
                input = await innerPipeline.Invoke(input);
                return await next.Invoke(input);
            },
            configurePipeline);
    }

    protected override PipelineBuilder<T> CreateBuilder() => new PipelineBuilder<T>(ServiceProvider);
}