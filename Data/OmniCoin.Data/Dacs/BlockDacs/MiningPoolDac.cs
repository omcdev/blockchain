


using OmniCoin.Entities;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class MiningPoolDac : BlockDbBase<MiningPoolDac>
    {
        public List<string> MiningPools = new List<string>();

        public MiningPoolDac()
        {
            MiningPools.AddRange(LoadMiningPoolNames());
        }

        private IEnumerable<string> LoadMiningPoolNames()
        {
            return BlockDomain.Get<List<string>>(BlockDataSetting.MiningPools)??new List<string>();
        }

        public void UpdateMiningPoolNames(IEnumerable<string> pools)
        {
            BlockDomain.Put(BlockDataSetting.MiningPools, pools);
        }

        public MiningPool Get(string name)
        {
            if (!MiningPools.Contains(name))
                return null;
            var key = GetKey(BlockTables.MiningPool, name);
            return BlockDomain.Get<MiningPool>(key);
        }

        public IEnumerable<MiningMsg> SelectAll()
        {
            var names = LoadMiningPoolNames();
            if (names == null)
                return null;
            var keys = names.Select(x => GetKey(BlockTables.MiningPool, x));
            return BlockDomain.Get<MiningMsg>(keys)??new List<MiningMsg>();
        }

        public void Del(IEnumerable<string> names)
        {
            var keys = names.Select(x => GetKey(BlockTables.MiningPool, x));
            BlockDomain.Del(keys);
            MiningPools.RemoveAll(x => names.Contains(x));
            UpdateMiningPoolNames(MiningPools);
        }

        public void Del(string name)
        {
            var key = GetKey(BlockTables.MiningPool, name);
            BlockDomain.Del(key);
            MiningPools.Remove(name);
            UpdateMiningPoolNames(MiningPools);
        }

        public void Put(MiningMsg mp)
        {
            var key = GetKey(BlockTables.MiningPool, mp.Name);
            BlockDomain.Put(key, mp);

            MiningPools.Add(mp.Name);
            UpdateMiningPoolNames(MiningPools);
        }
        public void Put(IEnumerable<MiningMsg> mps)
        {
            Dictionary<string, MiningMsg> updates = new Dictionary<string, MiningMsg>();
            foreach (var mp in mps)
            {
                var key = GetKey(BlockTables.MiningPool, mp.Name);
                if (!updates.ContainsKey(key))
                    updates.Add(key, mp);
                if (!MiningPools.Contains(mp.Name))
                {
                    MiningPools.Add(mp.Name);
                }
            }
            BlockDomain.Put(updates);
            UpdateMiningPoolNames(MiningPools);
        }
    }
}