using System;
using System.Reflection;
using System.Runtime.InteropServices;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    internal class Metamethods
    {
        public const string NetTypeMetatable = "luadotnet_type";
        public const string NetObjectMetatable = "luadotnet_object";

        public static int CallType(IntPtr state)
        {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var type = LuaModule.Instance.UserdataToNetObject(state, 1) as Type;
            var typeMetadata = type.GetOrCreateMetadata();
            var arguments = new object[LuaModule.Instance.LuaGetTop(state) - 1];
            for (var i = 2; i < LuaModule.Instance.LuaGetTop(state); ++i)
            {
                arguments[i - 2] = objectMarshal.GetObject(state, i);
            }

            var constructor =
                Utils.TryResolveMethodCall(typeMetadata.Constructors, arguments, out var convertedArguments) as
                    ConstructorInfo;
            if (constructor == null)
            {
                throw new LuaException(
                    $"Type {type.Name} does not contain a constructor that contains the provided arguments.");
            }

            objectMarshal.PushToStack(state, constructor.Invoke(convertedArguments));
            return 1;
        }

        public static void CreateMetatables(IntPtr state)
        {
            // Create the Type metatable
            LuaModule.Instance.LuaLNewMetatable(state, NetTypeMetatable);
            PushMetamethod("__gc", Gc);
            PushMetamethod("__call", CallType);
            LuaModule.Instance.LuaPop(state, 1); 
            
            // Create the Object metatable
            LuaModule.Instance.LuaLNewMetatable(state, NetObjectMetatable);
            LuaModule.Instance.LuaPop(state, 1);

            void PushMetamethod(string metamethod, LuaModule.FunctionSignatures.LuaCFunction luaCFunction)
            {
                LuaModule.Instance.LuaPushLString(state, metamethod);
                LuaModule.Instance.LuaPushCClosure(state, luaCFunction, 0);
                LuaModule.Instance.LuaSetTable(state, -3);
            }
        }

        public static int Gc(IntPtr luaState)
        {
            GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaModule.Instance.LuaToUserdata(luaState, -1))).Free();
            return 0;
        }
    }
}