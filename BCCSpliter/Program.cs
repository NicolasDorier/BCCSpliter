using BCCSpliter.Configuration;
using Microsoft.Extensions.Logging;
using BCCSpliter.Logging;
using System;
using System.IO;

namespace BCCSpliter
{
	class Program
	{
		static void Main(string[] args)
		{
			Logs.Configure(new FuncLoggerFactory(i => new CustomerConsoleLogger(i, (a, b) => true, false)));
			try
			{

				var config = new SpliterConfiguration();
				config.LoadArgs(args);

				var rpc = config.RPC.ConfigureRPCClient(config.Network);
				using(var repo = new Repository(Path.Combine(config.DataDir, "db")))
				{
					new Interactive() { Configuration = config, RPCClient = rpc, Repository = repo }.Run();
				}
			}
			catch(ConfigException ex)
			{
				if(!string.IsNullOrEmpty(ex.Message))
					Logs.Configuration.LogError(ex.Message);
			}
			catch(Exception ex)
			{
				Logs.Configuration.LogError(ex.Message);
				Logs.Configuration.LogDebug(ex.StackTrace);
			}
		}
	}
}