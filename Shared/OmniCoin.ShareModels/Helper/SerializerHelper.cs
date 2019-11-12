


using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OmniCoin.ShareModels.Helper
{
    public class SerializerHelper
    {
        public static byte[] ToBytes<T>(T model)
        {
            byte[] buff;
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter iFormatter = new BinaryFormatter();
                iFormatter.Serialize(ms, model);
                buff = ms.GetBuffer();
            }
            return buff;
        }

        public static T ToModel<T>(byte[] Bytes)
        {
            T result = default(T);
            using (MemoryStream ms = new MemoryStream(Bytes))
            {
                IFormatter iFormatter = new BinaryFormatter();
                var obj = iFormatter.Deserialize(ms);
                if (obj is T)
                    result = (T)obj;
            }
            return result;
        }
    }
}
