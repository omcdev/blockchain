using OmniCoin.MiningPool.Data;
using OmniCoin.MiningPool.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using OmniCoin.Framework;

namespace OmniCoin.MiningPool.Business
{
    public class HashRatesComponent
    {
        public HashRates SaveHashRates(long hashes)
        {
            HashRatesDac dac = new HashRatesDac();
            HashRates rates = new HashRates();
            rates.Time = Time.EpochTime;
            rates.Hashes = hashes;

            dac.Insert(rates);
            return rates;
        }

        public List<HashRates> SelectAll()
        {
            HashRatesDac dac = new HashRatesDac();
            return dac.SelectAll();
        }

        public void Delete(long id)
        {
            HashRatesDac dac = new HashRatesDac();
            dac.Delete(id);
        }

        public void Update(HashRates entity)
        {
            HashRatesDac dac = new HashRatesDac();
            dac.Update(entity);
        }

        public bool IsExist(long id)
        {
            HashRatesDac dac = new HashRatesDac();
            return dac.IsExisted(id);
        }

        public HashRates SelectById(long id)
        {
            HashRatesDac dac = new HashRatesDac();
            return dac.SelectById(id);
        }
    }
}
