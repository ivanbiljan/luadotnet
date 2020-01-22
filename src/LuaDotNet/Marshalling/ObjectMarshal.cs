using System;
using System.Collections.Generic;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    internal sealed class ObjectMarshal
    {
        private readonly LuaContext _lua;

        private readonly IDictionary<Type, ITypeParser> _typeParsers = new Dictionary<Type, ITypeParser>
        {
            [typeof(string)] = new StringParser(),
            [typeof(long)] = new NumberParser() // This works for both integers and floating-point numbers
        };

        public ObjectMarshal(LuaContext lua)
        {
            _lua = lua ?? throw new ArgumentNullException(nameof(lua));
        }

        public object GetObject(int stackIndex)
        {
            var luaType = LuaModule.Instance.LuaType(_lua.State, stackIndex);
            if (luaType == LuaType.String)
            {
                return _typeParsers[typeof(string)].Parse(_lua.State, stackIndex);
            }

            var objectType = typeof(object);
            switch (luaType)
            {
                case LuaType.Nil:
                    return null;
                case LuaType.Boolean:
                    objectType = typeof(bool);
                    break;
                case LuaType.LightUserdata:
                    break;
                case LuaType.Number:
                    objectType = typeof(long);
                    break;
                case LuaType.String:
                    objectType = typeof(string);
                    break;
                case LuaType.Table:
                    break;
                case LuaType.Function:
                    break;
                case LuaType.Userdata:
                    break;
                case LuaType.Thread:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var parser = _typeParsers.GetValueOrDefault(objectType);
            if (parser == null)
            {
                throw new LuaException($"Missing parser for type '{objectType.Name}'");
            }

            return parser.Parse(_lua.State, stackIndex);
        }

        public void PushToStack(object obj)
        {
            if (obj == null)
            {
                LuaModule.Instance.LuaPushNil(_lua.State);
                return;
            }

            var objType = obj.GetType();
            var parser = _typeParsers[objType];
            if (parser == null)
            {
                throw new LuaException($"Missing parser for type '{objType.Name}'");
            }

            parser.Push(obj, _lua.State);
        }

        public void RegisterTypeParser(Type type, ITypeParser typeParser)
        {
            _typeParsers[type] = typeParser ?? throw new ArgumentNullException(nameof(typeParser));
        }
    }
}