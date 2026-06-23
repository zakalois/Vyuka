namespace Vyuka.Services
{
    public class EmailAttachment
    {
        // ⭐ Embedded obrázek (QR, logo)
        public string? ContentId { get; set; }
        public string? FilePath { get; set; }

        // ⭐ Klasická příloha z paměti (IFormFile)
        public string? FileName { get; set; }
        public byte[]? Content { get; set; }
        public string? ContentType { get; set; }

        // ⭐ Prázdný konstruktor – pro přílohy z paměti
        public EmailAttachment() { }

        // ⭐ Konstruktor pro embedded obrázky (QR)
        public EmailAttachment(string contentId, string filePath)
        {
            ContentId = contentId;
            FilePath = filePath;
        }
    }
}
