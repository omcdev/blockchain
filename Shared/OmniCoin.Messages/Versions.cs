


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class Versions
    {
        public static int EngineVersion
        {
            get
            {
                return int.Parse(Resource.EngineVersion);
            }
        }

        public static int MsgVersion
        {
            get
            {
                return int.Parse(Resource.MsgVersion);
            }
        }

        public static int MinimumSupportVersion
        {
            get
            {
                return int.Parse(Resource.MinimumSupportVersion);
            }
        }
    }
}
