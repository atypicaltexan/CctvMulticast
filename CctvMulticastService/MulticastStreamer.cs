using CctvMulticastService.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CctvMulticastService
{
	public class MulticastStreamer
	{
		private readonly string _cameraUrl;
		private readonly Server _server;
		private readonly IPEndPoint _multicastEndpoint;
		private readonly ILogger<Worker> _logger;
		private static string _geovisionKey;

		public MulticastStreamer(string cameraUrl, Server server, IPEndPoint multicastEndpoint, ILogger<Worker> logger)
		{
			this._cameraUrl = this.FixForGeoVision(cameraUrl);
			this._server = server;
			this._multicastEndpoint = multicastEndpoint;
			this._logger = logger;
		}

		private string FixForGeoVision(string cameraUrl)
		{
			if(cameraUrl.Contains("{key}"))
			{
				//-- This is a geovision URL, so we need to log in and get the key to get the URL
				cameraUrl = cameraUrl.Replace("{key}", _geovisionKey ??= GetGeovisionKey());
			}

			return cameraUrl;
		}

		private static string GetGeovisionKey()
		{
			using var client = new HttpClient();
			var request = new HttpRequestMessage {
				Method = HttpMethod.Post,
				RequestUri = new Uri("http://172.10.10.14/webcam_login"),
				Content = new FormUrlEncodedContent(new[] {
					 new KeyValuePair<string, string> ("id", "video"),
					 new KeyValuePair<string, string> ("pwd", "video"),
					 new KeyValuePair<string, string> ("ViewType", "2"),
					 new KeyValuePair<string, string> ("Login", "Login")
				})
			};
			var response = client.SendAsync(request).Result;
			return Regex.Match(response.Content.ReadAsStringAsync().Result, "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase).Value;
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
					var writer = new MulticastMjpegWriter(this._multicastEndpoint, this._server, this._logger);

					//-- Create the new reader to read in the stream and pass each image to the writer callback
					var reader = new MotionJpegStreamReader(stream, writer.MulticastImage, stoppingToken);

					//-- Read in and write out
					await reader.ReadStream();
				}
				catch(Exception ex)
				{
					//-- Log the error and delay, then loop and try again
					this._logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
					await Task.Delay(TimeSpan.FromMinutes(.5));
				}
			}
		}
	}
}
