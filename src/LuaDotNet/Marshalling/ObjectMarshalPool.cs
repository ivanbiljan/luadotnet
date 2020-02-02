using System;
using System.Collections.Generic;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    internal static class ObjectMarshalPool
    {
        private static readonly Dictionary<IntPtr, ObjectMarshal> Marshals = new Dictionary<IntPtr, ObjectMarshal>();

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

            return Marshals.GetValueOrDefault(LuaModule.Instance.GetMainThreadPointer(state));
        }
    }
}