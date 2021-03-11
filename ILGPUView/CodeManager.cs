using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.OpenCL;
using ILGPU.Runtime.Cuda;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ILGPUView
{
    public enum AcceleratorType
    {
        Default,
        CPU,
        Cuda,
        OpenCL
    }

    public class CodeManager
    {
        public Context context;
        public Accelerator accelerator;

        public MemoryStream compiledCode;
        public setupDelegate setup;
        public loopDelegate loop;

        public CodeManager()
        {
            InitializeILGPU(AcceleratorType.CPU);

            //compiled at built time
            ILGPUViewTest.Test.setup(accelerator, 100, 100);

            //compiled at run time
            CompileCode(Templates.codeTemplate);
            setup(accelerator, 100, 100);
        }

        private bool InitializeILGPU(AcceleratorType type)
        {
            context = new Context(ContextFlags.EnableDebugSymbols | ContextFlags.EnableKernelDebugInformation);

            switch (type)
            {
                case AcceleratorType.Default:
                    accelerator = new CPUAccelerator(context);
                    break;
                case AcceleratorType.CPU:
                    accelerator = new CPUAccelerator(context);
                    break;
                case AcceleratorType.Cuda:
                    accelerator = new CudaAccelerator(context);
                    break;
                case AcceleratorType.OpenCL:
                    accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());
                    break;
            }

            return true;
        }

        private void CompileCode(string s)
        {
            // The following is magic taken from https://stackoverflow.com/a/29417053/1500733

            // define source code, then parse it (to the type used for compilation)
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(s);

            // define other necessary objects for compilation
            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Context).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Trace).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=5.0.0.0").Location),
            };

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

                foreach (Diagnostic diagnostic in failures)
                {
                    Trace.WriteLine(diagnostic.Id + ": " + diagnostic.GetMessage());
                }
            }
            else
            {
                // load this 'virtual' DLL so that we can use
                compiledCode.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(compiledCode.ToArray());

                // create instance of the desired class and call the desired function
                Type type = assembly.GetType("ILGPUViewTest.Test");
                //Object obj = Activator.CreateInstance(type);

                //this is where I save the delegates
                setup = (setupDelegate)Delegate.CreateDelegate(typeof(setupDelegate), type.GetMethod("setup"));
                loop = (loopDelegate)Delegate.CreateDelegate(typeof(loopDelegate), type.GetMethod("loop"));
            }

        }
    }
}
