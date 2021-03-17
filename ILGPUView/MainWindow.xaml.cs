using ILGPUViewTest;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ILGPUView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CodeManager code;
        Task compileTask = null;
        bool codeRunnable = false;
        bool isRunning = false;

        bool DEBUG = false;

        Thread renderThread;

        public MainWindow()
        {
            InitializeComponent();

            outputTabs.render.onResolutionChanged = onResolutionChanged;

            code = new CodeManager();
            fileTabs.defaultCodeBlock.Text = Templates.codeTemplate;
            fileTabs.defaultCodeBlock.TextChanged += Codeblock_TextChanged;

            for(int i = 0; i < 4; i++)
            {
                ((ComboBoxItem)acceleratorPicker.Items.GetItemAt(i)).Content += code.getDesc((AcceleratorType)i);
            }

            Closed += MainWindow_Closed;
        }

        private void renderThreadMain()
        {
            try
            {
                if(DEBUG)
                {
                    Test.setup(code.accelerator, outputTabs.render.width, outputTabs.render.height);
                }
                else
                {
                    code.setupUserCode(code.accelerator, outputTabs.render.width, outputTabs.render.height);
                }

                while (isRunning)
                {
                    if (DEBUG)
                    {
                        isRunning = Test.loop(code.accelerator, ref outputTabs.render.framebuffer);
                    }
                    else
                    {
                        isRunning = code.loopUserCode(code.accelerator, ref outputTabs.render.framebuffer);
                    }

                    Dispatcher.InvokeAsync(() =>
                    {
                        outputTabs.render.update(ref outputTabs.render.framebuffer);
                    });
                    Thread.Sleep(10);
                }

                code.dispose();
                isRunning = false;
                codeRunnable = false;
                Dispatcher.InvokeAsync(() =>
                {
                    runButton.Content = "Run";
                    status.Content = "Uncompiled";
                });
                Thread.Sleep(2);
            }
            catch(Exception e)
            {
                isRunning = false;
                codeRunnable = false;
                Dispatcher.InvokeAsync(() =>
                {
                    outputTabs.error.Text = e.ToString();
                });
            }
        }

        private void onResolutionChanged(int width, int height)
        {
            resolution.Content = width + " " + height + " @ " + outputTabs.render.scale + "x";
        }

        private void Codeblock_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            status.Content = "Uncompiled";
            codeRunnable = false;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            code.dispose();
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            if(compileTask != null)
            {
                compileTask.Wait();
            }

            isRunning = false;
            if(renderThread != null)
            {
                renderThread.Join();
                renderThread = null;
            }

            status.Content = "Compiling";

            string s = fileTabs.defaultCodeBlock.Text;
            AcceleratorType accelerator = (AcceleratorType)acceleratorPicker.SelectedIndex;

            Task.Run(() =>
            {
                code.InitializeILGPU(accelerator);
                if(code.CompileCode(s, out string err))
                {
                    Dispatcher.Invoke(() =>
                    {
                        outputTabs.error.Text = err;
                        status.Content = "Compiled " + s.Split("\n").Length + " lines OK";
                        codeRunnable = true;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        outputTabs.error.Text = err;
                        status.Content = "Failed to compile";
                    });
                }
            });
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if(isRunning)
            {
                isRunning = false;
                if (renderThread != null)
                {
                    renderThread.Join();
                    renderThread = null;
                }
            }
            else
            {
                if (codeRunnable)
                {
                    isRunning = true;
                    renderThread = new Thread(renderThreadMain);
                    renderThread.Start();
                    runButton.Content = "Stop";
                    return;
                }
                else
                {
                    //Compile_Click(sender, e);
                    return;
                }
            }
        }
    }
}
