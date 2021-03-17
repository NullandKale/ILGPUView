using ILGPUView.Files;
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
        //public List<CodeFile> displayedFiles;
        public CodeFile file;


        public FileTabs()
        {
            InitializeComponent();
            //displayedFiles = new List<CodeFile>();
        }

        public void AddCodeFile(CodeFile code)
        {
            files.Items.Clear();

            TabItem item = new TabItem();

            item.Header = code.name;
            item.Content = new FileTab(code, (string newText) =>
            {
                code.updateFileContents(newText);
            });

            //displayedFiles.Add(code);
            file = code;
            files.Items.Add(item);
            files.SelectedItem = item;
        }
    }
}
