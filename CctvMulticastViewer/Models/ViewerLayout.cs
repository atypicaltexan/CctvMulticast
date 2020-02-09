using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastViewer.Models
{
	public class ViewerLayout
	{
		public int ViewerID { get; set; }
		public int LayoutID { get; set; }
		public bool IsActive { get; set; }
		public int Rank { get; set; }
	}
}
