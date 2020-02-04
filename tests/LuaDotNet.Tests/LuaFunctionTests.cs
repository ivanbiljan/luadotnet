using LuaDotNet;
using NUnit.Compatibility;
using NUnit.Framework;

namespace Tests {
    public sealed class LuaFunctionTests {
        [Test]
        public void LoadString_SingleResult_IsCorrect() {
            using (var lua = new LuaContext()) {
                var function = lua.LoadString("return 5");
                Assert.AreEqual(5, function.Call()[0]);
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
    }
}