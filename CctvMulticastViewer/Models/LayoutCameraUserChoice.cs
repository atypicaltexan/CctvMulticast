using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace CctvMulticastViewer.Models
{
	public class LayoutCameraUserChoice
	{
		public int LayoutID { get; set; }
		public int CameraID { get; set; }
		public int OptionalCameraID { get; set; }
		[NotColumn]
		public Camera Camera { get; set; }
		public static LayoutCameraUserChoice WithCamera(LayoutCameraUserChoice choice, Camera camera)
		{
			choice.Camera = camera;
			return choice;
		}
	}
}
