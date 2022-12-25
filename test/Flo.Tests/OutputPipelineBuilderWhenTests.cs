using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribeOutputPipelineBuilderWhen
{
    [Fact]
    async ValueTask it_ignores_the_handler_if_the_predicate_returns_false()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.When(input => input == "hello world",
                builder => builder.Add((input, _) =>
                {
                    return ValueTask.FromResult(input.Length);
                })
            )
        );

        var result = await pipeline.Invoke("hello");
        Assert.Equal(0, result);
    }

    [Fact]
    async ValueTask it_ignores_the_handler_if_the_async_predicate_returns_false()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.When(input => ValueTask.FromResult(input == "hello world"),
                builder => builder.Add((input, _) =>
                {
                    return ValueTask.FromResult(input.Length);
                })
            )
        );

        var result = await pipeline.Invoke("hello");
        Assert.Equal(0, result);
    }

    [Fact]
    async ValueTask it_executes_the_handler_if_the_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.When(input => input == "hello world",
                builder => builder.Add((input, _) =>
                {
                    return ValueTask.FromResult(input.Length);
                })
            )
        );

        var result = await pipeline.Invoke("hello world");
        Assert.Equal(11, result);
    }

    [Fact]
    async ValueTask it_executes_the_handler_if_the_async_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.When(input => ValueTask.FromResult(input == "hello world"),
                builder => builder.Add((input, _) => ValueTask.FromResult(input.Length))
            )
        );

        var result = await pipeline.Invoke("hello world");
        Assert.Equal(11, result);
    }

    [Fact]
    async ValueTask it_continues_to_parent_pipeline_if_child_pipeline_returns_default_value()
    {
        var pipeline = Pipeline.Build<TestContext, TestContext>(cfg =>
            cfg.When(_ => true,
                    builder => builder.Add((_, _) => ValueTask.FromResult(default(TestContext)))
                )
                .Final(ctx => {
                    ctx.Add("Test", "TestValue");
                    return ValueTask.FromResult(ctx);
                })
        );

        var result = await pipeline.Invoke(new TestContext());
        Assert.Contains("Test", result.Keys);
    }

    [Fact]
    async ValueTask it_continues_to_parent_pipeline_if_child_pipeline_returns_default_value_with_async_predicate()
    {
        var pipeline = Pipeline.Build<TestContext, TestContext>(cfg =>
            cfg.When(_ => ValueTask.FromResult(true),
                    builder => builder.Add((_, _) => ValueTask.FromResult(default(TestContext)))
                )
                .Final(ctx => {
                    ctx.Add("Test", "TestValue");
                    return ValueTask.FromResult(ctx);
                })
        );

        var result = await pipeline.Invoke(new TestContext());
        Assert.Contains("Test", result.Keys);
    }

    [Fact]
    async ValueTask it_does_not_continue_to_parent_pipeline_if_child_pipeline_returns_value()
    {
        var pipeline = Pipeline.Build<TestContext, TestContext>(cfg =>
            cfg.When(_ => true,
                    builder => builder.Add((ctx, _) =>
                    {
                        return ValueTask.FromResult(ctx);
                    })
                )
                .Final(ctx => {
                    ctx.Add("Test", "TestValue");
                    return ValueTask.FromResult(ctx);
                })
        );

        var result = await pipeline.Invoke(new TestContext());
        Assert.DoesNotContain("Test", result.Keys);
    }

    [Fact]
    async ValueTask it_does_not_continue_to_parent_pipeline_if_child_pipeline_returns_value_with_async_predicate()
    {
        var pipeline = Pipeline.Build<TestContext, TestContext>(cfg =>
            cfg.When(_ => ValueTask.FromResult(true),
                    builder => builder.Add((ctx, _) =>
                    {
                        return ValueTask.FromResult(ctx);
                    })
                )
                .Final(ctx => {
                    ctx.Add("Test", "TestValue");
                    return ValueTask.FromResult(ctx);
                })
        );

        var result = await pipeline.Invoke(new TestContext());
        Assert.DoesNotContain("Test", result.Keys);
    }
}