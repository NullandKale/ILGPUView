using ILGPUView.Files;
using ILGPUView.UI;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ILGPUView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool sampleTestMode = true;
        public static Dictionary<string, string> sampleRunStatus = new Dictionary<string, string>();

        FileManager files;
        FileRunner fileRunner;

        public MainWindow()
        {
            InitializeComponent();

            outputTabs.render.onResolutionChanged = onResolutionChanged;

            fileTabs.onCurrentFileUpdated = onCurrentFileUpdated;
            fileTabs.onFileChanged = onFileChanged;

            files = new FileManager(fileTabs, onSampleSearchComplete);

            for(int i = 0; i < 4; i++)
            {
                ((ComboBoxItem)acceleratorPicker.Items.GetItemAt(i)).Content += FileRunner.getDesc((AcceleratorType)i);
            }

            Closed += MainWindow_Closed;
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
                    string Header = s;
                    MenuItem sampleItem = new MenuItem();
                    sampleItem.Header = Header.Substring(Header.LastIndexOf("\\") + 1);
                    sampleItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        files.LoadSample(Header);
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
                }
                else
                {
                    if (fileTabs.file.compiled && fileTabs.file.loaded)
                    {
                        outputTabs.log.clear();
                        runButton.Content = "Stop";
                        status.Content = "Running";

                        if (sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                        {
                            sampleRunStatus[fileTabs.file.assemblyNamespace] = "Attempting to Run";
                        }

                        fileRunner = new FileRunner(fileTabs.file, outputTabs, (AcceleratorType)acceleratorPicker.SelectedIndex, OnRunStop, FrameBufferSwap);
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
                                    if (sampleRunStatus.ContainsKey(fileTabs.file.assemblyNamespace))
                                    {
                                        sampleRunStatus[fileTabs.file.assemblyNamespace] = "Compiled OK";
                                    }
                                    Run_Click(null, null);
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
                                            sampleRunStatus[fileTabs.file.assemblyNamespace] = "Failed to compile";
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
    }
}
