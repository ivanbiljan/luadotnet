using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;
using LuaCFunction = LuaDotNet.PInvoke.LuaModule.FunctionSignatures.LuaCFunction;
using static LuaDotNet.Utils;

namespace LuaDotNet.Marshalling {
    internal static class Metamethods {
        private static readonly Dictionary<string, LuaCFunction> TypeMetamethods = new Dictionary<string, LuaCFunction> {
            ["__gc"] = Gc,
            ["__call"] = CallType
        };

        private static readonly Dictionary<string, LuaCFunction> ObjectMetamethods = new Dictionary<string, LuaCFunction> {
            ["__gc"] = Gc
        };

        public const string NetObjectMetatable = "luadotnet_object";
        public const string NetTypeMetatable = "luadotnet_type";

        public static void CreateMetatables(IntPtr state) {
            LuaModule.Instance.LuaLNewMetatable(state, NetTypeMetatable);
            PushMetamethod("__call", TypeMetamethods["__call"]);
            LuaModule.Instance.LuaPop(state, 1);
            
            LuaModule.Instance.LuaLNewMetatable(state, NetObjectMetatable);
            LuaModule.Instance.LuaPop(state, 1);

            void PushMetamethod(string metamethod, LuaModule.FunctionSignatures.LuaCFunction luaCFunction) {
                LuaModule.Instance.LuaPushLString(state, metamethod);
                LuaModule.Instance.LuaPushCClosure(state, luaCFunction, 0);
                LuaModule.Instance.LuaSetTable(state, -3);
            }
        }

        private static int CallType(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var type = LuaModule.Instance.UserdataToNetObject(state, 1) as Type;
            if (type == null) {
                throw new LuaException("Attempt to instantiate a null type reference.");
            }
            
            var typeMetadata = type.GetOrCreateMetadata();
            var arguments = objectMarshal.GetObjects(state, 2, LuaModule.Instance.LuaGetTop(state));
            var constructor = TryResolveMethodCall(typeMetadata.Constructors, arguments, out var convertedArguments) as ConstructorInfo;
            if (constructor == null) {
                throw new LuaException($"No candidates for {type.Name}({string.Join(", ", arguments.Select(a => a.GetType().Name))})");
            }
            
            var result = constructor.Invoke(convertedArguments);
            objectMarshal.PushToStack(state, result);
            return 1;
        }

        private static int Gc(IntPtr luaState) {
            GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaModule.Instance.LuaToUserdata(luaState, -1))).Free();
            return 0;
        }
    }
}