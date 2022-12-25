using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribeOutputPipelineBuilder
{
    [Fact]
    async ValueTask it_can_execute_single_handler()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Add((input, _) => {
                return ValueTask.FromResult(input.Length);
            })
        );

        var result = await pipeline.Invoke("hello world");
        Assert.Equal(11, result);
    }

    [Fact]
    async ValueTask it_can_execute_multiple_handlers()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Add((input, next) => {
                    input += "hello";
                    return next.Invoke(input);
                })
                .Add((input, next) => {
                    input += " world";
                    return next.Invoke(input);
                })
                .Add((input, _) => ValueTask.FromResult(input.Length))
        );

        var result = await pipeline.Invoke("");
        Assert.Equal(11, result);
    }

    [Fact]
    async ValueTask it_returns_default_output_value_when_final_handler_not_specified()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Add((input, next) => next.Invoke(input))
        );

        var result = await pipeline.Invoke("hello world");
        Assert.Equal(default, result);
    }

    [Fact]
    async ValueTask it_ignores_subsequent_handlers_when_final_is_used()
    {
        bool nextExecuted = false;
            
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Final(input => ValueTask.FromResult(input.Length))
                .Add((_, _) => {
                    nextExecuted = true;
                    return ValueTask.FromResult(1);
                })
        );

        await pipeline.Invoke("hello world");
        Assert.False(nextExecuted);
    }
}