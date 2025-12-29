using System;
using System.Collections.Generic;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

public sealed class ArrayParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        if (!Lua.LuaIsTable(state, stackIndex))
        {
            return null;
        }

        var objects = new List<object>();
        for (objectMarshal.PushToStack(state, null);
             Lua.LuaNext(state, -2) > 0;
             Lua.LuaPop(state, 1))
        {
            objects.Add(objectMarshal.GetObject(state, -1));
        }

        return objects.ToArray();
    }

    public void Push(IntPtr state, object obj)
    {
        var array = (Array) obj;
        var objectMarshal = ObjectMarshalPool.GetMarshal(state);
        Lua.LuaCreateTable(state, array.Length, 0);

        for (var i = 0; i < array.Length; ++i)
        {
            objectMarshal.PushToStack(state, array.GetValue(i));
            Lua.LuaRawSetI(state, -2, i + 1);
        }
    }
}