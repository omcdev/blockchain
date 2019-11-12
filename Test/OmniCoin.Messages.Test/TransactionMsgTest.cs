using OmniCoin.Business;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using OmniCoin.Data.Dacs;
using OmniCoin.Business.ParamsModel;

namespace OmniCoin.Messages.Test
{
    public class TransactionMsgTest
    {
        public static bool Test()
        {
            //TransactionMsg txSrc = new TransactionMsg();
            //var totalSize = txSrc.Serialize().Length;
            //txSrc.DepositTime = Time.EpochTime;            
            //var output = new OutputMsg();
            //output.Amount = 100000000000;//1000
            //output.Index = 0;
            //output.LockScript = "OP_DUP OP_HASH160 D3DC376BE16DC54B9E1583A2D63F05FCCA7013B3 OP_EQUALVERIFY OP_CHECKSIG";
            //output.Size = output.LockScript.Length;
            //txSrc.Outputs.Add(output);
            //txSrc.Timestamp = Time.EpochTime;
            //txSrc.Hash = txSrc.GetHash();

            //var strSrc = Newtonsoft.Json.JsonConvert.SerializeObject(txSrc);
            //var tmpBytes = txSrc.Serialize();

            //var txTarget = new TransactionMsg();
            //int index = 0;
            //txTarget.Deserialize(tmpBytes, ref index);
            //var strTarget = Newtonsoft.Json.JsonConvert.SerializeObject(txTarget);

            //Console.WriteLine("strSrc:\n"+strSrc);
            //Console.WriteLine("strTarget:\n" + strTarget);

            //return strSrc == strTarget;

            

            //TransactionPoolItem it;
            //using (var stream = File.Open("E:\\775B5040505BBFE92A9072B56B06AF0B0D7EC7987D2A5CFC8CE5E85C99743989", FileMode.Open))
            //{
            //    BinaryFormatter b = new BinaryFormatter();
            //    var obj = b.Deserialize(stream);
            //    it = obj as TransactionPoolItem;
            //}
            //if(it != null)
            //{
            //    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject( it.Transaction));
            //}

            string s = File.ReadAllText("E:\\tx");
            var tx = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionMsg>(s);
            var calcTxHash = tx.GetHash();
            Console.WriteLine( calcTxHash == tx.Hash);

            long txFee;
            long totalOutput;
            long totalInput;
            var VerifyTransactionResult = new TransactionComponent().VerifyTransaction(tx, out txFee, out totalOutput, out totalInput);
            var totalFee = 0L;
            VerifyTransactionModel model = new VerifyTransactionModel();
            long fee;
            model.transaction = tx;

            var VerifyTransactionMsgResult = new TransactionComponent().VerifyTransactionMsg(model, out fee);

            return false;
        }
    }
}
