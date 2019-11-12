namespace OmniCoin.Entities.Explorer
{
    public class OutputAccountDataEx
    {
        public string ReceiverId { get; set; }

        public long UserAmount { get; set; }

        public long Amount { get; set; }

        public long Timestamp { get; set; }
    }

    public class TotalAmountDataEx
    {
        public string TotalAmount { get; set; }

    }
}
