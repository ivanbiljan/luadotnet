using System;
using LuaDotNet;
using NUnit.Framework;

namespace Tests
{
    public class LuaContextTests
    {
        [Test]
        public void Constructor_IsCorrect()
        {
            var lua = new LuaContext();
            Assert.AreNotEqual(IntPtr.Zero, lua.State);
        }

        [TestCase("str", "Hello, World")]
        [TestCase("bool", true)]
        [TestCase("integer", -123456L)]
        [TestCase("float", 123.456)]
        public void GetSetGlobal_IsCorrect(string global, object value)
        {
            var lua = new LuaContext();
            lua.SetGlobal(global, value);
            Assert.AreEqual(value, lua.GetGlobal(global));
        }
    }
}