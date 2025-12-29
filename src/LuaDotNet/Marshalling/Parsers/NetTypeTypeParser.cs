using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class NetTypeTypeParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        return Lua.UserdataToNetObject(state, stackIndex);
    }

    public void Push(IntPtr state, object obj)
    {
        Lua.PushNetObjAsUserdata(state, obj);
        Lua.LuaGetField(state, (int) LuaRegistry.RegistryIndex, Metamethods.NetTypeMetatable);
        Lua.LuaSetMetatable(state, -2);
    }
}