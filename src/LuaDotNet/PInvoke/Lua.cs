using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using LuaInteger = long;

#pragma warning disable 649

namespace LuaDotNet.PInvoke;

internal sealed partial class Lua
{
    private const string RuntimesDirectory = "runtimes";
    private const string LibraryName = "lua";

    public const int LuaMultRet = -1;
    public const int LuaNoRef = -2;
    public const int LuaRefNil = -1;

    static Lua()
    {
        NativeLibrary.SetDllImportResolver(
            Assembly.GetExecutingAssembly(),
            (name, assembly, searchPath) =>
            {
                if (name != LibraryName)
                {
                    return IntPtr.Zero;
                }

                var architecture = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    _ => throw new PlatformNotSupportedException()
                };

                string runtime;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    runtime = "lua53.dll";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    runtime = "liblua53.so";
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                var path = Path.Combine(
                    Path.GetDirectoryName(assembly.Location)!,
                    RuntimesDirectory,
                    architecture,
                    runtime
                );

                try
                {
                    return NativeLibrary.Load(path);
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
        );
    }

    public static IntPtr GetMainThreadPointer(IntPtr state)
    {
        LuaRawGetI(state, (int) LuaRegistry.RegistryIndex, (long) LuaRegistry.MainThreadIndex);
        var mainThreadPointer = LuaToPointer(state, -1);
        LuaPop(state, 1);

        return mainThreadPointer;
    }

    public static bool LuaIsBoolean(IntPtr state, int stackIndex)
    {
        return LuaTypeExtImpl(state, stackIndex) == LuaType.Boolean;
    }

    public static bool LuaIsNil(IntPtr state, int stackIndex)
    {
        return LuaTypeExtImpl(state, stackIndex) == LuaType.Nil;
    }

    public static bool LuaIsNumber(IntPtr state, int stackIndex)
    {
        return LuaTypeExtImpl(state, stackIndex) == LuaType.Number;
    }

    public static bool LuaIsString(IntPtr state, int stackIndex)
    {
        return LuaTypeExtImpl(state, stackIndex) == LuaType.String;
    }

    public static bool LuaIsTable(IntPtr state, int stackIndex)
    {
        return LuaTypeExtImpl(state, stackIndex) == LuaType.Table;
    }

    public static void LuaPop(IntPtr state, int numberOfElements)
    {
        LuaSetTop(state, -numberOfElements - 1);
    }

    public static void LuaPushLString(IntPtr state, string str)
    {
        // UTF-8 is the encoding Lua uses. Possible TODO: Support multiple encodings like NLua does?
        var encodedString = str.GetEncodedString(Encoding.UTF8);
        NativeLuaPushLString(state, encodedString, new UIntPtr((uint) encodedString.Length));
    }

    public static void PushNetObjAsUserdata(IntPtr state, object obj)
    {
        var userdataPointer = LuaNewUserdata(state, new UIntPtr((uint) IntPtr.Size));
        Marshal.WriteIntPtr(userdataPointer, GCHandle.ToIntPtr(GCHandle.Alloc(obj)));
    }

    public static object UserdataToNetObject(IntPtr state, int stackIndex)
    {
        var userdataPointer = LuaToUserdata(state, stackIndex);

        return GCHandle.FromIntPtr(Marshal.ReadIntPtr(userdataPointer)).Target;
    }

    internal static object[] PCallKInternal(
        IntPtr state,
        IReadOnlyCollection<object> arguments = null,
        int numberOfResults = LuaMultRet
    )
    {
        // The function (which is currently at the top of the stack) gets popped along with the arguments when it's called
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var stackTop = LuaGetTop(state) - 1;

        // The function is already on the stack so the only thing left to do is push the arguments in direct order
        if (arguments != null)
        {
            foreach (var argument in arguments)
            {
                objectMarshal.PushToStack(state, argument);
            }
        }

        // Adjust the number of results to avoid errors
        var errorCode = LuaPCallK(state, arguments?.Count ?? 0, Math.Max(numberOfResults, -1));
        if (errorCode != LuaErrorCode.LuaOk)
        {
            // Lua pushes an error message in case of errors
            var errorMessage = (string) objectMarshal.GetObject(state, -1);
            LuaPop(state, 1);

            throw new LuaException(
                $"An exception has occured while calling a function: [{errorCode}]: {errorMessage}"
            );
        }

        var results = objectMarshal.GetObjects(state, stackTop + 1, LuaGetTop(state));
        LuaSetTop(state, stackTop);

        return results;
    }

    public delegate int LuaCFunction(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "lua_checkstack")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool LuaCheckStack(IntPtr luaState, int n);

    [LibraryImport(LibraryName, EntryPoint = "lua_close")]
    public static partial void LuaClose(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "lua_createtable")]
    public static partial void LuaCreateTable(
        IntPtr luaState,
        int numberOfSequentialElements,
        int numberOfOtherElements
    );

    [LibraryImport(LibraryName, EntryPoint = "lua_getfield", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int LuaGetField(IntPtr luaState, int tableIndex, string key);

    [LibraryImport(LibraryName, EntryPoint = "lua_getglobal", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int LuaGetGlobal(IntPtr luaState, string globalName);

    [DllImport(LibraryName, EntryPoint = "lua_getstack")]
    public static extern int LuaGetStack(IntPtr luaState, int level, out LuaDebug ar);

    [LibraryImport(LibraryName, EntryPoint = "lua_gettop")]
    public static partial int LuaGetTop(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "lua_isinteger")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool LuaIsInteger(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "luaL_loadstring")]
    public static partial LuaErrorCode LuaLLoadString(IntPtr luaState, [In] byte[] stringBytes);

    [LibraryImport(LibraryName, EntryPoint = "luaL_newmetatable", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool LuaLNewMetatable(IntPtr luaState, string name);

    [LibraryImport(LibraryName, EntryPoint = "luaL_newstate")]
    public static partial IntPtr LuaLNewState();

    [LibraryImport(LibraryName, EntryPoint = "luaL_openlibs")]
    public static partial void LuaLOpenLibs(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "luaL_ref")]
    public static partial int LuaLRef(IntPtr luaState, int tableIndex);

    [LibraryImport(LibraryName, EntryPoint = "luaL_unref")]
    public static partial void LuaLUnref(IntPtr luaState, int tableIndex, int reference);

    [LibraryImport(LibraryName, EntryPoint = "lua_newthread")]
    public static partial IntPtr LuaNewThread(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "lua_newuserdata")]
    public static partial IntPtr LuaNewUserdata(IntPtr luaState, UIntPtr size);

    [LibraryImport(LibraryName, EntryPoint = "lua_next")]
    public static partial int LuaNext(IntPtr luaState, int tableIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_pcallk")]
    public static partial LuaErrorCode LuaPCallK(
        IntPtr luaState,
        int numberOfArguments,
        int numberOfResults = LuaMultRet,
        int messageHandler = 0,
        IntPtr context = 0,
        IntPtr continuationFunction = 0
    );

    [LibraryImport(LibraryName, EntryPoint = "lua_pushboolean")]
    public static partial void LuaPushBoolean(IntPtr luaState, [MarshalAs(UnmanagedType.I1)] bool boolValue);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushcclosure")]
    public static partial void LuaPushCClosure(IntPtr luaState, LuaCFunction luaCFunction, int n);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushinteger")]
    public static partial void LuaPushInteger(IntPtr luaState, LuaInteger number);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushlstring")]
    public static partial IntPtr NativeLuaPushLString(IntPtr luaState, [In] byte[] stringBytes, UIntPtr length);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushnil")]
    public static partial void LuaPushNil(IntPtr luaState);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushnumber")]
    public static partial void LuaPushNumber(IntPtr luaState, double number);

    [LibraryImport(LibraryName, EntryPoint = "lua_pushvalue")]
    public static partial void LuaPushValue(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_rawgeti")]
    public static partial int LuaRawGetI(IntPtr luaState, int tableIndex, LuaInteger elementIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_rawset")]
    public static partial void LuaRawSet(IntPtr luaState, int tableIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_rawseti")]
    public static partial void LuaRawSetI(IntPtr luaState, int tableIndex, LuaInteger keyIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_resume")]
    public static partial LuaErrorCode LuaResume(
        IntPtr coroutineState,
        IntPtr fromCoroutineState,
        int nargs,
        out int nresults
    );

    [LibraryImport(LibraryName, EntryPoint = "lua_setglobal", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void LuaSetGlobal(IntPtr luaState, string globalName);

    [LibraryImport(LibraryName, EntryPoint = "lua_setmetatable")]
    public static partial void LuaSetMetatable(IntPtr luaState, int objectIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_settable")]
    public static partial void LuaSetTable(IntPtr luaState, int tableIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_settop")]
    public static partial void LuaSetTop(IntPtr luaState, int top);

    [LibraryImport(LibraryName, EntryPoint = "lua_status")]
    public static partial LuaErrorCode LuaStatus(IntPtr threadState);

    [LibraryImport(LibraryName, EntryPoint = "lua_toboolean")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool LuaToBoolean(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_tointegerx")]
    public static partial LuaInteger LuaToIntegerX(IntPtr luaState, int stackIndex, out IntPtr isNum);

    [LibraryImport(LibraryName, EntryPoint = "lua_tolstring")]
    public static partial IntPtr LuaToLString(IntPtr luaState, int stackIndex, out UIntPtr length);

    [LibraryImport(LibraryName, EntryPoint = "lua_tonumberx")]
    public static partial double LuaToNumberX(IntPtr luaState, int stackIndex, out IntPtr isNum);

    [LibraryImport(LibraryName, EntryPoint = "lua_topointer")]
    public static partial IntPtr LuaToPointer(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_tothread")]
    public static partial IntPtr LuaToThread(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_touserdata")]
    public static partial IntPtr LuaToUserdata(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_type")]
    public static partial LuaType LuaTypeExtImpl(IntPtr luaState, int stackIndex);

    [LibraryImport(LibraryName, EntryPoint = "lua_xmove")]
    public static partial void LuaXMove(IntPtr fromThreadState, IntPtr toThreadState, int nargs);
    
    [LibraryImport(LibraryName, EntryPoint = "luaL_error")]
    public static partial int LuaError(IntPtr luaState, [MarshalAs(UnmanagedType.LPStr)] string message);
}