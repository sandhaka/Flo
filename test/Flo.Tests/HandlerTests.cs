using System;
using System.Threading.Tasks;
using Xunit;

namespace Flo.Tests;

public class DescribeHandler
{
    [Fact]
    async ValueTask it_creates_and_invoke_handler_using_default_service_provider()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add<TestHandler>()
                .Add<TestHandler>()
        );

        var context = new TestContext();
        await pipeline.Invoke(context);
        Assert.Equal(2, context.Count);
    }

    [Fact]
    async ValueTask it_can_register_handler_instance()
    {
        var pipeline = Pipeline.Build<TestContext>(cfg =>
            cfg.Add(new TestHandler()));

        var context = new TestContext();
        await pipeline.Invoke(context);
        Assert.Single(context);
    }

    [Fact]
    async ValueTask it_initialises_handlers_lazily()
    {
        bool initialised = false;

        var pipeline = Pipeline.Build<object>(cfg =>
            cfg.Add(() => new LazyHandler(() => initialised = true))
        );

        Assert.False(initialised);
        await pipeline.Invoke(null);
        Assert.True(initialised);
    }

    [Fact]
    async ValueTask it_supports_handlers_with_result()
    {
        var pipeline = Pipeline.Build<string, int>(cfg =>
            cfg.Add<StringLengthCountHandler>()
        );

        var output = await pipeline.Invoke("hello world");
        Assert.Equal(11, output);
    }

    [Fact]
    async ValueTask it_can_use_a_custom_service_provider()
    {
        var pipeline = Pipeline.Build<string, string>(cfg =>
                cfg.Add<OverridingHandler>()
            , _ => new OverridingHandler("Override")); // always returns this handler type

        var output = await pipeline.Invoke("hello world");
        Assert.Equal("Override", output);
    }

    class TestHandler : IHandler<TestContext>
    {
        public ValueTask<TestContext> HandleAsync(TestContext input, Func<TestContext, ValueTask<TestContext>> next)
        {
            input.Add(Guid.NewGuid().ToString(), Guid.NewGuid());
            return next.Invoke(input);
        }
    }

    class LazyHandler : IHandler<object>
    {
        private readonly Action _callback;

        public LazyHandler(Action callback)
        {
            _callback = callback;
        }

        public ValueTask<object> HandleAsync(object input, Func<object, ValueTask<object>> next)
        {
            _callback.Invoke();
            return next.Invoke(input);
        }
    }

    class StringLengthCountHandler : IHandler<string, int>
    {
        public ValueTask<int> HandleAsync(string input, Func<string, ValueTask<int>> next)
        {
            return ValueTask.FromResult(input.Length);
        }
    }

    class OverridingHandler : IHandler<string, string>
    {
        private readonly string _output;

        public OverridingHandler() : this("Default") { }

        public OverridingHandler(string output)
        {
            _output = output;
        }

        public ValueTask<string> HandleAsync(string input, Func<string, ValueTask<string>> next)
        {
            return ValueTask.FromResult("Override");
        }
    }
}