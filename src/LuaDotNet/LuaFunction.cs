using System;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents a managed, reusable Lua function.
/// </summary>
public sealed class LuaFunction : LuaObject
{
    private readonly Lua.LuaCFunction _luaCFunction;

    internal LuaFunction(LuaContext lua, int reference) : base(lua, reference)
    {
    }

    internal LuaFunction(LuaContext lua, Lua.LuaCFunction luaCFunction) : base(
        lua,
        PInvoke.Lua.LuaNoRef
    )
    {
        _luaCFunction = luaCFunction ?? throw new ArgumentNullException(nameof(luaCFunction));
    }

    internal override void PushToStack(IntPtr state)
    {
        if (Reference == PInvoke.Lua.LuaNoRef)
        {
            PInvoke.Lua.LuaPushCClosure(state, _luaCFunction, 0);

            return;
        }

        base.PushToStack(state);
    }

    /// <summary>
    ///     Calls the function using the provided arguments.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The invocation's results.</returns>
    public object[] Call(params object[] arguments)
    {
        ObjectMarshalPool.GetMarshal(Lua.State).PushToStack(Lua.State, this);

        return PInvoke.Lua.PCallKInternal(Lua.State, arguments);
    }
}