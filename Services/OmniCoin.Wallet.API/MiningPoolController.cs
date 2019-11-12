

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.Wallet.API
{
    public class MiningPoolController : BaseRpcController
    {
        public IRpcMethodResult AddMiningPool(string name, string publicKey,string signature)
        {
            try
            {
                var miningPoolComponent = new MiningPoolComponent();
                if (miningPoolComponent.HasMiningPool(name, publicKey))
                    throw new CommonException(ErrorCode.Engine.Block.Verify.MINING_POOL_EXISTED);

                var miningMsg = new MiningMsg() { Name = name, PublicKey = publicKey, Signature = signature };
                var result = miningPoolComponent.AddMiningToPool(miningMsg);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAllMiningPool()
        {
            try
            {
                var miningPoolComponent = new MiningPoolComponent();
                var result = miningPoolComponent.GetAllMiningPools();
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
    }
}
