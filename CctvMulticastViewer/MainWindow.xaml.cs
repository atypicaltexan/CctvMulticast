using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
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
		private Control[] _contextMenuItems;

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

				//-- Save off the default menu items
				this._contextMenuItems = this.ctxMenu.Items.OfType<Control>().ToArray();
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

		private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
		{
			var itemsList = new List<Control>(this._contextMenuItems);

			var result = VisualTreeHelper.HitTest(this, new Point(e.CursorLeft, e.CursorTop));

			if(this.FindLayoutSlot(result.VisualHit) is LayoutSlot slot
				&& slot.CanUserChooseCamera)
			{
				this.AddSwapCameraMenuItems(itemsList, slot);
				this.AddCycleCameraMenuItems(itemsList, slot);
			}

			this.ctxMenu.Items.Clear();
			foreach(var item in itemsList)
			{
				this.ctxMenu.Items.Add(item);
			}
		}

		private void AddCycleCameraMenuItems(List<Control> itemsList, LayoutSlot slot)
		{
			itemsList.Add(new Separator());
			
			//-- Add the context menu items
			var menuItem = new MenuItem {
				Header = "Cycle Options",
			};
			itemsList.Add(menuItem);

			foreach(var optionalCamera in slot.OptionalCameras)
			{
				var chooseItem = new MenuItem {
					Header = optionalCamera.Camera.Name,
					Tag = optionalCamera,
					IsCheckable = true,
					IsChecked = slot.CyclingImages.Any(i => i.Camera.ID == optionalCamera.OptionalCameraID),
				};

				chooseItem.Click += (sender, e) =>
				{
					slot.ToggleCycleCamera(optionalCamera);
				};

				menuItem.Items.Add(chooseItem);
			}
		}

		private void AddSwapCameraMenuItems(List<Control> itemsList, LayoutSlot slot)
		{
			//-- Add the context menu items
			var menuItem = new MenuItem {
				Header = "Swap this camera",
			};
			itemsList.Add(new Separator());
			itemsList.Add(menuItem);
			foreach(var optionalCamera in slot.OptionalCameras)
			{
				var swapItem = new MenuItem {
					Header = optionalCamera.Camera.Name,
					Tag = optionalCamera,
				};
				swapItem.Click += (sender, e) =>
				{
					if(this.LayoutHolder.Content is LayoutControl existingLayout)
					{
						existingLayout.SwapCamera(slot, optionalCamera);
					}
				};

				menuItem.Items.Add(swapItem);
			}
		}

		private LayoutSlot FindLayoutSlot(DependencyObject visualHit)
		{
			var current = visualHit;

			while(current != null && !(current is LayoutSlot))
			{
				current = VisualTreeHelper.GetParent(current);
			}

			return current as LayoutSlot;
		}
	}
}
