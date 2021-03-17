using ILGPUView.Files;
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
        FileManager files;
        FileRunner fileRunner;

        public MainWindow()
        {
            InitializeComponent();

            outputTabs.render.onResolutionChanged = onResolutionChanged;

            files = new FileManager(fileTabs, onSampleSearchComplete);

            for(int i = 0; i < 4; i++)
            {
                ((ComboBoxItem)acceleratorPicker.Items.GetItemAt(i)).Content += FileRunner.getDesc((AcceleratorType)i);
            }

            Closed += MainWindow_Closed;
        }

        //private void renderThreadMain()
        //{
        //    try
        //    {
        //        if(DEBUG)
        //        {
        //            Test.setup(code.accelerator, outputTabs.render.width, outputTabs.render.height);
        //        }
        //        else
        //        {
        //            code.setupUserCode(code.accelerator, outputTabs.render.width, outputTabs.render.height);
        //        }

        //        while (isRunning)
        //        {
        //            if (DEBUG)
        //            {
        //                isRunning = Test.loop(code.accelerator, ref outputTabs.render.framebuffer);
        //            }
        //            else
        //            {
        //                isRunning = code.loopUserCode(code.accelerator, ref outputTabs.render.framebuffer);
        //            }

        //            Dispatcher.InvokeAsync(() =>
        //            {
        //                outputTabs.render.update(ref outputTabs.render.framebuffer);
        //            });
        //            Thread.Sleep(10);
        //        }

        //        code.dispose();
        //        isRunning = false;
        //        codeRunnable = false;
        //        Dispatcher.InvokeAsync(() =>
        //        {
        //            runButton.Content = "Run";
        //            status.Content = "Uncompiled";
        //        });
        //        Thread.Sleep(2);
        //    }
        //    catch(Exception e)
        //    {
        //        isRunning = false;
        //        codeRunnable = false;
        //        Console.WriteLine("Render Thread Failed\n" + e.ToString());
        //    }
        //}

        private void onResolutionChanged(int width, int height)
        {
            resolution.Content = width + " " + height + " @ " + outputTabs.render.scale + "x";
        }

        private void onSampleSearchComplete()
        {
            Dispatcher.Invoke(() =>
            {
                foreach(string s in files.getSampleNames())
                {
                    string Header = s;
                    MenuItem sampleItem = new MenuItem();
                    sampleItem.Header = Header.Substring(Header.LastIndexOf("\\") + 1);
                    sampleItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        files.LoadSample(Header);
                    };

                    samples.Items.Add(sampleItem);
                }

                foreach (string s in files.getTemplateNames())
                {
                    string Header = s;
                    MenuItem sampleItem = new MenuItem();
                    sampleItem.Header = Header;
                    sampleItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        files.LoadTemplate(Header);
                    };

                    templates.Items.Add(sampleItem);
                }
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {

        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            //if(compileTask != null)
            //{
            //    compileTask.Wait();
            //}

            //outputTabs.log.clear();

            //isRunning = false;
            //if(renderThread != null)
            //{
            //    renderThread.Join();
            //    renderThread = null;
            //}

            //status.Content = "Compiling";

            //string s = "";//fileTabs.defaultCodeBlock.Text;
            //AcceleratorType accelerator = (AcceleratorType)acceleratorPicker.SelectedIndex;

            //Task.Run(() =>
            //{
            //    code.InitializeILGPU(accelerator);
            //    if(code.CompileCode(s))
            //    {
            //        Dispatcher.Invoke(() =>
            //        {
            //            status.Content = "Compiled " + s.Split("\n").Length + " lines OK";
            //            codeRunnable = true;
            //        });
            //    }
            //    else
            //    {
            //        Dispatcher.Invoke(() =>
            //        {
            //            status.Content = "Failed to compile";
            //        });
            //    }
            //});
        }

        private void OnRunStop()
        {
            Dispatcher.InvokeAsync(() =>
            {
                status.Content = "Stopped";
            });
        }

        private void FrameBufferSwap()
        {
            Dispatcher.InvokeAsync(() =>
            {
                outputTabs.render.update(ref outputTabs.render.framebuffer);
            });
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (fileTabs.file != null)
            {
                if (fileRunner != null)
                {
                    fileRunner.Stop();
                    fileRunner = null;
                }
                else
                {
                    if (fileTabs.file.compiled && fileTabs.file.loaded)
                    {
                        fileRunner = new FileRunner(fileTabs.file, outputTabs, (AcceleratorType)acceleratorPicker.SelectedIndex, OnRunStop, FrameBufferSwap);
                        fileRunner.Run();
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            if (fileTabs.file.TryCompile())
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    status.Content = "Compiled " + fileTabs.file.fileContents.Split("\n").Length + " lines OK";
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    status.Content = "Failed to compile";
                                });
                            }
                        });

                    }
                }
                
            }

            //if(isRunning)
            //{
            //    isRunning = false;
            //    if (renderThread != null)
            //    {
            //        renderThread.Join();
            //        renderThread = null;
            //    }
            //}
            //else
            //{
            //    if (codeRunnable)
            //    {
            //        isRunning = true;
            //        renderThread = new Thread(renderThreadMain);
            //        renderThread.Start();
            //        runButton.Content = "Stop";
            //        return;
            //    }
            //    else
            //    {
            //        Compile_Click(sender, e);
            //        return;
            //    }
            //}
        }
    }
}
