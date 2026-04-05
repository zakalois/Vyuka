namespace Vyuka.Models
{
    public class Payment
    {
        public int Id { get; set; }

        // Student, kterému platba patří
        public int StudentId { get; set; }
        public Student Student { get; set; }

        // Datum úhrady
        public DateTime Date { get; set; }

        // Kolik zaplatil (v Kč)
        public decimal Amount { get; set; }

        // Kolik hodin si tím předplatil
        public decimal HoursPurchased { get; set; }

        // ⭐ Cena za hodinu (volitelné)
        public decimal? PricePerHour { get; set; }

        // ⭐ Poznámka k platbě (volitelné)
        public string? Note { get; set; }

        // ⭐ Způsob úhrady (hotově, převodem…)
        public string? Method { get; set; }
    }
}