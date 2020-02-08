using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace CctvMulticastService
{
	public static partial class DBHelper
	{
		private class MySettings : ILinqToDBSettings
      {
         public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

         public string DefaultConfiguration => "SqlServer";
         public string DefaultDataProvider => "SqlServer";

         public IEnumerable<IConnectionStringSettings> ConnectionStrings
         {
            get
            {
               yield return
                   new ConnectionStringSettings {
                      Name = "CCTV",
                      ProviderName = "SqlServer",
                      ConnectionString = Config.Instance.ConnectionString
                   };
            }
         }
      }
   }
}
