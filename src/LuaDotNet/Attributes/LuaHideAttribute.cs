using System;
using JetBrains.Annotations;

namespace LuaDotNet.Attributes
{
    /// <summary>
    ///     Indicates that the marked element is hidden (inaccessible) from the Lua runtime.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.Method |
        AttributeTargets.Property)]
    [PublicAPI]
    public sealed class LuaHideAttribute : Attribute
    {
    }
}