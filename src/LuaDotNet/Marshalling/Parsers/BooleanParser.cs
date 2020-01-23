using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    public sealed class BooleanParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex) => LuaModule.Instance.LuaToBoolean(state, stackIndex);

        public void Push(object obj, IntPtr state)
        {
            LuaModule.Instance.LuaPushBoolean(state, (bool) obj);
        }
    }
}