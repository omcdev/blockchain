

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Consensus;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.Business
{
    public class BlockchainComponent
    {
        public void Initialize()
        {
            DtoExtensions.GetAccountByLockScript = lockScript => AccountIdHelper.CreateAccountAddressByPublicKeyHash(Base16.Decode(Script.GetPublicKeyHashFromLockScript(lockScript)));

            OmniCoin.Update.StartUp.Start();

            DbDomains.Init();
            RepairBlockData();
            RepairUtxoSet();

            MiningPoolComponent.LoadMiningPools();

            DbDomains.InitData();
            LogHelper.Info("Load Data Completed");
        }
        
        private void RepairBlockData()
        {
            if (!GlobalParameters.IsTestnet)
            {
                var block = BlockDac.Default.SelectByHeight(46045);
                if (block == null)
                    return;
                var transactions = block.Transactions.First(x => x.Hash.Equals("E07887591481377EC145B2BFF3B99A50BB64F99C40D60A5AA0E3AA51AB6E8ED6"));
                if (transactions.Outputs[7].Amount != 1360580533)
                {
                    transactions.Outputs[7].Amount = 1360580533;
                }
                BlockDac.Default.SaveBlockOnly(block);

                var output = UtxoSetDac.Default.Get("E07887591481377EC145B2BFF3B99A50BB64F99C40D60A5AA0E3AA51AB6E8ED6", 7);
                if (output.Amount != 1360580533)
                {
                    output.Amount = 1360580533;
                    UtxoSetDac.Default.Put(output);
                }
            }
        }

        private void RepairUtxoSet()
        {
            if (!GlobalParameters.IsTestnet)
            {
                var last = BlockDac.Default.SelectLast();
                if (last == null || last.Header.Height <= 46045)
                    return;

                Dictionary<string, long> hashIndexs = new Dictionary<string, long>();
                hashIndexs.Add("6A7A181DFF77913F35F3F9118B3CAEEA280D12A3EE329A361C66877CDA586940_0", 46563);
                hashIndexs.Add("7668378E5CC675F1CC59F9A42A37E58690ADF16C98F6F55D7C10A84E2C0ABA91_51", 53697);
                hashIndexs.Add("DF95B94002EEC1B7CAF44A7C406C79F02223E5BD4CF97EBDE6841857B09DCBED_51", 57981);
                hashIndexs.Add("E07887591481377EC145B2BFF3B99A50BB64F99C40D60A5AA0E3AA51AB6E8ED6_13", 64848);
                hashIndexs.Add("E07887591481377EC145B2BFF3B99A50BB64F99C40D60A5AA0E3AA51AB6E8ED6_51", 64848);
                hashIndexs.Add("434316BE6605557B20255E35330D286ACA6E0487713E5129DFB113916269689F_51", 64848);
                hashIndexs.Add("0AC4E03A911D33DCFC873B47453B17CB9F02F810CADED046F2327CE04B1C503F_51", 64848);
                hashIndexs.Add("605921BA165EBE7A878B5A93712FFD137D692F3DF646DB7DE211864278C9E140_51", 64848);

                List<UtxoSet> updates = new List<UtxoSet>();

                foreach (var hashIndex in hashIndexs)
                {
                    if (last.Header.Height >= hashIndex.Value)
                    {
                        var output = UtxoSetDac.Default.Get(hashIndex.Key);
                        if (output != null && output.IsSpent == false)
                        {
                            output.IsSpent = true;
                            output.SpentHeight = hashIndex.Value;
                            updates.Add(output);
                        }
                    }
                }

                if (updates.Any())
                {
                    UtxoSetDac.Default.Update(updates);
                }
            }
        }
    }
}