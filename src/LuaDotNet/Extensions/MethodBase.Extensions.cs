using System.Reflection;
using System.Runtime.CompilerServices;

namespace LuaDotNet.Extensions;

/// <summary>
///     Provides extension methods for the <see cref="MethodBase" /> type.
/// </summary>
public static class MethodBaseExtensions
{
    /// <param name="methodBase">The method, which must not be <c>null</c>.</param>
    extension(MethodBase methodBase)
    {
        /// <summary>
        ///     Checks whether the given method is an extension method.
        /// </summary>
        /// <returns><c>true</c> if the method is an extension method; otherwise, <c>false</c>.</returns>
        public bool IsExtensionMethod()
        {
            return methodBase.IsDefined(typeof(ExtensionAttribute));
        }
    }
}