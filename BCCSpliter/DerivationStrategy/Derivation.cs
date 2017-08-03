using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter.DerivationStrategy
{
	public class Derivation
    {
		public Script ScriptPubKey
		{
			get; set;
		}
		public Script Redeem
		{
			get; set;
		}
		public Key Key
		{
			get; set;
		}
	}
}
