using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace BCCSpliter.DerivationStrategy
{
	public class BIP44P2PKHStrategy : IStrategy
	{
		private ExtKey rootDerivation;

		public BIP44P2PKHStrategy(ExtKey root, int account, bool change)
		{
			rootDerivation = root.Derive(new KeyPath("m/44'/0'/" + account + "'/" + (change ? "1" : "0")));
		}
		public Derivation Derive(int i)
		{
			var privateKey = rootDerivation.Derive((uint)i).PrivateKey;
			return new Derivation() { Key = privateKey, ScriptPubKey = privateKey.PubKey.Hash.ScriptPubKey };
		}
	}
}
