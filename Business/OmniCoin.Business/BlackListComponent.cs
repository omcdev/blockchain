

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Messages;
using OmniCoin.Entities;
using OmniCoin.DataAgent;
using System;
using System.Collections.Generic;
using System.Text;
using OmniCoin.Data;
using OmniCoin.Framework;
using OmniCoin.Consensus;
using System.Linq;
using OmniCoin.Data.Dacs;

namespace OmniCoin.Business
{
    public class BlackListComponent
    {
        public bool Add(string address, long? expiredTime)
        {
            if(!BlackPeerDac.Default.CheckExists(address))
            {
                BlackPeerDac.Default.Save(address, expiredTime);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Delete(string address)
        {
            if (!BlackPeerDac.Default.CheckExists(address))
            {
                return false;
            }
            else
            {
                BlackPeerDac.Default.Delete(address);
                return true;
            }
        }

        public void Clear()
        {
            BlackPeerDac.Default.DeleteAll();
        }

        public bool Exists(string address)
        {
            return BlackPeerDac.Default.CheckExists(address);
        }
    }
}
