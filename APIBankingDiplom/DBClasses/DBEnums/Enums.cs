namespace APIBankingDiplom.DBClasses.Enums
{
    public class BankingEnums
    {
        private BankingEnums() { }
        public enum CardType
        {
            Debit = 0,
            Credit = 1
        }
        public enum CardStatus
        {
            Active = 0,
            Limited = 1,
            Blocked = 2,
            Expired = 3,
        }
        public enum Currency
        {
            UAH = 0,
            USD = 1,
            PLN = 2,
        }
        // How hideous!
        public static string GetCurrencyCode(Currency currency)
        {
            switch(currency)
            {
                case Currency.UAH:
                    return "UAH";
                case Currency.USD:
                    return "USD";
                default:
                    return "PLN";
                    
            }
        }
    }
}
