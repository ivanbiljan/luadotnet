using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers
{
    public sealed class NetTypeTypeParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex) => LuaModule.Instance.UserdataToNetObject(state, stackIndex);

        public void Push(IntPtr state, object obj)
        {
            LuaModule.Instance.PushNetObjAsUserdata(state, obj);
            LuaModule.Instance.LuaGetField(state, (int)LuaRegistry.RegistryIndex, Metamethods.NetTypeMetatable);
            LuaModule.Instance.LuaSetMetatable(state, -2);
        }
    }
}