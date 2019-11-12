

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Consensus
{
    public class ScoopData
    {
        public int Index { get; set; }
        public ScoopDataItem FirstData { get; set; }
        public ScoopDataItem SecondData { get; set; }

        public byte[] FullData
        {
            get
            {
                if(FirstData == null || SecondData == null)
                {
                    return null;
                }
                else
                {
                    var bytes = new byte[64];
                    Array.Copy(FirstData.Hash, 0, bytes, 0, FirstData.Hash.Length);
                    Array.Copy(SecondData.Hash, 0, bytes, FirstData.Hash.Length, SecondData.Hash.Length);

                    return bytes;
                }
            }
        }
    }
}
