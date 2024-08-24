using APIBankingDiplom.DBClasses.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class CardModel
    {
        public int Id { get; set; }
        [StringLength(19)]
        public string CardNumber { get; set; } = string.Empty;
        public BankingEnums.CardType CardType { get; set; } = BankingEnums.CardType.Debit;
        public DateTime ExpireDate { get; set; } = DateTime.Now.AddYears(6).AddMonths(6);
        [StringLength(3)]
        public string CVV { get; set; } = string.Empty;
        public BankingEnums.CardStatus Status { get; set; } = BankingEnums.CardStatus.Active;
        public decimal? WithdrawnToday { get; set; }
        public decimal? WithdrawalLimit { get; set; }
        public decimal? TransactedToday { get; set; }
        public decimal? TransactionLimit { get; set; }
        public DateTime? LastTransactionDate { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
        [JsonIgnore]
        public UserModel? User { get; set; }
    }

}
