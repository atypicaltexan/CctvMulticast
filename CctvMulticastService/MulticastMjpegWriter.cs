﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CctvMulticastService
{
	internal class MulticastMjpegWriter
	{
		private readonly IPEndPoint _cameraEndpoint;
		private readonly ILogger<Worker> _logger;
		private UdpClient _udpClient;
		private uint _currentImageID;
		private const int BytesInDatagram = 1452;

		public MulticastMjpegWriter(IPEndPoint cameraEndpoint, ILogger<Worker> _logger)
		{
			this._cameraEndpoint = cameraEndpoint;
			this._logger = _logger;
			this._udpClient = new UdpClient(AddressFamily.InterNetwork);
			this._udpClient.JoinMulticastGroup(cameraEndpoint.Address, IPAddress.Parse("172.20.128.51"));
		}

		public async Task MulticastImage(byte[] imageData)
		{
			//-- Split up the image into datagrams that can be sent for the image
			var imageLength = imageData.Length;
			var dataGramChunkNumber = (ushort)0;
			var dataGramLength = 0;
			var dataGram = new byte[1460];

			//-- Determine how many chunks it will take to write this image
			var chunkCount = (ushort)Math.Ceiling((decimal)imageLength / BytesInDatagram);

			this._currentImageID++;
			//if(this._currentImageID % 25 == 0)
			//{
			//	this._logger.LogInformation("Image Size: " + imageData.Length.ToString("N0"));
			//}

			unchecked
			{
				while(dataGramChunkNumber * BytesInDatagram < imageLength)
				{
					dataGram[0] = (byte)(this._currentImageID >> 24);
					dataGram[1] = (byte)((this._currentImageID >> 16) & 0xFF);
					dataGram[2] = (byte)((this._currentImageID >> 8) & 0xFF);
					dataGram[3] = (byte)(this._currentImageID & 0xFF);
					dataGram[4] = (byte)(dataGramChunkNumber >> 8);
					dataGram[5] = (byte)(dataGramChunkNumber & 0xFF);
					dataGram[6] = (byte)(chunkCount >> 8);
					dataGram[7] = (byte)(chunkCount & 0xFF);

					Array.Copy(
						imageData,
						dataGramChunkNumber * BytesInDatagram,
						dataGram,
						8,
						dataGramLength = Math.Min(BytesInDatagram, imageLength - dataGramChunkNumber * BytesInDatagram));

					dataGramChunkNumber++;

					await this._udpClient.SendAsync(dataGram, dataGramLength + 8, this._cameraEndpoint);
				}
			}
		}
	}
}