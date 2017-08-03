using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace BCCSpliter.DerivationStrategy
{
	public class BIP45P2SH11Strategy : IStrategy
	{
		private ExtKey rootDerivation;

		public BIP45P2SH11Strategy(ExtKey root, bool change)
		{
			rootDerivation = root.Derive(new KeyPath("m/45'/2147483647/" + (change ? "1" : "0")));
		}
		public Derivation Derive(int i)
		{
			var privateKey = rootDerivation.Derive((uint)i).PrivateKey;
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, new[] { privateKey.PubKey });
			return new Derivation() { Key = privateKey, Redeem = redeem, ScriptPubKey = redeem.Hash.ScriptPubKey };
		}
	}
}
