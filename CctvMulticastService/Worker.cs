using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CctvMulticastService
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private List<MulticastStreamer> _cameraStreams;

		public Worker(ILogger<Worker> logger)
		{
			this._logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				//-- Delay to let the hosting system start up and finish logging
				await Task.Delay(TimeSpan.FromSeconds(2));

				//-- Log the settings
				this._logger.LogInformation($"Initialized - ServerID: {Config.Instance.ServerID} - ConnectionString: {Config.Instance.ConnectionString}");

				//-- Retrieve the configuration from SQL
				var multicastAddress = IPAddress.Parse(DBHelper.FetchMulticastIPAddress());
				var server = DBHelper.FetchServer();
				var cameraUrls = DBHelper.FetchCameras();

				//-- For each of the camera URLs start up a new camera stream
				this._cameraStreams = new List<MulticastStreamer>(cameraUrls.Select(cu =>
					new MulticastStreamer(cu.CameraUrl, server, new IPEndPoint(multicastAddress, cu.MulticastPort), this._logger)));

				//-- Wait on all of the streams
				await Task.WhenAll(this._cameraStreams.Select(cs => cs.Stream(stoppingToken)));
			}
			catch(TaskCanceledException)
			{
				//-- Snuff it, the service is shutting down
			}
			catch(Exception ex)
			{
				this._logger.LogError(ex, "Error running service");
				throw;
			}
		}
	}
}
