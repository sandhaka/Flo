using System;
using System.Threading.Tasks;

namespace Flo;

public interface IHandler<TIn, TOut>
{
    ValueTask<TOut> HandleAsync(TIn input, Func<TIn, ValueTask<TOut>> next);
}
    
public interface IHandler<T> : IHandler<T, T>
{
}