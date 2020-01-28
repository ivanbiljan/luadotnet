using System;
using System.Runtime.InteropServices;
using System.Text;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    public sealed class StringParser : ITypeParser
    {
        public object Parse(IntPtr state, int stackIndex)
        {
            var stringPointer = LuaModule.Instance.LuaToLString(state, stackIndex, out var lengthPtr);
            var stringBuffer = new byte[(byte) lengthPtr];
            Marshal.Copy(stringPointer, stringBuffer, 0, stringBuffer.Length);
            return Encoding.UTF8.GetString(stringBuffer);
        }

        public void Push(object obj, IntPtr state) => LuaModule.Instance.LuaPushLString(state, (string) obj);
    }
}