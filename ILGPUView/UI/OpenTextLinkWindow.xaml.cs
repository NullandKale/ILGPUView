using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for OpenTextLinkWindow.xaml
    /// </summary>
    public partial class OpenTextLinkWindow : Window
    {
        public string loadedString;
        public string file;
        public OpenTextLinkWindow()
        {
            InitializeComponent();
            label.Content = "Paste text link here, and click load\nYou can also just drag the link or any file into the main window";
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WebClient webclient = new WebClient())
                {
                    loadedString = webclient.DownloadString(urlText.Text);
                    file = filename.Text;
                    DialogResult = true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to download:\n" + ex.ToString());
            }
        }
    }
}
