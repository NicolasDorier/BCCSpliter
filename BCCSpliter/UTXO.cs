using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter
{
	public class UTXO
	{
		public OutPoint Outpoint
		{
			get; set;
		}
		public bool BeforeFork
		{
			get; set;
		}
		public Script RedeemScript
		{
			get;
			set;
		}
		public Script ScriptPubKey
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}


		public uint256 LockedBy
		{
			get; set;
		}

		public Coin AsCoin()
		{
			var c = new Coin(Outpoint, new TxOut(Amount, ScriptPubKey));
			if(RedeemScript == null)
				return c;
			return c.ToScriptCoin(RedeemScript);
		}
	}
}
