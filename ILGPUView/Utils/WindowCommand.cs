using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ILGPUView.Utils
{
    // Why is this so hard? wpf's pattern is not as good as androids pattern
    //https://stackoverflow.com/a/38601867/1500733
    public class WindowCommand : ICommand
    {
        public Action ExecuteDelegate { get; set; }
        public bool CanExecute(object parameter)
        {
            return true; 
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (ExecuteDelegate != null)
            {
                ExecuteDelegate();
            }
            else
            {
                Console.WriteLine("Command ExecuteDelegate not set");
            }
        }
    }
}
