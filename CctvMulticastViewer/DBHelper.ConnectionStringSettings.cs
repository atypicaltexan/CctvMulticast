using LinqToDB.Configuration;

namespace CctvMulticastViewer
{
	public static partial class DBHelper
	{
		private class ConnectionStringSettings : IConnectionStringSettings
      {
         public string ConnectionString { get; set; }
         public string Name { get; set; }
         public string ProviderName { get; set; }
         public bool IsGlobal => false;
      }
   }
}
