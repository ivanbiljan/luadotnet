using Xunit;

namespace LuaDotNet.Tests {
    public class LuaTableTests {
        [Theory]
        [InlineData("test", "Hello, World")]
        [InlineData(0, "Indexed value")]
        public void Add_IsCorrect(object key, object value) {
            using (var lua = new LuaContext()) {
                var table = lua.CreateTable(0, 0);
                table.Add(key, value);
                Xunit.Assert.Equal(value, table[key]);
            }
        }
    }
}