using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CctvMulticastViewer
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			//-- Make sure there is not already a copy running
			if(!TryKillOtherInstances())
			{
				return;
			}

			//-- Read the config file
			try
			{
				Config.Initialize();
			}
			catch(Exception ex)
			{
				MessageBox.Show($"Cannot read configuration: {ex.Message}");
				return;
			}

			//-- Fetch the viewer info
			Viewer viewer;
			try
			{
				DBHelper.Initialize();
				viewer = DBHelper.FetchViewer();
			}
			catch(Exception ex)
			{
				MessageBox.Show($"Cannot fetch viewer info from database: {ex.Message}");
				return;
			}

			//-- Snuff task exceptions if they are not caught properly
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			//-- Launch the main window and pass in the viewer
			var app = new Application();
			app.Run(new MainWindow(viewer));
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			//-- Snuff it
			e.SetObserved();
		}

		private static bool TryKillOtherInstances()
		{
			//-- Get all of the processes of this type
			var allProcesses = Process.GetProcessesByName(Assembly.GetExecutingAssembly().GetName().Name);

			//-- Get the current process id
			var currentProcessID = Process.GetCurrentProcess().Id;

			//-- Get the processes that are not this process
			var otherProcesses = allProcesses.Where(p => p.Id != currentProcessID).ToArray();

			//-- If there are others to close, then show a message and close them
			if(otherProcesses.Length > 0)
			{
				if(MessageBox.Show(
					"Another copy of the CCTV Viewer is open.\r\nClick [OK] to close the other copies and open this one.\r\nClick [Cancel] to not open this copy.",
					"Another Viewer Already Open",
					MessageBoxButton.OKCancel,
					MessageBoxImage.Warning) == MessageBoxResult.OK)
				{
					//-- Close the other processes
					foreach(var proc in otherProcesses)
					{
						proc.Kill();
					}

					return true;
				}
				else
				{
					//-- User chose not not open this copy
					return false;
				}
			}

			//-- There are no other copies open
			return true;
		}
	}
}
