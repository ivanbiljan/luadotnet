using System;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    public sealed class NumberParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex) =>
            LuaModule.Instance.LuaIsInteger(state, stackIndex)
                ? LuaModule.Instance.LuaToIntegerX(state, stackIndex, out _)
                : LuaModule.Instance.LuaToNumberX(state, stackIndex, out _);

        public void Push(object obj, IntPtr state)
        {
            if (obj.GetType().IsInteger())
            {
                LuaModule.Instance.LuaPushInteger(state, (long) obj);
                return;
            }

            LuaModule.Instance.LuaPushNumber(state, (double) obj);
        }
    }
}