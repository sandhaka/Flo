using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribePipelineBuilderFork
{
    [Fact]
    async ValueTask it_ignores_the_handler_if_the_predicate_returns_false()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Fork(ctx => ctx.ContainsKey("Item2"),
                    builder => builder.Add((ctx, next) =>
                    {
                        ctx.Add("Item3", "Item3Value");
                        return next.Invoke(ctx);
                    })
                )
                .Final(ctx =>
                {
                    ctx.Add("Item4", "Item4Value");
                    return ValueTask.FromResult(ctx);
                })
        );

        var context = new TestContext();
        await pipeline.Invoke(context);

        Assert.Equal(2, context.Count);
        Assert.DoesNotContain("Item3", context.Keys);
        Assert.Contains("Item4", context.Keys);
    }

    [Fact]
    async ValueTask it_executes_the_handler_and_does_not_continue_to_parent_if_the_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Fork(ctx => ctx.ContainsKey("Item1"),
                    builder => builder.Final(ctx =>
                    {
                        ctx.Add("Item2", "Item2Value");
                        return ValueTask.FromResult(ctx);
                    })
                )
                .Add((ctx, next) =>
                {
                    ctx.Add("Item3", "Item3Value");
                    return next.Invoke(ctx);
                })
        );

        var context = new TestContext();
        await pipeline.Invoke(context);

        Assert.Equal(2, context.Count);
        Assert.Contains("Item1", context.Keys);
        Assert.Contains("Item2", context.Keys);
        Assert.DoesNotContain("Item3", context.Keys);
    }
}