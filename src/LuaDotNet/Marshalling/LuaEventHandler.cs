using System;
using JetBrains.Annotations;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling {
    internal sealed class LuaEventHandler<TEventArgs> where TEventArgs : EventArgs {
        private readonly LuaFunction _luaFunction;

        public LuaEventHandler([NotNull] LuaFunction luaFunction) {
            _luaFunction = luaFunction ?? throw new ArgumentNullException(nameof(luaFunction));
        }

        public void HandleEvent(object sender, TEventArgs args) {
            _luaFunction.Call(sender, args);
        }
    }
}