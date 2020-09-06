using CctvMulticastViewer.Models;
using LinqToDB;
using LinqToDB.Data;

namespace CctvMulticastViewer
{
	public static partial class DBHelper
	{
		public class Cctv : DataConnection
		{
			public Cctv() : base("CCTV") { }

			public ITable<Camera> Camera => this.GetTable<Camera>();
			public ITable<Layout> Layout => this.GetTable<Layout>();
			public ITable<LayoutCamera> LayoutCamera => this.GetTable<LayoutCamera>();
			public ITable<LayoutCameraUserChoice> LayoutCameraUserChoice => this.GetTable<LayoutCameraUserChoice>();
			public ITable<Settings> Settings => this.GetTable<Settings>();
			public ITable<Viewer> Viewer => this.GetTable<Viewer>();
			public ITable<ViewerLayout> ViewerLayout => this.GetTable<ViewerLayout>();
		}
	}
}
