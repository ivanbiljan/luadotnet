using System;

namespace LuaDotNet.Marshalling
{
    /// <summary>
    ///     Represents a default .NET object parser.
    /// </summary>
    public sealed class NetObjectParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex) => throw new NotImplementedException();

        public void Push(object obj, IntPtr state)
        {
            throw new NotImplementedException();
        }
    }
}