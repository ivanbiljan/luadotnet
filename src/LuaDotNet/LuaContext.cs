using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LuaDotNet.Attributes;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using LuaDotNet.Marshalling.Parsers;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents an independent Lua context.
/// </summary>
public sealed class LuaContext : IDisposable
{
    private readonly ObjectMarshal _objectMarshal;
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="LuaContext" /> class.
    /// </summary>
    public LuaContext(bool openLibs = true)
    {
        State = Lua.LuaLNewState();
        if (openLibs)
        {
            Lua.LuaLOpenLibs(State);
        }

        ObjectMarshalPool.AddMarshal(this, _objectMarshal = new ObjectMarshal(this));
        Metamethods.CreateMetatables(State);

        RegisterFunction(
            "importType",
            typeof(LuaContext).GetMethod(nameof(ImportType), BindingFlags.Public | BindingFlags.Instance)!,
            this
        );
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
    public LuaCoroutine CreateCoroutine(LuaFunction luaFunction)
    {
        ArgumentNullException.ThrowIfNull(luaFunction);

        var statePointer = Lua.LuaNewThread(State);
        luaFunction.PushToStack(State);
        Lua.LuaXMove(State, statePointer, 1);
        var coroutine = (LuaCoroutine) _objectMarshal.GetObject(State, -1)!;
        Lua.LuaPop(State, 1);

        return coroutine;
    }

    /// <summary>
    ///     Creates and returns a Lua function which once executed runs the specified delegate.
    /// </summary>
    /// <param name="delegate">The delegate, which must not be <c>null</c>.</param>
    /// <returns>The function.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="delegate" /> is <c>null</c>.</exception>
    public LuaFunction CreateFunction(Delegate @delegate)
    {
        ArgumentNullException.ThrowIfNull(@delegate);

        return CreateFunction(@delegate.GetMethodInfo(), @delegate.Target!);
    }

    /// <summary>
    ///     Creates and returns a Lua function which once executed runs the method represented by the specified object.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo" /> object that represents the method.</param>
    /// <param name="target">The class instance on which the method is invoked.</param>
    /// <returns>The function.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="methodInfo" /> is <c>null</c>.</exception>
    public LuaFunction CreateFunction(MethodInfo methodInfo, object target = null)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

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
        Lua.LuaCreateTable(State, numberOfSeqElements, numberOfOtherElements);
        var table = (LuaTable) _objectMarshal.GetObject(State, -1)!;
        Lua.LuaPop(State, 1);

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
    public object[] DoFile(string file, int numberOfResults = Lua.LuaMultRet)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (!File.Exists(file) ||
            !Path.GetExtension(file).Equals(".lua", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new FileNotFoundException();
        }

        var errorCode = Lua.LuaLLoadString(
            State,
            File.ReadAllText(file).GetEncodedString(Encoding.UTF8)
        );

        if (errorCode == LuaErrorCode.LuaOk)
        {
            return Lua.PCallKInternal(State, null, numberOfResults);
        }

        var errorMessage = (string) _objectMarshal.GetObject(State, -1)!;
        Lua.LuaPop(State, 1);

        throw new LuaException($"[{errorCode}]: {errorMessage}");
    }

    /// <summary>
    ///     Executes the specified Lua chunk and returns the results.
    /// </summary>
    /// <param name="luaChunk">The Lua chunk to execute, which must not be <c>null</c>.</param>
    /// <param name="numberOfResults">The number of results to return.</param>
    /// <returns>The chunk's results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="luaChunk" /> is <c>null</c>.</exception>
    public object[] DoString(string luaChunk, int numberOfResults = Lua.LuaMultRet)
    {
        ArgumentNullException.ThrowIfNull(luaChunk);

        var errorCode = Lua.LuaLLoadString(State, luaChunk.GetEncodedString(Encoding.UTF8));
        if (errorCode == LuaErrorCode.LuaOk)
        {
            return Lua.PCallKInternal(State, numberOfResults: numberOfResults);
        }

        // Lua pushes an error message in case of errors
        var errorMessage = (string) _objectMarshal.GetObject(State, -1)!;
        Lua.LuaPop(State, 1);

        throw new LuaException($"[{errorCode}]: {errorMessage}");
    }

    /// <summary>
    ///     Returns the value of a global variable with the specified name.
    /// </summary>
    /// <param name="name">The name, which must not be <c>null</c>.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
    public object? GetGlobal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Lua.LuaGetGlobal(State, name);
        var obj = _objectMarshal.GetObject(State, -1);
        Lua.LuaPop(State, 1);

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
    public LuaFunction LoadString(string luaChunk)
    {
        ArgumentNullException.ThrowIfNull(luaChunk);

        if (Lua.LuaLLoadString(State, Encoding.UTF8.GetBytes(luaChunk)) != LuaErrorCode.LuaOk)
        {
            var errorMessage = (string) _objectMarshal.GetObject(State, -1)!;
            Lua.LuaPop(State, 1);

            throw new LuaException($"An exception has occured while creating a function: {errorMessage}");
        }

        var function = (LuaFunction) _objectMarshal.GetObject(State, -1)!;
        Lua.LuaPop(State, 1);

        return function;
    }

    /// <summary>
    ///     Registers a specified method as a global variable at the given path.
    /// </summary>
    /// <param name="path">The path, which must not be <c>null</c>.</param>
    /// <param name="method">The method, which must not be <c>null</c>.</param>
    /// <param name="target">The instance on which to invoke the method.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> or <paramref name="method" /> is <c>null</c>.</exception>
    public void RegisterFunction(string path, MethodInfo method, object target)
    {
        ArgumentNullException.ThrowIfNull(path);

        ArgumentNullException.ThrowIfNull(method);

        var oldTop = Lua.LuaGetTop(State);
        var function = CreateFunction(method, target);
        SetGlobal(path, function);
        Lua.LuaSetTop(State, oldTop);
    }

    /// <summary>
    ///     Registers a type parser for the specified type. This action will override any existing parsers.
    /// </summary>
    /// <param name="type">The type, which must not be <c>null</c>.</param>
    /// <param name="typeParser">The parser, which must not be <c>null</c>.</param>
    public void RegisterTypeParser(Type type, ITypeParser typeParser)
    {
        _objectMarshal.RegisterTypeParser(type, typeParser);
    }

    /// <summary>
    ///     Sets the value of the specified global variable.
    /// </summary>
    /// <param name="name">The name, which must not be <c>null</c>.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
    public void SetGlobal(string name, object value)
    {
        ArgumentNullException.ThrowIfNull(name);

        _objectMarshal.PushToStack(State, value);
        Lua.LuaSetGlobal(State, name);
    }

    private void ReleaseUnmanagedResources()
    {
        if (State == IntPtr.Zero)
        {
            return;
        }

        Lua.LuaClose(State);
    }
}