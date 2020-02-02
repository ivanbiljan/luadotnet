using System;

namespace LuaDotNet.Marshalling {
    public abstract class TypeParserBase : ITypeParser {
        protected LuaContext LuaContext;

        protected TypeParserBase(LuaContext lua) {
            LuaContext = lua ?? throw new ArgumentNullException(nameof(lua));
        }

        public abstract object Parse(IntPtr state, int stackIndex);
        public abstract void Push(object obj, IntPtr state);
    }
}