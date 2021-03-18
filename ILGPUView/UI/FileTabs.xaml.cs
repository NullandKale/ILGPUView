using ILGPUView.Files;
using ILGPUView.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for FileTabs.xaml
    /// </summary>
    public partial class FileTabs : UserControl
    {
        public List<CodeFile> displayedFiles;
        public CodeFile file;
        public Action onCurrentFileUpdated;
        public Action onFileChanged;

        public FileTabs()
        {
            InitializeComponent();
            displayedFiles = new List<CodeFile>();
            files.SelectionChanged += Files_SelectionChanged;
        }

        private void Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(files.SelectedIndex > -1 && files.SelectedIndex < displayedFiles.Count)
            {
                file = displayedFiles[files.SelectedIndex];
            }

            if(files.SelectedIndex == -1)
            {
                file = null;
            }

            if(onFileChanged != null)
            {
                onFileChanged();
            }
        }

        public void CloseCodeFile(CodeFile code)
        {
            if(code.needsSave)
            {
                SaveCodeFile(code);
            }

            if(displayedFiles.Contains(code))
            {
                int id = displayedFiles.IndexOf(code);
                if(code == file)
                {
                    file = null;
                }
                displayedFiles.RemoveAt(id);
                files.Items.RemoveAt(id);

                if(MainWindow.sampleTestMode && files.Items.Count == 0)
                {
                    Console.WriteLine("Finished");
                    Logger.staticInstance.Save();
                    AddCodeFile(new CodeFile("Program.cs", OutputType.bitmap, Templates.bitmapTemplate));
                }
            }
        }

        public void SaveCodeFile(CodeFile code)
        {
            SaveFileWindow sfw = new SaveFileWindow(code.name);
            if (sfw.ShowDialog() == true)
            {
                SaveFileDialog sfDialog = new SaveFileDialog();
                sfDialog.Filter = "C# file (*.cs)|*.cs|Text file (*.txt)|*.txt";
                sfDialog.InitialDirectory = code.path;
                sfDialog.FileName = code.name;

                if (sfDialog.ShowDialog() == true)
                {
                    code.TrySave();
                }
            }
        }

        public void AddCodeFile(CodeFile code)
        {
            if(displayedFiles.Contains(code))
            {
                files.SelectedIndex = displayedFiles.IndexOf(code);
                file = code;
            }
            else
            {
                TabItem item = new TabItem();
                Button header = new Button();
                header.Content = code.name;
                header.Click += (object sender, RoutedEventArgs e) =>
                {
                    files.SelectedIndex = displayedFiles.IndexOf(code);
                };
                header.MouseDown += (object sender, MouseButtonEventArgs e) => 
                {
                    if(e.MiddleButton == MouseButtonState.Pressed)
                    {
                        CloseCodeFile(code);
                    }
                };
                item.Header = header;
                item.Content = new FileTab(code, (string newText) =>
                {
                    code.updateFileContents(newText);
                    if (onCurrentFileUpdated != null)
                    {
                        onCurrentFileUpdated();
                    }
                });

                displayedFiles.Add(code);
                file = code;
                files.Items.Add(item);
                files.SelectedItem = item;
            }
        }
    }
}
