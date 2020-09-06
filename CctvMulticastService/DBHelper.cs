using CctvMulticastService.Models;
using LinqToDB;
using LinqToDB.Data;
using System;
using System.Linq;
using System.Text;

namespace CctvMulticastService
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

        public static Server FetchServer()
        {
            using var db = new Cctv();
            return (
                from s in db.Server
                where s.ID == Config.Instance.ServerID
                select s).First();
        }

        public static MulticastCamera[] FetchCameras()
        {
            using var db = new Cctv();

            return (
                from t in
                    from sc in db.ServerCamera
                    from c in db.Camera.InnerJoin(c => sc.CameraID == c.ID)
                    from cu in db.CameraUrl.InnerJoin(cu => c.ID == cu.CameraID)
                    where sc.IsActive
                        && sc.ServerID == Config.Instance.ServerID
                    select new
                    {
                        RowNum = Sql.Ext.RowNumber()
                            .Over()
                            .PartitionBy(c.ID)
                            .OrderBy(sc.ActiveRank)
                            .ToValue(),
                        CameraUrl = cu.Url,
                        c.MulticastPort,
                    }
                select new MulticastCamera
                {
                    CameraUrl = t.CameraUrl,
                    MulticastPort = t.MulticastPort,
                }).ToArray();
        }
    }
}
