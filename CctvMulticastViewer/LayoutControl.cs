using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CctvMulticastViewer
{
	public class LayoutControl :
		Grid
	{
		private Layout _layout;
		private readonly IPAddress _multicastAddress;
		private CancellationTokenSource _stoppingTokenSource;
		private List<LayoutImage> _images = new List<LayoutImage>();

		public LayoutControl(Layout layout, IPAddress multicastAddress)
		{
			//-- Save the layout ID
			this._layout = layout;
			this._multicastAddress = multicastAddress;

			//-- Retrieve the cameras in the layout
			var cameras = DBHelper.FetchCamerasForLayout(layout.ID);

			//-- Set the row and column definitions
			for(var i = 0; i < this._layout.Rows; i++)
			{
				this.RowDefinitions.Add(new RowDefinition {
					Height = new GridLength(1, GridUnitType.Star),
				});
			}
			for(var i = 0; i < this._layout.Columns; i++)
			{
				this.ColumnDefinitions.Add(new ColumnDefinition {
					Width = new GridLength(1, GridUnitType.Star),
				});
			}

			//-- Add the images
			foreach(var camera in cameras)
			{
				//-- Create the image
				var image = new LayoutImage(
					camera,
					new IPEndPoint(IPAddress.Any, camera.Camera.MulticastPort),
					new IPEndPoint(this._multicastAddress, camera.Camera.MulticastPort));

				this._images.Add(image);
				this.Children.Add(image);
			}
		}

		public void StopStreams()
		{
			//-- Set the cancellation token to cancelled and dispose of the streams
			this._stoppingTokenSource.Cancel();

			//-- Dispose of the clients
			foreach(var image in this._images)
			{
				image.Stop();
			}
		}

		public void StartStreams()
		{
			//-- Create the new cancellation token and start receiving images
			this._stoppingTokenSource = new CancellationTokenSource();

			foreach(var image in this._images)
			{
				_ = image.Start(this._stoppingTokenSource.Token);
			}
		}
	}
}
