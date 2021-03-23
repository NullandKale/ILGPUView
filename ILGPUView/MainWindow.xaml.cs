using ILGPUView.Files;
using ILGPUView.UI;
using ILGPUView.Utils;
using ILGPUViewTest;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ILGPUView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Sample mode test stuff
        public static bool sampleTestMode = false;
        public static Dictionary<string, string> sampleRunStatus = new Dictionary<string, string>();

        SampleManager files;
        FileRunner fileRunner;

        public MainWindow()
        {
            InitializeComponent();

            InputBindings.Add(new KeyBinding(new WindowCommand() { ExecuteDelegate = () => { Save_Click(null, null); } }, new KeyGesture(Key.S, ModifierKeys.Control)));

            outputTabs.render.onResolutionChanged = onResolutionChanged;

            fileTabs.onCurrentFileUpdated = onCurrentFileUpdated;
            fileTabs.onFileChanged = onFileChanged;

            files = new SampleManager(fileTabs, onSampleSearchComplete);

            for(int i = 0; i < 4; i++)
            {
                ((ComboBoxItem)acceleratorPicker.Items.GetItemAt(i)).Content += FileRunner.getDesc((AcceleratorType)i);
            }

            Closed += MainWindow_Closed;

            if(!sampleTestMode)
            {
                fileTabs.AddCodeFile(new CodeFile("Program.cs", OutputType.bitmap, Templates.bitmapTemplate));
            }
        }

        private void onFileChanged()
        {
            if (fileRunner != null)
            {
                fileRunner.Stop();
                fileRunner = null;
            }
        }

        private void onCurrentFileUpdated()
        {
            if(fileRunner != null)
            {
                fileRunner.Stop();
                fileRunner = null;
            }
            runButton.Content = "Compile";
            status.Content = "File Changed needs Compile";
        }

        private void onResolutionChanged(int width, int height)
        {
            resolution.Content = width + " " + height + " @ " + outputTabs.render.scale + "x";
        }

        private void onSampleSearchComplete()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (string s in files.getSampleNames())
                {
                    string Header = s.Substring(s.LastIndexOf("\\") + 1);
                    MenuItem sampleItem = new MenuItem();

                    if(Header == "Mandelbrot")
                    {
                        Header += " needs forms (Fails to compile)";
                    }
                    if (Header == "AlgorithmsReduce")
                    {
                        Header += " needs cuda 10 sdk";
                    }
                    if (Header == "MatrixMultiply" || Header == "DynamicSharedMemory")
                    {
                        Header += " BUG (Fails to compile)";
                    }

                    sampleItem.Header = Header;

                    string sRef = s;
                    sampleItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        files.LoadSample(sRef);
                    };

                    samples.Items.Add(sampleItem);
                }

                if (sampleTestMode)
                {
                    files.OpenAllSamples();
                    Console.WriteLine("START SAMPLE " + fileTabs.file.assemblyNamespace);
                    if(!sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                    {
                        sampleRunStatus.Add(fileTabs.file.assemblyNamespace, "Started");
                    }
                }
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if(fileRunner != null)
            {
                fileRunner.Stop();
            }
        }

        private void OnTimerUpdate(TimeSpan setupTime, double lastUpdateMS)
        {
            //Console.WriteLine("Setup: " + setupTime + " last update: " + lastUpdateMS);
        }

        private void OnRunStop()
        {
            Dispatcher.InvokeAsync(() =>
            {
                status.Content = "Stopped";
                if (fileRunner != null)
                {
                    if(sampleTestMode)
                    {
                        if (sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                        {
                            sampleRunStatus[fileTabs.file.assemblyNamespace] = fileRunner.crashed ? "Crashed" : "Finished";
                        }

                        fileTabs.CloseCodeFile(fileRunner.code);
                        Console.WriteLine("START SAMPLE: " + fileTabs.file.assemblyNamespace);
                        if (!sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                        {
                            sampleRunStatus.Add(fileTabs.file.assemblyNamespace, "Started");
                        }
                        Run_Click(null, null);
                    }
                    fileRunner = null;
                }
                runButton.Content = "Run";
            });
        }

        private void FrameBufferSwap()
        {
            Dispatcher.Invoke(() =>
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
                }
                else
                {
                    if (fileTabs.file.compiled && fileTabs.file.loaded)
                    {
                        outputTabs.log.clear();
                        runButton.Content = "Stop";
                        status.Content = "Running";

                        if (sampleTestMode && sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                        {
                            sampleRunStatus[fileTabs.file.assemblyNamespace] = "Attempting to Run";
                        }

                        fileRunner = new FileRunner(fileTabs.file, outputTabs, (AcceleratorType)acceleratorPicker.SelectedIndex, OnRunStop, FrameBufferSwap, OnTimerUpdate);
                        fileRunner.Run();
                    }
                    else
                    {
                        outputTabs.log.clear();
                        status.Content = "Compiling " + fileTabs.file.name;

                        Task.Run(() =>
                        {
                            if (fileTabs.file.TryCompile())
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    runButton.Content = "Run";
                                    status.Content = "Compiled " + fileTabs.file.fileContents.Split("\n").Length + " lines OK";
                                    if (sampleTestMode && sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                                    {
                                        sampleRunStatus[fileTabs.file.assemblyNamespace] = "Compiled OK";
                                        Run_Click(null, null);
                                    }
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    status.Content = "Failed to compile";
                                    if (sampleTestMode)
                                    {
                                        if (sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                                        {
                                            if(fileTabs.file.assemblyNamespace == "Mandelbrot")
                                            {
                                                sampleRunStatus[fileTabs.file.assemblyNamespace] = "Failed to compile (Mandelbrot sample requires forms.)";
                                            }
                                            else
                                            {
                                                sampleRunStatus[fileTabs.file.assemblyNamespace] = "Failed to compile";
                                            }
                                        }

                                        fileTabs.CloseCodeFile(fileTabs.file);
                                        Console.WriteLine("START SAMPLE " + fileTabs.file.assemblyNamespace);
                                        if (!sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                                        {
                                            sampleRunStatus.Add(fileTabs.file.assemblyNamespace, "Started");
                                        }
                                        Run_Click(null, null);
                                    }
                                });
                            }
                        });

                    }
                }
            }
        }

        private void BasicBitmap_Click(object sender, RoutedEventArgs e)
        {
            NewFileWindow nfw = new NewFileWindow(OutputType.bitmap);
            if(nfw.ShowDialog() == true)
            {
                fileTabs.AddCodeFile(new CodeFile(nfw.filename, OutputType.bitmap, Templates.bitmapTemplate));
            }
        }

        private void BasicTerminal_Click(object sender, RoutedEventArgs e)
        {
            NewFileWindow nfw = new NewFileWindow(OutputType.terminal);
            if (nfw.ShowDialog() == true)
            {
                fileTabs.AddCodeFile(new CodeFile(nfw.filename, OutputType.terminal, Templates.terminalTemplate));
            }
        }

        private void OpenBFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileName;
                CodeFile file = new CodeFile(Path.GetFileName(filename), filename.Substring(0, filename.Length - Path.GetFileName(filename).Length), OutputType.bitmap);
                if(file.TryLoad())
                {
                    fileTabs.AddCodeFile(file);
                }
            }
        }

        private void OpenTFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string filename = openFileDialog.FileName;
                    CodeFile file = new CodeFile(Path.GetFileName(filename), filename.Substring(0, filename.Length - Path.GetFileName(filename).Length), OutputType.terminal);
                    if (file.TryLoad())
                    {
                        fileTabs.AddCodeFile(file);
                    }
                }
            }
        }

        private void TutorialClicked(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                if(int.TryParse(((string)((MenuItem)sender).Header).Split(" ")[0], out int tutorial))
                {
                    fileTabs.AddCodeFile(new CodeFile("Program" + tutorial + ".cs", OutputType.terminal, Templates.getTutorial(tutorial)));
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if(fileTabs.file != null)
            {
                fileTabs.SaveCodeFile(fileTabs.file, false, false);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            fileTabs.Undo();
        }
    }
}
