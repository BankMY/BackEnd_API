using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class UserModel
    {
        public int Id { get; set; }
        [StringLength(128)]
        public string Login { get; set; } = string.Empty;
        [StringLength(128)]
        public string PasswordHash { get; set; } = string.Empty;
        [StringLength(128)]
        public string Email { get; set; } = string.Empty;
        [StringLength(128)]
        public string PhoneNumber { get; set; } = string.Empty;
        [StringLength(72)]
        public string PIN { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; } = false;
        [StringLength(6)]
        public string RecoveryCode { get; set; } = string.Empty;
        [StringLength(128)]
        public string AccessTokenHash { get; set; } = string.Empty;
        public DateTime AccessTokenExpire { get; set; } = DateTime.MinValue;
        public DateTime LastLoginTime { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}