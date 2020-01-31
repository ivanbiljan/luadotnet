using System;
using JetBrains.Annotations;
using LuaDotNet.PInvoke;

namespace LuaDotNet
{
    [PublicAPI]
    public abstract class LuaObject : IDisposable
    {
        private bool _disposed;

        protected LuaObject(LuaContext lua, int reference)
        {
            Lua = lua;
            Reference = reference;
        }

        protected LuaContext Lua { get; }

        protected int Reference { get; }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
            LuaModule.Instance.LuaLUnref(Lua.State, (int) LuaRegistry.RegistryIndex, Reference);
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            Dispose(true);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        ~LuaObject()
        {
            Dispose(false);
        }
    }
}