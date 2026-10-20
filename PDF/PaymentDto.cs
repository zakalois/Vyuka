using QuestPDF.Fluent;

namespace Vyuka.PDF
{
    public class PaymentDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public decimal HoursPurchased { get; set; }
        public string Note { get; set; }
    }
}
