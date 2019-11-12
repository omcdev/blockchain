

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using OmniCoin.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OmniCoin.Business
{
    public class WalletComponent
    {
        const string encryptExtensionName = ".omdatx";
        const string noEncryptExtensionName = ".omdat";

        public void BackupWallet(string filePath, string salt)
        {
            string extensionName = Path.GetExtension(filePath).ToLower();
            if (string.IsNullOrWhiteSpace(salt))
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + noEncryptExtensionName);
            }
            else
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + encryptExtensionName);
            }

            WalletBackup backup = null;
            try
            {
                backup = new WalletBackup()
                {
                    AccountList = AccountDac.Default.SelectAll()?.ToList(),
                    AddressBookItemList = AddressBookDac.Default.SelectAll()?.ToList(),
                    SettingList = new List<Setting>() { SettingDac.Default.Setting },
                    TransactionCommentList = TransactionCommentDac.Default.SelectAll()?.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DB.LOAD_DATA_ERROR, ex);
            }

            try
            {
                if (backup != null)
                {
                    if (extensionName == noEncryptExtensionName)
                    {
                        SaveFile(backup, filePath);
                    }
                    else
                    {
                        SaveFile(backup, filePath, salt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DATA_SAVE_TO_FILE_ERROR, ex);
            }
        }

        public bool RestoreWalletBackup(string filePath, string salt)
        {
            bool result = false;

            string extensionName = Path.GetExtension(filePath).ToLower();

            var setting = new SettingComponent().GetSetting();
            if (setting.Encrypt && string.IsNullOrEmpty(salt))
                throw new CommonException(ErrorCode.Engine.Wallet.CHECK_PASSWORD_ERROR);

            if (setting.Encrypt && (extensionName != encryptExtensionName))
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.EXTENSION_NAME_NOT_SUPPORT);
            }
            else if (!setting.Encrypt && extensionName != noEncryptExtensionName)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.EXTENSION_NAME_NOT_SUPPORT);
            }

            //Load backup file data.
            WalletBackup backup = null;
            if (extensionName == noEncryptExtensionName)
            {
                try
                {
                    backup = LoadFile<WalletBackup>(filePath);
                }
                catch
                {
                    var backup1 = LoadFile<WalletBackup1>(filePath);
                    backup = backup1.ToBackup();
                }
            }
            else
            {
                try
                {
                    backup = LoadFile<WalletBackup>(filePath, salt);
                }
                catch
                {
                    var backup1 = LoadFile<WalletBackup1>(filePath, salt);
                    backup = backup1.ToBackup();
                }
                backup.AccountList.RemoveAll(x => string.IsNullOrEmpty(x.PrivateKey));
            }

            
            //Create SQL Query Script and run
            try
            {
                if (backup != null)
                {
                    WalletBackupDac dac = WalletBackupDac.Default;
                    dac.Restore(backup);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DB.EXECUTE_SQL_ERROR, ex);
            }
            return result;
        }

        public bool EncryptWallet(string salt)
        {
            bool result = false;
            var aclist = AccountDac.Default.SelectAll();
            foreach (var item in aclist)
            {
                item.PrivateKey = AES128.Encrypt(item.PrivateKey, salt);
            }
            try
            {
                AccountDac.Default.UpdatePrivateKeyAr(aclist);

                SettingComponent sComponent = new SettingComponent();
                var setting = sComponent.GetSetting();
                setting.Encrypt = true;
                setting.PassCiphertext = MD5Helper.EncryptTo32(salt);
                sComponent.SaveSetting(setting);
                result = true;
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DB.EXECUTE_SQL_ERROR, ex);
            }

            return result;
        }

        public bool CheckPassword(string password)
        {
            SettingComponent sComponent = new SettingComponent();
            var setting = sComponent.GetSetting();
            if (MD5Helper.EncryptTo32(password) == setting.PassCiphertext)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ChangePassword(string oldPassword, string newPassword)
        {
            SettingComponent sComponent = new SettingComponent();
            var setting = sComponent.GetSetting();
            if (MD5Helper.EncryptTo32(oldPassword) == setting.PassCiphertext)
            {
                var aclist = AccountDac.Default.SelectAll();
                foreach (var item in aclist)
                {
                    item.PrivateKey = AES128.Decrypt(item.PrivateKey, oldPassword);
                    item.PrivateKey = AES128.Encrypt(item.PrivateKey, newPassword);
                }
                try
                {
                    AccountDac.Default.UpdatePrivateKeyAr(aclist);
                    setting = sComponent.GetSetting();
                    setting.Encrypt = true;
                    setting.PassCiphertext = MD5Helper.EncryptTo32(newPassword);
                    sComponent.SaveSetting(setting);
                }
                catch (Exception ex)
                {
                    throw new CommonException(ErrorCode.Engine.Wallet.DB.EXECUTE_SQL_ERROR, ex);
                }
            }
            else
            {
                throw new CommonException(ErrorCode.Engine.Wallet.CHECK_PASSWORD_ERROR);
            }
        }

        #region IO oprerations
        /// <summary>
        /// Object use json formatting, and save to file system
        /// </summary>
        /// <typeparam name="T">any type</typeparam>
        /// <param name="obj">any type entity</param>
        /// <param name="filePath">saved path</param>
        private static void SaveFile<T>(T obj, string filePath)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            FileHelper.StringSaveFile(jsonString, filePath);
        }

        /// <summary>
        /// [encrypt]Object use json formatting, and save to file system
        /// </summary>
        /// <typeparam name="T">any type</typeparam>
        /// <param name="obj">any type entity</param>
        /// <param name="filePath">saved path</param>
        /// <param name="salt">encrypt salt</param>
        private static void SaveFile<T>(T obj, string filePath, string salt)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            string encryptString = jsonString;

            if (!string.IsNullOrEmpty(salt))
            {
                encryptString = AES128.Encrypt(jsonString, salt);
            }
            FileHelper.StringSaveFile(encryptString, filePath);
        }


        /// <summary>
        /// Read Object from File
        /// </summary>
        /// <param name="filePath">file location path</param>
        /// <returns>Load entity.</returns>
        private static T LoadFile<T>(string filePath)
        {
            if (!File.Exists(filePath)) { throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_NOT_FOUND); }
            string fileString = string.Empty;
            try
            {
                fileString = FileHelper.LoadFileString(filePath);
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_DATA_INVALID, ex);
            }
            return JsonConvert.DeserializeObject<T>(fileString);
        }

        /// <summary>
        /// Read Object from File
        /// </summary>
        /// <param name="filePath">file location path</param>
        /// <param name="salt"></param>
        /// <returns>dencrypt entity.</returns>
        private static T LoadFile<T>(string filePath, string salt)
        {
            if (!File.Exists(filePath)) { throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_NOT_FOUND); }
            string fileString = string.Empty;
            try
            {
                fileString = FileHelper.LoadFileString(filePath);
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_DATA_INVALID, ex);
            }

            try
            {
                fileString = AES128.Decrypt(fileString, salt);
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DECRYPT_DATA_ERROR, ex);
            }
            return JsonConvert.DeserializeObject<T>(fileString);
        }
        #endregion
    }
}
