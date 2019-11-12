using OmniCoin.Framework;
using System.Collections.Generic;

namespace OmniCoin.MiningPool.Data
{
    public class BaseDac<T> : IBaseDac<T> where T:new()
    {
        public string CacheConnectionString
        {
            get
            {
                return GlobalParameters.IsTestnet ? Resource.TestnetConnectionString : Resource.MainnetConnectionString;
            }
        }

        public void Delete(long Id, string deleteSql)
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(CacheConnectionString))
            {
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(deleteSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public T Detail(long Id, string detailSql)
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(CacheConnectionString))
            {
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(detailSql, conn))
                {
                    object result = cmd.ExecuteScalar();
                    return (T)result;
                }
            }
        }

        public void Insert(T entity, string insertSql)
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(CacheConnectionString))
            {
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(insertSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<T> Select(string selectSql)
        {
            List<T> list = new List<T>();
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(CacheConnectionString))
            {
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(selectSql, conn))
                {
                    System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader();
                    System.Data.DataTable dt = reader.GetSchemaTable();
                    System.Type type = typeof(T); // 获得此模型的类型
                    string tempName = "";
                    foreach (System.Data.DataRow dr in dt.Rows)
                    {
                        T t = new T();
                        System.Reflection.PropertyInfo[] propertys = t.GetType().GetProperties();// 获得此模型的公共属性
                        foreach (System.Reflection.PropertyInfo pi in propertys)
                        {
                            tempName = pi.Name;
                            if (dt.Columns.Contains(tempName))
                            {
                                if (!pi.CanWrite) continue;
                                object value = dr[tempName];
                                if (value != System.DBNull.Value)
                                {
                                    pi.SetValue(t, value, null);
                                }
                            }
                        }
                        list.Add(t);
                    }
                    return list;
                }
            }
        }

        public void Update(T entity, string updateSql)
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(CacheConnectionString))
            {
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(updateSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
