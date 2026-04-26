using System.Collections.Generic;

namespace Vyuka.Services
{
    public class OfferEmailBuilder
    {
        private readonly ITemplateService _templateService;

        public OfferEmailBuilder(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        // ⭐ Nová verze – přijímá Dictionary (nutné pro náhled s QR)
        public string BuildOffer(Dictionary<string, string> values)
        {
            return _templateService.RenderOfferTemplate(values);
        }

        // ⭐ Původní verze – stále funkční, ale už ji nepoužíváme
        public string BuildOffer(
            string parentName,
            string studentName,
            string customText = ""
        )
        {
            var values = new Dictionary<string, string>
            {
                { "ParentName", parentName },
                { "StudentName", studentName },
                { "CustomText", customText }
            };

            return _templateService.RenderOfferTemplate(values);
        }
    }
}
