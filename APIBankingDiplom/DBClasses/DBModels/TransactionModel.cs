using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class TransactionModel
    {
        public int Id { get; set; }
        [StringLength(32)]
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; } = decimal.Zero;
        [StringLength(3)]
        public string Currency { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        public int SenderBalanceId { get; set; }
        public CardBalanceModel? SenderBalance { get; set; }
        public int ReceiverBalanceId { get; set; }
        public CardBalanceModel? ReceiverBalance { get; set; }
    }
}
