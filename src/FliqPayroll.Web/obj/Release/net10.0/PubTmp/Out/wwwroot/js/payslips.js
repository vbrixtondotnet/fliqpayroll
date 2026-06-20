(function ($) {
    "use strict";

    const api = {
        summary: "/api/reports/payroll-summary",
        defaultPeriod: "/api/payroll/defaultPeriod",
        payslipPdf: function (employeeId, fromDate, toDate) {
            return "/api/reports/payslip/pdf?employeeId=" + employeeId +
                "&fromDate=" + encodeURIComponent(fromDate) +
                "&toDate=" + encodeURIComponent(toDate);
        }
    };

    function formatCurrency(value) {
        return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(value || 0);
    }

    function formatDateInput(date) {
        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
    }

    function showError(message) {
        $("#payslips-alert").removeClass("d-none").text(message);
    }

    function getDateRange() {
        return {
            fromDate: $("#payslips-from-date").val(),
            toDate: $("#payslips-to-date").val()
        };
    }

    function setDefaultDates(callback) {
        $.getJSON(api.defaultPeriod)
            .done(function (response) {
                if (response && response.Success && response.Data) {
                    const period = response.Data;
                    $("#payslips-from-date").val(PhTime.parseDateKey(period.StartDate));
                    $("#payslips-to-date").val(PhTime.parseDateKey(period.EndDate));
                } else {
                    const today = PhTime.now();
                    const start = PhTime.startOfMonth(today);
                    $("#payslips-from-date").val(formatDateInput(start));
                    $("#payslips-to-date").val(formatDateInput(today));
                }

                if (callback) callback();
            })
            .fail(function () {
                const today = PhTime.now();
                const start = PhTime.startOfMonth(today);
                $("#payslips-from-date").val(formatDateInput(start));
                $("#payslips-to-date").val(formatDateInput(today));
                if (callback) callback();
            });
    }

    function renderSummary(report) {
        $("#payslip-total-gross").text(formatCurrency(report.TotalGrossPay));
        $("#payslip-total-deductions").text(formatCurrency(report.TotalDeductions));
        $("#payslip-total-net").text(formatCurrency(report.TotalNetPay));

        const range = getDateRange();
        const $body = $("#payslips-body");
        $body.empty();

        if (!report.Records || report.Records.length === 0) {
            $body.append('<tr><td colspan="7" class="text-center text-muted py-4">No records for this period.</td></tr>');
            return;
        }

        report.Records.forEach(function (record) {
            $body.append([
                "<tr>",
                "<td>" + record.EmployeeCode + "</td>",
                "<td>" + record.EmployeeName + "</td>",
                "<td>" + (record.Position || "—") + "</td>",
                "<td>" + formatCurrency(record.GrossPay) + "</td>",
                "<td>" + formatCurrency(record.TotalDeductions) + "</td>",
                "<td><strong>" + formatCurrency(record.NetPay) + "</strong></td>",
                '<td><a href="' + api.payslipPdf(record.EmployeeId, range.fromDate, range.toDate) +
                '" class="btn btn-sm btn-outline-primary" target="_blank">PDF</a></td>',
                "</tr>"
            ].join(""));
        });
    }

    function generatePayslips() {
        const range = getDateRange();

        if (!range.fromDate || !range.toDate) {
            showError("Select both From and To dates.");
            return;
        }

        if (range.toDate < range.fromDate) {
            showError("To date must be on or after From date.");
            return;
        }

        $("#payslips-alert").addClass("d-none");

        $.getJSON(api.summary, range)
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to generate payslips.");
                    return;
                }

                renderSummary(response.Data);
            })
            .fail(function () { showError("Failed to generate payslips."); });
    }

    $(function () {
        setDefaultDates();
        $("#payslips-generate-btn").on("click", generatePayslips);
    });
})(jQuery);
