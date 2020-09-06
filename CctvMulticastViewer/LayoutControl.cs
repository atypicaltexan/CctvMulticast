using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		private List<LayoutSlot> _slots = new List<LayoutSlot>();
		private Timer _cycleCamerasTimer;

		public LayoutControl(Layout layout, IPAddress multicastAddress)
		{
			//-- Save the layout ID
			this._layout = layout;
			this._multicastAddress = multicastAddress;

			//-- Retrieve the cameras in the layout
			var cameras = DBHelper.FetchCamerasForLayout(layout.ID);
			var optionalCameras = DBHelper.FetchUserChoiceForLayout(layout.ID);

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
				//-- Set the optional cameras for the camera
				camera.UserChoices.AddRange(optionalCameras.Where(oc => oc.CameraID == camera.CameraID).OrderBy(oc => oc.Camera.Name));

				//-- Create the slot for the camera
				var slot = new LayoutSlot(this._multicastAddress);
				slot.OriginalCamera = camera;
				Grid.SetRow(slot, camera.RowIndex - 1);
				Grid.SetRowSpan(slot, camera.RowSpan);
				Grid.SetColumn(slot, camera.ColumnIndex - 1);
				Grid.SetColumnSpan(slot, camera.ColumnSpan);
				slot.CreateInitialImages();

				this._slots.Add(slot);
				this.Children.Add(slot);
			}
		}

		public void SwapCamera(LayoutSlot slot, LayoutCameraUserChoice newCamera)
		{
			slot.SwapCamera(newCamera);
		}

		public void StopStreams()
		{
			this._cycleCamerasTimer.Dispose();

			//-- Set the cancellation token to cancelled and dispose of the streams
			this._stoppingTokenSource.Cancel();

			//-- Dispose of the clients
			foreach(var slot in this._slots)
			{
				slot.Stop();
			}
		}

		public void StartStreams()
		{
			//-- Create the new cancellation token and start receiving images
			this._stoppingTokenSource = new CancellationTokenSource();

			foreach(var slot in this._slots)
			{
				slot.StartImages(this._stoppingTokenSource.Token);
			}

			var cycleTime = Config.Instance.CycleCameraTime ?? TimeSpan.FromSeconds(5);
			this._cycleCamerasTimer = new Timer(this.CycleCameras, null, cycleTime, cycleTime);

		}

		private void CycleCameras(object state)
		{
			this.Dispatcher.BeginInvoke(new Action(() =>
			{
				foreach(var slot in this._slots)
				{
					slot.CycleNext();
				}
			}));
		}
	}
}
