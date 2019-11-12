using OmniCoin.MiningPool.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using OmniCoin.Framework;
using OmniCoin.MiningPool.Data;

namespace OmniCoin.MiningPool.Business
{
    public class BlockRatesComponent
    {
        public BlockRates SaveBlockRates(long blocks, long difficulty)
        {
            BlockRatesDac dac = new BlockRatesDac();
            BlockRates rates = new BlockRates();
            rates.Blocks = blocks;
            rates.Difficulty = difficulty;
            rates.Time = Time.EpochTime;

            dac.Insert(rates);
            return rates;
        }

        public List<BlockRates> SelectAll()
        {
            BlockRatesDac dac = new BlockRatesDac();
            return dac.SelectAll();
        }

        public void Delete(long id)
        {
            BlockRatesDac dac = new BlockRatesDac();
            dac.Delete(id);
        }

        public void Update(BlockRates entity)
        {
            BlockRatesDac dac = new BlockRatesDac();
            dac.Update(entity);
        }

        public bool IsExist(long id)
        {
            BlockRatesDac dac = new BlockRatesDac();
            return dac.IsExisted(id);
        }

        public BlockRates SelectById(long id)
        {
            BlockRatesDac dac = new BlockRatesDac();
            return dac.SelectById(id);
        }
    }
}
