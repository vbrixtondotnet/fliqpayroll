using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FliqPayroll.Services;

internal static class PayrollExportBuilder
{
    private static readonly string[] Headers =
    [
        "Code",
        "Employee",
        "Salary Type",
        "Basic",
        "Working Days",
        "Absent Days",
        "OT Pay",
        "Holiday Pay",
        "Gross",
        "Deductions",
        "Net Pay"
    ];

    static PayrollExportBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] BuildCsv(PayrollByDateRangeDto data)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Headers.Select(CsvEscape)));

        foreach (var record in data.Records)
        {
            builder.AppendLine(string.Join(",",
                CsvEscape(record.EmployeeCode),
                CsvEscape(record.EmployeeName),
                CsvEscape(record.SalaryType.ToString()),
                FormatAmount(record.BasicSalary),
                FormatNumber(record.WorkingDays),
                FormatNumber(record.AbsentDays),
                FormatAmount(record.OvertimePay),
                FormatAmount(record.HolidayPay),
                FormatAmount(record.GrossPay),
                FormatAmount(record.TotalDeductions),
                FormatAmount(record.NetPay)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    public static byte[] BuildExcel(PayrollByDateRangeDto data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Payroll");

        worksheet.Cell(1, 1).Value = AppConstants.CompanyName;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, Headers.Length).Merge();

        worksheet.Cell(2, 1).Value = $"Payroll Period: {data.PeriodName}";
        worksheet.Range(2, 1, 2, Headers.Length).Merge();

        for (var col = 0; col < Headers.Length; col++)
        {
            var cell = worksheet.Cell(4, col + 1);
            cell.Value = Headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = 5;
        foreach (var record in data.Records)
        {
            worksheet.Cell(row, 1).Value = record.EmployeeCode;
            worksheet.Cell(row, 2).Value = record.EmployeeName;
            worksheet.Cell(row, 3).Value = record.SalaryType.ToString();
            worksheet.Cell(row, 4).Value = record.BasicSalary;
            worksheet.Cell(row, 5).Value = record.WorkingDays;
            worksheet.Cell(row, 6).Value = record.AbsentDays;
            worksheet.Cell(row, 7).Value = record.OvertimePay;
            worksheet.Cell(row, 8).Value = record.HolidayPay;
            worksheet.Cell(row, 9).Value = record.GrossPay;
            worksheet.Cell(row, 10).Value = record.TotalDeductions;
            worksheet.Cell(row, 11).Value = record.NetPay;

            worksheet.Range(row, 4, row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(row, 7, row, 11).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildPdf(PayrollByDateRangeDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                page.Header().Column(header =>
                {
                    header.Item().Text(AppConstants.CompanyName).Bold().FontSize(14);
                    header.Item().Text($"Payroll Export — {data.PeriodName}").FontSize(11);
                    header.Item().PaddingBottom(8);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.3f);
                    });

                    foreach (var header in Headers)
                    {
                        table.Cell().Element(HeaderCell).Text(header).Bold().FontSize(8);
                    }

                    foreach (var record in data.Records)
                    {
                        table.Cell().Element(BodyCell).Text(record.EmployeeCode).FontSize(8);
                        table.Cell().Element(BodyCell).Text(record.EmployeeName).FontSize(8);
                        table.Cell().Element(BodyCell).Text(record.SalaryType.ToString()).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.BasicSalary)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatNumber(record.WorkingDays)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatNumber(record.AbsentDays)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.OvertimePay)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.HolidayPay)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.GrossPay)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.TotalDeductions)).FontSize(8);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(record.NetPay)).Bold().FontSize(8);
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Generated ").FontSize(8);
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .PaddingHorizontal(3);

    private static IContainer BodyCell(IContainer container) =>
        container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(3);

    private static string FormatAmount(decimal value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatNumber(decimal value) =>
        value % 1 == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
