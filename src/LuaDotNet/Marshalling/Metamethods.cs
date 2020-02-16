﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;
using static LuaDotNet.Utils;

namespace LuaDotNet.Marshalling {
    // https://github.com/NLua/NLua/blob/master/src/Metatables.cs
    // TODO implement member cache
    internal static class Metamethods {
        public const string NetObjectMetatable = "luadotnet_object";
        public const string NetTypeMetatable = "luadotnet_type";

        private static readonly Dictionary<string, LuaModule.FunctionSignatures.LuaCFunction> TypeMetamethods =
            new Dictionary<string, LuaModule.FunctionSignatures.LuaCFunction> {
                ["__gc"] = Gc,
                ["__tostring"] = ToString,
                ["__call"] = CallType,
                ["__index"] = GetTypeMember,
                ["__newindex"] = SetTypeMember
            };

        private static readonly Dictionary<string, LuaModule.FunctionSignatures.LuaCFunction> ObjectMetamethods =
            new Dictionary<string, LuaModule.FunctionSignatures.LuaCFunction> {
                ["__gc"] = Gc,
                ["__tostring"] = ToString,
                ["__index"] = GetObjectMember,
                ["__newindex"] = SetObjectMember
            };


        public static void CreateMetatables(IntPtr state) {
            LuaModule.Instance.LuaLNewMetatable(state, NetTypeMetatable);
            PushMetamethod("__gc", TypeMetamethods["__gc"]);
            PushMetamethod("__call", TypeMetamethods["__call"]);
            PushMetamethod("__index", TypeMetamethods["__index"]);
            LuaModule.Instance.LuaPop(state, 1);

            LuaModule.Instance.LuaLNewMetatable(state, NetObjectMetatable);
            PushMetamethod("__gc", ObjectMetamethods["__gc"]);
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
            var constructor = ResolveMethod(typeMetadata.Constructors, arguments, out var convertedArguments) as ConstructorInfo;
            if (constructor == null) {
                throw new LuaException($"No candidates for {type.Name}({string.Join(", ", arguments.Select(a => a.GetType().Name))})");
            }

            var result = constructor.Invoke(convertedArguments);
            objectMarshal.PushToStack(state, result);
            return 1;
        }

        private static int Gc(IntPtr state) {
            GCHandle.FromIntPtr(Marshal.ReadIntPtr(LuaModule.Instance.LuaToUserdata(state, 1))).Free();
            return 0;
        }

        private static int GetTypeMember(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var type = objectMarshal.GetObject(state, 1) as Type;
            if (type == null) {
                throw new LuaException("Attempt to index a null type reference.");
            }

            if (!(objectMarshal.GetObject(state, 2) is string memberName)) {
                throw new LuaException("Expected a proper member name.");
            }

            return GetMember(state, type, memberName, true);
        }

        private static int GetObjectMember(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var obj = objectMarshal.GetObject(state, 1);
            if (obj == null) {
                throw new LuaException("Attempt to index a null object reference.");
            }

            if (!(objectMarshal.GetObject(state, 2) is string memberName)) {
                throw new LuaException("Expected a proper member name.");
            }

            return GetMember(state, obj, memberName, false);
        }

        private static int GetMember(IntPtr state, object obj, string memberName, bool isStaticSearch) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var objType = obj is Type type ? type : obj.GetType();
            var typeMetadata = objType.GetOrCreateMetadata();
            var members = typeMetadata.GetMembers(memberName, !isStaticSearch).ToArray();
            if (members.Length == 0) {
                throw new LuaException($"Invalid member '{memberName}'");
            }

            obj = isStaticSearch ? null : obj;
            var member = members[0];
            switch (member.MemberType) {
                case MemberTypes.Event: // TODO
                    break;
                case MemberTypes.Field:
                    try {
                        var field = (FieldInfo) member;
                        objectMarshal.PushToStack(state, field.GetValue(obj));
                        return 1;
                    }
                    catch (TargetInvocationException ex) {
                        throw new LuaException($"An exception has occured while indexing a type's field: {ex}");
                    }
                case MemberTypes.Method:
                    try {
                        var wrapper = new MethodWrapper(memberName, objType, obj);
                        LuaModule.Instance.LuaPushCClosure(state, wrapper.Callback, 0);
                        return 1;
                    }
                    catch (TargetInvocationException ex) {
                        throw new LuaException($"An exception has occured while indexing a type's method: {ex}");
                    }
                case MemberTypes.NestedType:
                    // TODO
                    break;
                case MemberTypes.Property:
                    var property = (PropertyInfo) member;
                    try {
                        objectMarshal.PushToStack(state, property.GetValue(obj, null));
                        return 1;
                    }
                    catch (ArgumentException) {
                        // TODO recurse back to base classes
                    }
                    catch (TargetInvocationException ex) {
                        throw new LuaException($"An exception has occured while indexing a type's property: {ex}");
                    }

                    break;
            }

            return 0;
        }

        private static int SetTypeMember(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var type = objectMarshal.GetObject(state, 1);
            if (type == null) {
                throw new LuaException("Attempt to call __newindex on a null object reference.");
            }

            if (!(objectMarshal.GetObject(state, 2) is string memberName)) {
                throw new LuaException("Expected a proper member name.");
            }

            return SetMember(state, type, memberName, true);
        }

        private static int SetObjectMember(IntPtr state) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var obj = objectMarshal.GetObject(state, 1) as Type;
            if (obj == null) {
                throw new LuaException("Attempt to call __newindex on a null type reference.");
            }

            if (!(objectMarshal.GetObject(state, 2) is string memberName)) {
                throw new LuaException("Expected a proper member name.");
            }

            return SetMember(state, obj, memberName, false);
        }

        private static int SetMember(IntPtr state, object obj, string memberName, bool isStaticSearch) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var objType = obj is Type type ? type : obj.GetType();
            var typeMetadata = objType.GetOrCreateMetadata();
            var members = typeMetadata.GetMembers(memberName, !isStaticSearch).ToArray();
            if (members.Length == 0) {
                throw new LuaException($"Invalid member '{memberName}'");
            }
            
            obj = isStaticSearch ? null : obj;
            var member = members[0];
            switch (member.MemberType) {
                case MemberTypes.Field:
                    try {
                        var field = (FieldInfo) member;
                        var value = objectMarshal.GetObject(state, 3);
                        field.SetValue(obj, value);
                    }
                    catch (Exception ex) {
                        throw new LuaException($"An exception has occured while modifying a field: {ex}");
                    }
                    break;
                case MemberTypes.Property:
                    try {
                        var property = (PropertyInfo) member;
                        if (property.GetIndexParameters().Length > 0) {
                            throw new LuaException("Attempt to modify the value of an indexer.");
                        }
                        
                        var value = objectMarshal.GetObject(state, 3);
                        property.SetValue(obj, value, null);
                    }
                    catch (Exception ex) {
                        throw new LuaException($"An exception has occured while modifying a field: {ex}");
                    }
                    break;
                default:
                    throw new LuaException($"Member is not a .NET field or property.");
            }

            return 0;
        }

        private static int ToString(IntPtr state) {
            var obj = LuaModule.Instance.UserdataToNetObject(state, 1);
            if (obj == null) {
                LuaModule.Instance.LuaPushNil(state);
            }
            else {
                LuaModule.Instance.LuaPushLString(state, obj.ToString());
            }

            return 1;
        }
    }
}