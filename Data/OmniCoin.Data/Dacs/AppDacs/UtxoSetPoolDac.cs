// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Data.Dacs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FiiiChain.Data.Dacs
{
    /// <summary>
    /// 缓存中的UtxoSet,未经过打包交易
    /// </summary>
    public class UtxoSetPoolDac : AppDbBase<UtxoSetPoolDac>
    {
        public UtxoSetPoolDac()
        {
            var spents = LoadSpentedUtxoBook()?.ToList();
            SpentUtxoSets = spents ?? new List<string>();
        }

        private List<string> SpentUtxoSets;

        public void Insert(string hashIndex)
        {
            if (!SpentUtxoSets.Contains(hashIndex))
            {
                SpentUtxoSets.Add(hashIndex);
            }
            Update();
        }

        public void Insert(IEnumerable<string> hashIndexs)
        {
            var additems = hashIndexs.Where(x => !SpentUtxoSets.Contains(x));
            SpentUtxoSets.AddRange(additems);
            Update();
        }

        public void Del(IEnumerable<string> hashIndexs)
        {
            SpentUtxoSets.RemoveAll(x => hashIndexs.Contains(x));
            Update();
        }

        private void Update()
        {
            AppDomain.Put(AppSetting.UtxoSpentedBook, SpentUtxoSets);
        }

        public bool Contains(string hashIndex)
        {
            return SpentUtxoSets.Contains(hashIndex);
        }

        internal IEnumerable<string> LoadSpentedUtxoBook()
        {
            return AppDomain.Get<IEnumerable<string>>(AppSetting.UtxoSpentedBook);
        }
    }
}