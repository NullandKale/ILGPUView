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
using System.Windows.Shapes;

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for NewFileWindow.xaml
    /// </summary>
    public partial class NewFileWindow : Window
    {
        public string filename = "Program.cs";

        public NewFileWindow(OutputType type)
        {
            InitializeComponent();
            Title = type == OutputType.bitmap ? "New Bitmap File" : "New Terminal File";
            label.Content = "Enter filename for new " + (type == OutputType.bitmap ? "bitmap" : "terminal") + " file.";
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            filename = Filename.Text;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
