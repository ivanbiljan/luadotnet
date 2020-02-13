using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers {
    /// <summary>
    ///     Represents a default .NET object parser. This parser is used for all types that lack a type parser.
    /// </summary>
    public sealed class NetObjectParser : ITypeParser {
        public object Parse(IntPtr state, int stackIndex) {
            var netObject = LuaModule.Instance.UserdataToNetObject(state, stackIndex);
            if (netObject is Type) {
                return new NetTypeTypeParser().Parse(state, stackIndex);
            }

            return null;
        }

        public void Push(object obj, IntPtr state) {
            if (obj is Type) {
                new NetTypeTypeParser().Push(obj, state);
                return;
            }

            LuaModule.Instance.PushNetObjAsUserdata(state, obj);
            LuaModule.Instance.LuaGetField(state, (int) LuaRegistry.RegistryIndex, Metamethods.NetObjectMetatable);
            LuaModule.Instance.LuaSetMetatable(state, -2);
        }
    }
}