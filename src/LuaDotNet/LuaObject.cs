using System;
using JetBrains.Annotations;
using LuaDotNet.PInvoke;

namespace LuaDotNet
{
    [PublicAPI]
    public abstract class LuaObject : IDisposable
    {
        private readonly IntPtr _luaState;
        private readonly int _reference;
        private bool _disposed;

        public LuaObject(IntPtr luaState, int reference)
        {
            _luaState = luaState;
            _reference = reference;
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
            LuaModule.Instance.LuaLUnref(_luaState, (int) LuaRegistry.RegistryIndex, _reference);
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