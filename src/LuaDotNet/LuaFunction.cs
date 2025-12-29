using System;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents a managed, reusable Lua function.
/// </summary>
public sealed class LuaFunction : LuaObject
{
    private readonly LuaModule.LuaCFunction _luaCFunction;

    internal LuaFunction(LuaContext lua, int reference) : base(lua, reference)
    {
    }

    internal LuaFunction(LuaContext lua, LuaModule.LuaCFunction luaCFunction) : base(
        lua,
        LuaModule.LuaNoRef
    )
    {
        _luaCFunction = luaCFunction ?? throw new ArgumentNullException(nameof(luaCFunction));
    }

    internal override void PushToStack(IntPtr state)
    {
        if (Reference == LuaModule.LuaNoRef)
        {
            LuaModule.LuaPushCClosure(state, _luaCFunction, 0);

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

        return LuaModule.PCallKInternal(Lua.State, arguments);
    }
}