using System;
using System.Runtime.InteropServices;
using System.Text;
using LuaDotNet.Extensions;
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

        public void Push(object obj, IntPtr state)
        {
            if (!(obj is string str))
            {
                return;
            }

            // UTF-8 is the encoding Lua uses. Possible TODO: Support multiple encodings like NLua does?
            var encodedString = str.GetEncodedString(Encoding.UTF8); 
            LuaModule.Instance.LuaPushLString(state, encodedString, new UIntPtr((uint) encodedString.Length));
        }
    }
}