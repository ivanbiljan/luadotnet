using System;
using System.Linq;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using Xunit;

namespace LuaDotNet.Tests.Marshalling {
    public sealed class TypeTests {
        private class TestClass {
            public TestClass() {
            }

            public TestClass(string testProperty) {
                TestProperty = testProperty;
            }

            public static string StaticProperty { get; } = nameof(StaticProperty);
            public string TestProperty { get; } = "Hello, World";

            public static bool OverloadedMethod(bool firstArg) => firstArg;

            public static string OverloadedMethod(string firstArg) => firstArg;

            public static int OverloadedMethod(int firstArg) => firstArg;

            public static int OverloadedMethod(int firstArg, int secondArg) => firstArg + secondArg;

            public static int OverloadedMethod(params int[] @params) => @params.Sum() + 1;

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

        [Theory]
        [InlineData("test1")]
        [InlineData("test2")]
        public void TypeCtor_Arguments_IsCorrect(string argument) {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                lua.DoString($"instance = TestClass('{argument}')");

                var instance = lua.GetGlobal("instance") as TestClass;
                Assert.NotNull(instance);
                Assert.Equal(argument, instance.TestProperty);
            }
        }

        [Fact]
        public void CallOverload_ParamsArray_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Equal(26L, lua.DoString("return TestClass.OverloadedMethod({5,5,5,5,5})")[0]);
            }
        }

        [Fact]
        public void CallOverload_SingleArgIsBoolean_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Equal(true, lua.DoString("return TestClass.OverloadedMethod(true)")[0]);
            }
        }

        [Fact]
        public void CallOverload_SingleArgIsInteger_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Equal(5L, lua.DoString("return TestClass.OverloadedMethod(5)")[0]);
            }
        }

        [Fact]
        public void CallOverload_TwoArgs_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Equal(10L, lua.DoString("return TestClass.OverloadedMethod(5, 5)")[0]);
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

        [Fact]
        public void TypeCtor_DefaultCtor_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                lua.DoString("instance = TestClass()");

                var instance = lua.GetGlobal("instance") as TestClass;
                Assert.NotNull(instance);
                Assert.Equal("Hello, World", instance.TestProperty);
            }
        }

        [Fact]
        public void TypeCtor_InvalidCtor_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Throws<LuaException>(() => lua.DoString("instance = TestClass('this shouldnt work', 5)"));
            }
        }

        [Fact]
        public void TypeCtor_NullType_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                // ReSharper disable once RedundantCast
                lua.SetGlobal("TestClass", (Type) null);
                Assert.Throws<LuaException>(() => lua.DoString("instance = TestClass()"));
            }
        }
    }
}