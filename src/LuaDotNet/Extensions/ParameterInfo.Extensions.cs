using System;
using System.Reflection;

namespace LuaDotNet.Extensions {
    /// <summary>
    ///     Provides extension methods for the <see cref="ParameterInfo" /> class.
    /// </summary>
    public static class ParameterInfoExtensions {
        /// <summary>
        ///     Checks whether the provided parameter is a params array.
        /// </summary>
        /// <param name="parameterInfo">The parameter, which must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the parameter is a params array; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameterInfo" /> is <c>null</c>.</exception>
        public static bool IsParamsArray(this ParameterInfo parameterInfo) {
            if (parameterInfo == null) {
                throw new ArgumentNullException(nameof(parameterInfo));
            }

            return parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null;
        }
    }
}