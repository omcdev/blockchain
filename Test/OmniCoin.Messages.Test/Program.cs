using System;

namespace OmniCoin.Messages.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Test :");

            Console.WriteLine(TransactionMsgTest.Test()? "TransactionMsgTest Succeed ": "TransactionMsgTest Failed ");

            Console.WriteLine("End Test ,Click 'Enter' to exist");
            Console.ReadLine();
        }
    }
}
