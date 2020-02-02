using LuaDotNet;
using NUnit.Framework;

namespace Tests {
    public sealed class LuaFunctionTests {
        [Test]
        public void LuaContext_LoadString_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return 5");
                Assert.AreEqual(5, function.Call()[0]);
            }
        }
    }
}