using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace CctvMulticastViewer
{
	public class LayoutSlot :
		Grid
	{
		public LayoutSlot(IPAddress multicastAddress)
		{
			this.Background = Brushes.Transparent;
			this._multicastAddress = multicastAddress;
		}

		private List<LayoutImage> _cyclingImages = new List<LayoutImage>();
		private int _currentIndex;
		private readonly IPAddress _multicastAddress;
		private CancellationToken _stoppingToken;

		public LayoutCamera OriginalCamera { get; set; }

		public List<LayoutCameraUserChoice> OptionalCameras
		{
			get => this.OriginalCamera.UserChoices;
		}

		public bool CanUserChooseCamera
		{
			get => this.OriginalCamera.AllowUserChoice && this.OptionalCameras.Count > 0;
		}

		public List<LayoutImage> CyclingImages
		{
			get => this._cyclingImages;
		}

		public void CreateInitialImages()
		{
			this.CreateImageForCamera(this.OriginalCamera.Camera);
		}

		public void AddImage(LayoutImage image)
		{
			this._cyclingImages.Add(image);
			this.Children.Add(image);
		}

		public void RemoveImage(LayoutImage image)
		{
			this._cyclingImages.Remove(image);
			this.Children.Remove(image);
		}

		public void CycleNext()
		{
			//-- If there is only 1 camera, then do nothing
			if(this._cyclingImages.Count < 2)
			{
				return;
			}

			//-- Set the current image to hidden and the next image to visible
			foreach(var image in this._cyclingImages)
			{
				image.Visibility = System.Windows.Visibility.Hidden;
			}

			if(++this._currentIndex >= this._cyclingImages.Count)
			{
				this._currentIndex = 0;
			}
			this._cyclingImages[this._currentIndex].Visibility = System.Windows.Visibility.Visible;
		}

		public void SwapCamera(LayoutCameraUserChoice newCamera)
		{
			//-- Stop all of the cameras that are running
			foreach(var ci in this._cyclingImages)
			{
				ci.StopForSwap();
			}

			//-- Remove them from the grid
			this.Children.Clear();
			this._cyclingImages.Clear();

			//-- Fetch the camera from the database
			var camera = DBHelper.FetchCameraByID(newCamera.OptionalCameraID);

			//-- Create the new image
			var image = this.CreateImageForCamera(camera);

			//-- Start the image
			_ = image.Start(this._stoppingToken);
		}

		private LayoutImage CreateImageForCamera(Camera camera)
		{
			var image = new LayoutImage(
				camera,
				this.OriginalCamera.OverlayLocation_,
				this.OriginalCamera.OverlaySize_,
				new IPEndPoint(IPAddress.Any, camera.MulticastPort),
				new IPEndPoint(this._multicastAddress, camera.MulticastPort));
			this._cyclingImages.Add(image);
			this.Children.Add(image);
			return image;
		}

		public void StartImages(CancellationToken cancellationToken)
		{
			this._stoppingToken = cancellationToken;
			foreach(var image in this._cyclingImages)
			{
				_ = image.Start(cancellationToken);
			}
		}

		public void Stop()
		{
			foreach(var image in this._cyclingImages)
			{
				image.Stop();
			}
		}

		public void ToggleCycleCamera(LayoutCameraUserChoice optionalCamera)
		{
			if(this._cyclingImages.FirstOrDefault(i => i.Camera.ID == optionalCamera.OptionalCameraID) is LayoutImage image)
			{
				//-- Camera is being turned off, so shut it down and remove it
				image.StopForSwap();
				this._cyclingImages.Remove(image);
				this.Children.Remove(image);
			}
			else
			{
				//-- Camera is being added to the list of cameras to swap
				//-- Fetch the camera from the database
				var camera = DBHelper.FetchCameraByID(optionalCamera.OptionalCameraID);

				//-- Create the new image
				image = this.CreateImageForCamera(camera);

				_ = image.Start(this._stoppingToken);
			}
		}
	}
}
