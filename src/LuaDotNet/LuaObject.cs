using System;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents the base class for Lua objects.
/// </summary>
public abstract class LuaObject(LuaContext context, int reference) : IDisposable
{
    private bool _disposed;

    protected LuaContext Context { get; } = context;

    /// <summary>
    ///     Gets the object's reference in the registry.
    /// </summary>
    public int Reference { get; } = reference;

    /// <inheritdoc />
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

    /// <summary>
    ///     The finalizer.
    /// </summary>
    ~LuaObject()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
    }

    internal virtual void PushToStack(IntPtr state)
    {
        Lua.LuaRawGetI(state, (int) LuaRegistry.RegistryIndex, Reference);
    }

    private void ReleaseUnmanagedResources()
    {
        if (Reference == Lua.LuaRefNil || Reference == Lua.LuaNoRef)
        {
            return;
        }

        Lua.LuaLUnref(Context.State, (int) LuaRegistry.RegistryIndex, Reference);
    }
}