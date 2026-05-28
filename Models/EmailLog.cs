namespace Vyuka.Models
{
    public class EmailLog
    {
        public int Id { get; set; }
        public DateTime SentAt { get; set; }
        public string Recipient { get; set; } = "";
        public string Subject { get; set; } = "";
        public string EmailType { get; set; } = "";
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string Html { get; set; } = "";
        public bool HasQrCode { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; } = 1;
        public string SentBy { get; set; } = "system";
    }
}
