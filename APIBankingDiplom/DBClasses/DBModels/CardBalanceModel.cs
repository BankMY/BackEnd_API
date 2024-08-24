using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class CardBalanceModel
    {
        public int Id { get; set; }
        [StringLength(3)]
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; } = decimal.Zero;

        [JsonIgnore]
        public int CardId { get; set; }
        [JsonIgnore]
        public CardModel? Card { get; set; }
    }
}
