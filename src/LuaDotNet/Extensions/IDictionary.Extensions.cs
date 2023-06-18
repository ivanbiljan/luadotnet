using System.Collections.Generic;

namespace LuaDotNet.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="IDictionary{TKey,TValue}" /> type.
    /// </summary>
    public static class IDictionaryExtensions
    {
        /// <summary>
        ///     Gets the value of the specified key in the dictionary, or an optional default value if the key is not present in
        ///     the dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary, which must not be <c>null</c>.</param>
        /// <param name="key">The key, which must not be <c>null</c>.</param>
        /// <param name="defaultValue">An optional default value.</param>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <returns></returns>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default) =>
            dictionary.TryGetValue(key, out var returnValue) ? returnValue : defaultValue;
    }
}