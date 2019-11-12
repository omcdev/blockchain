

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Framework;
using System;

namespace OmniCoin.Node
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            SetNetType(args);
            SetIsLoadTransRecord(args);
            SetIsExplorer(args);
            try
            {
                BlockchainJob.Initialize();
                BlockchainJob.Current.Start();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                var ex = e.ExceptionObject as Exception;
                LogHelper.Error(ex.Message, ex);
            }
        }

        private static void SetNetType(string[] args)
        {
            try
            {
                if (args[0].ToLower() == "-testnet")
                {
                    GlobalParameters.IsTestnet = true;
                    LogHelper.Info("OmniCoin Testnet Engine is Started.");
                }
                else
                {
                    GlobalParameters.IsTestnet = false;
                    LogHelper.Info("OmniCoin Engine is Started.");
                }
            }
            catch
            {
                GlobalParameters.IsTestnet = false;
                LogHelper.Info("OmniCoin Engine is Started.");
            }
        }

        private static void SetIsLoadTransRecord(string[] args)
        {
            try
            {
                if (args[1].ToLower() == "false")
                {
                    GlobalParameters.IsLoadTransRecord = false;
                }
                else
                {
                    GlobalParameters.IsLoadTransRecord = true;
                }
            }
            catch
            {
                GlobalParameters.IsLoadTransRecord = true;
            }
        }

        private static void SetIsExplorer(string[] args)
        {
            try
            {
                if (args[2].ToLower() == "e")
                    GlobalParameters.IsExplorer = true;
                else
                    GlobalParameters.IsExplorer = true;
            }
            catch
            {
                GlobalParameters.IsExplorer = false;
            }
        }
    }
}