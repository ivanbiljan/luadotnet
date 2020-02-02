using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet {
    /// <summary>
    ///     Represents an independent Lua context.
    /// </summary>
    public sealed class LuaContext : IDisposable {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaContext" /> class.
        /// </summary>
        public LuaContext() {
            State = LuaModule.Instance.LuaLNewState();
            ObjectMarshalPool.AddMarshal(this, new ObjectMarshal(this));
            Metamethods.CreateMetatables(State);
        }

        /// <summary>
        ///     Gets the Lua state associated with this context.
        /// </summary>
        public IntPtr State { get; }

        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     The finalizer.
        /// </summary>
        ~LuaContext() {
            ReleaseUnmanagedResources();
        }

        public LuaFunction CreateFunction(Delegate @delegate) => throw new NotImplementedException();

        public LuaFunction CreateFunction(MethodInfo methodInfo, object target = null) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            var luaStateParameter = Expression.Parameter(typeof(IntPtr));
            var argumentExpressions = new List<Expression>();
            var methodParameters = methodInfo.GetParameters();
            for (var i = 0; i < methodParameters.Length; ++i) {
                var parameter = methodParameters[i];
                var getObjectCallExpression = Expression.Call(Expression.Constant(objectMarshal),
                    typeof(ObjectMarshal).GetMethod("GetObject"), luaStateParameter, Expression.Constant(i + 1));
                argumentExpressions.Add(Expression.Convert(getObjectCallExpression, parameter.ParameterType));
            }

            var methodCallExpression = Expression.Call(Expression.Constant(target), methodInfo, argumentExpressions);
            var functionBody = new List<Expression> {methodCallExpression, Expression.Constant(0)};
            var function = Expression
                .Lambda<LuaModule.FunctionSignatures.LuaCFunction>(Expression.Block(functionBody.ToArray()),
                    luaStateParameter).Compile();
            return new LuaFunction(function, this);
        }

        /// <summary>
        ///     Executes the specified Lua chunk and returns the results.
        /// </summary>
        /// <param name="luaState">The Lua state pointer.</param>
        /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The chunk's results.</returns>
        public object[] DoString(string luaChunk, int numberOfResults = LuaModule.LuaMultRet) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(State);

            LuaErrorCode errorCode;
            if ((errorCode = LuaModule.Instance.LuaLLoadString(State, luaChunk.GetEncodedString(Encoding.UTF8))) !=
                LuaErrorCode.LuaOk) {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) objectMarshal.GetObject(State, -1);
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
        public object GetGlobal(string name) {
            LuaModule.Instance.LuaGetGlobal(State, name);
            return ObjectMarshalPool.GetMarshal(State).GetObject(State, -1);
        }

        /// <summary>
        ///     Loads the given Lua chunk into a <see cref="LuaFunction" />.
        /// </summary>
        /// <param name="luaChunk">The chunk to load, which must not be <c>null</c>.</param>
        /// <returns>A reusable Lua function.</returns>
        /// <exception cref="LuaException">Something went wrong while loading the chunk.</exception>
        public LuaFunction LoadString(string luaChunk) {
            if (luaChunk == null) {
                throw new ArgumentNullException(nameof(luaChunk));
            }

            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            if (LuaModule.Instance.LuaLLoadString(State, Encoding.UTF8.GetBytes(luaChunk)) != LuaErrorCode.LuaOk) {
                var errorMessage = (string) objectMarshal.GetObject(State, -1);
                LuaModule.Instance.LuaPop(State, 1);
                throw new LuaException($"An exception has occured while creating a function: {errorMessage}");
            }

            var function = (LuaFunction) objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);
            return function;
        }

        /// <summary>
        ///     Registers a type parser for the specified type. This action will override any existing parsers.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <param name="typeParser">The parser, which must not be <c>null</c>.</param>
        public void RegisterTypeParser(Type type, ITypeParser typeParser) =>
            ObjectMarshalPool.GetMarshal(State).RegisterTypeParser(type, typeParser);

        /// <summary>
        ///     Sets the value of the specified global variable.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <param name="value">The value.</param>
        public void SetGlobal(string name, object value) {
            ObjectMarshalPool.GetMarshal(State).PushToStack(State, value);
            LuaModule.Instance.LuaSetGlobal(State, name);
        }

        internal object[] CallWithArguments(IReadOnlyCollection<object> arguments = null,
            int numberOfResults = LuaModule.LuaMultRet) {
            // The function (which is currently at the top of the stack) gets popped along with the arguments when it's called
            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            var stackTop = LuaModule.Instance.LuaGetTop(State) - 1;

            // The function is already on the stack so the only thing left to do is push the arguments in direct order
            if (arguments != null) {
                foreach (var argument in arguments) {
                    objectMarshal.PushToStack(State, argument);
                }
            }

            // Adjust the number of results to avoid errors
            numberOfResults = numberOfResults < -1 ? -1 : numberOfResults;
            LuaErrorCode errorCode;
            if ((errorCode = LuaModule.Instance.LuaPCallK(State, arguments?.Count ?? 0, numberOfResults)) !=
                LuaErrorCode.LuaOk) {
                // Lua pushes an error message in case of errors
                var errorMessage = (string) objectMarshal.GetObject(State, -1);
                LuaModule.Instance.LuaPop(State, 1); // Pop the error message and throw an exception
                throw new LuaException(
                    $"An exception has occured while calling a function: [{errorCode}]: {errorMessage}");
            }

            var newStackTop = LuaModule.Instance.LuaGetTop(State);
            var results = new object[newStackTop - stackTop];
            for (var i = newStackTop; i > stackTop; --i) // Results are also pushed in direct order
            {
                results[i - stackTop - 1] = objectMarshal.GetObject(State, i);
            }

            LuaModule.Instance.LuaSetTop(State, stackTop);
            return results;
        }

        private void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
            LuaModule.Instance.LuaClose(State);
        }
    }
}