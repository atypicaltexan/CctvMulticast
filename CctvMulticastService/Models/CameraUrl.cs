using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastService.Models
{
	public class CameraUrl
	{
		public int CameraID { get; set; }
		public bool IsActive { get; set; }
		public int ActiveRank { get; set; }
		public string Url { get; set; }
		public string Description { get; set; }
	}
}
