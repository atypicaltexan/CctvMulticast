using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastViewer.Models
{
	public class LayoutCamera
	{
		public int LayoutID { get; set; }
		public int CameraID { get; set; }
		public int RowIndex { get; set; }
		public int ColumnIndex { get; set; }
		public int RowSpan { get; set; }
		public int ColumnSpan { get; set; }
		[NotColumn]
		public Camera Camera { get; set; }
		public static LayoutCamera WithCamera(LayoutCamera layoutCamera, Camera camera)
		{
			layoutCamera.Camera = camera;
			return layoutCamera;
		}
	}
}
