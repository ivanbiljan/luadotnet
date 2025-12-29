using System;
using System.Reflection;

namespace LuaDotNet.Extensions;

/// <summary>
///     Provides extension methods for the <see cref="ParameterInfo" /> class.
/// </summary>
public static class ParameterInfoExtensions
{
    /// <param name="parameterInfo">The parameter, which must not be <c>null</c>.</param>
    extension(ParameterInfo parameterInfo)
    {
        /// <summary>
        ///     Checks whether the provided parameter is a params array.
        /// </summary>
        /// <returns><c>true</c> if the parameter is a params array; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameterInfo" /> is <c>null</c>.</exception>
        public bool IsParamsArray()
        {
            return parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null;
        }
    }
}