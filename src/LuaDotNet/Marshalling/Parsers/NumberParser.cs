using System;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class NumberParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        if (Lua.LuaIsInteger(state, stackIndex))
        {
            return Lua.LuaToIntegerX(state, stackIndex, out _);
        }

        return Lua.LuaToNumberX(state, stackIndex, out _);
    }

    public void Push(IntPtr state, object obj)
    {
        if (obj.GetType().IsInteger())
        {
            Lua.LuaPushInteger(state, (long) Convert.ChangeType(obj, typeof(long)));

            return;
        }

        Lua.LuaPushNumber(state, (double) Convert.ChangeType(obj, typeof(double)));
    }
}