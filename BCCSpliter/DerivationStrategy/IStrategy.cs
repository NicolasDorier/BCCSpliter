using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCCSpliter.DerivationStrategy
{
    public interface IStrategy
    {
		Derivation Derive(int i);
    }
}
