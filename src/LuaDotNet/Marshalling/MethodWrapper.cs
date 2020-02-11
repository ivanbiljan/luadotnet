using System;
using System.Reflection;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling {
    internal sealed class MethodWrapper {
        private readonly MethodInfo _method;

        public MethodWrapper(MethodInfo method) {
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public int Callback(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var args = objectMarshal.GetObjects(state, 1, LuaModule.Instance.LuaGetTop(state));
            var result = _method.Invoke(null, args);

            var numberOfResults = 0;
            if (_method.ReturnType != typeof(void)) {
                objectMarshal.PushToStack(state, result);
                ++numberOfResults;
            }
            
            return numberOfResults;
        }
    }
}