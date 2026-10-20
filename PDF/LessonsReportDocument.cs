using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Vyuka.PDF
{
    public class LessonsReportDocument : IDocument
    {
        public string StudentName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal TaughtHours { get; set; }
        public decimal RemainingHours { get; set; }
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

        // ⭐ Barevná hlavička s logem a učitelem
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Logo vlevo
                row.ConstantColumn(80).Image("wwwroot/images/logo.jpg");

                // Text vpravo
                row.RelativeColumn().Column(col =>
                {
                    col.Item().Text("Ing. Alois Žák")
                        .FontSize(20).Bold().FontColor("#2C3E50");

                    col.Item().Text($"Výkaz odučených hodin – {StudentName}")
                        .FontSize(14).FontColor("#34495E");
                });
            });
        }

        // ⭐ Obsah dokumentu
        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // Souhrnný barevný box
                col.Item().PaddingVertical(10).Background("#ECF0F1").Padding(10).Column(box =>
                {
                    box.Item().Text($"Student: {StudentName}")
                        .FontSize(16).Bold().FontColor("#2C3E50");

                    box.Item().Row(r =>
                    {
                        r.RelativeColumn().Text($"Předplaceno: {TotalHours} h").FontColor("#2980B9");
                        r.RelativeColumn().Text($"Odučeno: {TaughtHours} h").FontColor("#27AE60");
                        r.RelativeColumn().Text($"Zbývá: {RemainingHours} h").FontColor("#C0392B").Bold();
                    });
                });

                col.Item().PaddingTop(20).Element(ComposeLessonsTable);
            });
        }

        // ⭐ Moderní barevná tabulka
        void ComposeLessonsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(90);   // Datum
                    columns.ConstantColumn(60);   // Od
                    columns.ConstantColumn(60);   // Do
                    columns.ConstantColumn(60);   // Hodiny
                    columns.RelativeColumn();     // Typ
                });

                // Barevná hlavička
                table.Header(header =>
                {
                    header.Cell().Background("#2980B9").Padding(5).Text("Datum").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").Padding(5).Text("Od").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").Padding(5).Text("Do").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").Padding(5).Text("Hodin").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#2980B9").Padding(5).Text("Typ").FontColor("#FFFFFF").Bold();

                });

                foreach (var l in Lessons)
                {
                    table.Cell().Padding(5).Text(l.Date.ToString("dd.MM.yyyy"));
                    table.Cell().Padding(5).Text(l.Start.ToString(@"hh\:mm"));
                    table.Cell().Padding(5).Text(l.End.ToString(@"hh\:mm"));
                    table.Cell().Padding(5).Text($"{l.Hours:0.0}");
                    table.Cell().Padding(5).Text(l.Type);
                }
            });
        }
    }
}
