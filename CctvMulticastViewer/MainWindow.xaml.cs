using CctvMulticastViewer.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace CctvMulticastViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IPAddress _multicastAddress;
		private readonly Viewer _viewer;

		public MainWindow(Viewer viewer)
		{
			this.InitializeComponent();
						
			this._viewer = viewer;
			this.Title = $"CCTV Viewer - {this._viewer.Name}";

			this.ChangeLayoutCommand = new GenericCommand(this.ChangeLayoutCommandCallback);
		}

		private void ChangeLayoutCommandCallback(object parameter)
		{
			if(parameter is Layout layout)
			{
				this.LoadLayout(layout);
			}
		}

		public ICommand ChangeLayoutCommand { get; }

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			Task.Run(this.LoadLayoutMenu);
			Task.Run(() => this._multicastAddress = IPAddress.Parse(DBHelper.FetchMulticastIPAddress()));
		}

		private async Task LoadLayoutMenu()
		{
			//-- Retrieve the layouts attached to this viewer
			var layouts = await DBHelper.FetchLayoutsAsync();

			//-- Add the layouts to the context menu
			this.Dispatcher.Invoke(() => this.ctxMenu.ItemsSource = layouts);
		}

		private void LoadLayout(Layout layout)
		{
			//-- Stop the existing layout
			this.StopExistingLayout();

			//-- Create the new layout
			var control = new LayoutControl(layout, this._multicastAddress);
			this.LayoutHolder.Content = control;
			control.StartStreams();
		}

		private void StopExistingLayout()
		{
			if(this.LayoutHolder.Content is LayoutControl existingLayout)
			{
				existingLayout.StopStreams();
			}
		}
	}
}
