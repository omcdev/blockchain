


using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class TransactionCommentDac : UserDbBase<TransactionCommentDac>
    {
        public TransactionCommentDac()
        {
            Load();
        }

        internal List<string> CommentBook = new List<string>();
        void Load()
        {
            CommentBook.AddRange(LoadCommentBook());
        }

        public TransactionComment Get(string txHash,int vout)
        {
            var key = GetKey(UserTables.TxComment, $"{txHash}_{vout}");
            return UserDomain.Get<TransactionComment>(key);
        }

        public IEnumerable<TransactionComment> SelectAll()
        {
            var keys = CommentBook.Select(x => GetKey(UserSetting.AccountBook, x));
            return UserDomain.Get<TransactionComment>(keys);
        }

        public IEnumerable<TransactionComment> SelectByTransactionHash(string txid)
        {
            var keys = CommentBook.Where(x => x.Contains(txid)).Select(x => GetKey(UserSetting.AccountBook, x));
            return UserDomain.Get<TransactionComment>(keys);
        }

        public void Save(TransactionComment comment)
        {
            var key = GetKey(UserTables.TxComment, $"{comment.TransactionHash}_{comment.OutputIndex}");
            if (!CommentBook.Contains(key))
            {
                CommentBook.Add(key);
                UserDomain.Put(key, comment);
            }
        }

        #region Comment
        internal IEnumerable<string> LoadCommentBook()
        {
            return UserDomain.Get<IEnumerable<string>>(UserSetting.TxCommentBook)??new List<string>();
        }
        
        internal void UpdateCommentBook(IEnumerable<string> comments)
        {
            UserDomain.Put(UserSetting.TxCommentBook, comments);
        }
        #endregion
    }
}