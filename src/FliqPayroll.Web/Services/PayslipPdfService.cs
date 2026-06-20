using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FliqPayroll.Web.Services;

public class PayslipPdfService
{
    public byte[] Generate(PayslipDto payslip)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var payroll = payslip.Payroll;
        var employee = payslip.Employee;
        var period = payslip.Period;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("FliqPayroll").Bold().FontSize(18);
                    column.Item().Text("Employee Payslip").FontSize(14);
                    column.Item().PaddingTop(8).Text($"Period: {period.Name} ({period.StartDate:MMM dd} - {period.EndDate:MMM dd, yyyy})");
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Employee Information").Bold();
                            c.Item().Text($"Name: {employee.FullName}");
                            c.Item().Text($"Code: {employee.EmployeeCode}");
                            c.Item().Text($"Position: {employee.Position ?? "—"}");
                            c.Item().Text($"Department: {employee.Department ?? "—"}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Pay Details").Bold();
                            c.Item().Text($"Basic Salary: {FormatMoney(payroll.BasicSalary)}");
                            c.Item().Text($"Working Days: {payroll.WorkingDays:N2}");
                            c.Item().Text($"Absent Days: {payroll.AbsentDays:N2}");
                            c.Item().Text($"Status: {payroll.Status}");
                        });
                    });

                    column.Item().LineHorizontal(1);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Earnings").Bold().Underline();
                            AddLine(c, "Basic Pay", payroll.BasicPayAmount);
                            AddLine(c, "Overtime", payroll.OvertimePay);
                            AddLine(c, "Regular Holiday", payroll.RegularHolidayPay);
                            AddLine(c, "Special Non-Working", payroll.SpecialNonWorkingPay);
                            AddLine(c, "Leave w/ Pay", payroll.LeaveWithPay);
                            AddLine(c, "Incentives", payroll.Incentives);
                            AddLine(c, "Allowances", payroll.Allowances);
                            AddLine(c, "Bonuses", payroll.Bonuses);
                            AddLine(c, "Adjustments", payroll.AdjustmentsEarnings);
                            c.Item().PaddingTop(4).Text($"Gross Pay: {FormatMoney(payroll.GrossPay)}").Bold();
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Deductions").Bold().Underline();
                            AddLine(c, "Absence", payroll.AbsenceDeduction);
                            AddLine(c, "Late", payroll.LateDeduction);
                            AddLine(c, "Undertime", payroll.UndertimeDeduction);
                            AddLine(c, "Cash Advance", payroll.CashAdvance);
                            AddLine(c, "SSS", payroll.SssDeduction);
                            AddLine(c, "PhilHealth", payroll.PhilHealthDeduction);
                            AddLine(c, "Pag-IBIG", payroll.PagIbigDeduction);
                            AddLine(c, "SSS Loan", payroll.SssLoanDeduction);
                            AddLine(c, "Pag-IBIG Loan", payroll.PagIbigLoanDeduction);
                            AddLine(c, "Withholding Tax", payroll.WithholdingTax);
                            AddLine(c, "Other", payroll.OtherDeductions);
                            c.Item().PaddingTop(4).Text($"Total Deductions: {FormatMoney(payroll.TotalDeductions)}").Bold();
                        });
                    });

                    column.Item().PaddingTop(16).AlignCenter()
                        .Text($"NET PAY: {FormatMoney(payroll.NetPay)}").Bold().FontSize(16);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ");
                    text.Span(PhilippineTime.Now.ToString("MMM dd, yyyy HH:mm"));
                });
            });
        }).GeneratePdf();
    }

    private static void AddLine(ColumnDescriptor column, string label, decimal amount)
    {
        if (amount == 0)
        {
            return;
        }

        column.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(100).AlignRight().Text(FormatMoney(amount));
        });
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("N2");
}
