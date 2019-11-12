


using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    [Serializable]
    public class TransactionMsg : BasePayload
    {
        public TransactionMsg()
        {
            this.Version = Int32.Parse(Resource.MsgVersion);
            this.Inputs = new List<InputMsg>();
            this.Outputs = new List<OutputMsg>();
            this.Hash = Base16.Encode(HashHelper.EmptyHash());
        }

        public int Version { get; set; }
        public string Hash { get; set; }
        public long Timestamp { get; set; }
        public long Locktime { get; set; }
        public long ExpiredTime { get; set; }

        public long DepositTime { get; set; }

        public int InputCount
        {
            get
            {
                return this.Inputs.Count;
            }
        }
        public int OutputCount
        {
            get
            {
                return this.Outputs.Count;
            }
        }
        public List<InputMsg> Inputs { get; set; }
        public List<OutputMsg> Outputs { get; set; }

        public string GetHash()
        {
            var bytes = new List<byte>();
            var timestampBytes = new byte[8];
            var locktimeBytes = new byte[8];
            var expiredTimeBytes = new byte[8];
            var depositTimeBytes = new byte[8];
            var totalInputBytes = new byte[4];
            var totalOutputBytes = new byte[4];

            timestampBytes = BitConverter.GetBytes(Timestamp);
            locktimeBytes = BitConverter.GetBytes(Locktime);
            expiredTimeBytes = BitConverter.GetBytes(ExpiredTime);
            depositTimeBytes = BitConverter.GetBytes(DepositTime);
            totalInputBytes = BitConverter.GetBytes(InputCount);
            totalOutputBytes = BitConverter.GetBytes(OutputCount);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
                Array.Reverse(locktimeBytes);
                Array.Reverse(expiredTimeBytes);
                Array.Reverse(depositTimeBytes);
                Array.Reverse(totalInputBytes);
                Array.Reverse(totalOutputBytes);
            }

            bytes.AddRange(timestampBytes);
            bytes.AddRange(locktimeBytes);
            bytes.AddRange(expiredTimeBytes);
            bytes.AddRange(depositTimeBytes);
            bytes.AddRange(totalInputBytes);

            foreach (var inputMsg in this.Inputs)
            {
                bytes.AddRange(inputMsg.Serialize());
            }

            bytes.AddRange(totalOutputBytes);

            foreach (var outputMsg in this.Outputs)
            {
                bytes.AddRange(outputMsg.Serialize());
            }

            return Base16.Encode(
                HashHelper.Hash(
                    bytes.ToArray()
            ));
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var versionBytes = new byte[4];
            var hashBytes = new byte[32];
            var timestampBytes = new byte[8];
            var locktimeBytes = new byte[8];
            var expiredTimeBytes = new byte[8];
            var depositTimeBytes = new byte[8];
            var totalInputBytes = new byte[4];
            var totalOutputBytes = new byte[4];

            Array.Copy(bytes, index, versionBytes, 0, versionBytes.Length);
            index += versionBytes.Length;
            Array.Copy(bytes, index, hashBytes, 0, hashBytes.Length);
            index += hashBytes.Length;
            Array.Copy(bytes, index, timestampBytes, 0, timestampBytes.Length);
            index += timestampBytes.Length;
            Array.Copy(bytes, index, locktimeBytes, 0, locktimeBytes.Length);
            index += locktimeBytes.Length;
            Array.Copy(bytes, index, expiredTimeBytes, 0, expiredTimeBytes.Length);
            index += expiredTimeBytes.Length;
            Array.Copy(bytes, index, depositTimeBytes, 0, depositTimeBytes.Length);
            index += depositTimeBytes.Length;
            Array.Copy(bytes, index, totalInputBytes, 0, totalInputBytes.Length);
            index += totalInputBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(versionBytes);
                Array.Reverse(timestampBytes);
                Array.Reverse(locktimeBytes);
                Array.Reverse(expiredTimeBytes);
                Array.Reverse(depositTimeBytes);
                Array.Reverse(totalInputBytes);
            }

            this.Version = BitConverter.ToInt32(versionBytes, 0);
            this.Hash = Base16.Encode(hashBytes);
            this.Timestamp = BitConverter.ToInt64(timestampBytes, 0);
            this.Locktime = BitConverter.ToInt64(locktimeBytes, 0);
            this.ExpiredTime = BitConverter.ToInt64(expiredTimeBytes, 0);
            this.DepositTime = BitConverter.ToInt64(depositTimeBytes, 0);
            var totalInput = BitConverter.ToInt32(totalInputBytes, 0);

            var inputIndex = 0;
            while (inputIndex < totalInput)
            {
                var input = new InputMsg();
                input.Deserialize(bytes, ref index);
                this.Inputs.Add(input);

                inputIndex++;
            }

            Array.Copy(bytes, index, totalOutputBytes, 0, totalOutputBytes.Length);
            index += totalOutputBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(totalOutputBytes);
            }

            var totalOutput = BitConverter.ToInt32(totalOutputBytes, 0);

            var outputIndex = 0;
            while (outputIndex < totalOutput)
            {
                var output = new OutputMsg();
                output.Deserialize(bytes, ref index);
                this.Outputs.Add(output);

                outputIndex++;
            }
        }

        public override byte[] Serialize()
        {
            var bytes = new List<byte>();
            var versionBytes = new byte[4];
            var hashBytes = new byte[32];
            var timestampBytes = new byte[8];
            var locktimeBytes = new byte[8];
            var expiredTimeBytes = new byte[8];
            var depositTimeBytes = new byte[8];
            var totalInputBytes = new byte[4];
            var totalOutputBytes = new byte[4];

            versionBytes = BitConverter.GetBytes(Version);
            hashBytes = Base16.Decode(Hash);
            timestampBytes = BitConverter.GetBytes(Timestamp);
            locktimeBytes = BitConverter.GetBytes(Locktime);
            expiredTimeBytes = BitConverter.GetBytes(ExpiredTime);
            depositTimeBytes = BitConverter.GetBytes(DepositTime);
            totalInputBytes = BitConverter.GetBytes(InputCount);
            totalOutputBytes = BitConverter.GetBytes(OutputCount);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(versionBytes);
                Array.Reverse(timestampBytes);
                Array.Reverse(locktimeBytes);
                Array.Reverse(expiredTimeBytes);
                Array.Reverse(depositTimeBytes);
                Array.Reverse(totalInputBytes);
                Array.Reverse(totalOutputBytes);
            }

            bytes.AddRange(versionBytes);
            bytes.AddRange(hashBytes);
            bytes.AddRange(timestampBytes);
            bytes.AddRange(locktimeBytes);
            bytes.AddRange(expiredTimeBytes);
            bytes.AddRange(depositTimeBytes);
            bytes.AddRange(totalInputBytes);

            foreach (var inputMsg in this.Inputs)
            {
                bytes.AddRange(inputMsg.Serialize());
            }

            bytes.AddRange(totalOutputBytes);

            foreach (var outputMsg in this.Outputs)
            {
                bytes.AddRange(outputMsg.Serialize());
            }

            return bytes.ToArray();
        }

        public int Size
        {
            get
            {
                return this.Serialize().Length;
            }
        }
    }
}
