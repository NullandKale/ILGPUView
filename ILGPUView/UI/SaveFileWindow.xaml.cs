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
    /// Interaction logic for SaveFileDialog.xaml
    /// </summary>
    public partial class SaveFileWindow : Window
    {
        public SaveFileWindow(string filename)
        {
            InitializeComponent();
            label.Content = filename + "\nhas unsaved changes, would you like to save?";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
