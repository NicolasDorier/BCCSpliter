using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace BCCSpliter.DerivationStrategy
{
	public class DirectDerivationStrategy : IStrategy
	{
		private ExtKey rootDerivation;

		public DirectDerivationStrategy(ExtKey root, bool change)
		{
			rootDerivation = root.Derive(new KeyPath(change ? "1" : "0"));
		}
		public Derivation Derive(int i)
		{
			var privateKey = rootDerivation.Derive((uint)i).PrivateKey;
			return new Derivation() { Key = privateKey, ScriptPubKey = privateKey.PubKey.Hash.ScriptPubKey };
		}
	}
}
