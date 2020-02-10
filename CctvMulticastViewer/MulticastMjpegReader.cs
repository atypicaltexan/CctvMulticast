using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CctvMulticastViewer
{
	internal class MulticastMjpegReader
	{
		public delegate void ImageReceivedHandler(MulticastMjpegReader reader, MemoryStream image, uint frameNumber);

		private UdpClient _client;
		private readonly IPEndPoint _localEndpoint;
		private readonly IPEndPoint _cameraEndpoint;
		private readonly ImageReceivedHandler _imageReceivedCallback;
		private MemoryStream[] _streams;
		private const int StreamCacheCount = 50;
		private static readonly TimeSpan _timeoutInterval = TimeSpan.FromSeconds(10);
		private ManualResetEvent _listeningWaitHandle = new ManualResetEvent(initialState: false);

		public uint LastCompletedImage { get; private set; }
		public Action TimeoutCallback { get; set; }

		public MulticastMjpegReader(IPEndPoint localEndpoint, IPEndPoint cameraEndpoint, ImageReceivedHandler imageReceivedCallback)
		{
			this._localEndpoint = localEndpoint;
			this._cameraEndpoint = cameraEndpoint;
			this._imageReceivedCallback = imageReceivedCallback;

			//-- Create the memory streams and reserve the capacity
			this._streams = new MemoryStream[StreamCacheCount];
			for(var i = 0; i < this._streams.Length; i++)
			{
				this._streams[i] = new MemoryStream(125 * 1024);
			}
		}

		public void Stop()
		{
			this._listeningWaitHandle.WaitOne();
			this._client.DropMulticastGroup(this._cameraEndpoint.Address);
			this._client?.Client?.Close();
			this._client?.Close();
		}

		public async Task Start(CancellationToken stoppingToken)
		{
			try
			{
				this._listeningWaitHandle.Reset();
				this.SetupClient();

				var lastImageNumber = (uint)0;
				var chunksRead = (ushort)0;
				MemoryStream currentStream = null;

				//-- Loop while receiving packets
				while(!stoppingToken.IsCancellationRequested)
				{
					try
					{
						//-- Get the next datagram
						var receiveTask = this._client.ReceiveAsync();
						await Task.WhenAny(receiveTask, Task.Delay(_timeoutInterval));

						//-- See if the socket completed
						if(receiveTask.Status == TaskStatus.RanToCompletion)
						{
							var datagram = receiveTask.Result.Buffer;

							//-- Read the header
							var imageNumber = (uint)(datagram[0] << 24)
								+ (uint)(datagram[1] << 16)
								+ (uint)(datagram[2] << 8)
								+ datagram[3];
							var chunkNumber = (ushort)(datagram[4] << 8) + datagram[5];
							var chunkCount = (ushort)(datagram[6] << 8) + datagram[7];

							//-- If this is a new image number, then start the new stream
							if(imageNumber > lastImageNumber /* only check forward, in case packets arrive out of order */)
							{
								//-- Get the stream reference
								currentStream = this._streams[lastImageNumber % StreamCacheCount];

								//-- Clear the stream
								currentStream.SetLength(0);

								//-- Save off the image number
								lastImageNumber = imageNumber;

								//-- Reset the chunks read
								chunksRead = 0;
							}

							//-- Write the chunk to the stream
							if(imageNumber == lastImageNumber)
							{
								currentStream.Write(datagram, 8, datagram.Length - 8);
								chunksRead++;
							}

							//-- If this is the last chunk, then send the image up
							if(chunkNumber == chunkCount - 1 && chunksRead == chunkCount)
							{
								currentStream.Position = 0;
								this.LastCompletedImage = lastImageNumber;
								this._imageReceivedCallback?.Invoke(this, currentStream, lastImageNumber);
							}
						}
						else
						{
							//-- Invoke the timeout callback
							this.TimeoutCallback?.Invoke();

							//-- Timed out waiting for data, so kill the socket and create a new one
							this._client.Close();
							this.SetupClient();

							//-- Let it loop and try again
						}
					}
					catch(Exception)
					{
						//-- Snuff the exception because the reader is shutting down
					}
				}
			}
			finally
			{
				//-- Flag the wait handle as no longer listening
				this._listeningWaitHandle.Set();
			}
		}

		private void SetupClient()
		{
			this._client = new UdpClient(this._localEndpoint);
			
			//-- Bind to the multicast address
			this._client.JoinMulticastGroup(this._cameraEndpoint.Address);
		}
	}
}
