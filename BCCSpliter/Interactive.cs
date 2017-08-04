using BCCSpliter.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using CommandLine;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;
using BCCSpliter.Logging;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using BCCSpliter.DerivationStrategy;

namespace BCCSpliter
{
	public class Interactive
	{
		public RPCClient RPCClient
		{
			get; set;
		}
		public SpliterConfiguration Configuration
		{
			get; set;
		}

		public Repository Repository
		{
			get; set;
		}

		public void Run()
		{
			Parser.Default.ParseArguments<SelectOptions, ImportOptions, ImportSecretOptions, UnselectOptions, QuitOptions, ListOptions, DumpOptions>(new[] { "help" });
			bool quit = false;
			while(!quit)
			{
				Thread.Sleep(100);
				Console.Write(">>> ");
				var split = Console.ReadLine().Split(null) ?? new string[0];
				split = split.Select(s => s.Trim()).ToArray();
				try
				{
					Parser.Default.ParseArguments<SelectOptions, ImportOptions, ImportSecretOptions, UnselectOptions, QuitOptions, ListOptions, DumpOptions>(split)
						.WithParsed<SelectOptions>(_ => Select(_))
						.WithParsed<ImportOptions>(_ => ImportPrivateKeys(_))
						.WithParsed<ImportSecretOptions>(_ => ImportPrivateKey(_))
						.WithParsed<UnselectOptions>(_ => UnSelect(_))
						.WithParsed<ListOptions>(_ => List(_))
						.WithParsed<DumpOptions>(_ => Dump(_))
						.WithParsed<QuitOptions>(_ =>
						{
							quit = true;
						});
				}
				catch(FormatException)
				{
					Console.WriteLine("Invalid format");
					Parser.Default.ParseArguments<SelectOptions, ImportOptions, ImportSecretOptions, UnselectOptions, QuitOptions, ListOptions, DumpOptions>(new[] { "help", split[0] });
				}
			}
		}

		private void ImportPrivateKey(ImportSecretOptions o)
		{
			if(o.Key == null || o.Destination == null)
				throw new FormatException();
			var secret = new BitcoinSecret(o.Key, RPCClient.Network);
			var destination = BitcoinAddress.Create(o.Destination, RPCClient.Network);

			QBitNinjaClient client = CreateExplorer();
			var coins = GetPreforkCoins(client, secret.GetAddress(), null).GetAwaiter().GetResult();
			Logs.Main.LogInformation($"Found {coins.Count()} coins");
			DumpCoins(destination, coins, new[] { secret.PrivateKey });
		}

		private void ImportPrivateKeys(ImportOptions o)
		{
			if(o.ExtKey == null || o.Destination == null)
				throw new FormatException();

			var destination = BitcoinAddress.Create(o.Destination, RPCClient.Network);
			BitcoinExtKey root = new BitcoinExtKey(o.ExtKey, RPCClient.Network);

			var strategies = new[]
			{
				new
				{
					Description = "BIP45/P2SH/1-1",
					Strategy = (IStrategy)new BIP45P2SH11Strategy(root, false),
					ChangeStrategy = (IStrategy)new BIP45P2SH11Strategy(root, true),
				},
				new
				{
					Description = "BIP44",
					Strategy = (IStrategy)new BIP44P2PKHStrategy(root, false),
					ChangeStrategy = (IStrategy)new BIP44P2PKHStrategy(root, true),
				},
				new
				{
					Description = "Direct",
					Strategy = (IStrategy)new DirectDerivationStrategy(root, false),
					ChangeStrategy = (IStrategy)new DirectDerivationStrategy(root, true),
				}
			};

			IEnumerable<Tuple<Key, Coin>> all = new Tuple<Key, Coin>[0];

			foreach(var strategy in strategies)
			{
				Logs.Main.LogInformation("Scanning " + strategy.Description + " path");
				var found = Scan(strategy.Strategy);
				if(found.Count != 0)
				{
					Logs.Main.LogInformation($"Found {found.Count} coins");
					all = all.Concat(found);
					if(strategy.ChangeStrategy != null)
					{
						Logs.Main.LogInformation($"Scanning change addresses");
						found = Scan(strategy.ChangeStrategy);
						Logs.Main.LogInformation($"Found {found.Count} coins");
						all = all.Concat(found);
					}
				}
			}

			
			DumpCoins(destination, all.Select(a => a.Item2), all.Select(a => a.Item1));
		}

		int forkBlockHeight = 478559;
		private ICollection<Tuple<Key, Coin>> Scan(IStrategy derivationStrategy)
		{
			//List<Tuple<Key, Coin>> coins = new List<Tuple<Key, Coin>>();
			var coins = new Dictionary<OutPoint, Tuple<Key, Coin>>();
			QBitNinjaClient client = CreateExplorer();
			int i = 0;
			int gap = 20;
			var balances = new List<Tuple<Derivation, Task<Coin[]>>>();

			while(true)
			{
				var derivation = derivationStrategy.Derive(i);
				var address = derivation.ScriptPubKey.GetDestinationAddress(RPCClient.Network);
				i++;
				balances.Add(Tuple.Create(derivation, GetPreforkCoins(client, address, derivation.Redeem)));
				if(balances.Count == gap)
				{
					var hasMoney = false;
					foreach(var balance in balances)
					{
						foreach(var coin in balance.Item2.Result)
						{
							hasMoney = true;
							coins.Add(coin.Outpoint, Tuple.Create(balance.Item1.Key, coin));
						}
					}
					balances.Clear();
					if(!hasMoney)
						break;
				}
			}

			return coins.Values;
		}

		private QBitNinjaClient CreateExplorer()
		{
			return new QBitNinjaClient(RPCClient.Network);
		}

		private async Task<Coin[]> GetPreforkCoins(QBitNinjaClient client, BitcoinAddress address, Script redeem)
		{
			var balance = await client.GetBalanceBetween(new BalanceSelector(address), new BlockFeature(forkBlockHeight), null, false, true).ConfigureAwait(false);
			return balance.Operations.SelectMany(o => o.ReceivedCoins).OfType<Coin>()
				.Select(c => redeem == null ? c : c.ToScriptCoin(redeem))
				.ToArray();
		}

		private void Dump(DumpOptions o)
		{
			if(o.Address == null)
				throw new FormatException();
			var destination = BitcoinAddress.Create(o.Address);
			if(destination is BitcoinWitScriptAddress || destination is BitcoinWitPubKeyAddress)
				throw new FormatException("BCC disabled segwit, segwit address unsupported");
			UTXO[] utxos = GetDumpingUTXOs();
			if(utxos.Length == 0)
			{
				Logs.Main.LogWarning("No UTXO selected to dump, use `select` command to select UTXOs to select for dump");
				return;
			}
			Logs.Main.LogInformation("Dumping " + utxos.Length + " UTXOs");

			var coins = utxos.Select(u => u.AsCoin());
			var keys = FetchKeys(coins);
			DumpCoins(destination, coins, keys);
		}

		private void DumpCoins(BitcoinAddress destination, IEnumerable<Coin> coins, IEnumerable<Key> keys)
		{
			if(coins.Count() == 0)
			{
				Logs.Main.LogInformation("No coin to dump");
				return;
			}
			var total = coins.Select(u => u.Amount).Sum();
			var fee = Money.Zero;
			FeeRate feeRate = null;
			try
			{
				feeRate = RPCClient.EstimateFeeRate(6); //BCC having few fees, this will confirm fast
			}
			catch
			{
				if(RPCClient.Network == Network.RegTest)
					feeRate = new FeeRate(Money.Satoshis(30), 1);
				else
				{
					Logs.Main.LogWarning("Fee estimation unavailable, you need to wait Bitcoin Core to properly sync and retry to dump");
					return;
				}
			}
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddCoins(coins);
			builder.Send(destination, total);
			try
			{
				builder.SendEstimatedFees(feeRate);
				builder.BuildTransaction(false);
			}
			catch(NotEnoughFundsException ex)
			{
				fee = (Money)ex.Missing;
			}

			builder = new TransactionBuilder();
			builder.AddKeys(keys.ToArray());
			builder.AddCoins(coins);
			builder.Send(destination, total - fee);
			builder.SendFees(fee);
			var dumpTransaction = builder.BuildTransaction(true, SigHash.ForkId | SigHash.All);
			Logs.Main.LogInformation("Dump transaction created " + dumpTransaction.ToHex());

			Logs.Main.LogInformation("Dump transaction ID " + dumpTransaction.GetHash());
			//repository.lock(coins.select(c => c.outpoint).toarray(), dumptransaction.gethash());
			Thread.Sleep(1000);
			while(true)
			{
				Console.WriteLine("Are you sure to dump " + coins.Select(c => c.Amount).Sum().ToString() + " BCash coin to " + destination.ToString() + " ? (type `yes` to continue)");
				var response = Console.ReadLine();
				if(response.Equals("yes", StringComparison.OrdinalIgnoreCase))
					break;
			}

			Logs.Main.LogInformation("Connecting to a BCC node...");

			AddressManagerBehavior addrman = new AddressManagerBehavior(new AddressManager());
			addrman.PeersToDiscover = 10;
			addrman.Mode = AddressManagerBehaviorMode.Discover;
			NodesGroup group = new NodesGroup(RPCClient.Network, new NodeConnectionParameters()
			{
				IsRelay = false,
				Services = NodeServices.Nothing | NodeServices.NODE_BITCOIN_CASH,
				TemplateBehaviors = { addrman },
				UserAgent = "BCCSpliter",
				Advertize = false
			}, new NodeRequirement() { RequiredServices = NodeServices.NODE_BITCOIN_CASH });
			group.MaximumNodeConnection = 1;

			if(Configuration.BCCEndpoint != null)
			{
				addrman.PeersToDiscover = 1;
				addrman.Mode = AddressManagerBehaviorMode.None;
				addrman.AddressManager.Add(new NetworkAddress(Configuration.BCCEndpoint), IPAddress.Parse("127.0.0.1"));
				group.CustomGroupSelector = WellKnownGroupSelectors.ByEndpoint;
				group.AllowSameGroup = true;
			}

			ManualResetEvent done = new ManualResetEvent(false);
			group.ConnectedNodes.Added += (s, e) =>
			{
				Logs.Main.LogInformation("Connected to " + e.Node.RemoteSocketEndpoint);
				Logs.Main.LogInformation("Broadcasting...");
				e.Node.SendMessageAsync(new InvPayload(new InventoryVector(InventoryType.MSG_TX, dumpTransaction.GetHash())));
				e.Node.SendMessageAsync(new TxPayload(dumpTransaction));
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.CancelAfter(10000);
				try
				{
					e.Node.PingPong(cts.Token);
				}
				catch(Exception ex)
				{
					Logs.Main.LogWarning("Error while broadcasting transaction, retrying with another node...");
					e.Node.Disconnect("Error while broadcasting transaction", ex);
					return;
				}
				Logs.Main.LogInformation("Broadcasted " + dumpTransaction.GetHash());
				done.Set();
				group.Disconnect();
				group.Dispose();
			};

			group.Connect();
			done.WaitOne();
		}

		private UTXO[] GetDumpingUTXOs()
		{
			var utxos = GetWalletUTXOs();
			var selectedUTXOs = Repository.GetSelectedUTXOS().Where(c => c.LockedBy == null);
			var selected = new HashSet<OutPoint>(selectedUTXOs.Select(c => c.Outpoint));
			utxos = utxos.Where(u => selected.Contains(u.Outpoint)).ToArray();
			return utxos;
		}

		private IEnumerable<Key> FetchKeys(IEnumerable<Coin> coins)
		{
			var getSecrets = new List<Task<BitcoinSecret>>();
			var rpc = RPCClient.PrepareBatch();
			foreach(var c in coins)
			{
				var address = c.ScriptPubKey.GetDestinationAddress(RPCClient.Network);
				if(address == null)
					address = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(c.ScriptPubKey).Hash.GetAddress(RPCClient.Network);
				getSecrets.Add(rpc.DumpPrivKeyAsync(address));
			}
			rpc.SendBatch();
			return getSecrets.Select(c => c.Result.PrivateKey).ToArray();
		}

		private void List(ListOptions o)
		{
			var walletUTXOs = GetWalletUTXOs().ToDictionary(u => u.Outpoint);
			var selected = Repository.GetSelectedUTXOS().ToDictionary(u => u.Outpoint);

			HashSet<uint256> confirmingTransactions = new HashSet<uint256>();

			HashSet<OutPoint> outpoints = new HashSet<OutPoint>();
			foreach(var s in selected.Values)
			{
				outpoints.Add(s.Outpoint);
				if(s.LockedBy != null)
					confirmingTransactions.Add(s.LockedBy);
			}
			foreach(var w in walletUTXOs.Values)
				outpoints.Add(w.Outpoint);

			if(confirmingTransactions.Count != 0)
			{
				Console.WriteLine("------------------------");
				Console.WriteLine("Waiting confirmation for:");
				foreach(var id in confirmingTransactions)
				{
					Console.WriteLine("\t" + id);
				}
				Console.WriteLine("Type `confirm <txid>` once those are confirmed on BCC chain, so you can use your coin again on BTC");
				Console.WriteLine("------------------------");
			}

			Console.WriteLine("------------------------");
			Console.WriteLine("Selectable UTXOs in your wallet for next dump");

			foreach(var outpoint in outpoints)
			{
				var wallet = walletUTXOs.TryGet(outpoint);
				var selectedUTXO = selected.TryGet(outpoint);
				if(wallet != null && selectedUTXO == null)
				{
					Console.WriteLine("\t" + outpoint + "\t" + wallet.Amount.ToString());
				}
			}
			Console.WriteLine("Type `select <outpoint>` to select one of those UTXO, or `select all` to select all of them");
			Console.WriteLine("Once you selected your outpoints, use `dump <Destination on BCC>` to dump");
			Console.WriteLine("------------------------");

			Console.WriteLine("------------------------");
			Console.WriteLine("Selected UTXO in your wallet scheduled for the next dump");
			Money amount = Money.Zero;
			foreach(var utxo in GetDumpingUTXOs())
			{
				Console.WriteLine("\t" + utxo.Outpoint);
				amount += utxo.Amount;
			}
			Console.WriteLine("Total: " + amount.ToString() + " BTC");
			Console.WriteLine("`dump <Destination on BCC>` will dump all those UTXOS");
			Console.WriteLine("------------------------");
		}

		private void Select(SelectOptions o)
		{
			int height = RPCClient.GetBlockCount();
			UTXO[] utxos = GetWalletUTXOs();
			utxos = Select(o.Selector, utxos);
			var inserted = Repository.AddSelected(utxos);
			Logs.Main.LogInformation("Selected " + inserted + " UTXOs");
		}

		private UTXO[] GetWalletUTXOs()
		{
			var utxos = RPCClient.ListUnspent(0, 9999999).Where(i => i.IsSpendable)
				.Where(c => true) //Filter what can't be splitted
				.Select(i => new UTXO()
				{
					Outpoint = i.OutPoint,
					Amount = i.Amount,
					ScriptPubKey = i.ScriptPubKey,
					RedeemScript = i.RedeemScript
				}).ToArray();
			return utxos;
		}

		private UTXO[] Select(string selector, UTXO[] utxos)
		{
			if(selector == null)
				throw new FormatException();
			if(selector.Equals("all", StringComparison.OrdinalIgnoreCase))
				return utxos;
			var selectedOutpoints = new HashSet<OutPoint>(selector.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => OutPoint.Parse(s)).ToArray());
			return utxos.Where(u => selectedOutpoints.Contains(u.Outpoint)).ToArray();
		}

		private void UnSelect(UnselectOptions o)
		{
			var utxos = Repository.GetSelectedUTXOS();
			utxos = Select(o.Selector, utxos);
			Logs.Main.LogInformation("Unselected " + utxos.Length + " UTXOs");
			Repository.RemoveSelected(utxos);
		}
	}
}
