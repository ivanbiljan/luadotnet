using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;
using static LuaDotNet.Utils;

namespace LuaDotNet.Marshalling;

// https://github.com/NLua/NLua/blob/master/src/Metatables.cs
// TODO implement member cache
internal static class Metamethods
{
    public const string NetObjectMetatable = "luadotnet_object";
    public const string NetTypeMetatable = "luadotnet_type";

    private static readonly Dictionary<string, Lua.LuaCFunction> TypeMetamethods =
        new()
        {
            ["__gc"] = Gc,
            ["__tostring"] = ToString,
            ["__call"] = CallType,
            ["__index"] = GetTypeMember,
            ["__newindex"] = SetTypeMember
        };

    private static readonly Dictionary<string, Lua.LuaCFunction> ObjectMetamethods =
        new()
        {
            ["__gc"] = Gc,
            ["__tostring"] = ToString,
            ["__index"] = GetObjectMember,
            ["__newindex"] = SetObjectMember,
            ["__add"] = AddObjects,
            ["__sub"] = SubtractObjects,
            ["__mul"] = MultiplyObjects,
            ["__div"] = DivideObjects
        };

    public static void CreateMetatables(IntPtr state)
    {
        Lua.LuaLNewMetatable(state, NetTypeMetatable);
        PushMetamethod("__gc", TypeMetamethods["__gc"]);
        PushMetamethod("__tostring", TypeMetamethods["__tostring"]);
        PushMetamethod("__call", TypeMetamethods["__call"]);
        PushMetamethod("__index", TypeMetamethods["__index"]);
        PushMetamethod("__newindex", TypeMetamethods["__newindex"]);
        Lua.LuaPop(state, 1);

        Lua.LuaLNewMetatable(state, NetObjectMetatable);
        PushMetamethod("__gc", ObjectMetamethods["__gc"]);
        PushMetamethod("__tostring", ObjectMetamethods["__tostring"]);
        PushMetamethod("__index", ObjectMetamethods["__index"]);
        PushMetamethod("__newindex", ObjectMetamethods["__newindex"]);
        PushMetamethod("__add", ObjectMetamethods["__add"]);
        PushMetamethod("__sub", ObjectMetamethods["__sub"]);
        PushMetamethod("__mul", ObjectMetamethods["__mul"]);
        PushMetamethod("__div", ObjectMetamethods["__div"]);
        Lua.LuaPop(state, 1);

        void PushMetamethod(string metamethod, Lua.LuaCFunction luaCFunction)
        {
            Lua.LuaPushLString(state, metamethod);
            Lua.LuaPushCClosure(state, luaCFunction, 0);
            Lua.LuaSetTable(state, -3);
        }
    }

    private static int AddObjects(IntPtr state)
    {
        return HandleArithmeticMetamethod(state, "op_Addition");
    }

    private static int CallType(IntPtr state)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        if (Lua.UserdataToNetObject(state, 1) is not Type type)
        {
            return Lua.LuaError(state, "Attempt to instantiate a null type reference");
        }

        var typeMetadata = type.GetOrCreateMetadata();
        var arguments = objectMarshal.GetObjects(state, 2, Lua.LuaGetTop(state));
        var constructor = PickOverload(typeMetadata.Constructors, arguments, out var convertedArguments);
        if (constructor == null)
        {
            return Lua.LuaError(
                state,
                $"No candidates for {type.Name}({string.Join(", ", arguments.Select(a => a.GetType().Name))})"
            );
        }

        var result = constructor.Invoke(convertedArguments);
        objectMarshal.PushToStack(state, result);

        return 1;
    }

    private static int DivideObjects(IntPtr state)
    {
        return HandleArithmeticMetamethod(state, "op_Division");
    }

    private static int Gc(IntPtr state)
    {
        GCHandle.FromIntPtr(Marshal.ReadIntPtr(Lua.LuaToUserdata(state, 1))).Free();

        return 0;
    }

    private static int GetMember(IntPtr state, object obj, string memberName, bool isStaticSearch)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var objType = obj is Type type ? type : obj.GetType();
        var typeMetadata = objType.GetOrCreateMetadata();
        var members = typeMetadata.GetMembers(memberName, !isStaticSearch).ToArray();
        if (members.Length == 0)
        {
            return Lua.LuaError(state, $"Invalid member '{memberName}'");
        }

        obj = isStaticSearch ? null : obj;
        var member = members[0];
        switch (member.MemberType)
        {
            case MemberTypes.Event:
                objectMarshal.PushToStack(state, new RegisterEventHandler((EventInfo) member, obj));

                break;
            case MemberTypes.Field:
                try
                {
                    var field = (FieldInfo) member;
                    objectMarshal.PushToStack(state, field.GetValue(obj));

                    return 1;
                }
                catch (TargetInvocationException ex)
                {
                    return Lua.LuaError(state, $"An exception has occured while indexing a type's field: {ex}");
                }
            case MemberTypes.Method:
                try
                {
                    var wrapper = new MethodWrapper(memberName, objType, obj);
                    Lua.LuaPushCClosure(state, wrapper.Callback, 0);

                    return 1;
                }
                catch (TargetInvocationException ex)
                {
                    return Lua.LuaError(state, $"An exception has occured while indexing a type's method: {ex}");
                }
            case MemberTypes.NestedType:
                // TODO
                break;
            case MemberTypes.Property:
                var property = (PropertyInfo) member;
                try
                {
                    objectMarshal.PushToStack(state, property.GetValue(obj, null));

                    return 1;
                }
                catch (ArgumentException)
                {
                    // TODO recurse back to base classes
                }
                catch (TargetInvocationException ex)
                {
                    return Lua.LuaError(state, $"An exception has occured while indexing a type's property: {ex}");
                }

                break;
        }

        return 0;
    }

    private static int GetObjectMember(IntPtr state)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var obj = objectMarshal.GetObject(state, 1);
        if (obj == null)
        {
            return Lua.LuaError(state, "Attempt to index a null object reference.");
        }

        if (!(objectMarshal.GetObject(state, 2) is string memberName))
        {
            return Lua.LuaError(state, "Expected a proper member name.");
        }

        return GetMember(state, obj, memberName, false);
    }

    private static int GetTypeMember(IntPtr state)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var type = objectMarshal.GetObject(state, 1) as Type;
        if (type == null)
        {
            return Lua.LuaError(state, "Attempt to index a null type reference.");
        }

        if (!(objectMarshal.GetObject(state, 2) is string memberName))
        {
            return Lua.LuaError(state, "Expected a proper member name.");
        }

        return GetMember(state, type, memberName, true);
    }

    private static int HandleArithmeticMetamethod(IntPtr state, string opMethodName)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var firstOperand = objectMarshal.GetObject(state, 1);
        var secondOperand = objectMarshal.GetObject(state, 2);
        if (firstOperand == null && secondOperand == null)
        {
            return Lua.LuaError(state, "Cannot perform arithmetic operations on nil objects.");
        }

        var arguments = new[]
        {
            firstOperand, secondOperand
        };

        var method = PickOverload(
            [
                firstOperand?.GetType().GetOrCreateMetadata().GetMethods(opMethodName).ElementAtOrDefault(0),
                secondOperand?.GetType().GetOrCreateMetadata().GetMethods(opMethodName).ElementAtOrDefault(0)
            ],
            arguments,
            out _
        );

        if (method == null)
        {
            return Lua.LuaError(
                state,
                $"Attempt to perform an arithmetic operation on operands that do not overload the '{opMethodName}' operator."
            );
        }

        object result;
        try
        {
            result = method.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex)
        {
            return Lua.LuaError(state, $"An exception has occured while executing an operator method: {ex}");
        }

        objectMarshal.PushToStack(state, result);

        return 1;
    }

    private static int MultiplyObjects(IntPtr state)
    {
        return HandleArithmeticMetamethod(state, "op_Multiply");
    }

    private static int SetMember(IntPtr state, object obj, string memberName, bool isStaticSearch)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var objType = obj is Type type ? type : obj.GetType();
        var typeMetadata = objType.GetOrCreateMetadata();
        var members = typeMetadata.GetMembers(memberName, !isStaticSearch).ToArray();
        if (members.Length == 0)
        {
            return Lua.LuaError(state, $"Invalid member '{memberName}'");
        }

        obj = isStaticSearch ? null : obj;
        var member = members[0];
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                try
                {
                    var field = (FieldInfo) member;
                    var value = objectMarshal.GetObject(state, 3);
                    field.SetValue(obj, value);
                }
                catch (Exception ex)
                {
                    return Lua.LuaError(state, $"An exception has occured while modifying a field: {ex}");
                }

                break;
            case MemberTypes.Property:
                try
                {
                    var property = (PropertyInfo) member;
                    if (property.GetIndexParameters().Length > 0)
                    {
                        return Lua.LuaError(state, "Attempt to modify the value of an indexer.");
                    }

                    var value = objectMarshal.GetObject(state, 3);
                    property.SetValue(obj, value, null);
                }
                catch (Exception ex)
                {
                    return Lua.LuaError(state, $"An exception has occured while modifying a field: {ex}");
                }

                break;
            default:
                return Lua.LuaError(state, "Member is not a .NET field or property.");
        }

        return 0;
    }

    private static int SetObjectMember(IntPtr state)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var obj = objectMarshal.GetObject(state, 1);
        if (obj == null)
        {
            return Lua.LuaError(state, "Attempt to call __newindex on a null object reference.");
        }

        if (!(objectMarshal.GetObject(state, 2) is string memberName))
        {
            return Lua.LuaError(state, "Expected a proper member name.");
        }

        return SetMember(state, obj, memberName, false);
    }

    private static int SetTypeMember(IntPtr state)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        var type = objectMarshal.GetObject(state, 1) as Type;
        if (type == null)
        {
            return Lua.LuaError(state, "Attempt to call __newindex on a null type reference.");
        }

        if (!(objectMarshal.GetObject(state, 2) is string memberName))
        {
            return Lua.LuaError(state, "Expected a proper member name.");
        }

        return SetMember(state, type, memberName, true);
    }

    private static int SubtractObjects(IntPtr state)
    {
        return HandleArithmeticMetamethod(state, "op_Subtraction");
    }

    private static int ToString(IntPtr state)
    {
        var obj = Lua.UserdataToNetObject(state, 1);
        if (obj == null)
        {
            Lua.LuaPushNil(state);
        }
        else
        {
            Lua.LuaPushLString(state, obj.ToString());
        }

        return 1;
    }
}