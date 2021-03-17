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
    /// Interaction logic for FileTab.xaml
    /// </summary>
    public partial class FileTab : UserControl
    {
        public CodeFile codeFile;
        Action<string> onTextChange;

        public FileTab(CodeFile codeFile, Action<string> onTextChanged)
        {
            InitializeComponent();

            this.onTextChange = onTextChanged;
            this.codeFile = codeFile;

            code.Text = codeFile.fileContents;
            code.TextChanged += Code_TextChanged;
        }

        private void Code_TextChanged(object sender, TextChangedEventArgs e)
        {
            onTextChange(code.Text);
        }
    }
}
