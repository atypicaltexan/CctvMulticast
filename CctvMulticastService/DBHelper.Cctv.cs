using CctvMulticastService.Models;
using LinqToDB;
using LinqToDB.Data;

namespace CctvMulticastService
{
	public static partial class DBHelper
	{
		public class Cctv : DataConnection
		{
			public Cctv() : base("CCTV") { }

			public ITable<Camera> Camera => this.GetTable<Camera>();
			public ITable<CameraUrl> CameraUrl => this.GetTable<CameraUrl>();
			public ITable<Server> Server => this.GetTable<Server>();
			public ITable<ServerCamera> ServerCamera => this.GetTable<ServerCamera>();
			public ITable<Settings> Settings => this.GetTable<Settings>();
		}
	}
}
