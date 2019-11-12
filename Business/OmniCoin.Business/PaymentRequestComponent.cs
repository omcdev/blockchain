

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System.Collections.Generic;

namespace OmniCoin.Business
{
    public class PaymentRequestComponent
    {
        public static PaymentRequest Add(string accountId, string tag, string comment, long amount)
        {
            var item = new PaymentRequest();
            item.AccountId = accountId;
            item.Tag = tag;
            item.Comment = comment;
            item.Amount = amount;
            item.Timestamp = Time.EpochTime;

            PaymentRequestDac.Default.Insert(item);

            return item;
        }

        public static void DeleteByIds(string[] ids)
        {
            PaymentRequestDac.Default.Del(ids);
        }

        public static List<PaymentRequest> GetAll()
        {
            return PaymentRequestDac.Default.GetAll();
        }
    }
}
