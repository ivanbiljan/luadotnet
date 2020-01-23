using System;
using System.Text;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
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
            _objectMarshal = new ObjectMarshal();
        }

        /// <summary>
        ///     Gets the Lua state associated with this context.
        /// </summary>
        public IntPtr State { get; }

        /// <summary>
        ///     Executes the specified Lua chunk and returns the results.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The chunk's results.</returns>
        public object[] DoString(string luaChunk, int numberOfResults = LuaModule.LuaMultRet)
        {
            LuaErrorCode errorCode;
            if ((errorCode = LuaModule.Instance.LuaLLoadString(State, luaChunk.GetEncodedString(Encoding.UTF8))) !=
                LuaErrorCode.LuaOk)
            {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) _objectMarshal.GetObject(State, -1);
                LuaModule.Instance.LuaPop(State, 1); // Pop the error message and throw an exception
                throw new LuaException($"[{errorCode}]: {errorMessage}");
            }

            return CallWithArguments(numberOfResults: numberOfResults);
        }

        /// <summary>
        ///     Returns the value of a global variable with the specified name.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <returns>The value.</returns>
        public object GetGlobal(string name)
        {
            LuaModule.Instance.LuaGetGlobal(State, name);
            return _objectMarshal.GetObject(State, -1);
        }

        /// <summary>
        ///     Registers a type parser for the specified type. This action will override any existing parsers.
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
            _objectMarshal.PushToStack(State, value);
            LuaModule.Instance.LuaSetGlobal(State, name);
        }

        private object[] CallWithArguments(object[] arguments = null, int numberOfResults = LuaModule.LuaMultRet)
        {
            // The function (which is currently at the top of the stack) gets popped along with the arguments when it's called
            var stackTop = LuaModule.Instance.LuaGetTop(State) - 1;

            // The function is already on the stack so the only thing left to do is push the arguments in direct order
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    _objectMarshal.PushToStack(State, argument);
                }
            }

            // Adjust the number of results to avoid errors
            numberOfResults = numberOfResults < -1 ? -1 : numberOfResults;
            LuaErrorCode errorCode;
            if ((errorCode = LuaModule.Instance.LuaPCallK(State, arguments?.Length ?? 0, numberOfResults)) !=
                LuaErrorCode.LuaOk)
            {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) _objectMarshal.GetObject(State, -1);
                LuaModule.Instance.LuaPop(State, 1); // Pop the error message and throw an exception
                throw new LuaException(
                    $"An exception has occured while calling a function: [{errorCode}]: {errorMessage}");
            }

            var newStackTop = LuaModule.Instance.LuaGetTop(State);
            var results = new object[newStackTop - stackTop];
            for (var i = newStackTop; i > stackTop; --i) // Results are also pushed in direct order
            {
                results[i - stackTop - 1] = _objectMarshal.GetObject(State, i);
            }

            LuaModule.Instance.LuaSetTop(State, stackTop);
            return results;
        }
    }
}