namespace OmniCoin.DTO.Explorer
{
    public class BlockOM
    {
        public long Id { get; set; }

        public string Hash { get; set; }

        public int Version { get; set; }

        public long Height { get; set; }

        public string PreviousBlockHash { get; set; }

        public long Bits { get; set; }

        public long Nonce { get; set; }

        public long Timestamp { get; set; }

        public string NextBlockHash { get; set; }

        public long TotalAmount { get; set; }

        public long TotalFee { get; set; }

        public string GeneratorId { get; set; }

        public bool IsDiscarded { get; set; }

        public bool IsVerified { get; set; }
    }
}
