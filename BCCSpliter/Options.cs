using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter
{
	[Verb("select", HelpText = "Select UTXOs to dump, you can use `list` command to see which to dump")]
	public class SelectOptions
	{
		[Value(0, HelpText = "Selector of UTXO, can be `all`, or outpoint with the format delimited by commas '[hash]-[n],[hash2]-[n2]'")]
		public string Selector
		{
			get; set;
		}
	}

	[Verb("dumphd", HelpText = "Dump from external hd private key or BIP39 mnemonic (Using P2SH/BIP45/1-1, please ask me for broader support)")]
	public class ImportOptions
	{
		[Value(0, HelpText = "The root private key or mnemonic")]
		public string ExtKey
		{
			get; set;
		}
		[Value(1, HelpText = "The destination to dump")]
		public string Destination
		{
			get; set;
		}

		[Value(2, HelpText = "Mnemonic passphrase if needed")]
		public string Passphrase
		{
			get; set;
		}
	}

	[Verb("dumpprivkey", HelpText = "Dump from a BIP58 formatted private key (typically used with paper wallet)")]
	public class ImportSecretOptions
	{
		[Value(0, HelpText = "The private key")]
		public string Key
		{
			get; set;
		}
		[Value(1, HelpText = "The destination to dump")]
		public string Destination
		{
			get; set;
		}
	}

	[Verb("unselect", HelpText = "Unselect UTXOs to dump")]
	public class UnselectOptions
	{
		[Value(0, HelpText = "Selector of UTXO, can be `all`, or outpoint with the format delimited by commas '[hash]-[n],[hash2]-[n2]'")]
		public string Selector
		{
			get; set;
		}
	}

	[Verb("dump", HelpText = "Dump selected UTXOs, and lock those UTXO in the BTC wallet")]
	public class DumpOptions
	{
		[Value(0, HelpText = "Bitcoin Address on the BCC chain to dump")]
		public string Address
		{
			get; set;
		}
	}

	[Verb("exit", HelpText = "Quit.")]
	public class QuitOptions
	{
		//normal options here
	}


	[Verb("list", HelpText = "Get the current status of UTXOs")]
	public class ListOptions
	{
		//normal options here
	}
}
