


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class UtxoStateDac : BlockDbBase<UtxoStateDac>
    {
        public void Put(long height, List<UtxoSetState> setStates)
        {
            var key = GetKey(BlockTables.Link_Height_UpdateUtxo, $"{height}");
            BlockDomain.Put(key, setStates);
        }

        public List<UtxoSetState> Get(long height)
        {
            var key = GetKey(BlockTables.Link_Height_UpdateUtxo, $"{height}");
            return BlockDomain.Get<List<UtxoSetState>>(key);
        }

    }
}