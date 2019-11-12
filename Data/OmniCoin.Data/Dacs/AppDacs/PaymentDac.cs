


using OmniCoin.Entities.CacheModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class PaymentFilter
    {
        public string category;
        public string address;
        public long amount;
        public long time;
        public string txId;
        public long vout;

        public static PaymentFilter Parse(PaymentCache payment)
        {
            PaymentFilter pf = new PaymentFilter()
            {
                address = payment.address,
                amount = payment.amount,
                category = payment.category,
                time = payment.time,
                txId = payment.txId,
                vout = payment.vout
            };
            return pf;
        }

        public static PaymentFilter Parse(string paymentStr)
        {
            var ps = paymentStr.Split('_');
            PaymentFilter pf = new PaymentFilter()
            {
                address = ps[1],
                amount = long.Parse(ps[2]),
                category = ps[0],
                time = long.Parse(ps[3]),
                txId = ps[4],
                vout = long.Parse(ps[5])
            };
            return pf;
        }

        public override string ToString()
        {
            return $"{category}_{address}_{amount}_{time}_{txId}_{vout}";
        }
    }

    public class PaymentDac : AppDbBase<PaymentDac>
    {
        /// <summary>
        /// Db中的交易记录，已经打包的
        /// </summary>
        private List<string> PaymentBook;
        /// <summary>
        /// 内存中的交易记录，未打包的
        /// </summary>
        public List<PaymentCache> Payment_Mem;

        private List<PaymentFilter> PaymentFilters;

        object lockObj = new object();

        public PaymentDac()
        {
            PaymentBook = new List<string>();
            Payment_Mem = new List<PaymentCache>();
            PaymentFilters = new List<PaymentFilter>();
            PaymentBook = GetPaymentBookInDb();

            PaymentBook.ForEach(x =>
            {
                if (string.IsNullOrEmpty(x))
                    PaymentBook.Remove(x);
                else
                    PaymentFilters.Add(PaymentFilter.Parse(x));
            });

            //PaymentBook.RemoveAll(x => string.IsNullOrEmpty(x));
            //PaymentFilters.AddRange(PaymentBook.Select(x => PaymentFilter.Parse(x)));
        }

        #region PaymentBook
        private void PutPaymentBook(List<string> ids)
        {
            ids.RemoveAll(x => PaymentBook.Contains(x));
            if (!ids.Any())
                return;
            Payment_Mem.RemoveAll(x => ids.Contains(x.ToString()));
            ids.ForEach(id => PaymentBook.Add(id));
            UpdatePaymentBook();
        }

        private List<string> GetPaymentBookInDb()
        {
            if (PaymentBook != null && PaymentBook.Any())
            {
                return PaymentFilters.Select(x => x.ToString()).ToList();
            }
            else
            {
                var payments = AppDomain.Get<List<string>>(AppSetting.PaymentBook) ?? new List<string>();
                return payments;
            }
        }

        public List<string> GetPaymentBook()
        {
            return PaymentFilters.Select(x => x.ToString()).ToList();
        }

        public void UpdatePaymentBook()
        {
            AppDomain.Put(AppSetting.PaymentBook, PaymentBook);
        }
        #endregion
        
        public void Insert(PaymentCache paymentCache)
        {
            lock (lockObj)
            {
                var paymentKey = paymentCache.ToString();
                if (PaymentBook.Contains(paymentKey))
                {
                    return;
                }
                PutPaymentBook(new List<string> { paymentKey });
                PaymentFilters.Add(PaymentFilter.Parse(paymentCache));
                var key = GetKey(AppTables.TradeRecord, paymentKey);
                AppDomain.Put(key, paymentCache);
            }
        }

        public void Insert(IEnumerable<PaymentCache> payments)
        {
            lock (lockObj)
            {
                Dictionary<string, PaymentCache> insertMap = new Dictionary<string, PaymentCache>();
                List<string> paymentKeys = new List<string>();
                foreach (var payment in payments)
                {
                    var paymentKey = payment.ToString();
                    if (PaymentBook.Contains(paymentKey))
                        continue;
                    var key = GetKey(AppTables.TradeRecord, paymentKey);
                    paymentKeys.Add(paymentKey);
                    PaymentFilters.Add(PaymentFilter.Parse(payment));
                    Payment_Mem.RemoveAll(x => x.ToString() == paymentKey);

                    insertMap.Add(key, payment);
                }
                PutPaymentBook(paymentKeys);
                AppDomain.Put(insertMap);
            }
        }

        public void InsertMem(PaymentCache paymentCache)
        {
            lock (lockObj)
            {
                if (PaymentBook.Contains(paymentCache.ToString()))
                    return;
                if (Payment_Mem.Any(x => x.ToString() == paymentCache.ToString()))
                {
                    return;
                }
                Payment_Mem.Add(paymentCache);
                PaymentFilters.Add(PaymentFilter.Parse(paymentCache));
            }
        }

        public void InsertMem(IEnumerable<PaymentCache> paymentCaches)
        {
            paymentCaches = paymentCaches.Where(x => !PaymentBook.Contains(x.ToString()));
            var ms = Payment_Mem.Select(x => x.ToString());
            paymentCaches = paymentCaches.Where(x => !ms.Contains(x.ToString()));
            if (!paymentCaches.Any())
            {
                return;
            }
            PaymentFilters.AddRange(paymentCaches.Select(x => PaymentFilter.Parse(x)));
            Payment_Mem.AddRange(paymentCaches);
        }

        public List<PaymentCache> GetPayments(IEnumerable<string> keys)
        {
            List<PaymentCache> result = new List<PaymentCache>();
            result.AddRange(Payment_Mem.Where(x => keys.Contains(x.ToString())));
            var pkeys = keys.Select(x => GetKey(AppTables.TradeRecord, x));

            var payments = AppDomain.Get<PaymentCache>(pkeys);
            if (payments != null)
                result.AddRange(payments);
            return result;
        }

        public List<PaymentFilter> GetAllFilter()
        {
            return PaymentFilters.ToList();
        }

        public void Del(IEnumerable<string> keys)
        {
            lock (lockObj)
            {
                PaymentFilters.RemoveAll(x => keys.Contains(x.ToString()));
                Payment_Mem.RemoveAll(x => keys.Contains(x.ToString()));
                var payments = PaymentBook.Where(x => keys.Contains(x.ToString())).ToList();
                var paymentKeys = payments.Select(x => GetKey(AppTables.TradeRecord, x));
                AppDomain.Del(paymentKeys);
                PaymentBook.RemoveAll(x => payments.Contains(x));
            }
        }

        public void Clear()
        {
            lock (lockObj)
            {
                PaymentFilters.Clear();
                Payment_Mem.Clear();
                var paymentKeys = PaymentBook.Select(x => GetKey(AppTables.TradeRecord, x));
                AppDomain.Del(paymentKeys);
                PaymentBook.Clear();
            }
        }

    }
}