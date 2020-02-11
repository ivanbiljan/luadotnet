using System;
using System.Diagnostics;
using JetBrains.Annotations;
using LuaDotNet;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using NUnit.Framework;

namespace LuaDotNet.Tests.Marshalling {
    public sealed class TypeTests {
        private class TestClass {
            public static string StaticProperty { get; } = nameof(StaticProperty);
            public string TestProperty { get; } = "Hello, World";
            
            public TestClass() {
                
            }

            public TestClass(string testProperty) {
                TestProperty = testProperty;
            }

            public static string StaticMethod(string what) {
                return what.IsNullOrWhitespace() ?  nameof(StaticMethod) : what;
            }
        }

        [Test]
        public void CallStaticMethod_NoArgs_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.AreEqual(nameof(TestClass.StaticMethod), lua.DoString("return TestClass.StaticMethod()")[0]);
            }
        }

        [TestCase("test string")]
        public void CallStaticMethod_WithArgs_IsCorrect(string arg) {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.AreEqual(arg, lua.DoString($"return TestClass.StaticMethod('{arg}')")[0]);
            }
        }

        [Test]
        public void IndexType_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.AreEqual(nameof(TestClass.StaticProperty), lua.DoString("return TestClass.StaticProperty")[0]);
            }
        }

        [Test]
        public void IndexType_NonStringMember_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Throws<LuaException>(() => lua.DoString("TestClass.2"));
            }
        }

        [Test]
        public void IndexType_InvalidMember_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Throws<LuaException>(() => lua.DoString("return TestClass.ThisPropertyDoesNotExist"));
            }
        }

        [TestCase("test1")]
        [TestCase("test2")]
        public void TypeCtor_Arguments_IsCorrect(string argument) {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                lua.DoString($"instance = TestClass('{argument}')");
                
                var instance = lua.GetGlobal("instance") as TestClass;
                Assert.NotNull(instance);
                Assert.AreEqual(argument, instance.TestProperty);
            }
        }

        [Test]
        public void TypeCtor_DefaultCtor_IsCorrect() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                lua.DoString("instance = TestClass()");

                var instance = lua.GetGlobal("instance") as TestClass;
                Assert.NotNull(instance);
                Assert.AreEqual("Hello, World", instance.TestProperty);
            }
        }

        [Test]
        public void TypeCtor_NullType_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                // ReSharper disable once RedundantCast
                lua.SetGlobal("TestClass", (Type) null);
                Assert.Throws<LuaException>(() => lua.DoString("instance = TestClass()"));
            } 
        }
        
        [Test]
        public void TypeCtor_InvalidCtor_ThrowsLuaException() {
            using (var lua = new LuaContext()) {
                lua.SetGlobal("TestClass", typeof(TestClass));
                Assert.Throws<LuaException>(() => lua.DoString("instance = TestClass('this shouldnt work', 5)"));
            }
        }
    }
}