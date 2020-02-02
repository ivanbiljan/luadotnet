using System;

namespace LuaDotNet.PInvoke {
    /// <summary>
    ///     Specifies a Lua error code.
    /// </summary>
    public enum LuaErrorCode {
        /// <summary>
        ///     Represents the normal status for a thread. When resuming threads with <see cref="LUA_OK" /> a new coroutine is
        ///     started, whereas <see cref="LUA_YIELD" /> resumes a couroutine.
        /// </summary>
        LuaOk = 0,

        /// <summary>
        ///     Represents a suspended thread.
        /// </summary>
        LuaYield = 1,

        /// <summary>
        ///     Represents a runtime error.
        /// </summary>
        LuaRuntimeError = 2,

        /// <summary>
        ///     Represents a syntax error.
        /// </summary>
        LuaSyntaxError = 3,

        /// <summary>
        ///     Represents a memory allocation error.
        /// </summary>
        LuaMemoryError = 4,

        /// <summary>
        ///     Represents an error thrown while running the message handler.
        /// </summary>
        LuaMessageHandlerError = 5,

        /// <summary>
        ///     Represents an error thrown during the execution of a __gc metamethod.
        /// </summary>
        [Obsolete("As per Lua's source code, this error code is pending termination")]
        LuaGcMetamethodError = 6
    }
}