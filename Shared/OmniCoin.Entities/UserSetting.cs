


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class UserSetting
    {
        public int Id;
        public UserSettingType Type;
        public string Value;
    }

    public enum UserSettingType
    {
        DefaultAccount = 0,
        EnableAutoAccount = 1
    }
}