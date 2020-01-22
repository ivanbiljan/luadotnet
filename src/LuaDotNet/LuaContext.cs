using System;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet
{
    /// <summary>
    ///     Represents an independent Lua context.
    /// </summary>
    public sealed class LuaContext
    {
        private readonly ObjectMarshal _objectMarshal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaContext" /> class.
        /// </summary>
        public LuaContext()
        {
            State = LuaModule.Instance.LuaLNewState();
            _objectMarshal = new ObjectMarshal(this);
        }

        /// <summary>
        ///     Gets the Lua state associated with this context.
        /// </summary>
        public IntPtr State { get; }

        /// <summary>
        ///     Returns the value of a global variable with the specified name.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <returns>The value.</returns>
        public object GetGlobal(string name)
        {
            LuaModule.Instance.LuaGetGlobal(State, name);
            return _objectMarshal.GetObject(-1);
        }
        
        /// <summary>
        /// Registers a type parser for the specified type. This action will override any existing parsers.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <param name="typeParser">The parser, which must not be <c>null</c>.</param>
        public void RegisterTypeParser(Type type, ITypeParser typeParser) =>
            _objectMarshal.RegisterTypeParser(type, typeParser);

        /// <summary>
        ///     Sets the value of the specified global variable.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <param name="value">The value.</param>
        public void SetGlobal(string name, object value)
        {
            _objectMarshal.PushToStack(value);
            LuaModule.Instance.LuaSetGlobal(State, name);
        }
    }
}