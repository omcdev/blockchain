


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    internal class Link_Utxo_TxDac : AppDbBase<Link_Utxo_TxDac>
    {
        internal List<string> Get(string hashIndex)
        {
            var key = GetKey(AppTables.Utxo_TxLinkItem, hashIndex);
            return AppDomain.Get<List<string>>(key) ?? new List<string>();
        }

        public void Del(string hashIndex)
        {
            var key = GetKey(AppTables.Utxo_TxLinkItem, hashIndex);
            AppDomain.Del(key);
        }

        public void Insert(string hashIndex, List<string> links)
        {
            var key = GetKey(AppTables.Utxo_TxLinkItem, hashIndex);
            AppDomain.Put(key, links);
        }

        public void Insert(Dictionary<string, List<string>> links)
        {
            var dic = links.Select(x => new KeyValuePair<string, List<string>>(GetKey(AppTables.Utxo_TxLinkItem, x.Key), x.Value));
            AppDomain.Put(dic);
        }

        public void Update(IEnumerable<string> hashIndexs, string hash)
        {
            var keys = hashIndexs.Select(x => GetKey(AppTables.Utxo_TxLinkItem, x));

            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            foreach (var hashIndex in hashIndexs)
            {
                var vs = Get(hashIndex);
                if (!vs.Contains(hash))
                    vs.Add(hash);
                dic.Add(hashIndex, vs);
            }
            AppDomain.Put(dic);
        }
    }
}