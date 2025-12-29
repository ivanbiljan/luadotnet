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

    internal LuaFunction(LuaContext context, int reference) : base(context, reference)
    {
    }

    internal LuaFunction(LuaContext context, Lua.LuaCFunction luaCFunction) : base(
        context,
        Lua.LuaNoRef
    )
    {
        _luaCFunction = luaCFunction ?? throw new ArgumentNullException(nameof(luaCFunction));
    }

    internal override void PushToStack(IntPtr state)
    {
        if (Reference == Lua.LuaNoRef)
        {
            Lua.LuaPushCClosure(state, _luaCFunction, 0);

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
        ObjectMarshalPool.GetMarshal(Context.State).PushToStack(Context.State, this);

        return Lua.PCallKInternal(Context.State, arguments);
    }
}