using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using NLdr;
using NLdr.Framework;
using LuaInteger = System.Int64; // Just to avoid improper marshalling
#pragma warning disable 649

namespace LuaDotNet.PInvoke {
    internal sealed class LuaModule : NativeLibrary {
        public const int LuaMultRet = -1;
        public const int LuaNoRef = -2;
        public const int LuaRefNil = -1;

        public FunctionSignatures.LuaCheckStack LuaCheckStack;
        public FunctionSignatures.LuaClose LuaClose;
        public FunctionSignatures.LuaCreateTable LuaCreateTable;
        public FunctionSignatures.LuaGetField LuaGetField;
        public FunctionSignatures.LuaGetGlobal LuaGetGlobal;
        public FunctionSignatures.LuaGetStack LuaGetStack;
        public FunctionSignatures.LuaGetTop LuaGetTop;
        public FunctionSignatures.LuaIsInteger LuaIsInteger;
        public FunctionSignatures.LuaLLoadString LuaLLoadString;
        public FunctionSignatures.LuaLNewMetatable LuaLNewMetatable;
        public FunctionSignatures.LuaLNewState LuaLNewState;
        public FunctionSignatures.LuaLRef LuaLRef;
        public FunctionSignatures.LuaLUnref LuaLUnref;
        public FunctionSignatures.LuaNewThread LuaNewThread;
        private FunctionSignatures.LuaNewUserdata LuaNewUserdata;
        public FunctionSignatures.LuaNext LuaNext;
        public FunctionSignatures.LuaPCallK LuaPCallK;
        public FunctionSignatures.LuaPushBoolean LuaPushBoolean;
        public FunctionSignatures.LuaPushCClosure LuaPushCClosure;
        public FunctionSignatures.LuaPushInteger LuaPushInteger;
        private FunctionSignatures.LuaPushLString LuaPushLStringDelegate;
        public FunctionSignatures.LuaPushNil LuaPushNil;
        public FunctionSignatures.LuaPushNumber LuaPushNumber;
        public FunctionSignatures.LuaPushValue LuaPushValue;
        public FunctionSignatures.LuaRawGetI LuaRawGetI;
        public FunctionSignatures.LuaRawSet LuaRawSet;
        public FunctionSignatures.LuaRawSetI LuaRawSetI;
        public FunctionSignatures.LuaResume LuaResume;
        public FunctionSignatures.LuaSetGlobal LuaSetGlobal;
        public FunctionSignatures.LuaSetMetatable LuaSetMetatable;
        public FunctionSignatures.LuaSetTable LuaSetTable;
        public FunctionSignatures.LuaSetTop LuaSetTop;
        public FunctionSignatures.LuaStatus LuaStatus;
        public FunctionSignatures.LuaToBoolean LuaToBoolean;
        public FunctionSignatures.LuaToIntegerX LuaToIntegerX;
        public FunctionSignatures.LuaToLString LuaToLString;
        public FunctionSignatures.LuaToNumberX LuaToNumberX;
        public FunctionSignatures.LuaToPointer LuaToPointer;
        public FunctionSignatures.LuaToThread LuaToThread;
        public FunctionSignatures.LuaToUserdata LuaToUserdata;
        public FunctionSignatures.LuaTypeD LuaType;
        public FunctionSignatures.LuaXMove LuaXMove;

        static LuaModule() {
            var runtimesDirectory = Path.Combine(new Uri(Path.GetDirectoryName(typeof(LuaContext).Assembly.CodeBase)).LocalPath, "libs");
            if (runtimesDirectory.IsNullOrWhitespace()) {
                throw new DirectoryNotFoundException("Cannot find Lua runtimes directory.");
            }

            var architecture = IntPtr.Size == 8 ? "x64" : "x86";
            var runtime = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "lua53.so" : "lua53.dll";
            Instance.Load(Path.Combine(runtimesDirectory, architecture, runtime));
        }

        public static LuaModule Instance { get; } = new LuaModule();

        public IntPtr GetMainThreadPointer(IntPtr state) {
            LuaRawGetI(state, (int) LuaRegistry.RegistryIndex, (long) LuaRegistry.MainThreadIndex);
            var mainThreadPointer = LuaToPointer(state, -1);
            LuaPop(state, 1);
            return mainThreadPointer;
        }

        public bool LuaIsBoolean(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Boolean;

        public bool LuaIsNil(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Nil;

        public bool LuaIsNumber(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Number;

        public bool LuaIsString(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.String;

        public bool LuaIsTable(IntPtr state, int stackIndex) => LuaType(state, stackIndex) == PInvoke.LuaType.Table;

        public void LuaPop(IntPtr state, int numberOfElements) => LuaSetTop(state, -numberOfElements - 1);

        public void LuaPushLString(IntPtr state, string str) {
            // UTF-8 is the encoding Lua uses. Possible TODO: Support multiple encodings like NLua does?
            var encodedString = str.GetEncodedString(Encoding.UTF8);
            LuaPushLStringDelegate(state, encodedString, new UIntPtr((uint) encodedString.Length));
        }

        public void PushNetObjAsUserdata(IntPtr state, object obj) {
            var userdataPointer = LuaNewUserdata(state, new UIntPtr((uint) IntPtr.Size));
            Marshal.WriteIntPtr(userdataPointer, GCHandle.ToIntPtr(GCHandle.Alloc(obj)));
        }

        public object UserdataToNetObject(IntPtr state, int stackIndex) {
            var userdataPointer = LuaToUserdata(state, stackIndex);
            return GCHandle.FromIntPtr(Marshal.ReadIntPtr(userdataPointer)).Target;
        }

        internal object[] PCallKInternal(IntPtr state, IReadOnlyCollection<object> arguments = null, int numberOfResults = LuaMultRet) {
            // The function (which is currently at the top of the stack) gets popped along with the arguments when it's called
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var stackTop = LuaGetTop(state) - 1;

            // The function is already on the stack so the only thing left to do is push the arguments in direct order
            if (arguments != null) {
                foreach (var argument in arguments) {
                    objectMarshal.PushToStack(state, argument);
                }
            }

            // Adjust the number of results to avoid errors
            numberOfResults = Math.Max(numberOfResults, -1);
            LuaErrorCode errorCode;
            if ((errorCode = LuaPCallK(state, arguments?.Count ?? 0, numberOfResults)) != LuaErrorCode.LuaOk) {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) objectMarshal.GetObject(state, -1);
                LuaPop(state, 1);
                throw new LuaException($"An exception has occured while calling a function: [{errorCode}]: {errorMessage}");
            }

            var results = objectMarshal.GetObjects(state, stackTop + 1, Instance.LuaGetTop(state));
            LuaSetTop(state, stackTop);
            return results;
        }

        [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
        internal static class FunctionSignatures {
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaCFunction(IntPtr luaState);
            
            [UnmanagedFunction("lua_checkstack")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LuaCheckStack(IntPtr luaState, int n);
            
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

            [UnmanagedFunction("lua_getstack")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaGetStack(IntPtr luaState, int level, out LuaDebug ar);
            
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
            
            [UnmanagedFunction("luaL_ref")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaLRef(IntPtr luaState, int tableIndex);
            
            [UnmanagedFunction("luaL_unref")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaLUnref(IntPtr luaState, int tableIndex, int reference);
            
            [UnmanagedFunction("lua_newthread")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaNewThread(IntPtr luaState);
            
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
            public delegate void LuaPushInteger(IntPtr luaState, LuaInteger number);
            
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
            
            [UnmanagedFunction("lua_rawgeti")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaRawGetI(IntPtr luaState, int tableIndex, LuaInteger elementIndex);
            
            [UnmanagedFunction("lua_rawset")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaRawSet(IntPtr luaState, int tableIndex);
            
            [UnmanagedFunction("lua_rawseti")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaRawSetI(IntPtr luaState, int tableIndex, LuaInteger keyIndex);
            
            [UnmanagedFunction("lua_resume")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaResume(IntPtr coroutineState, IntPtr fromCoroutineState, int nargs,
                out int nresults);
            
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
            
            [UnmanagedFunction("lua_status")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int LuaStatus(IntPtr threadState);
            
            [UnmanagedFunction("lua_toboolean")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LuaToBoolean(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("lua_tointegerx")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaInteger LuaToIntegerX(IntPtr luaState, int stackIndex, out IntPtr isNum);
            
            [UnmanagedFunction("lua_tolstring")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToLString(IntPtr luaState, int stackIndex, out UIntPtr length);
            
            [UnmanagedFunction("lua_tonumberx")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate double LuaToNumberX(IntPtr luaState, int stackIndex, out IntPtr isNum);
            
            [UnmanagedFunction("lua_topointer")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToPointer(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("lua_tothread")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToThread(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("lua_touserdata")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr LuaToUserdata(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("lua_type")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType LuaTypeD(IntPtr luaState, int stackIndex);
            
            [UnmanagedFunction("lua_xmove")]
            [SuppressUnmanagedCodeSecurity]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LuaXMove(IntPtr fromThreadState, IntPtr toThreadState, int nargs);
        }
    }
}