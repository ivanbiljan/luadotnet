using System;

namespace LuaDotNet.Marshalling;

internal sealed class LuaEventHandler<TEventArgs>(LuaFunction luaFunction)
    where TEventArgs : EventArgs
{
    private readonly LuaFunction _luaFunction = luaFunction ?? throw new ArgumentNullException(nameof(luaFunction));

    public void HandleEvent(object sender, TEventArgs args)
    {
        _luaFunction.Call(sender, args);
    }
}