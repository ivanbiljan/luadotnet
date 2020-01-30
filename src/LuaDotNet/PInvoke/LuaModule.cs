using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using LuaDotNet.Extensions;
using NLdr;
using NLdr.Framework;
using LuaInteger = System.Int64; // Just to avoid improper marshalling

namespace LuaDotNet.PInvoke
{
    internal sealed class LuaModule : NativeLibrary
    {
        public const int LuaMultRet = -1;

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

        public void LuaPushLString(IntPtr state, string str)
        {
            // UTF-8 is the encoding Lua uses. Possible TODO: Support multiple encodings like NLua does?
            var encodedString = str.GetEncodedString(Encoding.UTF8);
            LuaPushLStringDelegate(state, encodedString, new UIntPtr((uint) encodedString.Length));
        }

        public void PushNetObjAsUserdata(IntPtr state, object obj)
        {
            var userdataPointer = LuaNewUserdata(state, new UIntPtr((uint) IntPtr.Size));
            Marshal.WriteIntPtr(userdataPointer, GCHandle.ToIntPtr(GCHandle.Alloc(obj)));
        }

        public object UserdataToNetObject(IntPtr state, int stackIndex)
        {
            var userdataPointer = LuaToUserdata(state, stackIndex);
            return GCHandle.FromIntPtr(Marshal.ReadIntPtr(userdataPointer)).Target;
        }

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

            [UnmanagedFunction("lua_getfield")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaGetField(IntPtr luaState, int tableIndex, string key);

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

            [UnmanagedFunction("luaL_loadstring")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaErrorCode LuaLLoadString(IntPtr luaState, [In] byte[] stringBytes);

            [UnmanagedFunction("luaL_newmetatable")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LuaLNewMetatable(IntPtr luaState, string name);

            [UnmanagedFunction("luaL_newstate")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public delegate IntPtr LuaLNewState();

            [UnmanagedFunction("lua_newuserdata")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaNewUserdata(IntPtr luaState, UIntPtr size);

            [UnmanagedFunction("lua_next")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaNext(IntPtr luaState, int tableIndex);

            [UnmanagedFunction("lua_pcallk")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaErrorCode LuaPCallK(IntPtr luaState, int numberOfArguments,
                int numberOfResults = LuaMultRet,
                int messageHandler = 0, IntPtr context = default(IntPtr),
                IntPtr continuationFunction = default(IntPtr));

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

            [UnmanagedFunction("lua_setmetatable")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaSetMetatable(IntPtr luaState, int objectIndex);

            [UnmanagedFunction("lua_settable")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaSetTable(IntPtr luaState, int tableIndex);

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

            [UnmanagedFunction("lua_touserdata")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToUserdata(IntPtr luaState, int stackIndex);

            [UnmanagedFunction("lua_type")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType LuaTypeD(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("luaL_ref")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaLRef(IntPtr luaState, int tableIndex);
            
            [UnmanagedFunction("luaL_unref")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaLUnref(IntPtr luaState, int tableIndex, int reference);
        }

#pragma warning disable 649 
        public FunctionSignatures.LuaClose LuaClose;
        public FunctionSignatures.LuaGetGlobal LuaGetGlobal;
        public FunctionSignatures.LuaSetGlobal LuaSetGlobal;
        public FunctionSignatures.LuaLNewState LuaLNewState;
        public FunctionSignatures.LuaPushBoolean LuaPushBoolean;
        public FunctionSignatures.LuaPushCClosure LuaPushCClosure;
        public FunctionSignatures.LuaPushInteger LuaPushInteger;
        public FunctionSignatures.LuaPushLString LuaPushLStringDelegate;
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
        public FunctionSignatures.LuaPCallK LuaPCallK;
        public FunctionSignatures.LuaLLoadString LuaLLoadString;
        public FunctionSignatures.LuaNewUserdata LuaNewUserdata;
        public FunctionSignatures.LuaToUserdata LuaToUserdata;
        public FunctionSignatures.LuaSetMetatable LuaSetMetatable;
        public FunctionSignatures.LuaGetField LuaGetField;
        public FunctionSignatures.LuaLNewMetatable LuaLNewMetatable;
        public FunctionSignatures.LuaSetTable LuaSetTable;
        public FunctionSignatures.LuaLRef LuaLRef;
        public FunctionSignatures.LuaLUnref LuaLUnref;
#pragma warning restore 649
    }
}