using System;
using LuaDotNet.Exceptions;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Specifies the status of a Lua coroutine.
/// </summary>
public enum CoroutineStatus
{
    /// <summary>
    ///     The coroutine is currently running.
    /// </summary>
    Running = 0,

    /// <summary>
    ///     The coroutine has either finished its execution or encountered an error.
    /// </summary>
    Dead = 1,

    /// <summary>
    ///     The coroutine yielded.
    /// </summary>
    Suspended = 2,

    /// <summary>
    ///     The coroutine has invoked a subroutine.
    /// </summary>
    Normal = 3
}

/// <summary>
///     Represents a Lua coroutine.
/// </summary>
public sealed class LuaCoroutine : LuaObject
{
    internal LuaCoroutine(LuaContext context, int reference) : base(context, reference)
    {
    }

    /// <summary>
    ///     Gets the underlying Lua thread (coroutine state) pointer.
    /// </summary>
    public IntPtr CoroutineState
    {
        get
        {
            var marshal = ObjectMarshalPool.GetMarshal(Context.State);
            marshal.PushToStack(Context.State, this);
            var thread = Lua.LuaToThread(Context.State, -1);
            Lua.LuaPop(Context.State, 1);

            return thread;
        }
    }

    /// <summary>
    ///     Gets the current status of the coroutine.
    /// </summary>
    public CoroutineStatus Status
    {
        get
        {
            var thread = CoroutineState;
            if (thread == Context.State)
            {
                return CoroutineStatus.Running;
            }

            var luaStatus = Lua.LuaStatus(thread);

            switch (luaStatus)
            {
                case LuaErrorCode.LuaOk:
                    if (Lua.LuaGetStack(thread, 0, out _) == 1)
                    {
                        return CoroutineStatus.Normal;
                    }

                    return Lua.LuaGetTop(thread) == 0 ? CoroutineStatus.Dead : CoroutineStatus.Suspended;

                case LuaErrorCode.LuaYield:
                    return CoroutineStatus.Suspended;

                default:
                    return CoroutineStatus.Dead;
            }
        }
    }

    /// <summary>
    ///     Resumes the coroutine, passing arguments from the main thread's stack.
    /// </summary>
    public (bool success, string? errorMessage, object[] results) Resume(int nargs = 0)
    {
        var thread = CoroutineState;

        if (Lua.LuaStatus(thread) == LuaErrorCode.LuaOk && Lua.LuaGetTop(thread) == 0)
        {
            throw new LuaException("Cannot resume a dead coroutine.");
        }

        if (!Lua.LuaCheckStack(thread, nargs))
        {
            throw new LuaException("Not enough stack space for arguments.");
        }

        if (nargs > 0)
        {
            Lua.LuaXMove(Context.State, thread, nargs);
        }

        return ResumeShared(thread, nargs);
    }

    /// <summary>
    ///     Resumes the coroutine with arguments pushed directly.
    /// </summary>
    public (bool success, string? errorMessage, object[] results) Resume(params object[] arguments)
    {
        var thread = CoroutineState;

        if (Lua.LuaStatus(thread) == LuaErrorCode.LuaOk && Lua.LuaGetTop(thread) == 0)
        {
            throw new LuaException("Cannot resume a dead coroutine.");
        }

        var marshal = ObjectMarshalPool.GetMarshal(Context.State);
        arguments ??= [];

        if (!Lua.LuaCheckStack(thread, arguments.Length))
        {
            throw new LuaException("Not enough stack space for arguments.");
        }

        foreach (var arg in arguments)
        {
            marshal.PushToStack(thread, arg);
        }

        return ResumeShared(thread, arguments.Length);
    }

    private (bool success, string? errorMessage, object[] results) ResumeShared(IntPtr thread, int nargs)
    {
        var marshal = ObjectMarshalPool.GetMarshal(Context.State);
        var mainTopBefore = Lua.LuaGetTop(Context.State);
        var status = Lua.LuaResume(thread, 0, nargs, out _);

        if (status == LuaErrorCode.LuaOk || status == LuaErrorCode.LuaYield)
        {
            var numResults = Lua.LuaGetTop(thread);

            if (!Lua.LuaCheckStack(Context.State, numResults))
            {
                throw new LuaException("Not enough stack space for results.");
            }

            Lua.LuaXMove(thread, Context.State, numResults);

            var results = new object[numResults];
            for (var i = 0; i < numResults; i++)
            {
                results[i] = marshal.GetObject(Context.State, mainTopBefore + i + 1);
            }

            Lua.LuaPop(Context.State, numResults);

            return (true, null, results);
        }

        Lua.LuaXMove(thread, Context.State, 1);

        var errorMessage = (string?) marshal.GetObject(Context.State, -1);
        Lua.LuaPop(Context.State, 1);

        return (false, errorMessage ?? "Unknown error", []);
    }
}