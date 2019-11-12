


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Update
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (args[0].ToLower() == "-testnet")
                    Framework.GlobalParameters.IsTestnet = true;
            }
            catch
            {
                Framework.GlobalParameters.IsTestnet = false;
            }
            StartUp.Start();
            Console.WriteLine("转换用户数据成功");
        }
    }
}
