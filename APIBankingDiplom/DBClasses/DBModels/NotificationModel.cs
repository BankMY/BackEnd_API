using System.ComponentModel.DataAnnotations;

namespace APIBankingDiplom.DBClasses.DBModels
{
    public class NotificationModel
    {
        public int Id { get; set; }
        [StringLength(32)]
        public string Type { get; set; } = string.Empty;
        [StringLength(512)]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime TimeSent { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public UserModel? User { get; set; }
    }
}
