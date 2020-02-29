using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CctvMulticastService
{
	public class MotionJpegStreamReader :
		IDisposable
	{
		private Stream _stream;
		private Func<byte[], Task> _imageReadCallback;
		private readonly CancellationToken _cancellationToken;

		public MotionJpegStreamReader(Stream stream, Func<byte[], Task> imageReadCallback, CancellationToken cancellationToken)
		{
			this._stream = stream;
			this._imageReadCallback = imageReadCallback;
			this._cancellationToken = cancellationToken;
		}

		public async Task ReadStream()
		{
			while(!this._cancellationToken.IsCancellationRequested)
			{
				var contentLength = 0;
				for(var i = 0; i < 10; i++) //-- Cap our header checks at 10
				{
					var line = this.ReadLine();
					if(line.StartsWith("Content-Length"))
					{
						//-- Parse off the int
						contentLength = int.Parse(line.Split(": ")[1]);

						//-- Read the next blank line
						this.ReadLine();

						break;
					}
				}

				if(contentLength == 0)
				{
					//-- Close down this reader because there is no image coming across the stream
					return;
				}

				//-- Read off the next image into a memory stream
				var imageStream = await this.ReadNextImage(contentLength);
				await this._imageReadCallback?.Invoke(imageStream);

				//-- Read off the newline after the image
				this.ReadLine();
			}
		}

		private async Task<byte[]> ReadNextImage(int contentLength)
		{
			//-- Read the bytes from the stream
			var buffer = new byte[contentLength];
			var bytesRead = 0;
			do
			{
				bytesRead += await this._stream.ReadAsync(buffer, bytesRead, contentLength - bytesRead);
			} while(bytesRead < contentLength);
			return buffer;
		}

		private string ReadLine()
		{
			var bytes = new List<byte>(100);

			//-- Read one character at a time adding it to the buffer, until we read the end of stream, or we reach \r\n
			var readCR = false;
			while(true)
			{
				var b = this._stream.ReadByte();
				if(b < 0)
				{
					break;
				}
				else if(b == 13)
				{
					readCR = true;
				}
				else if(b == 10 && readCR)
				{
					break;
				}
				else
				{
					bytes.Add((byte)b);
				}
			}

			return Encoding.UTF8.GetString(bytes.ToArray());
		}

		public void Dispose()
		{
			try
			{
				(this._stream as IDisposable)?.Dispose();
			}
			catch(Exception)
			{
				//-- Snuff it
			}
		}
	}
}
