using System;
using JetBrains.Annotations;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet
{
    /// <summary>
    ///     Represents a managed, reusable Lua function.
    /// </summary>
    [PublicAPI]
    public sealed class LuaFunction : LuaObject
    {
        private readonly LuaModule.FunctionSignatures.LuaCFunction _luaCFunction;

        internal LuaFunction(LuaContext lua, int reference) : base(lua, reference)
        {
        }

        internal LuaFunction(LuaModule.FunctionSignatures.LuaCFunction luaCFunction, LuaContext lua) : base(lua, 0)
        {
            _luaCFunction = luaCFunction;
        }

        internal override void PushToStack(IntPtr state)
        {
            if (Reference == 0)
            {
                LuaModule.Instance.LuaPushCClosure(state, _luaCFunction, 0);
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
            return Lua.CallWithArguments(arguments);
        }
    }
}