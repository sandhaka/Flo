using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribePipelineBuilderWhen
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
                .When(ctx => ctx.ContainsKey("Item2"),
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
    }

    [Fact]
    async ValueTask it_ignores_the_handler_if_the_async_predicate_returns_false()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .When(ctx => ValueTask.FromResult(ctx.ContainsKey("Item2")),
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
    }

    [Fact]
    async ValueTask it_executes_the_handler_if_the_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Add((ctx, next) =>
                {
                    ctx.Add("Item2", "Item2Value");
                    return next.Invoke(ctx);
                })
                .When(ctx => ctx.ContainsKey("Item2"),
                    builder => builder.Final(ctx =>
                    {
                        ctx.Add("Item3", "Item3Value");
                        return ValueTask.FromResult(ctx);
                    })
                )
        );

        var context = new TestContext();
        await pipeline.Invoke(context);

        Assert.Equal(3, context.Count);
        Assert.Contains("Item3", context.Keys);
    }

    [Fact]
    async ValueTask it_executes_the_handler_if_the_async_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .Add((ctx, next) =>
                {
                    ctx.Add("Item2", "Item2Value");
                    return next.Invoke(ctx);
                })
                .When(ctx => ValueTask.FromResult(ctx.ContainsKey("Item2")),
                    builder => builder.Final(ctx =>
                    {
                        ctx.Add("Item3", "Item3Value");
                        return ValueTask.FromResult(ctx);
                    })
                )
        );

        var context = new TestContext();
        await pipeline.Invoke(context);

        Assert.Equal(3, context.Count);
        Assert.Contains("Item3", context.Keys);
    }

    [Fact]
    async ValueTask it_continues_to_parent_pipeline_after_child_pipeline_has_completed()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .When(ctx => ctx.Count == 1,
                    builder => builder
                        .Add((ctx, next) =>
                        {
                            ctx.Add("Item2", "Item2Value");
                            return next.Invoke(ctx);
                        })
                        .Add((ctx, _) =>
                        {
                            ctx.Add("Item3", "Item3Value");
                            return ValueTask.FromResult(ctx);
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

        Assert.Contains("Item1", context.Keys);
        Assert.Contains("Item2", context.Keys);
        Assert.Contains("Item3", context.Keys);
        Assert.Contains("Item4", context.Keys);
    }

    [Fact]
    async ValueTask it_continues_to_parent_pipeline_after_child_pipeline_has_completed_with_async_predicate()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add((ctx, next) =>
                {
                    ctx.Add("Item1", "Item1Value");
                    return next.Invoke(ctx);
                })
                .When(ctx => ValueTask.FromResult(ctx.Count == 1),
                    builder => builder
                        .Add((ctx, next) =>
                        {
                            ctx.Add("Item2", "Item2Value");
                            return next.Invoke(ctx);
                        })
                        .Add((ctx, _) =>
                        {
                            ctx.Add("Item3", "Item3Value");
                            return ValueTask.FromResult(ctx);
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

        Assert.Contains("Item1", context.Keys);
        Assert.Contains("Item2", context.Keys);
        Assert.Contains("Item3", context.Keys);
        Assert.Contains("Item4", context.Keys);
    }
}