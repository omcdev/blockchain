// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace FiiiChain.DataAgent
{
    public class AsyncUdpEventArgs : EventArgs
    {
        /// <summary>  
        /// 提示信息  
        /// </summary>  
        public string _msg;

        /// <summary>  
        /// 客户端状态封装类  
        /// </summary>  
        public AsyncUdpState _state;

        /// <summary>  
        /// 是否已经处理过了  
        /// </summary>  
        public bool IsHandled { get; set; }

        public AsyncUdpEventArgs(string msg)
        {
            this._msg = msg;
            IsHandled = false;
        }
        public AsyncUdpEventArgs(AsyncUdpState state)
        {
            this._state = state;
            IsHandled = false;
        }
        public AsyncUdpEventArgs(string msg, AsyncUdpState state)
        {
            this._msg = msg;
            this._state = state;
            IsHandled = false;
        }
    }
}
