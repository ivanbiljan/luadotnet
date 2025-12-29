using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling.Parsers;

/// <summary>
///     Represents a default .NET object parser. This parser is used for all types that lack a type parser.
/// </summary>
public sealed class NetObjectParser : ITypeParser
{
    public object Parse(IntPtr state, int stackIndex)
    {
        var netObject = Lua.UserdataToNetObject(state, stackIndex);
        if (netObject is Type)
        {
            return new NetTypeTypeParser().Parse(state, stackIndex);
        }

        return null;
    }

    public void Push(IntPtr state, object obj)
    {
        if (obj is Type)
        {
            new NetTypeTypeParser().Push(state, obj);

            return;
        }

        Lua.PushNetObjAsUserdata(state, obj);
        Lua.LuaGetField(state, (int) LuaRegistry.RegistryIndex, Metamethods.NetObjectMetatable);
        Lua.LuaSetMetatable(state, -2);
    }
}