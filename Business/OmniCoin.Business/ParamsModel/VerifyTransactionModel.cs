


using OmniCoin.Entities;
using OmniCoin.Entities.ExtensionModels;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Business.ParamsModel
{
    public class VerifyTransactionModel
    {
        public TransactionMsg transaction;
        public BlockMsg block;
        public long localHeight;
    }
}
