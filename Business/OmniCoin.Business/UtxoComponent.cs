

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OmniCoin.DataAgent;
using OmniCoin.Messages;
using OmniCoin.Consensus;
using OmniCoin.Framework;
using OmniCoin.Entities;
using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;

namespace OmniCoin.Business
{
    public class UtxoComponent
    {
        public void Initialize()
        {
            
        }

        public long GetConfirmedBlanace(string accountId)
        {
            var result = UtxoSetDac.Default.GetByAccounts(new string[] { accountId });
            if (result == null)
                return 0;
            return result.Sum(x=>x.Amount);
        }

        public List<UtxoSet> GetAllConfirmedOutputs(int start, int limit)
        {
            var result = UtxoSetDac.Default.GetMyUnspents(start, limit);
            if (result == null)
                return new List<UtxoSet>();
            else
                return result.ToList();
        }

        public List<UtxoSet> GetAllConfirmedOutputs()
        {
            List<UtxoSet> result = UtxoSetDac.Default.GetMyUnspents();
            if (result == null)
            {
                return new List<UtxoSet>();
            }
            else
            {
                return result;
            }
        }
    }
}