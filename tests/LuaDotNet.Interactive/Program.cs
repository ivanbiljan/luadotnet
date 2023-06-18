using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using LuaDotNet;
using LuaDotNet.DebuggingHelpers;

namespace LuaDotNet.Interactive {

    internal sealed class Ne {
        
    }
    
    public sealed class TestClass {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(int hWnd, String text, String caption, uint type);
        
        public static bool Boolean = true;

        public string InstanceString { get; set; } = "Hello, World";

        public string ReadOnlyProp { get; } = nameof(ReadOnlyProp);

        public static void StaticMethod(string text) {
            MessageBox(0, text, "Ovo je pozvano iz staticke metode", 0);
        }

        public void InstanceMethod() {
            Console.WriteLine("Hello, World");
        }

        private void PrivateMethod() {
            
        }
    }

    class Program {
        static void Main(string[] args) {
            var testClass = new TestClass();
            var debugTable = new DebugTable("StaticField", "InstanceString", "ReadOnlyProp");
            debugTable.AddRow(TestClass.Boolean, testClass.InstanceString, testClass.ReadOnlyProp);

            using (var lua = new LuaContext()) {
                // Environment setup
                lua.ImportType(nameof(Console));
                lua.ImportType(nameof(ConsoleColor));
                lua.ImportType(nameof(TestClass));
                
                lua.SetGlobal("testClass", testClass);
                
                lua.SetGlobal("clear", lua.CreateFunction(new Action(Console.Clear)));
                lua.SetGlobal("exit", lua.CreateFunction(new Action(() => Environment.Exit(0))));
                
                var input = string.Empty;
                do {
                    Console.Write(">>> ");
                    input = Console.ReadLine() ?? string.Empty;
                    try {
                        if (File.Exists(input)) {
                            lua.DoFile(input);
                        }
                        else {
                            var results = lua.DoString(input);
                            if (results.Length > 0) {
                                Console.WriteLine(string.Join(", ", results));
                            }
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                    }
                } while (!input.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
                
                debugTable.AddRow(TestClass.Boolean, testClass.InstanceString, testClass.ReadOnlyProp);
                Console.WriteLine(debugTable.GetOutput());
            }
        }
    }
}