

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.Business
{
    public class AddressBookComponent
    {
        public void Upsert(string oldAddress, string address, string tag)
        {
            if (!string.IsNullOrEmpty(oldAddress))
                Delete(oldAddress);
            SetTag(address, tag);
        }

        public void SetTag(string address, string tag)
        {
            AddressBookDac.Default.InsertOrUpdate(address, tag);
        }

        public void Delete(string address)
        {
            AddressBookDac.Default.Delete(address);
        }

        public void Delete(IEnumerable<string> addresses)
        {
            AddressBookDac.Default.Delete(addresses);
        }

        public List<AddressBookItem> GetWholeAddressBook()
        {
            return AddressBookDac.Default.SelectAll()?.ToList();
        }

        public List<AddressBookItem> GetByTag(string tag)
        {
            return AddressBookDac.Default.SelectAddessListByTag(tag)?.ToList();
        }

        public AddressBookItem GetByAddress(string address)
        {
            return AddressBookDac.Default.SelectByAddress(address);
        }

        public string GetTagByAddress(string address)
        {
            var item = AddressBookDac.Default.SelectByAddress(address);

            if(item != null)
            {
                return item.Tag;
            }
            else
            {
                return null;
            }
        }
    }
}
