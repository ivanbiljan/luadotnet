using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using LuaDotNet.PInvoke.LuaEvents;

namespace LuaDotNet.PInvoke {
    /// <summary>
    ///     Represents the lua_Debug struct which is marshaled to and from unmanaged code when calling events.
    /// </summary>
    [PublicAPI]
    [StructLayout(LayoutKind.Sequential)]
    public struct LuaDebug {
        /// <summary>
        ///     Gets the event code.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)] public LuaEventCode EventCode;

        /// <summary>
        ///     The variable name.
        /// </summary>
        public string Name;

        /// <summary>
        ///     Gets the type of variable.
        /// </summary>
        public string NameWhat;

        /// <summary>
        ///     Gets the source.
        /// </summary>
        public string Source;

        /// <summary>
        ///     Gets the current line.
        /// </summary>
        public int CurrentLine;

        /// <summary>
        ///     TODO
        /// </summary>
        public int LineDefined;

        /// <summary>
        ///     TODO
        /// </summary>
        public int LastLineDefined;

        /// <summary>
        ///     Gets the number of upvalues.
        /// </summary>
        public byte NumberOfUpValues;

        /// <summary>
        ///     Gets the number of parameters.
        /// </summary>
        public byte NumberOfParameters;

        /// <summary>
        ///     Gets a value indicating whether the function is a vararg function.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)] public bool IsVarArg;

        /// <summary>
        ///     Gets a value indicating whether the function is a tail call.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)] public bool IsTailCall;

        /// <summary>
        ///     Gets the index of the first transferred value.
        /// </summary>
        public ushort FirstTransferIndex;

        /// <summary>
        ///     Gets the number of transferred values.
        /// </summary>
        public ushort NumberOfTransferredValues;

        /// <summary>
        ///     Gets the short source.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string ShortSource;

        /// <summary>
        ///     Marshals data from the specified pointer to a <see cref="LuaDebug" /> struct.
        /// </summary>
        /// <param name="pointer">A pointer to an unmanaged block of memory.</param>
        /// <returns>The resulting <see cref="LuaDebug" /> struct.</returns>
        public static LuaDebug FromPointer(IntPtr pointer) => Marshal.PtrToStructure<LuaDebug>(pointer);
    }
}