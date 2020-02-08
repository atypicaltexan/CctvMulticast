using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastService.Models
{
	public class ServerCamera
	{
		public int ServerID { get; set; }
		public int CameraID { get; set; }
		public bool IsActive { get; set; }
		public int ActiveRank { get; set; }
	}
}
