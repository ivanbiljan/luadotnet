using System;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling {
    public sealed class NumberParser : ITypeParser {
        public object Parse(IntPtr state, int stackIndex) {
            if (LuaModule.Instance.LuaIsInteger(state, stackIndex)) {
                return LuaModule.Instance.LuaToIntegerX(state, stackIndex, out _);
            }

            return LuaModule.Instance.LuaToNumberX(state, stackIndex, out _);
        }

        public void Push(object obj, IntPtr state) {
            if (obj.GetType().IsInteger()) {
                LuaModule.Instance.LuaPushInteger(state, (long) Convert.ChangeType(obj, typeof(long)));
                return;
            }

            LuaModule.Instance.LuaPushNumber(state, (double) Convert.ChangeType(obj, typeof(double)));
        }
    }
}