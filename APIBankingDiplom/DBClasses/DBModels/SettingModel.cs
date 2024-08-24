using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class SettingModel
    {
        public int Id { get; set; }
        [StringLength(2)]
        public string Language { get; set; } = string.Empty;
        [StringLength(32)]
        public string Theme { get; set; } = string.Empty;

        public int UserId { get; set; }
        public UserModel? User { get; set; }
    }
}
