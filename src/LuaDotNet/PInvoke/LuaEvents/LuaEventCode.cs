using JetBrains.Annotations;

namespace LuaDotNet.PInvoke.LuaEvents {
    /// <summary>
    ///     Specifies the event code of a Lua event. These values are constants pulled from the lua.h file.
    /// </summary>
    [PublicAPI]
    public enum LuaEventCode {
        /// <summary>
        ///     The call event code. Used when Lua calls a function.
        /// </summary>
        LuaHookCall = 0,

        /// <summary>
        ///     The ret event code. Used when Lua returns from a function.
        /// </summary>
        LuaHookRet = 1,

        /// <summary>
        ///     The line event code. Used when Lua executes a line of code.
        /// </summary>
        LuaHookLine = 2,

        /// <summary>
        ///     The count event code. Used when Lua executes a specified number of instructions.
        /// </summary>
        LuaHookCount = 3,

        /// <summary>
        ///     The tail call event code.
        /// </summary>
        LuaHookTailCall = 4
    }
}