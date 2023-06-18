using System;
using JetBrains.Annotations;

namespace LuaDotNet.PInvoke.LuaEvents
{
    /// <summary>
    ///     Specifies the hook mask of a Lua event. These values are constants pulled from the lua.h file.
    /// </summary>
    [Flags]
    [PublicAPI]
    public enum LuaEventMask
    {
        /// <summary>
        ///     The hook is called when Lua calls a function, right before the function gets its arguments.
        /// </summary>
        LuaMaskCall = 1 << LuaEventCode.LuaHookCall,

        /// <summary>
        ///     The hook is called when Lua is about to exit a function.
        /// </summary>
        LuaMaskRet = 1 << LuaEventCode.LuaHookRet,

        /// <summary>
        ///     The hook is called when Lua is about to execute a new line of code, or when it jumps back in code.
        /// </summary>
        LuaMaskLine = 1 << LuaEventCode.LuaHookLine,

        /// <summary>
        ///     The hook is called when Lua executes a certain number of instructions.
        /// </summary>
        LuaMaskCount = 1 << LuaEventCode.LuaHookCount
    }
}