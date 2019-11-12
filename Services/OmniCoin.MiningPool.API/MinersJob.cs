using OmniCoin.Framework;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace OmniCoin.MiningPool.API
{
    public class MinersJob
    {
        private static MinersJob current = null;
        public static MinersJob Current
        {
            get
            {
                if(current == null)
                {
                    current = new MinersJob();
                }

                return current;
            }
        }
        public Timer timer;
        public SafeCollection<Miners> Pool_Miners;


        public MinersJob()
        {
            Pool_Miners = new List<Miners>();
            this.UpdateMiners();

            this.timer = new Timer(5 * 60 * 1000);
            this.timer.AutoReset = true;
            this.timer.Elapsed += Timer_Elapsed;
            this.timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.UpdateMiners();
        }

        private void UpdateMiners()
        {
            MinersComponent minersComponent = new MinersComponent();
            Pool_Miners = new SafeCollection<Miners>(minersComponent.GetAllMiners());
        }
    }
}
