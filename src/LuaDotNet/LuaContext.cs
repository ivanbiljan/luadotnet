﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using LuaDotNet.Attributes;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using LuaDotNet.Marshalling.Parsers;
using LuaDotNet.PInvoke;

namespace LuaDotNet
{
    /// <summary>
    ///     Represents an independent Lua context.
    /// </summary>
    [PublicAPI]
    public sealed class LuaContext : IDisposable
    {
        private readonly ObjectMarshal _objectMarshal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaContext" /> class.
        /// </summary>
        public LuaContext(bool openLibs = true)
        {
            State = LuaModule.Instance.LuaLNewState();
            if (openLibs)
            {
                LuaModule.Instance.LuaLOpenLibs(State);
            }

            ObjectMarshalPool.AddMarshal(this, _objectMarshal = new ObjectMarshal(this));
            Metamethods.CreateMetatables(State);

            RegisterFunction(
                "importType",
                typeof(LuaContext).GetMethod("ImportType", BindingFlags.Public | BindingFlags.Instance),
                this);
//            RegisterFunction("loadAssembly", typeof(LuaContext).GetMethod("LoadAssembly", BindingFlags.NonPublic | BindingFlags.Instance),
//                this);

            // TODO code below leaves unit tests hanging when executed in bulk
//            var exportedTypes = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.GetExportedTypes());
//            foreach (var type in exportedTypes) {
//                var globalAttribute = type.GetCustomAttribute<LuaGlobalAttribute>();
//                if (globalAttribute != null) {
//                    ImportType(State);
//                    continue;
//                }
//
//                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
//                    globalAttribute = method.GetCustomAttribute<LuaGlobalAttribute>();
//                    if (globalAttribute == null) {
//                        continue;
//                    }
//
//                    var name = globalAttribute.NameOverride ?? method.Name;
//                    SetGlobal(name, CreateFunction(method));
//                }
//            }
        }

        /// <summary>
        ///     Gets the Lua state associated with this context.
        /// </summary>
        public IntPtr State { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     The finalizer.
        /// </summary>
        ~LuaContext()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        ///     Creates and returns a new coroutine with the specified Lua function to execute.
        /// </summary>
        /// <param name="luaFunction">The Lua function which the coroutine will execute, which must not be <c>null</c>.</param>
        /// <returns>The coroutine.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="luaFunction" /> is <c>null</c>.</exception>
        public LuaCoroutine CreateCoroutine([NotNull] LuaFunction luaFunction)
        {
            if (luaFunction == null)
            {
                throw new ArgumentNullException(nameof(luaFunction));
            }

            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            var statePointer = LuaModule.Instance.LuaNewThread(State);
            luaFunction.PushToStack(State);
            LuaModule.Instance.LuaXMove(State, statePointer, 1);
            var coroutine = (LuaCoroutine)objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            return coroutine;
        }

        /// <summary>
        ///     Creates and returns a Lua function which once executed runs the specified delegate.
        /// </summary>
        /// <param name="delegate">The delegate, which must not be <c>null</c>.</param>
        /// <returns>The function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegate" /> is <c>null</c>.</exception>
        public LuaFunction CreateFunction([NotNull] Delegate @delegate)
        {
            if (@delegate == null)
            {
                throw new ArgumentNullException(nameof(@delegate));
            }

            return CreateFunction(@delegate.GetMethodInfo(), @delegate.Target);
        }

        /// <summary>
        ///     Creates and returns a Lua function which once executed runs the method represented by the specified object.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo" /> object that represents the method.</param>
        /// <param name="target">The class instance on which the method is invoked.</param>
        /// <returns>The function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="methodInfo" /> is <c>null</c>.</exception>
        public LuaFunction CreateFunction([NotNull] MethodInfo methodInfo, object target = null)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

//            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
//            var objectMarshalGetObjectMethod = typeof(ObjectMarshal).GetMethod("GetObject");
//            var objectMarshalPushObjectMethod = typeof(ObjectMarshal).GetMethod("PushToStack");
//
//            var luaStateParameter = Expression.Parameter(typeof(IntPtr));
//            var argumentExpressions = new List<Expression>();
//
//            var methodParameters = methodInfo.GetParameters();
//            for (var i = 0; i < methodParameters.Length; ++i) {
//                var parameter = methodParameters[i];
//                var getObjectCallExpression = Expression.Call(
//                    Expression.Constant(objectMarshal),
//                    objectMarshalGetObjectMethod,
//                    luaStateParameter,
//                    Expression.Constant(i + 1));
//                var coerceObjectCallExpression = Expression.Call(
//                    typeof(Utils).GetMethod("CoerceObjectMaybe"),
//                    getObjectCallExpression,
//                    Expression.Constant(parameter.ParameterType));
//                argumentExpressions.Add(Expression.Convert(coerceObjectCallExpression, parameter.ParameterType));
//            }
//
//            var methodCallExpression = Expression.Call(Expression.Convert(Expression.Constant(target), methodInfo.DeclaringType),
//                methodInfo, argumentExpressions);
//            var functionBody = new List<Expression>();
//            if (methodInfo.ReturnType == typeof(void)) {
//                functionBody.Add(methodCallExpression);
//                functionBody.Add(Expression.Constant(0));
//            }
//            else {
//                // TODO push ref/out parameters as well?
//                // In case of a non-void method we have to push the result of the method call to Lua's stack
//                functionBody.Add(Expression.Call(
//                    Expression.Constant(objectMarshal),
//                    objectMarshalPushObjectMethod,
//                    luaStateParameter,
//                    Expression.Convert(methodCallExpression, typeof(object))));
//                functionBody.Add(Expression.Constant(1));
//            }
//
//            var functionExpression = Expression.Block(functionBody.ToArray());
//            var luaCFunction = Expression.Lambda<LuaModule.FunctionSignatures.LuaCFunction>(functionExpression, luaStateParameter)
//                .Compile();
            return new LuaFunction(this, new MethodWrapper(methodInfo, target).Callback);
        }

        /// <summary>
        ///     Creates a new <see cref="LuaTable" /> with the specified size.
        /// </summary>
        /// <param name="numberOfSeqElements">The number of sequential elements.</param>
        /// <param name="numberOfOtherElements">The number of other elements.</param>
        /// <returns>The table.</returns>
        public LuaTable CreateTable(int numberOfSeqElements = 0, int numberOfOtherElements = 0)
        {
            numberOfSeqElements = Math.Max(0, numberOfSeqElements);
            numberOfOtherElements = Math.Max(0, numberOfOtherElements);
            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            LuaModule.Instance.LuaCreateTable(State, numberOfSeqElements, numberOfOtherElements);
            var table = (LuaTable)objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            return table;
        }

        /// <summary>
        ///     Loads the given Lua file and runs it.
        /// </summary>
        /// <param name="file">The Lua file.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file" /> is <c>null</c>.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="file" /> is invalid or not a .lua file.</exception>
        /// <exception cref="LuaException">Something went wrong while executing the file.</exception>
        public object[] DoFile([NotNull] string file, int numberOfResults = LuaModule.LuaMultRet)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!File.Exists(file) ||
                !Path.GetExtension(file).Equals(".lua", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new FileNotFoundException();
            }

            var errorCode = LuaModule.Instance.LuaLLoadString(
                State,
                File.ReadAllText(file).GetEncodedString(Encoding.UTF8));
            if (errorCode == LuaErrorCode.LuaOk)
            {
                return LuaModule.Instance.PCallKInternal(State, null, numberOfResults);
            }

            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            var errorMessage = (string)objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            throw new LuaException($"[{errorCode}]: {errorMessage}");
        }

        /// <summary>
        ///     Executes the specified Lua chunk and returns the results.
        /// </summary>
        /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
        /// <param name="numberOfResults">The number of results to return.</param>
        /// <returns>The chunk's results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
        public object[] DoString([NotNull] string luaChunk, int numberOfResults = LuaModule.LuaMultRet)
        {
            if (luaChunk == null)
            {
                throw new ArgumentNullException(nameof(luaChunk));
            }

            var errorCode = LuaModule.Instance.LuaLLoadString(State, luaChunk.GetEncodedString(Encoding.UTF8));
            if (errorCode == LuaErrorCode.LuaOk)
            {
                return LuaModule.Instance.PCallKInternal(State, numberOfResults: numberOfResults);
            }

            // Lua pushes an error message in case of errors
            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            var errorMessage = (string)objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            throw new LuaException($"[{errorCode}]: {errorMessage}");
        }

        /// <summary>
        ///     Returns the value of a global variable with the specified name.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        public object GetGlobal([NotNull] string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            LuaModule.Instance.LuaGetGlobal(State, name);
            var obj = ObjectMarshalPool.GetMarshal(State).GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            return obj;
        }

        public void ImportType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.Name != typeName && type.FullName != typeName)
                    {
                        continue;
                    }

                    if (type.GetCustomAttribute<LuaHideAttribute>() != null)
                    {
                        continue;
                    }

                    SetGlobal(type.Name, type);
                    Console.WriteLine(type.Name);
                }
            }
        }

        public void LoadAssembly(string name)
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFrom(name);
            }
            catch (FileNotFoundException)
            {
                // Swallow the exception and attempt to resolve the assembly using the AssemblyName
            }

            if (assembly == null)
            {
                Assembly.Load(AssemblyName.GetAssemblyName(name));
            }
        }

        /// <summary>
        ///     Loads the given Lua chunk into a <see cref="LuaFunction" />.
        /// </summary>
        /// <param name="luaChunk">The chunk to load, which must not be <c>null</c>.</param>
        /// <returns>A reusable Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
        /// <exception cref="LuaException">Something went wrong while loading the chunk.</exception>
        public LuaFunction LoadString([NotNull] string luaChunk)
        {
            if (luaChunk == null)
            {
                throw new ArgumentNullException(nameof(luaChunk));
            }

            var objectMarshal = ObjectMarshalPool.GetMarshal(State);
            if (LuaModule.Instance.LuaLLoadString(State, Encoding.UTF8.GetBytes(luaChunk)) != LuaErrorCode.LuaOk)
            {
                var errorMessage = (string)objectMarshal.GetObject(State, -1);
                LuaModule.Instance.LuaPop(State, 1);

                throw new LuaException($"An exception has occured while creating a function: {errorMessage}");
            }

            var function = (LuaFunction)objectMarshal.GetObject(State, -1);
            LuaModule.Instance.LuaPop(State, 1);

            return function;
        }

        /// <summary>
        ///     Registers a specified method as a global variable at the given path.
        /// </summary>
        /// <param name="path">The path, which must not be <c>null</c>.</param>
        /// <param name="method">The method, which must not be <c>null</c>.</param>
        /// <param name="target">The instance on which to invoke the method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> or <paramref name="method" /> is <c>null</c>.</exception>
        public void RegisterFunction([NotNull] string path, [NotNull] MethodInfo method, object target)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var oldTop = LuaModule.Instance.LuaGetTop(State);
            var function = CreateFunction(method, target);
            SetGlobal(path, function);
            LuaModule.Instance.LuaSetTop(State, oldTop);
        }

        /// <summary>
        ///     Registers a type parser for the specified type. This action will override any existing parsers.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <param name="typeParser">The parser, which must not be <c>null</c>.</param>
        public void RegisterTypeParser([NotNull] Type type, [NotNull] ITypeParser typeParser) =>
            ObjectMarshalPool.GetMarshal(State).RegisterTypeParser(type, typeParser);

        /// <summary>
        ///     Sets the value of the specified global variable.
        /// </summary>
        /// <param name="name">The name, which must not be <c>null</c>.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        public void SetGlobal([NotNull] string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            ObjectMarshalPool.GetMarshal(State).PushToStack(State, value);
            LuaModule.Instance.LuaSetGlobal(State, name);
        }

        private void ReleaseUnmanagedResources()
        {
            LuaModule.Instance.LuaClose(State);
        }
    }
}