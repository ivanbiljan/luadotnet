using System;

namespace LuaDotNet.Marshalling.Parsers;

public abstract class TypeParserBase(LuaContext lua) : ITypeParser
{
    protected LuaContext LuaContext = lua ?? throw new ArgumentNullException(nameof(lua));

    public abstract object Parse(IntPtr state, int stackIndex);

    public abstract void Push(IntPtr state, object obj);
}