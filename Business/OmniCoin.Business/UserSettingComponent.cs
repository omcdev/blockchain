


using OmniCoin.Data.Dacs;

namespace OmniCoin.Business
{
    public class UserSettingComponent
    {
        public string GetDefaultAccount()
        {
            return AppDac.Default.GetDefaultAccount();
        }

        public void SetDefaultAccount(string id)
        {
            AppDac.Default.SetDefaultAccount(id);
        }

        public void SetEnableAutoAccount(bool enable)
        {
            //var dac = UserSettingDac.Default;
            //dac.Upsert(new Entities.UserSetting { Type = Entities.UserSettingType.EnableAutoAccount, Value = enable.ToString() });
        }
    }
}
