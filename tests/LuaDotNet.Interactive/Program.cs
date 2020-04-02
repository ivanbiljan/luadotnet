using System;
using System.IO;
using System.Net.Mime;
using LuaDotNet;

namespace LuaDotNet.Interactive {
    class Program {
        static void Main(string[] args) {
            using (var lua = new LuaContext()) {
                // Environment setup
                lua.ImportType(nameof(Console));
                lua.ImportType(nameof(ConsoleColor));
                
                lua.SetGlobal("clear", lua.CreateFunction(new Action(Console.Clear)));
                lua.SetGlobal("exit", lua.CreateFunction(new Action(() => Environment.Exit(0))));
                
                var input = string.Empty;
                do {
                    Console.Write("> ");
                    input = Console.ReadLine() ?? string.Empty;
                    try {
                        if (File.Exists(input)) {
                            lua.DoFile(input);
                        }
                        else {
                            lua.DoString(input);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                    }
                } while (!input.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}