namespace OmniCoin.RabbitMQ
{
    public class RabbitMqSetting
    {        
        public static readonly string EXCHANGENAME = "direct";
        public static string CONNECTIONSTRING = "amqp://fff:fffpwd@192.168.31.25:55672/";
        
    }

    public class RabbitMqName
    {
        public const string StartMining = "StartMining";
        public const string StopMining = "StopMining";
        public const string Login = "Login";
        public const string ForgetBlock = "ForgetBlock";
        public const string HeartPool = "HeartPool";
        public const string Default = "Default";
        public const string PosInviteReward = "PosInviteReward";
    }
}
