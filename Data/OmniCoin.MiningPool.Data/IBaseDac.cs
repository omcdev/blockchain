using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Data
{
    public interface IBaseDac<T>
    {
        void Insert(T entity, string insertSql);

        void Update(T entity, string updateSql);

        void Delete(long Id, string deleteSql);

        List<T> Select(string selectSql);

        T Detail(long Id, string detailSql);

    }
}
