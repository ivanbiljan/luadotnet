using System;

namespace LuaDotNet.Attributes;

/// <summary>
///     Indicates that the marked element is hidden (inaccessible) from the Lua runtime.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Event |
    AttributeTargets.Field |
    AttributeTargets.Method |
    AttributeTargets.Property
)]
public sealed class LuaHideAttribute : Attribute
{
}