using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class BooleanParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        return Lua.LuaToBoolean(state, stackIndex);
    }

    public void Push(IntPtr state, object obj)
    {
        Lua.LuaPushBoolean(state, (bool) obj);
    }
}