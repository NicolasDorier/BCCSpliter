using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace BCCSpliter
{
	public class Repository : IDisposable
	{
		DBreezeRepository _Repo;
		public Repository(string folder)
		{
			if(folder == null)
				throw new ArgumentNullException(nameof(folder));
			_Repo = new DBreezeRepository(folder);
		}
		internal int AddSelected(UTXO[] utxos)
		{
			int total = 0;
			foreach(var utxo in utxos)
			{
				bool added = true;
				_Repo.UpdateOrInsert("1", utxo.Outpoint.ToString(), utxo, (o, n) =>
				{
					added = false;
					return o;
				});
				if(added)
					total++;
			}
			return total;
		}

		public void RemoveSelected(UTXO[] utxos)
		{
			foreach(var utxo in utxos)
				_Repo.Delete<UTXO>("1", utxo.Outpoint.ToString());
		}

		internal int Unlock(uint256 transactionId)
		{
			int unlocked = 0;
			var existing = _Repo.List<UTXO>("1");
			foreach(var utxo in existing)
			{
				if(utxo.LockedBy == transactionId)
				{
					_Repo.Delete<UTXO>("1", utxo.Outpoint.ToString());
					unlocked++;
				}
			}
			return unlocked;
		}

		internal UTXO[] GetSelectedUTXOS()
		{
			return _Repo.List<UTXO>("1");
		}

		public void Lock(OutPoint[] outpoints, uint256 txid)
		{
			var existing = _Repo.List<UTXO>("1");
			var locked = new HashSet<OutPoint>(outpoints);
			foreach(var utxo in existing)
			{
				if(locked.Contains(utxo.Outpoint))
				{
					utxo.LockedBy = txid;
					_Repo.UpdateOrInsert("1", utxo.Outpoint.ToString(), utxo, (o, n) => n);
				}
			}
		}

		public void Dispose()
		{
			_Repo.Dispose();
		}
	}
}
