using System;
using System.Collections.Generic;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling.Parsers;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling;

internal sealed class ObjectMarshal
{
    private readonly NetObjectParser _defaultNetObjectParser = new();
    private readonly LuaContext _lua;

    private readonly IDictionary<Type, Func<ITypeParser>> _typeParsers = new Dictionary<Type, Func<ITypeParser>>
    {
        [typeof(string)] = () => new StringParser(),
        [typeof(sbyte)] = () => new NumberParser(),
        [typeof(byte)] = () => new NumberParser(),
        [typeof(short)] = () => new NumberParser(),
        [typeof(int)] = () => new NumberParser(),
        [typeof(long)] = () => new NumberParser(),
        [typeof(ushort)] = () => new NumberParser(),
        [typeof(uint)] = () => new NumberParser(),
        [typeof(ulong)] = () => new NumberParser(),
        [typeof(float)] = () => new NumberParser(),
        [typeof(double)] = () => new NumberParser(),
        [typeof(bool)] = () => new BooleanParser(),
        [typeof(Array)] = () => new ArrayParser()
    };

    public ObjectMarshal(LuaContext lua)
    {
        _lua = lua ?? throw new ArgumentNullException(nameof(lua));
    }

    public object GetObject(IntPtr state, int stackIndex)
    {
        var luaType = LuaModule.LuaTypeExtImpl(state, stackIndex);
        var objectType = typeof(object);
        switch (luaType)
        {
            case LuaType.Nil:
                return null;
            case LuaType.Boolean:
                objectType = typeof(bool);

                break;
            case LuaType.LightUserdata:
                throw new NotSupportedException();
            case LuaType.Number:
                objectType = typeof(long);

                break;
            case LuaType.String:
                objectType = typeof(string);

                break;
            case LuaType.Table:
                //objectType = typeof(Array); Hmm
                return new LuaTable(_lua, GetRegistryReference());
            case LuaType.Function:
                return new LuaFunction(_lua, GetRegistryReference());
            case LuaType.Userdata:
                return LuaModule.UserdataToNetObject(state, stackIndex);
            case LuaType.Thread:
                return new LuaCoroutine(_lua, GetRegistryReference());
            default:
                throw new ArgumentOutOfRangeException();
        }

        var parser = _typeParsers.GetValueOrDefault(objectType);
        if (parser == null)
        {
            return _defaultNetObjectParser.Parse(state, stackIndex);
        }

        return parser().Parse(state, stackIndex);

        int GetRegistryReference()
        {
            LuaModule.LuaPushValue(state, stackIndex);

            return LuaModule.LuaLRef(state, (int) LuaRegistry.RegistryIndex);
        }
    }

    public object[] GetObjects(IntPtr state, int startIndex, int endIndex)
    {
        var numElements = endIndex - startIndex + 1 >= 0 ? endIndex - startIndex + 1 : 0;
        var objs = new object[numElements];
        for (var i = startIndex; i <= endIndex; ++i)
        {
            objs[i - startIndex] = GetObject(state, i);
        }

        return objs;
    }

    public void PushToStack(IntPtr state, object obj)
    {
        switch (obj)
        {
            case null:
                LuaModule.LuaPushNil(state);

                return;
            case LuaModule.LuaCFunction luaCFunction:
                LuaModule.LuaPushCClosure(state, luaCFunction, 0);

                return;
            case LuaObject luaObject:
                luaObject.PushToStack(state);

                return;
        }

        var objType = obj.GetType();
        var parser = _typeParsers.GetValueOrDefault(objType);
        if (parser == null)
        {
            _defaultNetObjectParser.Push(state, obj);
        }
        else
        {
            parser().Push(state, obj);
        }
    }

    public void RegisterTypeParser(Type type, ITypeParser typeParser)
    {
        if (typeParser == null)
        {
            throw new ArgumentNullException(nameof(typeParser));
        }

        _typeParsers[type] = () => typeParser;
    }
}