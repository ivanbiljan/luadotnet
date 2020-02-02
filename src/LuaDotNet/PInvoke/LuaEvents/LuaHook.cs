using System;

namespace LuaDotNet.PInvoke.LuaEvents {
    /// <summary>
    ///     Represents the type used for debugging functions.
    /// </summary>
    /// <param name="luaState">The Lua state pointer.</param>
    /// <param name="ar">The hook information.</param>
    public delegate void LuaHook(IntPtr luaState, LuaDebug ar);
}