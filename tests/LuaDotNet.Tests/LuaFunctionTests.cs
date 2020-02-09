using System;
using LuaDotNet;
using LuaDotNet.Exceptions;
using NUnit.Compatibility;
using NUnit.Framework;

namespace LuaDotNet.Tests {
    public sealed class LuaFunctionTests {
        private readonly Func<int, int, int> _testDelegate = (x, y) => x + y;

        [Test]
        public void LoadString_SingleResult_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return 5");
                Assert.AreEqual(5, function.Call()[0]);
            }
        }

        [Test]
        public void LoadString_SyntaxError_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<LuaException>(() => lua.DoString("test = "));
            }
        }

        [TestCase(new object[] {1, 2, 3})]
        [TestCase(new object[] {true, 1, "true"})]
        public void LoadString_VarArgs_IsCorrect(object[] args) {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return ...");
                var results = function.Call(args);
                Assert.AreEqual(args, results);
            }
        }

        [Test]
        public void CreateFunction_Delegate_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.CreateFunction(_testDelegate);
                Assert.AreEqual(5, function.Call(2, 3)[0]);
            }
        }

        [Test]
        public void CreateFunction_NullDelegate_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.CreateFunction(@delegate: null));
            }
        }
        
        [Test]
        public void CreateFunction_NullMethodInfo_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.CreateFunction(methodInfo: null));
            }
        }
    }
}