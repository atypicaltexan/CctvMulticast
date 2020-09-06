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
		public int? OverlayLocation { get; set; }
		[NotColumn]
		public OverlayLocation? OverlayLocation_
		{
			get => (OverlayLocation?)this.OverlayLocation;
			set => this.OverlayLocation = (int?)value;
		}
		public int? OverlaySize { get; set; }
		[NotColumn]
		public OverlaySize? OverlaySize_
		{
			get => (OverlaySize?)this.OverlaySize;
			set => this.OverlaySize = (int?)value;
		}
		[NotColumn]
		public Camera Camera { get; set; }
		[NotColumn]
		public List<LayoutCameraUserChoice> UserChoices { get; } = new List<LayoutCameraUserChoice>();
		public bool AllowUserChoice { get; set; }
		public static LayoutCamera WithCamera(LayoutCamera layoutCamera, Camera camera)
		{
			layoutCamera.Camera = camera;
			return layoutCamera;
		}
	}

	public enum OverlaySize
	{
		Small = 1,
		Medium = 2,
		Large = 3,
	}

	public enum OverlayLocation
	{
		TopLeft = 1,
		TopRight = 2,
		BottomRight = 3,
		BottomLeft = 4,
	}
}
