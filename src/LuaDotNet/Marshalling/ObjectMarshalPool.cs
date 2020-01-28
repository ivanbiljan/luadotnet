using System;
using System.Collections.Generic;

namespace LuaDotNet.Marshalling
{
    internal static class ObjectMarshalPool
    {
        private static readonly Dictionary<IntPtr, ObjectMarshal> Marshals = new Dictionary<IntPtr, ObjectMarshal>();

        public static ObjectMarshal GetMarshal(IntPtr state)
        {
            if (!Marshals.TryGetValue(state, out var marshal))
            {
                Marshals[state] = marshal = new ObjectMarshal();
            }

            return marshal;
        }
    }
}