using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Vyuka.PDF;

namespace Vyuka.PDF
{
    public class PaymentsReportDocument : IDocument
    {
    public string StudentName { get; set; } = "";
    public List<PaymentDto> Payments { get; set; } = new();

    public DocumentMetadata GetMetadata() => new DocumentMetadata();
    public DocumentSettings GetSettings() => new DocumentSettings();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);

            page.Header().Text($"Přehled plateb – {StudentName}")
                .FontSize(20).SemiBold();

            page.Content().Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(80); // Datum
                        cols.ConstantColumn(80); // Částka
                        cols.ConstantColumn(80); // Hodiny
                        cols.RelativeColumn();   // Poznámka
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Datum").SemiBold();
                        header.Cell().Text("Částka").SemiBold();
                        header.Cell().Text("Hodiny").SemiBold();
                        header.Cell().Text("Poznámka").SemiBold();
                    });

                    foreach (var p in Payments)
                    {
                        table.Cell().Text(p.Date.ToString("dd.MM.yyyy"));
                        table.Cell().Text($"{p.Amount} Kč");
                        table.Cell().Text($"{p.HoursPurchased} h");
                        table.Cell().Text(p.Note);
                    }
                });
            });

            page.Footer().AlignCenter().Text("Vygenerováno aplikací Výuka").FontSize(10);
        });
    }
}
}
