using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribePipelineBuilder
{       
    [Fact]
    async ValueTask it_can_execute_single_handler()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, _) => {
                ctx.Add("SomeKey", "SomeValue");
                return ValueTask.FromResult(ctx);
            })
        );

        var context = new TestContext();
        context = await pipeline.Invoke(context);

        Assert.Equal("SomeValue", context["SomeKey"]);
    }

    [Fact]
    async ValueTask it_can_execute_multiple_handlers()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) => {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Add((ctx, _) => {
                    ctx.Add("Item2", "Item2Value");
                    return ValueTask.FromResult(ctx);
                })
        );

        var context = new TestContext();
        context = await pipeline.Invoke(context);

        Assert.Equal("Item1Value", context["Item1"]);
        Assert.Equal("Item2Value", context["Item2"]);
    }

    [Fact]
    async ValueTask it_returns_final_handler_result()
    {
        // Despite invoking next on final handler, the final result is returned
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) => {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Add((ctx, next) => {
                    ctx.Add("Item2", "Item2Value");
                    return next.Invoke(ctx);
                })
        );

        var context = new TestContext();
        context = await pipeline.Invoke(context);

        Assert.NotNull(context);
    }

    [Fact]
    async ValueTask it_ignores_subsequent_handlers_when_final_is_used()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) => {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Final(ctx => {
                    ctx.Add("Item2", "Item2Value");
                    return ValueTask.FromResult(ctx);
                })
                .Add((ctx, next) => {
                    ctx.Add("Item3", "Item3Value");
                    return next.Invoke(ctx);
                })
        );

        var context = new TestContext();
        await pipeline.Invoke(context);

        Assert.Equal(2, context.Count);
        Assert.DoesNotContain("Item3", context.Keys);
    }
}