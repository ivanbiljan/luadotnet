using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class NetTypeTypeParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        return LuaModule.UserdataToNetObject(state, stackIndex);
    }

    public void Push(IntPtr state, object obj)
    {
        LuaModule.PushNetObjAsUserdata(state, obj);
        LuaModule.LuaGetField(state, (int) LuaRegistry.RegistryIndex, Metamethods.NetTypeMetatable);
        LuaModule.LuaSetMetatable(state, -2);
    }
}