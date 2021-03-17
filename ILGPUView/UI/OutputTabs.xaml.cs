using ILGPUView.Files;
using ILGPUView.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for OutputTabs.xaml
    /// </summary>
    public partial class OutputTabs : UserControl
    {
        public Logger log;
        DispatcherOperation logTask;

        public OutputTabs()
        {
            InitializeComponent();
            log = new Logger(OnLogUpdated);
        }

        public void SetOutputType(OutputType type)
        {
            switch (type)
            {
                case OutputType.bitmap:
                    render.Visibility = Visibility.Visible;
                    break;
                case OutputType.terminal:
                    render.Visibility = Visibility.Collapsed;
                    break;
            }

            log.clear();
        }

        private void OnLogUpdated()
        {
            if(logTask == null || logTask.Status == DispatcherOperationStatus.Completed || logTask.Status == DispatcherOperationStatus.Aborted)
            {
                string s = log.text;
                logTask = Dispatcher.InvokeAsync(() =>
                {
                    terminal.Text = s;
                    terminalScroll.ScrollToBottom();
                });
            }
        }
    }
}
