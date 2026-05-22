using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QRCoder;

namespace Vyuka.Services
{
    public class QrCodeGeneratorService
    {
        public byte[] GeneratePaymentQr(decimal amount, string message)
        {
            string iban = "CZ0330300000002349690015";

            string amountString = amount.ToString("0.00", CultureInfo.InvariantCulture);
            string msg = RemoveDiacritics(message);

            string payload =
                $"SPD*1.0*ACC:{iban}*AM:{amountString}*CC:CZK*MSG:{msg}";

            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qr = new PngByteQRCode(data);

            return qr.GetGraphic(10);
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            string cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

            cleaned = Regex.Replace(cleaned, @"[^A-Za-z0-9 \-\.]", " ");

            return cleaned.Trim();
        }
    }
}
