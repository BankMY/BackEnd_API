using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class LogModel
    {
        public int Id { get; set; }
        [StringLength(32)]
        public string ActionType { get; set; } = string.Empty;
        [StringLength(255)]
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public UserModel? User { get; set; }
    }
}
