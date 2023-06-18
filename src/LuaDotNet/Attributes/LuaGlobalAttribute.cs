using System;
using JetBrains.Annotations;

namespace LuaDotNet.Attributes
{
    /// <summary>
    ///     Indicates that the marked method is exported as a global Lua function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    [PublicAPI]
    public sealed class LuaGlobalAttribute : Attribute
    {
        /// <summary>
        ///     Gets or sets the global name override.
        /// </summary>
        [CanBeNull]
        public string NameOverride { get; set; }
    }
}