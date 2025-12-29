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
        var netObject = LuaModule.UserdataToNetObject(state, stackIndex);
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

        LuaModule.PushNetObjAsUserdata(state, obj);
        LuaModule.LuaGetField(state, (int) LuaRegistry.RegistryIndex, Metamethods.NetObjectMetatable);
        LuaModule.LuaSetMetatable(state, -2);
    }
}