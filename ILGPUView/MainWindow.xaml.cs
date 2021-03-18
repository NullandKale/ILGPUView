using ILGPUView.Files;
using ILGPUViewTest;
using System;
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
        FileManager files;
        FileRunner fileRunner;

        public MainWindow()
        {
            InitializeComponent();

            outputTabs.render.onResolutionChanged = onResolutionChanged;

            fileTabs.onCurrentFileUpdated = onCurrentFileUpdated;

            files = new FileManager(fileTabs, onSampleSearchComplete);

            for(int i = 0; i < 4; i++)
            {
                ((ComboBoxItem)acceleratorPicker.Items.GetItemAt(i)).Content += FileRunner.getDesc((AcceleratorType)i);
            }

            Closed += MainWindow_Closed;
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
            if(fileRunner != null)
            {
                fileRunner.Stop();
            }
        }

        private void OnRunStop()
        {
            Dispatcher.InvokeAsync(() =>
            {
                status.Content = "Stopped - Still Compiled";
                if (fileRunner != null)
                {
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
        }
    }
}
