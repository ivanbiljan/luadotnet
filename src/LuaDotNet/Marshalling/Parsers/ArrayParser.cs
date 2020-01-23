using System;
using System.Collections.Generic;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    public sealed class ArrayParser : ITypeParser
    {
        private readonly ObjectMarshal _objectMarshal = new ObjectMarshal();

        public object Parse(IntPtr state, int stackIndex)
        {
            if (!LuaModule.Instance.LuaIsTable(state, stackIndex))
            {
                return null;
            }

            var objects = new List<object>();
            for (_objectMarshal.PushToStack(state, null);
                LuaModule.Instance.LuaNext(state, -2) > 0;
                LuaModule.Instance.LuaPop(state, 1))
            {
                objects.Add(_objectMarshal.GetObject(state, -1));
            }

            return objects.ToArray();
        }

        public void Push(object obj, IntPtr state)
        {
            var array = (Array) obj;
            LuaModule.Instance.LuaCreateTable(state, array.Length, 0);

            for (var i = 0; i < array.Length; ++i)
            {
                _objectMarshal.PushToStack(state, array.GetValue(i));
                LuaModule.Instance.LuaRawSetI(state, -2, i + 1);
            }
        }

//        public ArrayParser(LuaContext lua) : base(lua)
//        {
//            _objectMarshal = new ObjectMarshal(lua);
//        }
    }
}