using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Villain
{
    internal static class Extensions
    {
        /// <summary>
        /// Get a value by <see cref="key"/> in <see cref="Dictionary{TKey,TValue}"/> if exists. Otherwise add a new one with default <seealso cref="TValue"/> and return it.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="this"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (@this.ContainsKey(key))
            {
                return @this[key];
            }
            
            @this.Add(key, value);
            return value;
        }

        /// <summary>
        /// Apply <see cref="f"/> on every <see cref="T"/>s in <see cref="@this"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="f"></param>
        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> f)
        {
            foreach (var t in @this)
            {
                f(t);
            }
        }
    }
}
