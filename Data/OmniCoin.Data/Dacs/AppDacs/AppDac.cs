


using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    /// <summary>
    /// 程序运行的一些基本数据
    /// </summary>
    public class AppDac : AppDbBase<AppDac>
    {

        public const string AppVersion = "0";

        private string DefaultAccount = null;
        /// <summary>
        /// 确认过的余额
        /// </summary>
        public long ConfirmedAmount = 0;
        /// <summary>
        /// 未确认的余额
        /// </summary>
        public long UnConfirmedAmount = 0;

        public AppDac()
        {
            DefaultAccount = GetDefaultAccount();
        }

        #region DefaultAccount
        public void SetDefaultAccount(string id)
        {
            DbDomains.AppDomain.Put(AppSetting.DefaultAccount, id);
            DefaultAccount = id;
        }

        public string GetDefaultAccount()
        {
            return DbDomains.AppDomain.Get(AppSetting.DefaultAccount);
        }
        #endregion

        #region Version

        public string GetVersion()
        {
            return AppDomain.Get(AppSetting.Version);
        }

        public void UpdateVersion()
        {
            AppDomain.Put(AppSetting.Version, AppVersion);
        }
        #endregion
    }
}