using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace CctvMulticastViewer
{
	public class GenericCommand :
		ICommand
	{
		private readonly Action<object> _executeCallback;

		public GenericCommand(Action<object> executeCallback)
		{
			this._executeCallback = executeCallback;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			this._executeCallback?.Invoke(parameter);
		}
	}
}
