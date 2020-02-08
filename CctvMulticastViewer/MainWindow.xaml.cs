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
		public MainWindow()
		{
			this.InitializeComponent();

			for(var i = 0; i < 25; i++)
			{
				var index = i;
				var imgControl = this.Dispatcher.Invoke(() => this.FindName($"img{index / 5 + 1}{index % 5 + 1}") as Image);
				var reader = new MulticastMjpegReader(
					new IPEndPoint(IPAddress.Parse($"172.20.128.{100 + 1 + index}"), 20001),
					new IPEndPoint(IPAddress.Parse("239.192.168.21"), 20001), 
					(reader, ms, frameNumber) => this.DecodeFrame(imgControl, reader, ms, frameNumber));
				Task.Run(reader.Start);
			}
		}


		//private async Task ReadStream(int index)
		//{
		//	using(var client = new HttpClient())
		//	{
		//		var stream = await client.GetStreamAsync("http://172.20.128.51:81/mjpg/FE1/video.mjpg&w=384");
		//		

		//		var reader = new MotionJpegStreamReader(stream, (ms) =>
		//		{
					
		//		});

		//		await reader.ReadStream();
		//	}
		//}



		private void DecodeFrame(Image img, MulticastMjpegReader reader, MemoryStream image, uint frameNumber)
		{
			this.Dispatcher.BeginInvoke(new Action<Image, MulticastMjpegReader, MemoryStream, uint>(this.DecodeFrameInternal), DispatcherPriority.Background, img, reader, image, frameNumber);
		}

		private void DecodeFrameInternal(Image img, MulticastMjpegReader reader, MemoryStream image, uint frameNumber)
		{
			//-- Only change the image if we're on the last frame
			if(reader.LastCompletedImage != frameNumber)
			{
				//System.Diagnostics.Debug.WriteLine($"Skipping frame {frameNumber}");
				return;
			}

			try
			{
				img.Source = BitmapDecoder.Create(image, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None).Frames[0];
			}
			catch(Exception)
			{
				//-- Snuff the exceptions
			}
		}
	}
}
