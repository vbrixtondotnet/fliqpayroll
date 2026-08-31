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
    private static readonly ExportColumn[] Columns =
    [
        Text("Employee Code", r => r.EmployeeCode, 1.2f),
        Text("Full Name", r => r.EmployeeName, 2.2f),
        Text("Salary Type", r => r.SalaryType.ToString(), 1.2f),
        Money("Salary - Monthly", r => r.MonthlySalary),
        Money("Salary - Bi-Monthly", r => r.BiMonthlySalary),
        Money("Salary - Daily", r => r.DailyRate),
        Money("Salary - Hourly", r => r.HourlyRate),
        Number("Working - Days", r => r.WorkingDays),
        Number("Absent - Days", r => r.AbsentDays),
        Money("Absent - Amount", r => r.AbsentAmount),
        Money("Gross Salary", r => r.GrossSalary),
        Number("Regular OT - Rate", r => r.RegularOtRate),
        Number("Regular OT - Hours", r => r.RegularOtHours),
        Money("Regular OT - Amount", r => r.OvertimePay),
        Number("Special Non-Working/Rest Day - Rate", r => r.SpecialOtRate),
        Number("Special Non-Working/Rest Day - Hours", r => r.SpecialOtHours),
        Money("Special Non-Working/Rest Day - Amount", r => r.SpecialOtPay),
        Number("Regular Holiday - Rate", r => r.HolidayOtRate),
        Number("Regular Holiday - Days", r => r.HolidayDays),
        Money("Regular Holiday - Amount", r => r.HolidayOtPay),
        Number("Night Diff - Rate", r => r.NightDiffOtRate),
        Number("Night Diff - Hours", r => r.NightDiffHours),
        Money("Night Diff - Amount", r => r.NightDiffOtPay),
        Number("Leave With Pay - Days", r => r.LeaveDays),
        Money("Leave With Pay - Amount", r => r.LeaveWithPay),
        Money("Government Dues - SSS", r => r.SssDeduction),
        Money("Government Dues - PhilHealth", r => r.PhilHealthDeduction),
        Money("Government Dues - Pag-IBIG", r => r.PagIbigDeduction),
        Number("Late/Undertime - Minutes", r => r.LateUndertimeHours),
        Money("Late/Undertime - Amount", r => r.LateUndertimeAmount),
        Money("Loan - SSS Salary", r => r.SssLoanDeduction),
        Money("Loan - SSS Calamity", r => r.SssCalamityDeduction),
        Money("Loan - Pag-IBIG Salary", r => r.PagIbigLoanDeduction),
        Money("To Add", r => r.ToAdd),
        Money("To Deduct", r => r.ToDeduct),
        Money("Actual Salary This Cutoff", r => r.NetPay),
        Text("BPI / Cash", r => r.PaymentMethod ?? "Cash", 1.2f)
    ];

    static PayrollExportBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] BuildCsv(PayrollByDateRangeDto data)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Columns.Select(column => CsvEscape(column.Header))));

        foreach (var record in data.Records)
        {
            builder.AppendLine(string.Join(",", Columns.Select(column => CsvEscape(column.Display(record)))));
        }

        var summary = GetSummary(data.Records);
        builder.AppendLine();
        builder.AppendLine($"Total Gross Pay: {FormatAmount(summary.TotalGrossPay)}");
        builder.AppendLine($"Total Deductions: {FormatAmount(summary.TotalDeductions)}");
        builder.AppendLine($"Total Net Pay: {FormatAmount(summary.TotalNetPay)}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    public static byte[] BuildExcel(PayrollByDateRangeDto data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Payroll");

        worksheet.Cell(1, 1).Value = AppConstants.CompanyName;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, Columns.Length).Merge();

        worksheet.Cell(2, 1).Value = $"Payroll Period: {data.PeriodName}";
        worksheet.Range(2, 1, 2, Columns.Length).Merge();

        for (var col = 0; col < Columns.Length; col++)
        {
            var cell = worksheet.Cell(4, col + 1);
            cell.Value = Columns[col].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.WrapText = true;
        }

        var row = 5;
        foreach (var record in data.Records)
        {
            for (var col = 0; col < Columns.Length; col++)
            {
                var column = Columns[col];
                var cell = worksheet.Cell(row, col + 1);

                if (column.NumericValue is not null)
                {
                    cell.Value = column.NumericValue(record);
                    cell.Style.NumberFormat.Format = column.IsMoney ? "#,##0.00" : "0.##";
                }
                else
                {
                    cell.Value = column.Display(record);
                }
            }

            row++;
        }

        worksheet.SheetView.FreezeRows(4);
        worksheet.Range(4, 1, Math.Max(4, row - 1), Columns.Length).SetAutoFilter();
        worksheet.Columns().AdjustToContents();

        var summary = GetSummary(data.Records);
        var summaryLabelRow = row + 1;
        var summaryValueRow = row + 2;
        AddExcelSummaryCard(
            worksheet,
            summaryLabelRow,
            summaryValueRow,
            1,
            "Total Gross Pay",
            summary.TotalGrossPay);
        AddExcelSummaryCard(
            worksheet,
            summaryLabelRow,
            summaryValueRow,
            3,
            "Total Deductions",
            summary.TotalDeductions);
        AddExcelSummaryCard(
            worksheet,
            summaryLabelRow,
            summaryValueRow,
            5,
            "Total Net Pay",
            summary.TotalNetPay);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildPdf(PayrollByDateRangeDto data)
    {
        var summary = GetSummary(data.Records);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A3.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(5).FontFamily(Fonts.Arial));

                page.Header().Column(header =>
                {
                    header.Item().Text(AppConstants.CompanyName).Bold().FontSize(10);
                    header.Item().Text($"Payroll Export — {data.PeriodName}").FontSize(8);
                    header.Item().PaddingBottom(5);
                });

                page.Content().Column(content =>
                {
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var column in Columns)
                            {
                                columns.RelativeColumn(column.RelativeWidth);
                            }
                        });

                        foreach (var column in Columns)
                        {
                            table.Cell().Element(HeaderCell).Text(column.Header).Bold().FontSize(4.5f);
                        }

                        foreach (var record in data.Records)
                        {
                            foreach (var column in Columns)
                            {
                                var cell = table.Cell().Element(BodyCell);
                                if (column.NumericValue is not null)
                                {
                                    cell = cell.AlignRight();
                                }

                                cell.Text(column.Display(record)).FontSize(4.5f);
                            }
                        }
                    });

                    content.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Element(SummaryCard).Column(card =>
                        {
                            card.Item().Text("Total Gross Pay").FontSize(5).FontColor(Colors.Grey.Darken1);
                            card.Item().Text(FormatCurrency(summary.TotalGrossPay)).Bold().FontSize(8);
                        });
                        row.ConstantItem(6);
                        row.RelativeItem().Element(SummaryCard).Column(card =>
                        {
                            card.Item().Text("Total Deductions").FontSize(5).FontColor(Colors.Grey.Darken1);
                            card.Item().Text(FormatCurrency(summary.TotalDeductions)).Bold().FontSize(8);
                        });
                        row.ConstantItem(6);
                        row.RelativeItem().Element(SummaryCard).Column(card =>
                        {
                            card.Item().Text("Total Net Pay").FontSize(5).FontColor(Colors.Grey.Darken1);
                            card.Item().Text(FormatCurrency(summary.TotalNetPay)).Bold().FontSize(8);
                        });
                    });
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Generated ").FontSize(5);
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).FontSize(5);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(2)
            .PaddingHorizontal(1);

    private static IContainer BodyCell(IContainer container) =>
        container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(1.5f)
            .PaddingHorizontal(1);

    private static IContainer SummaryCard(IContainer container) =>
        container
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(5)
            .PaddingHorizontal(7);

    private static void AddExcelSummaryCard(
        IXLWorksheet worksheet,
        int labelRow,
        int valueRow,
        int firstColumn,
        string label,
        decimal value)
    {
        const int cardColumnSpan = 2;
        const double minimumCardColumnWidth = 14;
        var lastColumn = firstColumn + cardColumnSpan - 1;

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            if (worksheet.Column(column).Width < minimumCardColumnWidth)
            {
                worksheet.Column(column).Width = minimumCardColumnWidth;
            }
        }

        var range = worksheet.Range(labelRow, firstColumn, valueRow, lastColumn);
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var labelRange = worksheet.Range(labelRow, firstColumn, labelRow, lastColumn).Merge();
        labelRange.Value = label;
        labelRange.Style.Font.FontSize = 9;
        labelRange.Style.Font.FontColor = XLColor.FromHtml("#6B7280");
        labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        labelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;

        var valueRange = worksheet.Range(valueRow, firstColumn, valueRow, lastColumn).Merge();
        valueRange.Value = value;
        valueRange.Style.Font.Bold = true;
        valueRange.Style.Font.FontSize = 12;
        valueRange.Style.NumberFormat.Format = "₱#,##0.00";
        valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        valueRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        worksheet.Row(labelRow).Height = 16;
        worksheet.Row(valueRow).Height = 20;
    }

    private static PayrollSummary GetSummary(IReadOnlyList<PayrollDto> records) =>
        new(
            records.Sum(record => record.GrossPay),
            records.Sum(record => record.TotalDeductions),
            records.Sum(record => record.NetPay));

    private static string FormatCurrency(decimal value) =>
        $"₱{value.ToString("N2", CultureInfo.InvariantCulture)}";

    private static ExportColumn Text(
        string header,
        Func<PayrollDto, string> value,
        float relativeWidth = 1f) =>
        new(header, value, null, false, relativeWidth);

    private static ExportColumn Number(
        string header,
        Func<PayrollDto, decimal> value,
        float relativeWidth = 1f) =>
        new(header, record => FormatNumber(value(record)), value, false, relativeWidth);

    private static ExportColumn Money(
        string header,
        Func<PayrollDto, decimal> value,
        float relativeWidth = 1f) =>
        new(header, record => FormatAmount(value(record)), value, true, relativeWidth);

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

    private sealed record ExportColumn(
        string Header,
        Func<PayrollDto, string> Display,
        Func<PayrollDto, decimal>? NumericValue,
        bool IsMoney,
        float RelativeWidth);

    private sealed record PayrollSummary(
        decimal TotalGrossPay,
        decimal TotalDeductions,
        decimal TotalNetPay);
}
