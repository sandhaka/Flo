using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flo;

/// <summary>
/// Provides linking and building functionality for chain of responsibility pipelines
/// </summary>
public abstract class Builder<TIn, TOut, TBuilder> 
    where TBuilder : Builder<TIn, TOut, TBuilder>
{
    private readonly TBuilder _builderInstance;
    private readonly List<Func<TIn, Func<TIn, ValueTask<TOut>>, ValueTask<TOut>>> _handlers = new();

    protected Func<Type, object> ServiceProvider { get; set; }
    protected Func<TIn, ValueTask<TOut>> InnerHandler { get; set; }

    protected Builder(Func<TIn, ValueTask<TOut>> innerHandler, Func<Type, object> serviceProvider)
    {
        InnerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
        ServiceProvider = serviceProvider ?? DefaultServiceProvider;

        _builderInstance = (TBuilder)this;
    }

    public TBuilder Fork(
        Func<TIn, bool> predicate,
        Action<TBuilder> configurePipeline)
    {
        return When(predicate, async (input, innerPipeline, _) => await innerPipeline.Invoke(input), configurePipeline);
    }

    protected TBuilder When(
        Func<TIn, bool> predicate,
        Func<TIn, Func<TIn, ValueTask<TOut>>, Func<TIn, ValueTask<TOut>>, ValueTask<TOut>> handler,
        Action<TBuilder> configurePipeline)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (configurePipeline == null) throw new ArgumentNullException(nameof(configurePipeline));

        return Add((input, next) =>
        {
            if (predicate.Invoke(input))
            {
                var builder = CreateBuilder();
                configurePipeline(builder);
                return handler.Invoke(input, builder.Build(), next);
            }

            return next.Invoke(input);
        });
    }

    protected TBuilder When(
        Func<TIn, ValueTask<bool>> predicate,
        Func<TIn, Func<TIn, ValueTask<TOut>>, Func<TIn, ValueTask<TOut>>, ValueTask<TOut>> handler,
        Action<TBuilder> configurePipeline)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (configurePipeline == null) throw new ArgumentNullException(nameof(configurePipeline));

        return Add(async (input, next) =>
        {
            var doInvoke = await predicate.Invoke(input);
            if (doInvoke)
            {
                var builder = CreateBuilder();
                configurePipeline(builder);
                return await handler.Invoke(input, builder.Build(), next);
            }

            return await next.Invoke(input);
        });
    }

    protected abstract TBuilder CreateBuilder();

    public TBuilder Final(Func<TIn, ValueTask<TOut>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return Add((input, _) => handler.Invoke(input));
    }

    public TBuilder Add<THandler>() where THandler : class, IHandler<TIn, TOut>
    {
        THandler HandlerFactory() => ServiceProvider.Invoke(typeof(THandler)) as THandler;
        return Add((Func<THandler>)HandlerFactory);
    }

    public TBuilder Add(Func<IHandler<TIn, TOut>> handlerFactory)
    {
        if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));

        return Add((input, next) => 
        {
            var handler = handlerFactory.Invoke();
                
            if (handler != null)
            {
                return handler.HandleAsync(input, next);
            }

            return next.Invoke(input);
        });
    }

    public TBuilder Add(IHandler<TIn, TOut> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        return Add((input, next) => handler.HandleAsync(input, next));
    }
        
    public TBuilder Add(Func<TIn, Func<TIn, ValueTask<TOut>>, ValueTask<TOut>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _handlers.Add(handler);
        return _builderInstance;
    }

    public virtual Func<TIn, ValueTask<TOut>> Build()
    {
        Func<TIn, ValueTask<TOut>> pipeline = InnerHandler;
        for (int i = _handlers.Count - 1; i >= 0; i--)
        {
            var handler = _handlers[i];
            var prev = pipeline;
            pipeline = input => handler.Invoke(input, prev);
        }

        return pipeline;
    }

    private static object DefaultServiceProvider(Type type) => Activator.CreateInstance(type);
}