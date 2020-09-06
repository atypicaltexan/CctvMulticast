using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CctvMulticastService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Config.Initialize();
			DBHelper.Initialize();

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			var builder = Host
				.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					services.AddHostedService<Worker>();
				});

			if(args.Length > 0 && args[0] == "svc")
			{
				return builder.UseWindowsService();
			}

			return builder;
		}
	}
}
