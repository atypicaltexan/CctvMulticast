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
		private Viewer _viewer;

		public MainWindow(Viewer viewer)
		{
			this._viewer = viewer;

			this.InitializeComponent();
		}

		public ICommand ChangeLayoutCommand { get; }

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			(this.ctxMenu.Items[0] as MenuItem).Header = this._viewer.Name;
			Task.Run(this.LoadLayoutMenu);
			Task.Run(() => this._multicastAddress = IPAddress.Parse(DBHelper.FetchMulticastIPAddress()));
		}

		private async Task LoadLayoutMenu()
		{
			//-- Retrieve the layouts attached to this viewer
			var layouts = await DBHelper.FetchLayoutsAsync();

			//-- Add the layouts to the context menu
			this.Dispatcher.Invoke(() =>
			{
				foreach(var layout in layouts)
				{
					var menuItem = new MenuItem {
						DataContext = layout,
						Header = layout.Description,
					};
					menuItem.Click += this.ChangeLayout_Click;
					this.ctxMenu.Items.Add(menuItem);
				}
			});
		}

		private void ChangeLayout_Click(object sender, RoutedEventArgs e)
		{
			if(sender is MenuItem menuItem 
				&& menuItem.DataContext is Layout layout)
			{
				this.LoadLayout(layout);
			}
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

		private void Minimize_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
