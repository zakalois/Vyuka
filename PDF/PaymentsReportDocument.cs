using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Vyuka.Models;

namespace Vyuka.PDF
{
    public class PaymentsReportDocument : IDocument
    {
        public string StudentName { get; set; } = "";

        // Souhrnné hodnoty
        public int PaymentCount { get; set; }
        public int PrepaidHours { get; set; }
        public int TaughtHours { get; set; }
        public int RemainingHours => PrepaidHours - TaughtHours;

        // Cena za hodinu dle poslední platby
        public decimal LastPricePerHour { get; set; }

        public List<PaymentDto> Payments { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer()
                    .AlignCenter()
                    .Text($"Vygenerováno aplikací Výuka – {DateTime.Now:dd.MM.yyyy}")
                    .FontColor("#7F8C8D")
                    .FontSize(10);
            });
        }

        // HLAVIČKA – stejná jako u hodin
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.ConstantColumn(80).Image("wwwroot/images/logo.jpg");

                row.RelativeColumn().AlignCenter().Column(col =>
                {
                    col.Item().Text("Ing. Alois Žák")
                        .FontSize(20).Bold().FontColor("#2C3E50");

                    col.Item().Text("");

                    col.Item().Text($"Přehled plateb – {StudentName}")
                        .FontSize(16).Bold().FontColor("#34495E");
                });
            });
        }

        // OBSAH
        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // ⭐ Souhrnný box – první řádek
                col.Item().PaddingVertical(10).Background("#ECF0F1").Padding(10).Row(r =>
                {
                    r.RelativeColumn().AlignCenter().Row(inner =>
                    {
                        inner.AutoItem().Text("Počet plateb:\u00A0").FontColor("#2980B9");
                        inner.AutoItem().Text($"{PaymentCount}").FontColor("#2980B9").Bold();
                    });

                    r.RelativeColumn().AlignCenter().Row(inner =>
                    {
                        inner.AutoItem().Text("Předplacené hodiny:\u00A0").FontColor("#27AE60");
                        inner.AutoItem().Text($"{PrepaidHours} ").FontColor("#27AE60").Bold();
                    });
                });

                // ⭐ Souhrnný box – druhý řádek
                col.Item().Background("#ECF0F1").Padding(10).Row(r =>
                {
                    r.RelativeColumn().AlignCenter().Row(inner =>
                    {
                        inner.AutoItem().Text("Cena za hodinu:\u00A0").FontColor("#C0392B");
                        inner.AutoItem().Text($"{LastPricePerHour:N0} Kč").FontColor("#C0392B").Bold();
                    });

                    r.RelativeColumn().AlignCenter().Row(inner =>
                    {
                        inner.AutoItem().Text("Zbývající počet hodin:\u00A0").FontColor("#C0392B");
                        inner.AutoItem().Text($"{RemainingHours}").FontColor("#C0392B").Bold();
                    });
                });



                col.Item().PaddingTop(20).Element(ComposePaymentsTable);
            });
        }

        // TABULKA PLATEB – stejný styl jako u hodin
        void ComposePaymentsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();   // Číslo
                    columns.RelativeColumn();   // Datum
                    columns.RelativeColumn();   // Částka
                    columns.RelativeColumn();   // Hodiny
                    columns.RelativeColumn();   // Poznámka
                });

                // Hlavička
                table.Header(header =>
                {
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5)
                        .Text("Číslo").FontColor("#FFFFFF").Bold();

                    header.Cell().Background("#2980B9").AlignCenter().Padding(5)
                        .Text("Datum").FontColor("#FFFFFF").Bold();

                    header.Cell().Background("#2980B9").AlignCenter().Padding(5)
                        .Text("Částka").FontColor("#FFFFFF").Bold();

                    header.Cell().Background("#2980B9").AlignCenter().Padding(5)
                        .Text("Hodiny").FontColor("#FFFFFF").Bold();

                    header.Cell().Background("#2980B9").AlignCenter().Padding(5)
                        .Text("Poznámka").FontColor("#FFFFFF").Bold();
                });

                int index = 0;

                foreach (var p in Payments)
                {
                    var bg = (index % 2 == 1) ? "#DDDDDD" : "#FFFFFF";

                    table.Cell().Background(bg).AlignCenter().Padding(5)
                        .Text((index + 1).ToString());

                    table.Cell().Background(bg).AlignCenter().Padding(5)
                        .Text(p.Date.ToString("dd.MM.yyyy"));

                    table.Cell().Background(bg).AlignCenter().Padding(5)
                        .Text($"{p.Amount:N0} Kč");

                    table.Cell().Background(bg).AlignCenter().Padding(5)
                        .Text($"{(int)p.HoursPurchased} h");

                    table.Cell().Background(bg).AlignCenter().Padding(5)
                        .Text(p.Note ?? "");

                    index++;
                }
            });
        }
    }
}
