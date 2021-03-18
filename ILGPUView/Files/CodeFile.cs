using ILGPU;
using ILGPUView.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ILGPUView.Files
{
    public enum OutputType
    {
        bitmap,
        terminal
    }

    public class CodeFile
    {
        public string name = "";
        public string path = "";
        public string assemblyNamespace = "";
        public string fileContents = "";

        public OutputType type;

        public bool loaded = false;
        public bool compiled = false;

        private MemoryStream compiledCode;

        public setupDelegate userCodeSetup;
        public loopDelegate userCodeLoop;
        public disposeDelegate userCodeDispose;
        public terminalDelegate userCodeMain;

        public CodeFile(string name, string assemblyNamespace, string path, OutputType type)
        {
            this.path = path;
            this.assemblyNamespace = assemblyNamespace;
            this.name = name;
            this.type = type;
        }

        public CodeFile(string name, OutputType type, string fileContents)
        {
            this.name = name;
            this.type = type;
            this.fileContents = fileContents;
        }

        public void updateFileContents(string newFileContents)
        {
            fileContents = newFileContents;
            compiled = false;
            loaded = false;
        }

        public bool TrySave()
        {
            string totalPath = path + "\\" + name;
            if (totalPath != "" && Path.IsPathFullyQualified(totalPath))
            {
                try
                {
                    File.WriteAllText(totalPath, fileContents);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error saving file " + totalPath + "\n" + e.ToString());
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryLoad()
        {
            string totalPath = path + "\\" + name;
            if (totalPath != "" && File.Exists(totalPath))
            {
                try
                {
                    fileContents = File.ReadAllText(totalPath);

                    if(type == OutputType.terminal)
                    {
                        assemblyNamespace = Regex.Match(fileContents, "(?<=\\bnamespace\\s+)\\p{L}+").Value;

                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error loading file " + totalPath + "\n" + e.ToString());
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryCompile()
        {
            if(CompileCode())
            {
                return LoadAssembly();
            }
            else
            {
                return false;
            }
        }

        private bool CompileCode()
        {
            try
            {
                // The following is magic taken from https://stackoverflow.com/a/29417053/1500733

                // define source code, then parse it (to the type used for compilation)
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContents);

                // define other necessary objects for compilation
                string assemblyName = Path.GetRandomFileName();
                MetadataReference[] references = AssemblyHelpers.getAsManyAsPossible().ToArray();

                // analyse and generate IL code from syntax tree
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                //I save the memoryStream so that I can cache function delegates and call them
                compiledCode = new MemoryStream();
                EmitResult result = compilation.Emit(compiledCode);

                if (!result.Success)
                {
                    // handle exceptions
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    Console.WriteLine("Compilation Failed with error(s):");

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.WriteLine(diagnostic.Id + ": " + diagnostic.GetMessage() + " @ " + diagnostic.Location.GetLineSpan());
                    }

                    return false;
                }
                else
                {
                    compiled = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

        }

        private bool LoadAssembly()
        {
            try
            {
                compiledCode.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(compiledCode.ToArray());

                switch (type)
                {
                    case OutputType.bitmap:
                        Type bitmapAssembly = assembly.GetType("ILGPUViewTest.Test");

                        MethodInfo setup = bitmapAssembly.GetMethod("setup");
                        MethodInfo loop = bitmapAssembly.GetMethod("loop");
                        MethodInfo dispose = bitmapAssembly.GetMethod("dispose");

                        if (setup != null && loop != null && dispose != null)
                        {

                            userCodeSetup = (setupDelegate)Delegate.CreateDelegate(typeof(setupDelegate), setup);
                            userCodeLoop = (loopDelegate)Delegate.CreateDelegate(typeof(loopDelegate), loop);
                            userCodeDispose = (disposeDelegate)Delegate.CreateDelegate(typeof(disposeDelegate),dispose);
                        }
                        else
                        {
                            Console.WriteLine("Missing or cannot find Setup, Loop, or Dispose functions");
                            return false;
                        }

                        break;
                    case OutputType.terminal:
                        Type TerminalAssembly = assembly.GetType(assemblyNamespace + ".Program");
                        MethodInfo main = TerminalAssembly.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn);
                        if(main != null)
                        {
                            userCodeMain = (terminalDelegate)Delegate.CreateDelegate(typeof(terminalDelegate), main);
                        }
                        else
                        {
                            Console.WriteLine("Missing or cannot find Main function");
                            return false;
                        }
                        break;
                }

                loaded = true;
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to load compiled assembly\n" + e.ToString());
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is CodeFile file &&
                   name == file.name &&
                   path == file.path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name, path);
        }
    }
}
