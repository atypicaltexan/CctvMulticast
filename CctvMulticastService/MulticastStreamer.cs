using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CctvMulticastService
{
	public class MulticastStreamer
	{
		private readonly string _cameraUrl;
		private readonly IPEndPoint _multicastEndpoint;
		private readonly ILogger<Worker> _logger;

		public MulticastStreamer(string cameraUrl, IPEndPoint multicastEndpoint, ILogger<Worker> logger)
		{
			this._cameraUrl = cameraUrl;
			this._multicastEndpoint = multicastEndpoint;
			this._logger = logger;
		}

		public async Task Stream(CancellationToken stoppingToken)
		{
			while(!stoppingToken.IsCancellationRequested)
			{
				try
				{
					//-- Log what's happening
					this._logger.LogInformation($"Streaming '{this._cameraUrl}' to {this._multicastEndpoint}");

					//-- Create the http client
					using var client = new HttpClient();

					//-- Get the stream from the camera
					var stream = await client.GetStreamAsync(this._cameraUrl);

					//-- Create the new multicast writer
					var writer = new MulticastMjpegWriter(this._multicastEndpoint, this._logger);

					//-- Create the new reader to read in the stream and pass each image to the writer callback
					var reader = new MotionJpegStreamReader(stream, writer.MulticastImage, stoppingToken);

					//-- Read in and write out
					await reader.ReadStream();
				}
				catch(Exception ex)
				{
					//-- Log the error and delay, then loop and try again
					this._logger.LogError(ex.Message);
					await Task.Delay(TimeSpan.FromMinutes(.5));
				}
			}
		}
	}
}
