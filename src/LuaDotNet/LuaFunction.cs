using System;
using LuaDotNet.Marshalling;

namespace LuaDotNet
{
    public sealed class LuaFunction : LuaObject
    {
        internal LuaFunction(LuaContext lua, int reference) : base(lua, reference)
        {
        }

        public object[] Call(params object[] arguments)
        {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            objectMarshal.PushToStack(Lua.State, this);
            return Lua.CallWithArguments(arguments);
        }
    }
}