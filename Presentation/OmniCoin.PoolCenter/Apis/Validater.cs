


using OmniCoin.Consensus;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolCenter.Apis
{
    public class Validater
    {
        public static bool PoolAccount(string account)
        {
            return AccountIdHelper.AddressVerify(account);
        }
    }
}
