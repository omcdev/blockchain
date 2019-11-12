


using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class AddressBookDac : UserDbBase<AddressBookDac>
    {
        internal List<string> AddressBook = new List<string>();

        public AddressBookDac()
        {
            AddressBook.AddRange(LoadAddressBook());
        }

        #region Update
        public virtual void InsertOrUpdate(string address, string tag)
        {
            var key = GetKey(UserTables.AddressItem, address);
            AddressBookItem item = new AddressBookItem { Address = address, Tag = tag, Timestamp = Time.EpochTime };
            UserDomain.Put(key, item);

            if(!AddressBook.Contains(address))
                AddressBook.Add(address);
            UpdateAddressBook(AddressBook);
        }

        public virtual void InsertOrUpdate(IEnumerable<AddressBookItem> addressBooks)
        {
            Dictionary<string, AddressBookItem> pairs = new Dictionary<string, AddressBookItem>();
            List<string> addresses = new List<string>();
            foreach (var address in addressBooks)
            {
                var key = GetKey(UserTables.AddressItem, address.Address);
                pairs.Add(key, address);
                if (!AddressBook.Contains(address.Address))
                    addresses.Add(address.Address);
            }
            UserDomain.Put(pairs);

            AddressBook.AddRange(addresses);
            UpdateAddressBook(AddressBook);
        }

        public virtual void UpdateTimestamp(string address)
        {
            var key = GetKey(UserTables.AddressItem, address);
            var addressItem = UserDomain.Get<AddressBookItem>(key);
            if (addressItem == null)
                return;
            addressItem.Timestamp = Time.EpochTime;
            UserDomain.Put(key, addressItem);
        }
        public virtual void Delete(string address)
        {
            var key = GetKey(UserTables.AddressItem, address);
            UserDomain.Del(key);

            AddressBook.Remove(address);
            UpdateAddressBook(AddressBook);
        }

        public virtual void Delete(IEnumerable<string> addresses)
        {
            var keys = addresses.Select(address => GetKey(UserTables.AddressItem, address));
            UserDomain.Del(keys);

            AddressBook.RemoveAll(address=> addresses.Contains(address));
            UpdateAddressBook(AddressBook);
        }

        public virtual void DeleteByTag(string tag)
        {
            var removeItems = SelectAddessListByTag(tag);
            if (removeItems == null)
                return;
            var addresses = removeItems.Select(x => x.Address);
            Delete(addresses);
        }
        #endregion

        #region Query
        public virtual IEnumerable<AddressBookItem> SelectAddessListByTag(string tag)
        {
            var addressItems = SelectAll();
            if (addressItems == null)
                return null;
            var matchItems = addressItems.Where(x => x.Tag == tag);
            return matchItems;
        }

        public virtual AddressBookItem SelectByAddress(string address)
        {
            var key = GetKey(UserTables.AddressItem, address);
            return UserDomain.Get<AddressBookItem>(key);
        }

        public virtual IEnumerable<AddressBookItem> SelectAll()
        {
            var keys = AddressBook.Select(x => GetKey(UserTables.AddressItem, x));
            return UserDomain.Get<AddressBookItem>(keys);
        }
        #endregion
        
        #region AddressBook
        internal IEnumerable<string> LoadAddressBook()
        {
            return UserDomain.Get<IEnumerable<string>>(UserSetting.AddressBook)??new List<string>();
        }

        internal void UpdateAddressBook(IEnumerable<string> addressbook)
        {
            UserDomain.Put(UserSetting.AddressBook, addressbook);
        }
        #endregion
    }
}
