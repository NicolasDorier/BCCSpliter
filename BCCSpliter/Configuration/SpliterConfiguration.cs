using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NBitcoin;
using BCCSpliter.Logging;
using System.Net;

namespace BCCSpliter.Configuration
{
	public class SpliterConfiguration
	{
		public string ConfigurationFile
		{
			get;
			private set;
		}
		public string DataDir
		{
			get;
			private set;
		}

		public Network Network
		{
			get; set;
		}
		public RPCArgs RPC
		{
			get;
			private set;
		}
		public IPEndPoint BCCEndpoint
		{
			get;
			set;
		}

		internal void LoadArgs(string[] args)
		{
			ConfigurationFile = args.Where(a => a.StartsWith("-conf=", StringComparison.Ordinal)).Select(a => a.Substring("-conf=".Length).Replace("\"", "")).FirstOrDefault();
			DataDir = args.Where(a => a.StartsWith("-datadir=", StringComparison.Ordinal)).Select(a => a.Substring("-datadir=".Length).Replace("\"", "")).FirstOrDefault();
			if(DataDir != null && ConfigurationFile != null)
			{
				var isRelativePath = Path.GetFullPath(ConfigurationFile).Length > ConfigurationFile.Length;
				if(isRelativePath)
				{
					ConfigurationFile = Path.Combine(DataDir, ConfigurationFile);
				}
			}

			Network = args.Contains("-testnet", StringComparer.OrdinalIgnoreCase) ? Network.TestNet :
				args.Contains("-regtest", StringComparer.OrdinalIgnoreCase) ? Network.RegTest :
				Network.Main;

			if(ConfigurationFile != null)
			{
				AssetConfigFileExists();
				var configTemp = TextFileConfiguration.Parse(File.ReadAllText(ConfigurationFile));
				Network = configTemp.GetOrDefault<bool>("testnet", false) ? Network.TestNet :
						  configTemp.GetOrDefault<bool>("regtest", false) ? Network.RegTest :
						  Network.Main;
			}

			if(DataDir == null)
			{
				DataDir = DefaultDataDirectory.GetDefaultDirectory("BCCSpliter", Network);
			}

			if(ConfigurationFile == null)
			{
				ConfigurationFile = GetDefaultConfigurationFile(DataDir, Network);
			}
			Logs.Configuration.LogInformation("Network: " + Network);

			Logs.Configuration.LogInformation("Data directory set to " + DataDir);
			Logs.Configuration.LogInformation("Configuration file set to " + ConfigurationFile);

			if(!Directory.Exists(DataDir))
				throw new ConfigException("Data directory does not exists");


			var consoleConfig = new TextFileConfiguration(args);
			var config = TextFileConfiguration.Parse(File.ReadAllText(ConfigurationFile));
			consoleConfig.MergeInto(config, true);

			RPC = RPCArgs.Parse(config, Network);
			BCCEndpoint = config.GetOrDefault<IPEndPoint>("bcc.endpoint", null);
			if(BCCEndpoint != null)
				Logs.Configuration.LogInformation("Using custom BCC endpoint " + BCCEndpoint.Address + ":" + BCCEndpoint.Port);
		}

		private void AssetConfigFileExists()
		{
			if(!File.Exists(ConfigurationFile))
				throw new ConfigException("Configuration file does not exists");
		}

		public static string GetDefaultConfigurationFile(string dataDirectory, Network network)
		{
			var config = Path.Combine(dataDirectory, "spliter.config");
			Logs.Configuration.LogInformation("Configuration file set to " + config);
			if(!File.Exists(config))
			{
				Logs.Configuration.LogInformation("Creating configuration file");
				StringBuilder builder = new StringBuilder();
				builder.AppendLine("####Common Commands####");
				builder.AppendLine("#Connection to the bitcoin core wallet. BCCspliter will try to autoconfig based on default settings of Bitcoin Core.");
				builder.AppendLine("#rpc.url=http://localhost:" + network.RPCPort + "/");
				builder.AppendLine("#rpc.user=bitcoinuser");
				builder.AppendLine("#rpc.password=bitcoinpassword");
				builder.AppendLine("#rpc.cookiefile=yourbitcoinfolder/.cookie");
				builder.AppendLine("#bcc.endpoint is optional, NBitcoin will try to connect to a random node for broadcasting your transaction");
				builder.AppendLine("#bcc.endpoint=ip:port");
				File.WriteAllText(config, builder.ToString());
			}
			return config;
		}
	}
}
