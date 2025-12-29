using System;
using LuaDotNet.Exceptions;
using LuaDotNet.Marshalling;
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
    /// <inheritdoc />
    internal LuaCoroutine(LuaContext lua, int reference) : base(lua, reference)
    {
    }

    /// <summary>
    ///     Gets the state associated with this coroutine.
    /// </summary>
    public IntPtr CoroutineState
    {
        get
        {
            ObjectMarshalPool.GetMarshal(Lua.State).PushToStack(Lua.State, this);
            var handle = LuaModule.LuaToThread(Lua.State, -1);
            LuaModule.LuaPop(Lua.State, 1);

            return handle;
        }
    }

    /// <summary>
    ///     Gets the coroutine's status.
    /// </summary>
    public CoroutineStatus Status
    {
        get
        {
            var coroutineState = LuaModule.LuaToThread(Lua.State, 1);
            if (Lua.State == coroutineState)
            {
                return CoroutineStatus.Running;
            }

            var status = LuaModule.LuaStatus(Lua.State);
            switch (status)
            {
                case LuaErrorCode.LuaOk:
                    if (LuaModule.LuaGetStack(Lua.State, 0, out _) == 1)
                    {
                        return CoroutineStatus.Normal;
                    }

                    if (LuaModule.LuaGetTop(Lua.State) == 0)
                    {
                        return CoroutineStatus.Dead;
                    }

                    return CoroutineStatus.Suspended;
                case LuaErrorCode.LuaYield:
                    return CoroutineStatus.Suspended;
                default:
                    return CoroutineStatus.Dead;
            }
        }
    }

    /// <summary>
    ///     Resumes (or starts) the coroutine.
    /// </summary>
    /// <param name="nargs">The number of arguments to be passed to the coroutine from the main state.</param>
    /// <returns>
    ///     A <see cref="ValueTuple{T1, T2, T3}" /> denoting the status, the error message and the results. If the
    ///     coroutine finishes its execution without errors the method returns <c>true</c> and the coroutine's results. The
    ///     error message is <c>null</c> in this case. If the coroutine encounters an error during its execution the method
    ///     returns <c>false</c> and the error message describing the error. The results are empty in this case.
    /// </returns>
    /// <exception cref="LuaException">The callee's stack does not have enough space to fit the arguments.</exception>
    /// <exception cref="LuaException">The coroutine is dead.</exception>
    /// <exception cref="LuaException">The callee's stack does not have enough space to fit the results.</exception>
    public (bool success, string errorMessage, object[] results) Resume(int nargs = 0)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
        if (!LuaModule.LuaCheckStack(CoroutineState, nargs))
        {
            throw new LuaException("The stack does not have enough space to fit that many arguments.");
        }

        if (LuaModule.LuaStatus(CoroutineState) == LuaErrorCode.LuaOk &&
            LuaModule.LuaGetTop(CoroutineState) == 0)
        {
            throw new LuaException("Cannot resume a dead coroutine.");
        }

        LuaModule.LuaXMove(
            Lua.State,
            CoroutineState,
            nargs
        ); // Exchange the requested arguments between threads

        return ResumeShared(nargs);
    }

    /// <summary>
    ///     Resumes (or starts) the coroutine.
    /// </summary>
    /// <param name="arguments">The arguments to be pushed directly to the coroutine's stack.</param>
    /// <returns>
    ///     A <see cref="ValueTuple{T1, T2, T3}" /> denoting the status, the error message and the results. If the
    ///     coroutine finishes its execution without errors the method returns <c>true</c> and the coroutine's results. The
    ///     error message is <c>null</c> in this case. If the coroutine encounters an error during its execution the method
    ///     returns <c>false</c> and the error message describing the error. The results are empty in this case.
    /// </returns>
    /// <exception cref="LuaException">The callee's stack does not have enough space to fit the arguments.</exception>
    /// <exception cref="LuaException">The coroutine is dead.</exception>
    /// <exception cref="LuaException">The callee's stack does not have enough space to fit the results.</exception>
    public (bool success, string errorMessage, object[] results) Resume(params object[] arguments)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
        arguments ??= new object[0];
        if (!LuaModule.LuaCheckStack(CoroutineState, arguments.Length))
        {
            throw new LuaException("The stack does not have enough space to fit that many arguments.");
        }

        if (LuaModule.LuaStatus(CoroutineState) == LuaErrorCode.LuaOk &&
            LuaModule.LuaGetTop(CoroutineState) == 0)
        {
            throw new LuaException("Cannot resume a dead coroutine.");
        }

        foreach (var arg in arguments)
        {
            objectMarshal.PushToStack(CoroutineState, arg);
        }

        return ResumeShared(arguments.Length);
    }

    private (bool success, string errorMessage, object[] results) ResumeShared(int numberOfArguments)
    {
        var result = (success: false, error: string.Empty, results: new object[0]);
        var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
        var threadStatus = LuaModule.LuaResume(CoroutineState, default, numberOfArguments, out _);
        var oldStackTop = LuaModule.LuaGetTop(Lua.State);
        if (threadStatus == LuaErrorCode.LuaOk || threadStatus == LuaErrorCode.LuaYield)
        {
            // The results are all that's left on the stack; ensure that there's enough space left to push them back to the caller's stack
            var numberOfResults = LuaModule.LuaGetTop(CoroutineState);
            if (!LuaModule.LuaCheckStack(Lua.State, numberOfResults + 1))
            {
                throw new LuaException("The stack does not have enough space to fit that many results.");
            }

            // Propagate the results back to the caller
            LuaModule.LuaXMove(CoroutineState, Lua.State, numberOfResults);
            result.results = new object[numberOfResults];
            var newStackTop = LuaModule.LuaGetTop(Lua.State);
            for (var i = oldStackTop + 1; i <= newStackTop; ++i)
            {
                result.results[i - oldStackTop - 1] = objectMarshal.GetObject(Lua.State, i);
            }

            LuaModule.LuaPop(Lua.State, numberOfResults);
            result.success = true;
        }
        else
        {
            // Propagate the error message back to the caller
            LuaModule.LuaXMove(Lua.State, Lua.State, 1);
            result.error = (string) objectMarshal.GetObject(Lua.State, -1);
            LuaModule.LuaPop(Lua.State, 1);
            result.success = false;
        }

        return result;
    }
}