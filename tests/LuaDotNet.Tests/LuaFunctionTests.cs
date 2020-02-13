using System;
using System.ComponentModel;
using LuaDotNet.Exceptions;
using Xunit;

namespace LuaDotNet.Tests {
    public sealed class LuaFunctionFacts {
        private readonly Func<int, int, int> _FactDelegate = (x, y) => x + y;

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(true, 1, "true")]
        public void LoadString_VarArgs_IsCorrect(params object[] args) {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return ...");
                var results = function.Call(args);
                Assert.Equal(args, results);
            }
        }

        [Fact]
        public void CreateFunction_Delegate_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.CreateFunction(_FactDelegate);
                Assert.Equal(5, function.Call(2, 3)[0]);
            }
        }

        [Fact]
        public void CreateFunction_NullDelegate_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.CreateFunction(null));
            }
        }

        [Fact]
        public void CreateFunction_NullMethodInfo_ThrowsArgumentNullException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<ArgumentNullException>(() => lua.CreateFunction(methodInfo: null));
            }
        }

        [Fact]
        public void LoadString_SingleResult_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return 5");
                Assert.Equal(5, function.Call()[0]);
            }
        }

        [Fact]
        public void LoadString_SyntaxError_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                Assert.Throws<LuaException>(() => lua.DoString("Fact = "));
            }
        }
    }
}