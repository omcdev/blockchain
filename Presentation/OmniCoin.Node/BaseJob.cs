

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Node
{
    public abstract class BaseJob
    {
        public abstract void Start();
        public abstract void Stop();
        public abstract JobStatus Status { get;}       
        
    }
}
