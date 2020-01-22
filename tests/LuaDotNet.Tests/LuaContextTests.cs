using System;
using LuaDotNet;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void Constructor_IsCorrect()
        {
            var lua = new LuaContext();
            Assert.AreNotEqual(IntPtr.Zero, lua.State);
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var lua = new LuaContext();
            lua.SetGlobal("test", "Hello, World");
            Assert.AreEqual("Hello, World", lua.GetGlobal("test"));
            //Assert.Pass();
        }
    }
}