using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using Xunit;

namespace LuaDotNet.Tests.Marshalling {
    public sealed class TypeTests {
        private class TestClass {
            public static string StaticProperty { get; } = nameof(StaticProperty);

            public static string StaticMethod(string what = null) => what.IsNullOrWhitespace() ? nameof(StaticMethod) : what;
        }

        [Theory]
        [InlineData("test string")]
        public void CallStaticMethod_WithArgs_IsCorrect(string arg) {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Equal(arg, lua.DoString($"return TestClass.StaticMethod('{arg}')")[0]);
            }
        }

        [Fact]
        public void CallStaticMethod_NoArgs_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));

                Assert.Equal(nameof(TestClass.StaticMethod), lua.DoString("return TestClass.StaticMethod()")[0]);
            }
        }

        [Fact]
        public void IndexType_InvalidMember_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));

                Assert.Throws<LuaException>(() => lua.DoString("return TestClass.ThisPropertyDoesNotExist"));
            }
        }

        [Fact]
        public void IndexType_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));

                Assert.Equal(nameof(TestClass.StaticProperty), lua.DoString("return TestClass.StaticProperty")[0]);
            }
        }

        [Fact]
        public void IndexType_NonStringMember_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Throws<LuaException>(() => lua.DoString("TestClass.2"));
            }
        }
    }
}