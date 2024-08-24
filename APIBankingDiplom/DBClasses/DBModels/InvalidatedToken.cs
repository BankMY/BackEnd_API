using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class InvalidatedToken
    {
        public int Id { get; set; }
        [StringLength(128)]
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }
    }
}
