


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OmniCoin.Messages
{
    public class AddrMsg : BasePayload
    {
        public int Count
        {
            get { return this.AddressList.Count; }
        }
        public List<AddressInfo> AddressList { get; set; }

        public AddrMsg()
        {
            this.AddressList = new List<AddressInfo>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var buffer = new byte[bytes.Length - index];
            Array.Copy(bytes, 0, buffer, 0, buffer.Length);

            var textData = Encoding.UTF8.GetString(buffer);
            var addressArray = textData.Split(';');

            foreach(var address in addressArray)
            {
                var items = address.Split(',');

                if(items.Length == 3)
                {
                    int port;
                    
                    if(int.TryParse(items[1], out port))
                    {
                        this.AddressList.Add(new AddressInfo() {
                            Ip = items[0],
                            Port = port,
                            Identity = items[2]
                        });
                    }
                }
            }
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();

            for (int i = 0; i < this.AddressList.Count; i++)
            {
                var item = this.AddressList[i];
                data.AddRange(Encoding.UTF8.GetBytes(item.Ip + "," + item.Port + "," + item.Identity));

                if(i < this.AddressList.Count - 1)
                {
                    data.Add(Convert.ToByte(';'));
                }
            }

            return data.ToArray();
        }

        public class AddressInfo
        {
            public string Ip { get; set; }
            public int Port { get; set; }
            public string Identity { get; set; }
        }
    }
}
