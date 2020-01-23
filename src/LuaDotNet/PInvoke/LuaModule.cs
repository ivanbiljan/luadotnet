using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using LuaDotNet.Extensions;
using NLdr;
using NLdr.Framework;
using LuaInteger = System.Int64; // Just to avoid improper marshalling

namespace LuaDotNet.PInvoke
{
    internal sealed class LuaModule : NativeLibrary
    {
        static LuaModule()
        {
            var runtimesDirectory =
                Path.Combine(new Uri(Path.GetDirectoryName(typeof(LuaContext).Assembly.CodeBase)).LocalPath, "libs");
            if (runtimesDirectory.IsNullOrWhitespace())
            {
                throw new DirectoryNotFoundException("Cannot find Lua runtimes directory.");
            }

            var architecture = IntPtr.Size == 8 ? "x64" : "x86";
            var runtime = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "lua53.so" : "lua53.dll";
            Instance.Load(Path.Combine(runtimesDirectory, architecture, runtime));
        }

        public static LuaModule Instance { get; } = new LuaModule();

        public bool LuaIsBoolean(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Boolean;

        public bool LuaIsNil(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Nil;

        public bool LuaIsNumber(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Number;

        public bool LuaIsString(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.String;

        public bool LuaIsTable(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Table;

        public void LuaPop(IntPtr state, int numberOfElements) => LuaSetTop(state, -numberOfElements - 1);

        internal static class FunctionSignatures
        {
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaCFunction(IntPtr luaState);

            [UnmanagedFunction("lua_close")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public delegate void LuaClose(IntPtr luaState);

            [UnmanagedFunction("lua_createtable")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaCreateTable(IntPtr luaState, int numberOfSequentialElements,
                int numberOfOtherElements);

            [UnmanagedFunction("lua_getglobal")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public delegate int LuaGetGlobal(IntPtr luaState, string globalName);

            [UnmanagedFunction("lua_gettop")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaGetTop(IntPtr luaState);

            [UnmanagedFunction("lua_isinteger")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LuaIsInteger(IntPtr luaState, int stackIndex);

            [UnmanagedFunction("luaL_newstate")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public delegate IntPtr LuaLNewState();

            [UnmanagedFunction("lua_next")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaNext(IntPtr luaState, int tableIndex);

            [UnmanagedFunction("lua_pushboolean")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushBoolean(IntPtr luaState, bool boolValue);

            [UnmanagedFunction("lua_pushcclosure")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushCClosure(IntPtr luaState, LuaCFunction luaCFunction, int n);

            [UnmanagedFunction("lua_pushinteger")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushInteger(IntPtr luaState, long number);

            [UnmanagedFunction("lua_pushlstring")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaPushLString(IntPtr luaState, [In] byte[] stringBytes, UIntPtr length);

            [UnmanagedFunction("lua_pushnil")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushNil(IntPtr luaState);

            [UnmanagedFunction("lua_pushnumber")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushNumber(IntPtr luaState, double number);

            [UnmanagedFunction("lua_pushvalue")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaPushValue(IntPtr luaState, int stackIndex);

            [UnmanagedFunction("lua_rawseti")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaRawSetI(IntPtr luaState, int tableIndex, long keyIndex);

            [UnmanagedFunction("lua_setglobal")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public delegate void LuaSetGlobal(IntPtr luaState, string globalName);

            [UnmanagedFunction("lua_settop")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaSetTop(IntPtr luaState, int top);

            [UnmanagedFunction("lua_toboolean")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LuaToBoolean(IntPtr luaState, int stackIndex);

            [UnmanagedFunction("lua_tointegerx")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long LuaToIntegerX(IntPtr luaState, int stackIndex, out IntPtr isNum);

            [UnmanagedFunction("lua_tolstring")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToLString(IntPtr luaState, int stackIndex, out UIntPtr length);

            [UnmanagedFunction("lua_tonumberx")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate double LuaToNumberX(IntPtr luaState, int stackIndex, out IntPtr isNum);

            [UnmanagedFunction("lua_type")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType LuaTypeD(IntPtr luaState, int stackIndex);
        }

#pragma warning disable 649 
        public FunctionSignatures.LuaClose LuaClose;
        public FunctionSignatures.LuaGetGlobal LuaGetGlobal;
        public FunctionSignatures.LuaSetGlobal LuaSetGlobal;
        public FunctionSignatures.LuaLNewState LuaLNewState;
        public FunctionSignatures.LuaPushBoolean LuaPushBoolean;
        public FunctionSignatures.LuaPushCClosure LuaPushCClosure;
        public FunctionSignatures.LuaPushInteger LuaPushInteger;
        public FunctionSignatures.LuaPushLString LuaPushLString;
        public FunctionSignatures.LuaPushNil LuaPushNil;
        public FunctionSignatures.LuaPushNumber LuaPushNumber;
        public FunctionSignatures.LuaPushValue LuaPushValue;
        public FunctionSignatures.LuaTypeD LuaType;
        public FunctionSignatures.LuaToLString LuaToLString;
        public FunctionSignatures.LuaIsInteger LuaIsInteger;
        public FunctionSignatures.LuaToBoolean LuaToBoolean;
        public FunctionSignatures.LuaToIntegerX LuaToIntegerX;
        public FunctionSignatures.LuaToNumberX LuaToNumberX;
        public FunctionSignatures.LuaGetTop LuaGetTop;
        public FunctionSignatures.LuaCreateTable LuaCreateTable;
        public FunctionSignatures.LuaRawSetI LuaRawSetI;
        public FunctionSignatures.LuaSetTop LuaSetTop;
        public FunctionSignatures.LuaNext LuaNext;
#pragma warning restore 649
    }
}