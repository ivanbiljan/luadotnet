using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class BooleanParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        return LuaModule.LuaToBoolean(state, stackIndex);
    }

    public void Push(IntPtr state, object obj)
    {
        LuaModule.LuaPushBoolean(state, (bool) obj);
    }
}