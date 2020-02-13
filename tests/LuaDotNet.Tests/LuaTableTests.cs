using System.Collections.Generic;
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
                Assert.Equal(value, table[key]);
            }
        }

        [Fact]
        public void Count_IsCorrect() {
            using (var lua = new LuaContext()) {
                var table = lua.CreateTable();
                
                table.Add("testkey", "value");
                Assert.Single(table);

                table.Remove("testkey");
                Assert.Empty(table);
            }
        }

        [Fact]
        public void Clear_IsCorrect() {
            using (var lua = new LuaContext()) {
                var table = lua.CreateTable();
                table.Add("testkey", "value");
                table.Add("testkey2", "value2");
                table.Clear();
                Assert.Empty(table);
            }
        }

        [Fact]
        public void Remove_IsCorrect() {
            using (var lua = new LuaContext()) {
                var table = lua.CreateTable(0, 0);
                table.Add("testkey", 1);
                table.Add("testkey2", 2);
                table.Remove("testkey");
                Assert.Equal(2, table["testkey2"]);
                Assert.Null(table["testkey"]);
            }
        }

        [Fact]
        public void ContainsKey_IsCorrect() {
            using (var lua = new LuaContext()) {
                var table = lua.CreateTable();
                table.Add("testkey", "value");
                Assert.True(table.ContainsKey("testkey"));
                Assert.False(table.ContainsKey("this key does not exist"));
            }
        }
    }
}