using System;
using JetBrains.Annotations;
using LuaDotNet.Exceptions;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet {
    /// <summary>
    ///     Specifies the status of a Lua coroutine.
    /// </summary>
    [PublicAPI]
    public enum CoroutineStatus {
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
    public sealed class LuaCoroutine : LuaObject {
        /// <inheritdoc />
        internal LuaCoroutine(LuaContext lua, int reference) : base(lua, reference) {
        }

        /// <summary>
        ///     Gets the state associated with this coroutine.
        /// </summary>
        public IntPtr CoroutineState {
            get {
                ObjectMarshalPool.GetMarshal(Lua.State).PushToStack(Lua.State, this);
                var handle = LuaModule.Instance.LuaToThread(Lua.State, -1);
                LuaModule.Instance.LuaPop(Lua.State, 1);
                return handle;
            }
        }

        /// <summary>
        ///     Gets the coroutine's status.
        /// </summary>
        public CoroutineStatus Status {
            get {
                var coroutineState = LuaModule.Instance.LuaToThread(Lua.State, 1);
                if (Lua.State == coroutineState) {
                    return CoroutineStatus.Running;
                }

                var status = (LuaErrorCode) LuaModule.Instance.LuaStatus(Lua.State);
                switch (status) {
                    case LuaErrorCode.LuaOk:
                        if (LuaModule.Instance.LuaGetStack(Lua.State, 0, out _) == 1) {
                            return CoroutineStatus.Normal;
                        }

                        if (LuaModule.Instance.LuaGetTop(Lua.State) == 0) {
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
        public (bool success, string errorMessage, object[] results) Resume(int nargs = 0) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            var (success, errorMsg, res) = new ValueTuple<bool, string, object[]>(false, null, new object[0]);
            if (!LuaModule.Instance.LuaCheckStack(Lua.State, nargs)) {
                throw new LuaException("The stack does not have enough space to fit that many arguments.");
            }

            if (LuaModule.Instance.LuaStatus(Lua.State) == (int) LuaErrorCode.LuaOk &&
                LuaModule.Instance.LuaGetTop(Lua.State) == 0) {
                throw new LuaException("Cannot resume a dead coroutine.");
            }

            LuaModule.Instance.LuaXMove(Lua.State, Lua.State, nargs); // Exchange the requested arguments between threads
            var threadStatus = (LuaErrorCode) LuaModule.Instance.LuaResume(Lua.State, default(IntPtr), nargs, out _);
            var oldStackTop = LuaModule.Instance.LuaGetTop(Lua.State);
            if (threadStatus == LuaErrorCode.LuaOk || threadStatus == LuaErrorCode.LuaYield) {
                // The results are all that's left on the stack; ensure that there's enough space left to push them back to the caller's stack
                var numberOfResults = LuaModule.Instance.LuaGetTop(Lua.State);
                if (!LuaModule.Instance.LuaCheckStack(Lua.State, numberOfResults + 1)) {
                    LuaModule.Instance.LuaPop(Lua.State, numberOfResults);
                    throw new LuaException("The stack does not have enough space to fit that many results.");
                }

                // Propagate the results back to the caller
                LuaModule.Instance.LuaXMove(Lua.State, Lua.State, numberOfResults);
                res = new object[numberOfResults];
                var newStackTop = LuaModule.Instance.LuaGetTop(Lua.State);
                for (var i = oldStackTop + 1; i <= newStackTop; ++i) {
                    res[i - oldStackTop - 1] = objectMarshal.GetObject(Lua.State, i);
                }

                LuaModule.Instance.LuaPop(Lua.State, numberOfResults);
                success = true;
            }
            else {
                LuaModule.Instance.LuaXMove(Lua.State, Lua.State, 1); // Propagate the error message back to the caller
                errorMsg = (string) objectMarshal.GetObject(Lua.State, -1); // Get the error message
                LuaModule.Instance.LuaPop(Lua.State, 1); // Pop the error message
                success = false;
            }

            return (success, errorMsg, res);
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
        public (bool success, string errorMessage, object[] results) Resume(object[] arguments = null) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            var args = arguments ?? new object[0];
            var (success, errorMsg, res) = new ValueTuple<bool, string, object[]>(false, null, new object[0]);
            if (!LuaModule.Instance.LuaCheckStack(Lua.State, args.Length)) {
                throw new LuaException("The stack does not have enough space to fit that many arguments.");
            }

            if (LuaModule.Instance.LuaStatus(Lua.State) == (int) LuaErrorCode.LuaOk &&
                LuaModule.Instance.LuaGetTop(Lua.State) == 0) {
                throw new LuaException("Cannot resume a dead coroutine.");
            }

            foreach (var arg in args) {
                objectMarshal.PushToStack(Lua.State, arg);
            }

            var threadStatus =
                (LuaErrorCode) LuaModule.Instance.LuaResume(Lua.State, default(IntPtr), args.Length, out _);
            var oldStackTop = LuaModule.Instance.LuaGetTop(Lua.State);
            if (threadStatus == LuaErrorCode.LuaOk || threadStatus == LuaErrorCode.LuaYield) {
                // The results are all that's left on the stack; ensure that there's enough space left to push them back to the caller's stack
                var numberOfResults = LuaModule.Instance.LuaGetTop(Lua.State);
                if (!LuaModule.Instance.LuaCheckStack(Lua.State, numberOfResults + 1)) {
                    LuaModule.Instance.LuaPop(Lua.State, numberOfResults);
                    throw new LuaException("The stack does not have enough space to fit that many results.");
                }

                // Propagate the results back to the caller
                LuaModule.Instance.LuaXMove(Lua.State, Lua.State, numberOfResults);
                res = new object[numberOfResults];
                var newStackTop = LuaModule.Instance.LuaGetTop(Lua.State);
                for (var i = oldStackTop + 1; i <= newStackTop; ++i) {
                    res[i - oldStackTop - 1] = objectMarshal.GetObject(Lua.State, i);
                }

                LuaModule.Instance.LuaPop(Lua.State, numberOfResults);
                success = true;
            }
            else {
                // Propagate the error message back to the caller
                LuaModule.Instance.LuaXMove(Lua.State, Lua.State, 1);
                errorMsg = (string) objectMarshal.GetObject(Lua.State, -1);
                LuaModule.Instance.LuaPop(Lua.State, 1);
                success = false;
            }

            return (success, errorMsg, res);
        }
    }
}