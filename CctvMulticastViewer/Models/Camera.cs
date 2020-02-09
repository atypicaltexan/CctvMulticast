using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastViewer.Models
{
	public class Camera
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public string Location { get; set; }
		public int MulticastPort { get; set; }
	}
}
