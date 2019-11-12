

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Consensus;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.DataAgent;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OmniCoin.Business
{
    public class MiningPoolComponent
    {
        public static List<MiningMsg> CurrentMiningPools = new List<MiningMsg>();

        public static void LoadMiningPools()
        {
            CurrentMiningPools = MiningPoolDac.Default.SelectAll()?.ToList();
            CurrentMiningPools = CurrentMiningPools ?? new List<MiningMsg>();
        }

        public MiningPoolComponent()
        {
            if (CurrentMiningPools == null)
            {
                CurrentMiningPools = MiningPoolDac.Default.SelectAll()?.ToList();
                CurrentMiningPools = CurrentMiningPools ?? new List<MiningMsg>();
            }
        }

        private List<MiningMsg> GetAllMiningPoolsInDb()
        {
            var result = MiningPoolDac.Default.SelectAll()?.ToList();
            return result ?? new List<MiningMsg>();
        }

        public List<MiningMsg> GetAllMiningPools()
        {
            return CurrentMiningPools.ToList();
        }

        public List<MiningMsg> UpdateMiningPools(List<MiningMsg> miningMsgs)
        {
            List<MiningMsg> newMsgs = new List<MiningMsg>();
            if (miningMsgs == null || miningMsgs.Count == 0)
                return newMsgs;

            var items = miningMsgs.Where(x => !HasMiningPool(x.Name, x.PublicKey));

            if (!items.Any())
                return newMsgs;
            MiningPoolDac.Default.Put(items);
            newMsgs.AddRange(miningMsgs);
            CurrentMiningPools = this.GetAllMiningPoolsInDb();
            return newMsgs;
        }

        //public List<MiningMsg> UpdateMiningPoolsBackup(List<MiningMsg> miningMsgs)
        //{
        //    List<MiningMsg> newMsgs = new List<MiningMsg>();
        //    if (miningMsgs == null || miningMsgs.Count == 0)
        //        return newMsgs;

        //    var items = miningMsgs.Where(x => HasMiningPool(x.Name, x.PublicKey));

        //    if (!items.Any())
        //        return newMsgs;

        //    MiningPoolDac.Default.Put(items);
        //    newMsgs.AddRange(miningMsgs);
        //    CurrentMiningPools = this.GetAllMiningPoolsInDb();
        //    return newMsgs;
        //}

        public bool AddMiningToPool(MiningMsg msg)
        {
            if (HasMiningPool(msg.Name, msg.PublicKey))
                return false;

            if (!POC.VerifyMiningPoolSignature(msg.PublicKey, msg.Signature))
                return false;

            MiningPoolDac.Default.Put(msg);
            CurrentMiningPools = GetAllMiningPoolsInDb();
            return true;
        }

        public long GetLocalMiningPoolCount()
        {
            return CurrentMiningPools.Count;
        }

        public MiningMsg GetMiningPoolByName(string poolName)
        {
            return CurrentMiningPools.FirstOrDefault(x => x.Name.ToLower().Equals(poolName.ToLower()));
        }

        public bool HasMiningPool(string poolName,string publicKey = null)
        {
            return CurrentMiningPools.Any(x => x.Name.ToLower().Equals(poolName.ToLower()) || x.PublicKey.Equals(publicKey));
        }
    }
}
