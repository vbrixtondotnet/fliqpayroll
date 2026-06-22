using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FliqPayroll.Web.Services;

public class PayslipPdfService
{
    private const float BorderThickness = 1f;
    private static readonly string BorderColor = Colors.Black;
    private const string LogoRelativePath = "images/fliq-athletics-logo.png";

    private readonly IWebHostEnvironment _environment;
    private byte[]? _logoBytes;

    static PayslipPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PayslipPdfService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public byte[] Generate(PayslipDto payslip)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(PayslipLayout.Full.DefaultFontSize).FontFamily(Fonts.Arial));
                page.Content().AlignTop().Element(c => ComposePayslip(c, payslip, PayslipLayout.Full));
            });
        }).GeneratePdf();
    }

    public byte[] GenerateAll(IReadOnlyList<PayslipDto> payslips)
    {
        if (payslips.Count == 0)
        {
            throw new ArgumentException("No payslips to generate.");
        }

        var layout = PayslipLayout.Compact;

        return Document.Create(container =>
        {
            foreach (var payslip in payslips)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.MarginHorizontal(18);
                    page.MarginVertical(14);
                    page.DefaultTextStyle(x => x.FontSize(layout.DefaultFontSize).FontFamily(Fonts.Arial));

                    page.Content().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposePayslip(c, payslip, layout));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => ComposePayslip(c, payslip, layout));
                    });
                });
            }
        }).GeneratePdf();
    }

    private void ComposePayslip(IContainer container, PayslipDto payslip, PayslipLayout layout)
    {
        var payroll = payslip.Payroll;
        var employee = payslip.Employee;
        var period = payslip.Period;
        var dailyRate = payroll.SalaryType == SalaryType.Daily
            ? payroll.BasicSalary
            : payroll.DailyRate;

        container.AlignTop().Border(BorderThickness).BorderColor(BorderColor).Column(slip =>
        {
            slip.Item().PaddingHorizontal(layout.HorizontalPadding).PaddingTop(layout.HeaderPaddingTop).Column(header =>
            {
                header.Item().Row(row =>
                {
                    row.ConstantItem(layout.LogoWidth).Height(layout.LogoHeight).Element(ComposeLogo);
                    row.RelativeItem().AlignRight().Column(address =>
                    {
                        foreach (var line in AppConstants.CompanyAddressLines)
                        {
                            address.Item().AlignRight().Text(line)
                                .FontSize(layout.AddressFontSize)
                                .LineHeight(1.1f);
                        }
                    });
                });

                header.Item().PaddingTop(layout.TitlePaddingTop).AlignCenter().Text(AppConstants.CompanyName)
                    .Bold().FontSize(layout.CompanyFontSize);
                header.Item().PaddingTop(1).AlignCenter().Text("PAYSLIP")
                    .Bold().FontSize(layout.PayslipTitleFontSize);
            });

            slip.Item().PaddingHorizontal(layout.HorizontalPadding).PaddingTop(layout.TablePaddingTop).Element(c =>
                ComposePayrollTable(c, payroll, dailyRate, employee.FullName, period.Name, layout));

            slip.Item().PaddingHorizontal(layout.HorizontalPadding).PaddingTop(layout.FooterPaddingTop)
                .PaddingBottom(layout.FooterPaddingBottom).Column(footer =>
                {
                    footer.Item().Text("Personal Copy Received:").FontSize(layout.FooterFontSize);
                    footer.Item().PaddingTop(layout.SignaturePaddingTop).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(layout.SignatureBlockWidth).Column(signatureBlock =>
                        {
                            signatureBlock.Item().Height(layout.SignatureSpacerHeight);
                            signatureBlock.Item().Height(BorderThickness).Background(BorderColor);
                            signatureBlock.Item().PaddingTop(2).AlignCenter()
                                .Text("Signature Over Printed Name & Date")
                                .FontSize(layout.SignatureFontSize);
                        });
                        row.RelativeItem();
                    });
                });
        });
    }

    private void ComposeLogo(IContainer container)
    {
        container.Image(GetLogoBytes()).FitArea();
    }

    private byte[] GetLogoBytes()
    {
        if (_logoBytes is not null)
        {
            return _logoBytes;
        }

        var logoPath = Path.Combine(_environment.WebRootPath, LogoRelativePath);
        if (!File.Exists(logoPath))
        {
            throw new FileNotFoundException("Company logo not found.", logoPath);
        }

        _logoBytes = File.ReadAllBytes(logoPath);
        return _logoBytes;
    }

    private static void ComposePayrollTable(
        IContainer container,
        PayrollDto payroll,
        decimal dailyRate,
        string employeeName,
        string periodName,
        PayslipLayout layout)
    {
        container.Border(BorderThickness).BorderColor(BorderColor).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1f);
            });

            AddEmployeeRow(table, employeeName, periodName, layout);
            AddAmountRow(table, "Daily Rate", dailyRate, layout);
            AddNumberRow(table, "Working Days", payroll.WorkingDays, layout);
            AddAmountRow(table, "Add Paid Leave", payroll.LeaveWithPay, layout);

            AddSectionLabel(table, "ADD", layout);
            AddAmountRow(table, "Regular Overtime", payroll.OvertimePay, layout, indent: true);
            AddAmountRow(table, "Special Non-Working / Rest Day", payroll.SpecialNonWorkingPay, layout, indent: true);
            AddAmountRow(table, "Regular Holiday", payroll.RegularHolidayPay, layout, indent: true);
            AddAmountRow(table, "Night Differential", payroll.NightDiffOtPay, layout, indent: true);
            AddAmountRow(table, "Others:", payroll.ToAdd + payroll.AdjustmentsEarnings, layout, indent: true);

            AddTotalRow(table, "TOTAL GROSS SALARY", payroll.GrossPay, layout);

            AddSectionLabel(table, "LESS", layout);
            AddAmountRow(table, "SSS Premuim", payroll.SssDeduction, layout, indent: true);
            AddAmountRow(table, "Philhealth Contribution", payroll.PhilHealthDeduction, layout, indent: true);
            AddAmountRow(table, "HDMF Contribution", payroll.PagIbigDeduction, layout, indent: true);
            AddAmountRow(table, "SSS Loan", payroll.SssLoanDeduction, layout, indent: true);
            AddAmountRow(table, "HDMF Loan", payroll.PagIbigLoanDeduction, layout, indent: true);
            AddAmountRow(table, "W/holding Tax", payroll.WithholdingTax, layout, indent: true);
            AddAmountRow(table, "Personal Loan", payroll.CashAdvance + payroll.OtherDeductions, layout, indent: true);
            AddAmountRow(table, "Lates/Undertime", payroll.LateUndertimeAmount, layout, indent: true);
            AddAmountRow(table, "Others", payroll.ToDeduct + payroll.SssCalamityDeduction, layout, indent: true);

            AddNetPayRow(table, payroll.NetPay, layout);
        });
    }

    private static void AddEmployeeRow(
        TableDescriptor table,
        string employeeName,
        string periodName,
        PayslipLayout layout)
    {
        table.Cell().Element(c => EmployeeLabelCellStyle(c, layout)).Text(text =>
        {
            text.Span("Employee Name: ").Bold();
            text.Span(employeeName);
        });

        table.Cell().Element(c => EmployeeAmountCellStyle(c, layout)).AlignRight().Text(text =>
        {
            text.Span("Period: ").Bold();
            text.Span(periodName);
        });
    }

    private static void AddSectionLabel(TableDescriptor table, string title, PayslipLayout layout)
    {
        table.Cell().Element(c => BodyLabelCellStyle(c, layout)).Text(title).Bold();
        table.Cell().Element(c => BodyAmountCellStyle(c, layout)).Text(string.Empty);
    }

    private static void AddAmountRow(
        TableDescriptor table,
        string label,
        decimal amount,
        PayslipLayout layout,
        bool indent = false)
    {
        table.Cell().Element(c => BodyLabelCellStyle(c, layout, indent)).Text(label);
        table.Cell().Element(c => BodyAmountCellStyle(c, layout)).AlignRight().Text(FormatPeso(amount));
    }

    private static void AddNumberRow(
        TableDescriptor table,
        string label,
        decimal value,
        PayslipLayout layout)
    {
        table.Cell().Element(c => BodyLabelCellStyle(c, layout)).Text(label);
        table.Cell().Element(c => BodyAmountCellStyle(c, layout)).AlignRight().Text(FormatNumber(value));
    }

    private static void AddTotalRow(
        TableDescriptor table,
        string label,
        decimal amount,
        PayslipLayout layout)
    {
        table.Cell().Element(c => BodyLabelCellStyle(c, layout)).Text(label).Bold();
        table.Cell().Element(c => BodyAmountCellStyle(c, layout)).AlignRight().Text(FormatPeso(amount)).Bold();
    }

    private static void AddNetPayRow(TableDescriptor table, decimal amount, PayslipLayout layout)
    {
        table.Cell().Element(c => BodyLabelCellStyle(c, layout)).Text("NET SALARY").Bold();
        table.Cell().Element(c => BodyAmountCellStyle(c, layout)).AlignRight().Text(FormatPeso(amount)).Bold();
    }

    private static IContainer EmployeeLabelCellStyle(IContainer container, PayslipLayout layout) =>
        container
            .BorderBottom(BorderThickness)
            .BorderColor(BorderColor)
            .PaddingVertical(layout.CellPaddingVertical)
            .PaddingRight(layout.CellPaddingHorizontal)
            .PaddingLeft(layout.CellPaddingHorizontal);

    private static IContainer EmployeeAmountCellStyle(IContainer container, PayslipLayout layout) =>
        container
            .BorderBottom(BorderThickness)
            .BorderLeft(BorderThickness)
            .BorderColor(BorderColor)
            .PaddingVertical(layout.CellPaddingVertical)
            .PaddingHorizontal(layout.CellPaddingHorizontal);

    private static IContainer BodyLabelCellStyle(
        IContainer container,
        PayslipLayout layout,
        bool indent = false) =>
        container
            .PaddingVertical(layout.CellPaddingVertical)
            .PaddingRight(layout.CellPaddingHorizontal)
            .PaddingLeft(indent ? layout.IndentPaddingLeft : layout.CellPaddingHorizontal);

    private static IContainer BodyAmountCellStyle(IContainer container, PayslipLayout layout) =>
        container
            .BorderLeft(BorderThickness)
            .BorderColor(BorderColor)
            .PaddingVertical(layout.CellPaddingVertical)
            .PaddingHorizontal(layout.CellPaddingHorizontal);

    private static string FormatPeso(decimal value) => "₱" + value.ToString("N2");

    private static string FormatNumber(decimal value) =>
        value % 1 == 0 ? value.ToString("N1") : value.ToString("N2");

    private sealed record PayslipLayout(
        float DefaultFontSize,
        float AddressFontSize,
        float CompanyFontSize,
        float PayslipTitleFontSize,
        float FooterFontSize,
        float SignatureFontSize,
        float HorizontalPadding,
        float HeaderPaddingTop,
        float TitlePaddingTop,
        float TablePaddingTop,
        float FooterPaddingTop,
        float FooterPaddingBottom,
        float SignaturePaddingTop,
        float SignatureSpacerHeight,
        float SignatureBlockWidth,
        float LogoWidth,
        float LogoHeight,
        float CellPaddingVertical,
        float CellPaddingHorizontal,
        float IndentPaddingLeft)
    {
        public static PayslipLayout Full { get; } = new(
            DefaultFontSize: 9f,
            AddressFontSize: 8.5f,
            CompanyFontSize: 11f,
            PayslipTitleFontSize: 10f,
            FooterFontSize: 9f,
            SignatureFontSize: 8f,
            HorizontalPadding: 18f,
            HeaderPaddingTop: 16f,
            TitlePaddingTop: 18f,
            TablePaddingTop: 10f,
            FooterPaddingTop: 12f,
            FooterPaddingBottom: 8f,
            SignaturePaddingTop: 20f,
            SignatureSpacerHeight: 18f,
            SignatureBlockWidth: 260f,
            LogoWidth: 165f,
            LogoHeight: 36f,
            CellPaddingVertical: 5f,
            CellPaddingHorizontal: 8f,
            IndentPaddingLeft: 24f);

        public static PayslipLayout Compact { get; } = new(
            DefaultFontSize: 6.5f,
            AddressFontSize: 6f,
            CompanyFontSize: 8f,
            PayslipTitleFontSize: 7.5f,
            FooterFontSize: 6.5f,
            SignatureFontSize: 6f,
            HorizontalPadding: 8f,
            HeaderPaddingTop: 6f,
            TitlePaddingTop: 8f,
            TablePaddingTop: 4f,
            FooterPaddingTop: 6f,
            FooterPaddingBottom: 4f,
            SignaturePaddingTop: 8f,
            SignatureSpacerHeight: 10f,
            SignatureBlockWidth: 145f,
            LogoWidth: 95f,
            LogoHeight: 20f,
            CellPaddingVertical: 2f,
            CellPaddingHorizontal: 4f,
            IndentPaddingLeft: 12f);
    }
}
