namespace OmniCoin.DTO.Explorer
{
    public class OutputOM
    {
        public long Id { get; set; }

        public string ReceiverId { get; set; }

        public decimal Amount { get; set; }

        public string LockScript { get; set; }

        public decimal Affirm { get; set; }

        public bool Spent { get; set; }
    }
}
