using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using LuaDotNet.PInvoke;

namespace LuaDotNet.DebuggingHelpers {
    internal static class DebugHelper {
        public static void DumpStack(IntPtr luaState, [CallerMemberName] string caller = "") {
            var table = new DebugTable("Stack Index", "Type", "Value");
            Debug.WriteLine($"--------------- STACK ({caller}) ---------------");
            for (var i = 1; i <= LuaModule.Instance.LuaGetTop(luaState); ++i) {
                var value = string.Empty;
                var luaType = LuaModule.Instance.LuaType(luaState, i);
                switch (luaType) {
                    case LuaType.Nil:
                        value = "NULL";
                        break;
                    case LuaType.Boolean:
                        value = LuaModule.Instance.LuaToBoolean(luaState, i).ToString();
                        break;
                    case LuaType.LightUserdata:
                    case LuaType.Table:
                    case LuaType.Function:
                    case LuaType.Userdata:
                    case LuaType.Thread:
                        break;
                    case LuaType.Number:
                        value = LuaModule.Instance.LuaIsInteger(luaState, i)
                            ? LuaModule.Instance.LuaToIntegerX(luaState, i, out _).ToString()
                            : LuaModule.Instance.LuaToNumberX(luaState, i, out _).ToString(CultureInfo.CurrentCulture);
                        break;
                    case LuaType.String:
                        break;
                }

                table.AddRow(i, luaType, value);
            }

            Debug.WriteLine(table.GetOutput());
            Debug.WriteLine("--------------- END OF STACK ---------------");
        }
    }
}