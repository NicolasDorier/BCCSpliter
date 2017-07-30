using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter
{
	[Verb("select", HelpText = "Select UTXOs to dump, you can use `list` command to see which to dump")]
	public class SelectOptions
	{
		[Value(0, HelpText = "Selector of UTXO, can be `all`, `beforesplit`, or outpoint with the format delimited by commas '[hash]-[n],[hash2]-[n2]'")]
		public string Selector
		{
			get; set;
		}
	}

	[Verb("unselect", HelpText = "Unselect UTXOs to dump")]
	public class UnselectOptions
	{
		[Value(0, HelpText = "Selector of UTXO, can be `all`, `beforesplit`, or outpoint with the format delimited by commas '[hash]-[n],[hash2]-[n2]'")]
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

	[Verb("confirm", HelpText = "Unlock UTXO in your BTC wallet once they have been confirmed on BCC chain")]
	public class ConfirmOptions
	{
		[Value(0, HelpText = "Transaction Id which got confirmed on BCC chain")]
		public string TransactionId
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
