using System;
using System.Collections.Generic;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling {
    public sealed class ArrayParser : ITypeParser {
        public object Parse(IntPtr state, int stackIndex) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            if (!LuaModule.Instance.LuaIsTable(state, stackIndex)) {
                return null;
            }

            var objects = new List<object>();
            for (objectMarshal.PushToStack(state, null);
                LuaModule.Instance.LuaNext(state, -2) > 0;
                LuaModule.Instance.LuaPop(state, 1)) {
                objects.Add(objectMarshal.GetObject(state, -1));
            }

            return objects.ToArray();
        }

        public void Push(object obj, IntPtr state) {
            var array = (Array) obj;
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            LuaModule.Instance.LuaCreateTable(state, array.Length, 0);

            for (var i = 0; i < array.Length; ++i) {
                objectMarshal.PushToStack(state, array.GetValue(i));
                LuaModule.Instance.LuaRawSetI(state, -2, i + 1);
            }
        }
    }
}