using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class RefreshToken
    {
        public int Id { get; set; }
        [StringLength(128)]
        public string HashedToken { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }

        public int UserId { get; set; }
        public UserModel? User { get; set; }
    }
}
