using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CctvMulticastViewer
{
	public class Config
	{
		public static void Initialize()
		{
			var configPath = Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				"Config.json");

			Instance = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
		}

		public static Config Instance { get; private set; }
		public string ConnectionString { get; set; }
		public int ViewerID { get; set; }
	}
}
