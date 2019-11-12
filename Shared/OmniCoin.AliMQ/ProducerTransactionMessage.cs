using ons;
using System;

namespace OmniCoin.AliMQ
{
    /// <summary>
    /// 发送事务消息
    /// </summary>
    public class ProducerTransactionMessage<T> : ProducerMessage<T> where T : class
    {
        private TransactionProducer transactionProducer;
        private LocalTransactionChecker myChecker;

        public ProducerTransactionMessage()
        {
            Initialize("PayReward", "PID_MinerReward_Producer");
        }

        public void InitializeOrderProducer()
        {
            myChecker = new MyLocalTransactionChecker();
            transactionProducer = ONSFactory.getInstance().createTransactionProducer(factoryInfo, myChecker);
            transactionProducer.start();
        }

        public void SendTransactionMessage(string tag, string byteBody, string shardingKey)
        {
            try
            {
                Message msg = new Message(factoryInfo.getPublishTopics(), tag, byteBody);
                msg.setKey(shardingKey);
                LocalTransactionExecuter myExecuter = new MyLocalTransactionExecuter();
                //事务检查
                myChecker.check(msg);
                SendResultONS sendResult = transactionProducer.send(msg, myExecuter);
                //事务执行
                myExecuter.execute(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nexception of sendmsg:{0}");
            }
        }
    }

    public class MyLocalTransactionChecker : LocalTransactionChecker
    {
        public MyLocalTransactionChecker()
        {
        }
        ~MyLocalTransactionChecker()
        {
        }
        public override TransactionStatus check(Message value)
        {
            bool isCommit = true;
            TransactionStatus transactionStatus = TransactionStatus.Unknow;
            try
            {
                Console.WriteLine($"execute topic: {value.getTopic()}, tag:{value.getTag()}, key:{value.getKey()}, msgId:{value.getMsgID()},msgbody:{value.getBody()}, userProperty:{value.getUserProperties("VincentNoUser")}");
                string msgId = value.getMsgID();
                // do something

                if (isCommit)
                {
                    // 本地事务成功、提交消息
                    transactionStatus = TransactionStatus.CommitTransaction;
                }
                else
                {
                    // 本地事务失败、回滚消息
                    transactionStatus = TransactionStatus.RollbackTransaction;
                }
            }
            catch (Exception ex)
            {
                //exception handle
                transactionStatus = TransactionStatus.RollbackTransaction;
            }
            return transactionStatus;
        }
    }

    public class MyLocalTransactionExecuter : LocalTransactionExecuter
    {
        public MyLocalTransactionExecuter()
        {
        }

        ~MyLocalTransactionExecuter()
        {
        }

        public override TransactionStatus execute(Message value)
        {
            bool isCommit = true;
            TransactionStatus transactionStatus = TransactionStatus.Unknow;
            try
            {
                Console.WriteLine($"execute topic: {value.getTopic()}, tag:{value.getTag()}, key:{value.getKey()}, msgId:{value.getMsgID()},msgbody:{value.getBody()}, userProperty:{value.getUserProperties("VincentNoUser")}");
                string msgId = value.getMsgID();
                //do something
                
                if (isCommit)
                {
                    // 本地事务成功则提交消息
                    transactionStatus = TransactionStatus.CommitTransaction;
                }
                else
                {
                    // 本地事务失败则回滚消息
                    transactionStatus = TransactionStatus.RollbackTransaction;
                }
            }
            catch (Exception ex)
            {
                //exception handle
                transactionStatus = TransactionStatus.RollbackTransaction;
            }
            return transactionStatus;
        }
    }
}
