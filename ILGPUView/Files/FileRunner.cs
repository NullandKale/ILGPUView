using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using ILGPUView.UI;
using ILGPUViewTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ILGPUView.Files
{
    public enum AcceleratorType
    {
        Default = 0,
        CPU = 1,
        Cuda = 2,
        OpenCL = 3
    }

    public class FileRunner
    {
        public static readonly bool DEBUG = false;

        public Context context;
        public Accelerator accelerator;

        public CodeFile code;
        public OutputTabs output;
        public AcceleratorType type;

        private bool isRunning = false;
        private Thread renderThread;

        private Action onRunStop;
        private Action framebufferSwap;

        public FileRunner(CodeFile code, OutputTabs output, AcceleratorType type, Action onRunStop, Action framebufferSwap)
        {
            this.code = code;
            this.output = output;
            this.type = type;
            this.onRunStop = onRunStop;
            this.framebufferSwap = framebufferSwap;
        }

        public void dispose()
        {
            if (code.userCodeDispose != null)
            {
                code.userCodeDispose();
            }

            if (accelerator != null)
            {
                accelerator.Dispose();
            }

            if(context != null)
            {
                context.Dispose();
            }
        }

        public static string getDesc(AcceleratorType type)
        {
            switch (type)
            {
                case AcceleratorType.Default:
                case AcceleratorType.CPU:
                    return CPUAccelerator.Accelerators.FirstOrDefault().ToString();
                case AcceleratorType.Cuda:
                    return CudaAccelerator.CudaAccelerators.FirstOrDefault().ToString();
                case AcceleratorType.OpenCL:
                    return CLAccelerator.AllCLAccelerators.FirstOrDefault().ToString();
            }

            return "";

        }

        public bool InitializeILGPU()
        {
            context = new Context();

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

        public bool Run()
        {
            if (code.compiled && code.loaded && !isRunning && InitializeILGPU())
            {
                isRunning = true;
                renderThread = new Thread(renderThreadMain);
                renderThread.Start();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Stop()
        {
            isRunning = false;
            dispose();
        }

        private void renderThreadMain()
        {
            try
            {
                if (DEBUG)
                {
                    Test.setup(accelerator, output.render.width, output.render.height);
                }
                else
                {
                    if(code.type == OutputType.terminal)
                    {
                        code.userCodeMain();
                        return;
                    }
                    else
                    {
                        code.userCodeSetup(accelerator, output.render.width, output.render.height);
                    }
                }

                while (isRunning)
                {
                    if (DEBUG)
                    {
                        isRunning = Test.loop(accelerator, ref output.render.framebuffer);
                    }
                    else
                    {
                        isRunning = code.userCodeLoop(accelerator, ref output.render.framebuffer);
                    }

                    framebufferSwap();
                    Thread.Sleep(10);
                }

                isRunning = false;
                onRunStop();
            }
            catch (Exception e)
            {
                isRunning = false;
                onRunStop();
                Console.WriteLine("Render Thread Failed\n" + e.ToString());
            }
        }
    }
}
