


using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class BlackPeerDac : AppDbBase<BlackPeerDac>
    {
        private List<BlackListItem> BlackList;

        public BlackPeerDac()
        {
            BlackList = LoadBlackPeers();
            if (BlackList.Any(x => x == null))
            {
                BlackList.RemoveAll(x => x == null);
                Update();
            }
        }

        private List<BlackListItem> LoadBlackPeers()
        {
            return AppDomain.Get<List<BlackListItem>>(AppSetting.BlackPeers) ?? new List<BlackListItem>();
        }

        private void Update()
        {
            AppDomain.Put(AppSetting.BlackPeers, BlackList);
        }

        public void Save(string address, long? expired)
        {
            var item = BlackList.FirstOrDefault(x => x.Address.Equals(address));
            if (item != null && expired.HasValue)
            {
                item.Expired = expired.Value;
            }
            else
            {
                var blackListItem = new BlackListItem();
                blackListItem.Address = address;
                blackListItem.Expired = expired.HasValue ? expired.Value : 0;
                blackListItem.Timestamp = Time.EpochTime;
                BlackList.Add(blackListItem);
            }
            Update();
        }

        public void Delete(string address)
        {
            var item = BlackList.FirstOrDefault(x => x.Address.Equals(address));
            if (item != null)
            {
                BlackList.Remove(item);
            }
            Update();
        }

        public void DeleteAll()
        {
            BlackList.Clear();
            Update();
        }

        public bool CheckExists(string address)
        {
            return BlackList.Any(x => x.Address.Equals(address));
        }

        public List<BlackListItem> GetAll()
        {
            return BlackList.ToList();
        }
    }
}