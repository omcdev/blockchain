// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FiiiChain.Framework;
using FiiiChain.IModules;
using FiiiChain.TempData;
using LightDB;
using Newtonsoft.Json;

namespace FiiiChain.DataAgent
{
    public class CacheAccess
    {
        private static IHotDataAccess _default;
        public static IHotDataAccess Default
        {
            get
            {
                if (_default == null)
                {
                    _default = DbAccessHelper.GetDataAccessInstance();
                }
                return _default;
            }
        }
    }
}