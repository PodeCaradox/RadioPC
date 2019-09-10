using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RadioSX
{
    public class MyCopyCommand : ICommand
    {
        public string Name { get { return "Copy"; } }

        public void Execute(object parameter)
        {
            Clipboard.SetText(parameter != null ? parameter.ToString() : "<null>");
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
