using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using ILGPUView.UI;
using ILGPUView.Utils;
using ILGPUViewTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static bool DEBUG = false;

        public Context context;
        public Accelerator accelerator;

        public FrameTimer timer;
        public CodeFile code;
        public OutputTabs output;
        public AcceleratorType type;

        private bool isRunning = false;
        public bool crashed = false;
        private Thread renderThread;

        private Action onRunStop;
        private Action framebufferSwap;
        private Action<TimeSpan, double> onTimersUpdate;

        public FileRunner(CodeFile code, OutputTabs output, AcceleratorType type, Action onRunStop, Action framebufferSwap, Action<TimeSpan, double> onTimersUpdate)
        {
            this.code = code;
            this.output = output;
            this.type = type;
            this.onRunStop = onRunStop;
            this.framebufferSwap = framebufferSwap;
            this.onTimersUpdate = onTimersUpdate;
            timer = new FrameTimer();
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
                    return CPUAccelerator.CPUAccelerators.FirstOrDefault().ToString();
                case AcceleratorType.Cuda:
                    return CudaAccelerator.CudaAccelerators.FirstOrDefault().ToString();
                case AcceleratorType.OpenCL:
                    return CLAccelerator.AllCLAccelerators.FirstOrDefault().ToString();
            }

            return "";

        }

        public bool InitializeILGPU()
        {
            context = new Context(ContextFlags.EnableAssertions);
            context.EnableAlgorithms();

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
                renderThread.IsBackground = true;
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
        }

        private void renderThreadMain()
        {
            Stopwatch setupTimer = Stopwatch.StartNew();

            try
            {
                crashed = false;
                if (DEBUG)
                {
                    Test.setup(accelerator, output.render.scaledWidth, output.render.scaledHeight);
                    setupTimer.Stop();
                    onTimersUpdate(setupTimer.Elapsed, -1);
                }
                else
                {
                    if(code.outputType == OutputType.terminal)
                    {
                        code.userCodeMain();
                        setupTimer.Stop();
                        onTimersUpdate(setupTimer.Elapsed, -1);
                        isRunning = false;
                        crashed = false;
                        onRunStop();
                        return;
                    }
                    else
                    {
                        code.userCodeSetup(accelerator, output.render.scaledWidth, output.render.scaledHeight);
                        setupTimer.Stop();
                        onTimersUpdate(setupTimer.Elapsed, -1);
                    }
                }

                while (isRunning)
                {
                    timer.startUpdate();
                    bool notStop = true;

                    if (DEBUG)
                    {
                        notStop = Test.loop(accelerator, ref output.render.framebuffer);
                    }
                    else
                    {
                        notStop = code.userCodeLoop(accelerator, ref output.render.framebuffer);
                    }

                    if(notStop == false)
                    {
                        isRunning = false;
                    }

                    framebufferSwap();
                    timer.endUpdate();
                    onTimersUpdate(setupTimer.Elapsed, timer.averageUpdateRate);
                }

                isRunning = false;
                dispose();
                onRunStop();
            }
            catch (Exception e)
            {
                isRunning = false;
                crashed = true;
                onRunStop();
                Console.WriteLine("Render Thread Failed\n" + e.ToString());
            }
        }
    }
}
