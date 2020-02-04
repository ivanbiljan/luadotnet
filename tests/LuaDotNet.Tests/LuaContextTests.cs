using System;
using JetBrains.Annotations;
using LuaDotNet;
using NUnit.Framework;

namespace Tests {
    public class LuaContextTests {
        [Test]
        public void Constructor_IsCorrect() {
            var lua = new LuaContext();
            Assert.AreNotEqual(IntPtr.Zero, lua.State);
        }

        [Test]
        public void DoString_SingleResult_IsCorrect() {
            var lua = new LuaContext();
            var result = lua.DoString("return 5")[0];
            Assert.AreEqual(5, result);
        }

        [Test]
        public void DoString_VarArg_IsCorrect() {
            using (var lua = new LuaContext()) {
                var results = lua.DoString("return 1, 2, 3");
                Assert.AreEqual(new int[] {1, 2, 3}, results);
            }
        }

        [Test]
        public void LoadString_NullChunk_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.LoadString(null));
            }
        }

        [Test]
        public void GetGlobal_NullName_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.GetGlobal(null));
            }
        }

        [Test]
        public void SetGlobal_NullName_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.SetGlobal(null, null));
            }
        }

        [Test]
        public void DoString_NullChunk_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.DoString(null));
            }
        }

        [TestCase("str", "Hello, World")]
        [TestCase("bool", true)]
        [TestCase("integer", -123456L)]
        [TestCase("float", 123.456)]
        [TestCase("array", new long[] {1, 2, 3})]
        public void GetSetGlobal_IsCorrect(string global, object value) {
            var lua = new LuaContext();
            lua.SetGlobal(global, value);
            Assert.AreEqual(value, lua.GetGlobal(global));
        }
    }
}