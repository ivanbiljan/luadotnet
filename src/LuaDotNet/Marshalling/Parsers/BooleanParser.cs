using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers
{
    public sealed class BooleanParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex) => LuaModule.Instance.LuaToBoolean(state, stackIndex);

        public void Push(IntPtr state, object obj)
        {
            LuaModule.Instance.LuaPushBoolean(state, (bool)obj);
        }
    }
}