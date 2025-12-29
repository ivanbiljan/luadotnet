using System.Collections.Generic;

namespace LuaDotNet.Extensions;

/// <summary>
///     Provides extension methods for the <see cref="IDictionary{TKey,TValue}" /> type.
/// </summary>
public static class IDictionaryExtensions
{
    /// <param name="dictionary">The dictionary, which must not be <c>null</c>.</param>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        /// <summary>
        ///     Gets the value of the specified key in the dictionary, or an optional default value if the key is not present in
        ///     the dictionary.
        /// </summary>
        /// <param name="key">The key, which must not be <c>null</c>.</param>
        /// <param name="defaultValue">An optional default value.</param>
        /// <returns></returns>
        public TValue? GetValueOrDefault(TKey key, TValue? defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var returnValue) ? returnValue : defaultValue;
        }
    }
}