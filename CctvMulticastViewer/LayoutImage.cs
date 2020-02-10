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
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CctvMulticastViewer
{
	public class LayoutImage :
		Border
	{
		private MulticastMjpegReader _reader;
		private Image _image;
		private TextBlock _timeoutMessage;
		private bool _isTimedOut;
		private readonly LayoutCamera _camera;
		private IPEndPoint _localEndPoint;
		private IPEndPoint _multicastEndPoint;

		public LayoutImage(LayoutCamera camera, IPEndPoint localEndPoint, IPEndPoint multicastEndPoint)
		{
			this._camera = camera;
			this._localEndPoint = localEndPoint;
			this._multicastEndPoint = multicastEndPoint;

			//-- Create the border, image, and reader and add them to the collections
			this.Child = this._image = new Image();
			this.Background = Brushes.Gray;
			this.Margin = new Thickness(2);
			this.Padding = new Thickness(2);

			Grid.SetRow(this, camera.RowIndex - 1);
			Grid.SetRowSpan(this, camera.RowSpan);
			Grid.SetColumn(this, camera.ColumnIndex - 1);
			Grid.SetColumnSpan(this, camera.ColumnSpan);

			this._reader = new MulticastMjpegReader(
				this._localEndPoint,
				this._multicastEndPoint,
				(reader, imageStream, frameNumber) =>
				{
					this.Dispatcher.BeginInvoke(
						new Action<MemoryStream, uint>(this.UpdateImage),
						imageStream,
						frameNumber);
				}) {
				TimeoutCallback = () => this.Dispatcher.BeginInvoke(new Action(this.TimeoutCallback))
			};
		}

		private void TimeoutCallback()
		{
			//-- Flag as timed out
			this._isTimedOut = true;

			//-- Create the label if needed
			if(this._timeoutMessage == null)
			{
				this._timeoutMessage = new TextBlock {
					HorizontalAlignment = HorizontalAlignment.Center,
					FontSize = 24,
					FontWeight = FontWeights.Bold,
					Foreground = Brushes.LightGray,
					TextAlignment = TextAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
				};
				this._timeoutMessage.Inlines.Add(new Run {
					Text = this._camera.Camera.Name,
				});
				this._timeoutMessage.Inlines.Add(new LineBreak());
				this._timeoutMessage.Inlines.Add(new Run {
					FontWeight = FontWeights.Normal,
					Foreground = Brushes.DarkGray,
					Text = "no video available",
				});
			}

			//-- Set the child if needed
			if(this.Child != this._timeoutMessage)
			{
				this.Child = this._timeoutMessage;
			}
		}

		public Task Start(CancellationToken token)
		{
			return Task.Run(() => this._reader.Start(token), token);
		}

		public void Stop()
		{
			this._reader.Stop();
		}

		private void UpdateImage(MemoryStream imageStream, uint frameNumber)
		{
			//-- If we are timed out, then we need to swap the child of this border
			if(this._isTimedOut)
			{
				this.Child = this._image;
				this._isTimedOut = false;
			}

			//-- If the frame is not up to date, then the video has moved beyond this frame, so skip it so we don't fall behind
			if(this._reader.LastCompletedImage != frameNumber)
			{
				return;
			}

			try
			{
				this._image.Source = BitmapDecoder.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.None).Frames[0];
			}
			catch(Exception)
			{
				//-- Snuff the exception in case we overrun our image cache or we lose packets and have a bad image
			}
		}

	}
}
