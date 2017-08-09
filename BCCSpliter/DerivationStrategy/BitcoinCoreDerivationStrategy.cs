using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter.DerivationStrategy
{
	public class BitcoinCoreDerivationStrategy : IStrategy
	{
		private ExtKey rootDerivation;

		public BitcoinCoreDerivationStrategy(ExtKey root, bool change)
		{
			rootDerivation = root.Derive(new KeyPath("m/0'/" + (change ? "1'" : "0'")));
		}

		public Derivation Derive(int i)
		{
			var privateKey = rootDerivation.Derive(i, true).PrivateKey;
			return new Derivation() { Key = privateKey, ScriptPubKey = privateKey.PubKey.Hash.ScriptPubKey };
		}
	}
}
