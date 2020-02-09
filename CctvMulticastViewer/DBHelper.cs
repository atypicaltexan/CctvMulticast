using CctvMulticastViewer.Models;
using LinqToDB;
using LinqToDB.Data;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CctvMulticastViewer
{
	public static partial class DBHelper
	{
		public static void Initialize()
		{
         DataConnection.DefaultSettings = new MySettings();
		}

		public static string FetchMulticastIPAddress()
		{
			using var db = new Cctv();
			return (
				from s in db.Settings
				select s.MulticastIPAddress).First();
		}

		public static Viewer FetchViewer()
		{
			using var db = new Cctv();
			return (
				from v in db.Viewer
				where v.ID == Config.Instance.ViewerID
				select v).First();
		}

		public static async Task<Layout[]> FetchLayoutsAsync()
		{
			using var db = new Cctv();
			return await (
				from l in db.Layout
				from vl in db.ViewerLayout.InnerJoin(vl => l.ID == vl.LayoutID)
				where vl.ViewerID == Config.Instance.ViewerID
					&& vl.IsActive
				orderby vl.Rank
				select l).ToArrayAsync();
		}

		public static LayoutCamera[] FetchCamerasForLayout(int layoutID)
		{
			using var db = new Cctv();

			return (
				from lc in db.LayoutCamera
				from c in db.Camera.InnerJoin(c => lc.CameraID == c.ID)
				where lc.LayoutID == layoutID
				orderby lc.RowIndex, lc.ColumnIndex
				select LayoutCamera.WithCamera(lc, c)).ToArray();
		}
	}
}
