


using OmniCoin.Update.SqliteDb;
using System;
using System.IO;
using System.Linq;

namespace OmniCoin.Update
{
    public class StartUp
    {
        public static void Start()
        {
            if (Directory.Exists(DbDomains.TempFile))
            {
                Directory.CreateDirectory(DbDomains.TempFile);
            }

            if (!(!File.Exists(DbDomains.UserFile) && File.Exists(DbDomains.SqliteFile)))
                return;

            DbDomains.Init();

            SqlDac sqlDac = new SqlDac();

            var accountBook = sqlDac.SelectAccountBook();
            Db.AccountDac.Default.Insert(accountBook);

            var addressBook = sqlDac.SelectAddressBook();
            Db.AddressBookDac.Default.InsertOrUpdate(addressBook);

            var setting = sqlDac.GetSetting();
            Db.SettingDac.Default.SetAppSetting(setting);

            var newSetting = Db.SettingDac.Default.GetAppSetting();
            Console.WriteLine($"Accounts Count{Db.AccountDac.Default.SelectAll().Count()}/{accountBook.Count}");
            Console.WriteLine($"AddressBook Count{Db.AddressBookDac.Default.SelectAll().Count()}/{addressBook.Count}");
            Console.WriteLine($"Setting.Encrypt {newSetting.Encrypt}/{setting.Encrypt}");
            Console.WriteLine($"Setting.FeePerKB {newSetting.FeePerKB}/{setting.FeePerKB}");
            Console.WriteLine($"Setting.PassCiphertext {newSetting.PassCiphertext}/{setting.PassCiphertext}");

            DbDomains.Close();
        }
    }
}
