﻿using System;
using System.Linq;
using LuaDotNet.Exceptions;
using Xunit;

namespace LuaDotNet.Tests.Marshalling
{
    public static class TestExtensions
    {
        public static string ExtensionMethod(this MethodTests.Test test, string returnValue) => returnValue;
    }

    public class MethodTests
    {
        public class Test
        {
            #region Do Not Run Code Cleanup, Order Is Important

            public int Method1(int x, int y, bool optionalTest = false)
            {
                if (!optionalTest)
                {
                    return -1;
                }

                return x - y;
            }

            public int Method1(int x, int y) => x + y;

            public int Method2(params int[] @params) => @params.Sum() + 1;

            public bool Method2(bool firstArg) => firstArg;

            public string Method2(string firstArg) => firstArg;

            public int Method2(int firstArg) => firstArg;

            public double Method2(double firstArg) => firstArg;

            public int Method2(int firstArg, int secondArg) => firstArg + secondArg;

            public int Method3(string s, out int retVal)
            {
                if (int.TryParse(s, out retVal))
                {
                    return retVal;
                }

                return -1;
            }

            #endregion
        }

        [Fact]
        public void ExtensionMethod_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:ExtensionMethod('Hello, World')")[0];

                Assert.Equal("Hello, World", result);
            }
        }

        [Fact]
        public void Method1_InsufficientArgs_ThrowsLuaException()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                Assert.Throws<LuaException>(() => lua.DoString("return test:Method1(5)"));
            }
        }

        [Fact]
        public void Method1_WithOptionalParameter_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal(nameof(Test), typeof(Test));
                lua.DoString("test = Test()");

                var result = lua.DoString("return test:Method1(5, 5, false)")[0];

                Assert.Equal(-1L, result);
            }
        }


        [Fact]
        public void Method1_WithoutOptionalParameter_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal(nameof(Test), typeof(Test));
                lua.DoString("test = Test()");

                var result = lua.DoString("return test:Method1(5, 5)")[0];

                Assert.Equal(10L, result);
            }
        }

        [Fact]
        public void Method2_ParamsArray_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2(5, 5, 5, 5, 5)")[0];

                Assert.Equal(26L, result);
            }
        }

        [Fact]
        public void Method2_SingleArgBoolean_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2(true)")[0];

                Assert.Equal(true, result);
            }
        }

        [Fact]
        public void Method2_SingleArgDouble_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2(5.0)")[0];

                Assert.Equal(5D, result);
            }
        }

        [Fact]
        public void Method2_SingleArgInteger_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2(5)")[0];

                Assert.Equal(5L, result);
            }
        }

        [Fact]
        public void Method2_SingleArgString_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2('Hello, World')")[0];

                Assert.Equal("Hello, World", result);
            }
        }

        [Fact]
        public void Method2_TwoArgs_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("test", new Test());

                var result = lua.DoString("return test:Method2(5, 6)")[0];

                Assert.Equal(11L, result);
            }
        }

        [Fact]
        public void Method3_OutParameter_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal(nameof(Int32), typeof(int));

                var results = lua.DoString("return Int32.TryParse('5')");

                Assert.Equal(true, results[0]);
                Assert.Equal(5L, results[1]);
            }
        }
    }
}