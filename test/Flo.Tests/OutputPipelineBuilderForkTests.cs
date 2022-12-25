using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribeOutputPipelineBuilderFork
{
    [Fact]
    async ValueTask it_ignores_the_handler_if_the_predicate_returns_false()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Fork(input => input == "hello world",
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
    async ValueTask it_executes_the_handler_and_does_not_continue_to_parent_if_the_predicate_returns_true()
    {
        var pipeline = Pipeline.Build<TestContext, TestContext>(cfg =>
            cfg.Fork(_ => true,
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