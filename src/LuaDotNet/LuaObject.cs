using System;
using JetBrains.Annotations;
using LuaDotNet.PInvoke;

namespace LuaDotNet {
    /// <summary>
    ///     Represents the base class for Lua objects.
    /// </summary>
    [PublicAPI]
    public abstract class LuaObject : IDisposable {
        private bool _disposed;

        protected LuaObject(LuaContext lua, int reference) {
            Lua = lua;
            Reference = reference;
        }

        protected LuaContext Lua { get; }

        /// <summary>
        ///     Gets the object's reference in the registry.
        /// </summary>
        public int Reference { get; }

        /// <inheritdoc />
        public void Dispose() {
            if (_disposed) {
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        /// <summary>
        ///     The finalizer.
        /// </summary>
        ~LuaObject() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
        }

        internal virtual void PushToStack(IntPtr state) {
            LuaModule.Instance.LuaRawGetI(state, (int) LuaRegistry.RegistryIndex, Reference);
        }

        private void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
            LuaModule.Instance.LuaLUnref(Lua.State, (int) LuaRegistry.RegistryIndex, Reference);
        }
    }
}