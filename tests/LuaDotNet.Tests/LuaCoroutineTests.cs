using LuaDotNet.Exceptions;
using Xunit;

namespace LuaDotNet.Tests
{
    public sealed class LuaCoroutineTests
    {
        [Fact]
        public void Resume_CoroutineCompleted_ThrowsLuaException()
        {
            using (var lua = new LuaContext())
            {
                var function = lua.LoadString("return 0");
                var coroutine = lua.CreateCoroutine(function);

                var results = coroutine.Resume().results;

                Assert.Single(results);
                Assert.Equal(0L, results[0]);
                Assert.Throws<LuaException>(() => coroutine.Resume());
            }
        }

        [Fact]
        public void Resume_ReturnsSomething_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                var function = lua.LoadString("return 1, 3.3, 5, 'test', false");
                var coroutine = lua.CreateCoroutine(function);

                var (success, _, results) = coroutine.Resume();

                Assert.True(success);
                Assert.Equal(5, results.Length);
                Assert.Equal(
                    new object[]
                    {
                        1L, 3.3, 5L, "test", false
                    },
                    results);
            }
        }

        [Fact]
        public void Resume_TooManyArguments_ThrowsLuaException()
        {
            using (var lua = new LuaContext())
            {
                var function = lua.LoadString("return ...");
                var coroutine = lua.CreateCoroutine(function);

                Assert.Throws<LuaException>(() => coroutine.Resume(int.MaxValue));
            }
        }
    }
}