namespace OmniCoin.DTO.Explorer
{
    public class InputOM
    {
        public long Id { get; set; }
        public string AccountId { get; set; }
        public string UnlockScript { get; set; }

        public decimal Amount { get; set; }

        public string OutputTransactionHash { get; set; }
    }
}
