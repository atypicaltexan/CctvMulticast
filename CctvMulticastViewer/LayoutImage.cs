using CctvMulticastViewer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
		Grid
	{
		private MulticastMjpegReader _reader;
		private Image _image;
		private Grid _timeoutMessage;
		private bool _isTimedOut;
		private readonly Camera _camera;
		private readonly OverlayLocation? _overlayLocation;
		private readonly OverlaySize? _overlaySize;
		private IPEndPoint _localEndPoint;
		private IPEndPoint _multicastEndPoint;

		public Camera Camera
		{
			get => this._camera;
		}

		public LayoutImage(
			Camera camera, 
			OverlayLocation? overlayLocation, 
			OverlaySize? overlaySize, 
			IPEndPoint localEndPoint, 
			IPEndPoint multicastEndPoint)
		{
			this._camera = camera;
			this._overlayLocation = overlayLocation;
			this._overlaySize = overlaySize;
			this._localEndPoint = localEndPoint;
			this._multicastEndPoint = multicastEndPoint;

			//-- Create the border, image, and reader and add them to the collections
			this.Children.Add(this._image = new Image());
			this.AddOverlay();
			this.Background = Brushes.Gray;
			this.Margin = new Thickness(1);

			this._reader = new MulticastMjpegReader(
				this._localEndPoint,
				this._multicastEndPoint,
				(reader, imageStream, frameNumber) =>
				{
					this.Dispatcher.BeginInvoke(
						new Action<MemoryStream, uint>(this.UpdateImage),
						imageStream,
						frameNumber);
				})
			{
				TimeoutCallback = () => this.Dispatcher.BeginInvoke(new Action(this.TimeoutCallback))
			};
		}

		private void TimeoutCallback()
		{
			//-- Flag as timed out
			this._isTimedOut = true;

			//-- Create the label if needed
			if (this._timeoutMessage == null)
			{
				this._timeoutMessage = new Grid();
				this._timeoutMessage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
				this._timeoutMessage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
				this._timeoutMessage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

				var textblock = new TextBlock
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					FontSize = 24,
					FontWeight = FontWeights.Bold,
					Foreground = Brushes.LightGray,
					TextAlignment = TextAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
				};
				textblock.Inlines.Add(new Run
				{
					Text = this._camera.Name,
				});
				textblock.Inlines.Add(new LineBreak());
				textblock.Inlines.Add(new Run
				{
					FontWeight = FontWeights.Normal,
					Foreground = Brushes.DarkGray,
					Text = "no video available",
				});

				var viewbox = new Viewbox
				{
					Child = textblock,
					Stretch = Stretch.Uniform,
				};
				Grid.SetRow(viewbox, 1);
				this._timeoutMessage.Children.Add(viewbox);
			}

			//-- Set the child if needed
			if (!this.Children.Contains(this._timeoutMessage))
			{
				this.Children.Clear();
				this.Children.Add(this._timeoutMessage);
			}
		}

		public Task Start(CancellationToken token)
		{
			return Task.Run(() => this._reader.Start(token), token);
		}

		public void StopForSwap()
		{
			this._reader.StopForSwap();
		}

		public void Stop()
		{
			this._reader.Stop();
		}

		private void AddOverlay()
		{
			if (this._overlayLocation.HasValue)
			{
				//Background = Brushes.Gray,
				var overlay = new System.Windows.Shapes.Path
				{
					Data = new FormattedText(
						this._camera.Name + (string.IsNullOrWhiteSpace(this._camera.QuickLabel) ? null : (": " + this._camera.QuickLabel)),
						CultureInfo.CurrentUICulture,
						FlowDirection.LeftToRight,
						new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
						(this._overlaySize ?? OverlaySize.Small) switch
						{
							OverlaySize.Small => 14,
							OverlaySize.Medium => 26,
							_ => 38,
						},
						Brushes.White,
						1).BuildGeometry(new Point(0, 0)),
					IsHitTestVisible = false,
					Stroke = Brushes.Black,
					StrokeThickness = 1,
					Fill = Brushes.White,
					Margin = new Thickness(10),
				};

				switch (this._overlayLocation.Value)
				{
					case OverlayLocation.TopLeft:
						{
							overlay.HorizontalAlignment = HorizontalAlignment.Left;
							overlay.VerticalAlignment = VerticalAlignment.Top;
							break;
						}
					case OverlayLocation.TopRight:
						{
							overlay.HorizontalAlignment = HorizontalAlignment.Right;
							overlay.VerticalAlignment = VerticalAlignment.Top;
							break;
						}
					case OverlayLocation.BottomRight:
						{
							overlay.HorizontalAlignment = HorizontalAlignment.Right;
							overlay.VerticalAlignment = VerticalAlignment.Bottom;
							break;
						}
					case OverlayLocation.BottomLeft:
					default:
						{
							overlay.HorizontalAlignment = HorizontalAlignment.Left;
							overlay.VerticalAlignment = VerticalAlignment.Bottom;
							break;
						}
				}

				this.Children.Add(overlay);
			}
		}

		private void UpdateImage(MemoryStream imageStream, uint frameNumber)
		{
			//-- If we are timed out, then we need to swap the child of this border
			if (this._isTimedOut)
			{
				this.Children.Clear();
				this.Children.Add(this._image);
				this.AddOverlay();
				this._isTimedOut = false;
			}

			//-- If the frame is not up to date, then the video has moved beyond this frame, so skip it so we don't fall behind
			if (this._reader.LastCompletedImage != frameNumber)
			{
				return;
			}

			try
			{
				this._image.Source = BitmapDecoder.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.None).Frames[0];
			}
			catch (Exception)
			{
				//-- Snuff the exception in case we overrun our image cache or we lose packets and have a bad image
			}
		}

	}
}
