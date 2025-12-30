using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling;

internal static class ObjectMarshalPool
{
    private static readonly ConcurrentDictionary<IntPtr, ObjectMarshal> Marshals = new();

    public static void AddMarshal(LuaContext lua, ObjectMarshal objectMarshal)
    {
        // Each context gets its own ObjectMarshal
        Marshals[lua.State] = objectMarshal;
    }

    public static ObjectMarshal GetMarshal(IntPtr state)
    {
        if (Marshals.TryGetValue(state, out var marshal))
        {
            return marshal;
        }

        return Marshals.GetValueOrDefault(Lua.GetMainThreadPointer(state));
    }

    public static void Remove(IntPtr state)
    {
        Marshals.Remove(state, out _);
    }
}