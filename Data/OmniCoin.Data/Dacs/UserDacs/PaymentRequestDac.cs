


using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class PaymentRequestDac : UserDbBase<PaymentRequestDac>
    {
        private List<PaymentRequest> paymentRequests;
        private List<string> paymentBook;

        public PaymentRequestDac()
        {
            paymentRequests = new List<PaymentRequest>();
            paymentBook = new List<string>();
            Load();
        }

        public void Insert(PaymentRequest request)
        {
            var key = GetKey(UserTables.PaymentRequest, request.AccountId);
            var item = paymentRequests.FirstOrDefault(x => x.AccountId == request.AccountId);
            if (item != null)
            {
                var index = paymentRequests.IndexOf(item);
                paymentRequests[index] = request;
            }
            else
            {
                paymentRequests.Add(request);
                paymentBook.Add(request.AccountId);
            }

            UserDomain.Put(key, request);
            Update();
        }

        public void Del(IEnumerable<string> ids)
        {
            var items = paymentRequests.Where(x => ids.Contains(x.AccountId)).ToArray();
            paymentRequests.RemoveAll(x => items.Contains(x));

            var delKeys = ids.Select(x => GetKey(UserTables.PaymentRequest, x));
            UserDomain.Del(delKeys);
            paymentBook.RemoveAll(x => ids.Contains(x));
            Update();
        }

        public List<PaymentRequest> GetAll()
        {
            return paymentRequests.ToList();
        }

        public List<PaymentRequest> GetAllInDb()
        {
            return paymentRequests.ToList();
        }

        private void Update()
        {
            UserDomain.Put(UserSetting.PaymentRequestBook, paymentBook);
        }

        private void Load()
        {
            try
            {
                paymentBook = UserDomain.Get<List<string>>(UserSetting.PaymentRequestBook) ?? new List<string>();
            }
            catch
            {
                try
                {
                    var paymentRequest = UserDomain.Get<List<PaymentRequest>>(UserSetting.PaymentRequestBook);
                    if (paymentRequest != null || paymentRequest.Any())
                    {
                        paymentBook.AddRange(paymentRequest.Select(x => x.AccountId));
                        Update();
                    }
                }
                catch { }
            }
            if (!paymentBook.Any())
                return;
            var keys = paymentBook.Select(x => GetKey(UserTables.PaymentRequest, x));
            paymentRequests.AddRange(UserDomain.Get<PaymentRequest>(keys));
        }
    }
}