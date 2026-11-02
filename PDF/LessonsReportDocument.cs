using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Vyuka.Models;

namespace Vyuka.PDF
{
    public class LessonsReportDocument : IDocument
    {
        public string StudentName { get; set; } = "";
        public int TotalHours { get; set; }
        public int TaughtHours { get; set; }
        public int RemainingHours { get; set; }

        // ⭐ Nové – plánované neodučené hodiny
        public int PlannedNotTaughtHours { get; set; }

        public List<LessonDto> Lessons { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text($"Vygenerováno aplikací Výuka – {DateTime.Now:dd.MM.yyyy}")
                    .FontColor("#7F8C8D").FontSize(10);
            });
        }

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

                    col.Item().Text($"Výkaz odučených hodin – {StudentName}")
                        .FontSize(16).Bold().FontColor("#34495E");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // ⭐ Horní souhrnný pruh – nyní 4 položky
                col.Item().PaddingVertical(10).Background("#ECF0F1").Padding(10).Column(box =>
                {
                    box.Item().Row(r =>
                    {
                        r.RelativeColumn().AlignCenter().Text($"Předplaceno: {TotalHours} h").FontColor("#2980B9");
                        r.RelativeColumn().AlignCenter().Text($"Odučeno: {TaughtHours} h").FontColor("#27AE60");
                        r.RelativeColumn().AlignCenter().Text($"Plánováno: {PlannedNotTaughtHours} h").FontColor("#F39C12");
                        r.RelativeColumn().AlignCenter().Text($"Zbývá: {RemainingHours} h").FontColor("#C0392B").Bold();
                    });
                });

                col.Item().PaddingTop(20).Element(ComposeLessonsTable);
            });
        }

        void ComposeLessonsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();   // Číslo
                    columns.RelativeColumn();   // Datum
                    columns.RelativeColumn();   // Od
                    columns.RelativeColumn();   // Do
                    columns.RelativeColumn();   // Hodiny
                    columns.RelativeColumn();   // Stav
                });

                table.Header(header =>
                {
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Číslo").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Datum").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Od").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Do").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Hodiny").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").AlignCenter().Padding(5).Text("Stav").FontColor("#FFFFFF").Bold();
                });

                // ⭐ Najdeme poslední plánovanou hodinu
                var lastPlanned = Lessons
                    .Where(l => l.Type == "Plánováno")
                    .OrderByDescending(l => l.Date)
                    .ThenByDescending(l => l.Start)
                    .FirstOrDefault();

                int index = 0;

                foreach (var l in Lessons)
                {
                    // Jemné pruhování řádků
                    var bg = (index % 2 == 1) ? "#dddddd" : "#FFFFFF";

                    // ⭐ Zvýraznění poslední plánované hodiny
                    if (lastPlanned != null && l.Order == lastPlanned.Order)
                        bg = "#FFF3CD";   // jemná žlutá

                    table.Cell().Background(bg).AlignCenter().Padding(5).Text(l.Order.ToString());
                    table.Cell().Background(bg).AlignCenter().Padding(5).Text(l.Date.ToString("dd.MM.yyyy"));
                    table.Cell().Background(bg).AlignCenter().Padding(5).Text(l.Start.ToString(@"hh\:mm"));
                    table.Cell().Background(bg).AlignCenter().Padding(5).Text(l.End.ToString(@"hh\:mm"));
                    table.Cell().Background(bg).AlignCenter().Padding(5).Text($"{l.Hours}");
                    table.Cell().Background(bg).AlignCenter().Padding(5).Text(l.Type);

                    index++;
                }
            });
        }
    }
}
