using System;
using System.Collections.Generic;

namespace ViewsSourceGenerator.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<TResult> SelectWhere<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, (bool include, TResult result)> selector)
        {
            foreach (var item in source)
            {
                var (include, result) = selector(item);
                if (include)
                {
                    yield return result;
                }
            }
        }

        public static IEnumerable<TResult> SelectWhere<TSource, TResult, TState>(
            this IEnumerable<TSource> source,
            Func<TSource, TState, (bool include, TResult result)> selector,
            TState state)
        {
            foreach (var item in source)
            {
                var (include, result) = selector(item, state);
                if (include)
                {
                    yield return result;
                }
            }
        }
        
        public static IEnumerable<TResult> SelectWhere<TSource, TResult, TState0, TState1>(
            this IEnumerable<TSource> source,
            Func<TSource, TState0, TState1, (bool include, TResult result)> selector,
            TState0 state0,
            TState1 state1)
        {
            foreach (var item in source)
            {
                var (include, result) = selector(item, state0, state1);
                if (include)
                {
                    yield return result;
                }
            }
        }
        
        public static IEnumerable<TResult> SelectWhere<TSource, TResult, TState0, TState1, TState2>(
            this IEnumerable<TSource> source,
            Func<TSource, TState0, TState1, TState2, (bool include, TResult result)> selector,
            TState0 state0,
            TState1 state1,
            TState2 state2)
        {
            foreach (var item in source)
            {
                var (include, result) = selector(item, state0, state1, state2);
                if (include)
                {
                    yield return result;
                }
            }
        }
        
        public static IEnumerable<TResult> SelectManyWhere<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, (bool include, IEnumerable<TResult> results)> selector)
        {
            foreach (var item in source)
            {
                var (include, results) = selector(item);
                if (!include)
                {
                    continue;
                }
                
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }
    }
}