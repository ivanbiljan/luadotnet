using System;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class NumberParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        if (LuaModule.LuaIsInteger(state, stackIndex))
        {
            return LuaModule.LuaToIntegerX(state, stackIndex, out _);
        }

        return LuaModule.LuaToNumberX(state, stackIndex, out _);
    }

    public void Push(IntPtr state, object obj)
    {
        if (obj.GetType().IsInteger())
        {
            LuaModule.LuaPushInteger(state, (long) Convert.ChangeType(obj, typeof(long)));

            return;
        }

        LuaModule.LuaPushNumber(state, (double) Convert.ChangeType(obj, typeof(double)));
    }
}