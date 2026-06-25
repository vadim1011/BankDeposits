using BankDeposits.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BankDeposits.Services;

public class LoadPDFService
{
    public async Task SaveReportAsync(string filePath, string title, List<string[]> rows, string[] headers)
    {
        await Task.Run(() =>
        {
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            doc.Add(new Paragraph(title)
                .SetFont(boldFont)  
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(new Paragraph(""));

            var table = new Table(headers.Length);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            foreach (var h in headers)
            {
                table.AddHeaderCell(
                    new Cell()
                        .Add(new Paragraph(h).SetFont(boldFont))  
                        .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            }

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.AddCell(new Cell().Add(new Paragraph(cell).SetFont(regularFont)));
                }
            }

            doc.Add(table);
        });
    }
}
