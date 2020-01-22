using JetBrains.Annotations;

namespace LuaDotNet.PInvoke
{
    /// <summary>
    ///     Specifies a Lua type.
    /// </summary>
    [PublicAPI]
    public enum LuaType
    {
        /// <summary>
        ///     Represents no type.
        /// </summary>
        None = -1,

        /// <summary>
        ///     Represents absence of useful value.
        /// </summary>
        Nil = 0,

        /// <summary>
        ///     Represents a boolean value.
        /// </summary>
        Boolean = 1,

        /// <summary>
        ///     Represents light userdata.
        /// </summary>
        LightUserdata = 2,

        /// <summary>
        ///     Represents a number.
        /// </summary>
        Number = 3,

        /// <summary>
        ///     Represents a string.
        /// </summary>
        String = 4,

        /// <summary>
        ///     Represents a Lua table.
        /// </summary>
        Table = 5,

        /// <summary>
        ///     Represents a Lua function.
        /// </summary>
        Function = 6,

        /// <summary>
        ///     Represents a type used to store arbitrary C data.
        /// </summary>
        Userdata = 7,

        /// <summary>
        ///     Represents a Lua coroutine.
        /// </summary>
        Thread = 8
    }
}