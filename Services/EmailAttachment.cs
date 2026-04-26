namespace Vyuka.Services
{
    public class EmailAttachment
    {
        public string ContentId { get; }
        public string FilePath { get; }

        public EmailAttachment(string contentId, string filePath)
        {
            ContentId = contentId;
            FilePath = filePath;
        }
    }
}
