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
using ILGPUViewTest;

namespace ILGPUView
{
    public class CodeManager
    {
        public Context context;
        public Accelerator accelerator;

        public MemoryStream compiledCode;

        public setupDelegate setupUserCode;
        public loopDelegate loopUserCode;
        public disposeDelegate disposeUserCode;

        public CodeManager()
        {
        }

        public void dispose()
        {
            if(disposeUserCode != null)
            {
                disposeUserCode();
            }

            if (accelerator != null)
            {
                accelerator.Dispose();
                context.Dispose();
            }
        }

        //public string getDesc(AcceleratorType type)
        //{
        //    switch (type)
        //    {
        //        case AcceleratorType.Default:
        //        case AcceleratorType.CPU:
        //            return CPUAccelerator.Accelerators.FirstOrDefault().ToString();
        //        case AcceleratorType.Cuda:
        //            return CudaAccelerator.CudaAccelerators.FirstOrDefault().ToString();
        //        case AcceleratorType.OpenCL:
        //            return CLAccelerator.AllCLAccelerators.FirstOrDefault().ToString();
        //    }

        //    return "";

        //}

        //public bool InitializeILGPU(AcceleratorType type)
        //{
        //    context = new Context();

        //    switch (type)
        //    {
        //        case AcceleratorType.Default:
        //            accelerator = new CPUAccelerator(context);
        //            break;
        //        case AcceleratorType.CPU:
        //            accelerator = new CPUAccelerator(context);
        //            break;
        //        case AcceleratorType.Cuda:
        //            accelerator = new CudaAccelerator(context);
        //            break;
        //        case AcceleratorType.OpenCL:
        //            accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());
        //            break;
        //    }

        //    return true;
        //}

        public bool CompileCode(string s)
        {
            try
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
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
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
                        Console.WriteLine(diagnostic.Id + ": " + diagnostic.GetMessage());
                    }

                    return false;
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
                    setupUserCode = (setupDelegate)Delegate.CreateDelegate(typeof(setupDelegate), type.GetMethod("setup"));
                    loopUserCode = (loopDelegate)Delegate.CreateDelegate(typeof(loopDelegate), type.GetMethod("loop"));
                    disposeUserCode = (disposeDelegate)Delegate.CreateDelegate(typeof(disposeDelegate), type.GetMethod("dispose"));

                    return true;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

        }
    }
}
