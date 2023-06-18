using System;
using Xunit;

namespace LuaDotNet.Tests
{
    public class LuaContextFacts
    {
        [Theory]
        [InlineData("str", "Hello, World")]
        [InlineData("bool", true)]
        [InlineData("integer", -123456L)]
        [InlineData("float", 123.456)]
        [InlineData(
            "array",
            new long[]
            {
                1, 2, 3
            })]
        public void GetSetGlobal_IsCorrect(string global, object value)
        {
            var lua = new LuaContext();
            lua.SetGlobal(global, value);
            Assert.Equal(value, lua.GetGlobal(global));
        }

        [Fact]
        public void Constructor_IsCorrect()
        {
            var lua = new LuaContext();
            Assert.NotEqual(IntPtr.Zero, lua.State);
        }

        [Fact]
        public void DoString_NullChunk_ThrowsArgumentNullException()
        {
            using (var lua = new LuaContext())
            {
                Assert.Throws<ArgumentNullException>(() => lua.DoString(null));
            }
        }

        [Fact]
        public void DoString_SingleResult_IsCorrect()
        {
            var lua = new LuaContext();
            var result = lua.DoString("return 5")[0];
            Assert.Equal(5L, result);
        }

        [Fact]
        public void DoString_VarArg_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                var results = lua.DoString("return 1, 2, 3");
                Assert.Equal(
                    new object[]
                    {
                        1L, 2L, 3L
                    },
                    results);
            }
        }

        [Fact]
        public void GetGlobal_NullName_ThrowsArgumentNullException()
        {
            using (var lua = new LuaContext())
            {
                Assert.Throws<ArgumentNullException>(() => lua.GetGlobal(null));
            }
        }

        [Fact]
        public void LoadString_NullChunk_ThrowsArgumentNullException()
        {
            using (var lua = new LuaContext())
            {
                Assert.Throws<ArgumentNullException>(() => lua.LoadString(null));
            }
        }

        [Fact]
        public void SetGlobal_NullName_ThrowsArgumentNullException()
        {
            using (var lua = new LuaContext())
            {
                Assert.Throws<ArgumentNullException>(() => lua.SetGlobal(null, null));
            }
        }
    }
}