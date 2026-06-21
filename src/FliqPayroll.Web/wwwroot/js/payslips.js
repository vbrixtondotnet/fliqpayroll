(function ($) {
    "use strict";

    const api = {
        savedPeriods: "/api/payroll/savedPeriods",
        summary: "/api/reports/payroll-summary",
        payslipPdf: function (employeeId, payrollPeriodId) {
            return "/api/reports/payslip/pdf?employeeId=" + employeeId +
                "&payrollPeriodId=" + payrollPeriodId;
        }
    };

    let selectedPeriodId = null;

    function formatCurrency(value) {
        return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(value || 0);
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function showError(message) {
        $("#payslips-alert").removeClass("d-none").text(message);
    }

    function hideError() {
        $("#payslips-alert").addClass("d-none").text("");
    }

    function setGenerateEnabled(isEnabled) {
        $("#payslips-generate-btn").prop("disabled", !isEnabled);
    }

    function resetSummary() {
        $("#payslip-total-gross").text("—");
        $("#payslip-total-deductions").text("—");
        $("#payslip-total-net").text("—");
    }

    function populatePeriodSelect(periods) {
        var $select = $("#payslips-period-select");
        $select.find("option:not(:first)").remove();

        if (!periods || periods.length === 0) {
            $select.append('<option value="" disabled>No saved payroll periods</option>');
            setGenerateEnabled(false);
            return;
        }

        periods.forEach(function (period) {
            var label = period.PeriodName;
            if (period.RecordCount != null) {
                label += " (" + period.RecordCount + " employees)";
            }

            $select.append(
                $("<option></option>")
                    .val(String(period.PayrollPeriodId))
                    .text(label)
            );
        });
    }

    function loadSavedPeriods() {
        $.getJSON(api.savedPeriods)
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load saved payroll periods.");
                    populatePeriodSelect([]);
                    return;
                }

                hideError();
                populatePeriodSelect(response.Data || []);
            })
            .fail(function () {
                showError("Failed to load saved payroll periods.");
                populatePeriodSelect([]);
            });
    }

    function renderSummary(report) {
        $("#payslip-total-gross").text(formatCurrency(report.TotalGrossPay));
        $("#payslip-total-deductions").text(formatCurrency(report.TotalDeductions));
        $("#payslip-total-net").text(formatCurrency(report.TotalNetPay));

        var $body = $("#payslips-body");
        $body.empty();

        if (!report.Records || report.Records.length === 0) {
            $body.append('<tr><td colspan="7" class="text-center text-muted py-4">No records for this period.</td></tr>');
            return;
        }

        report.Records.forEach(function (record) {
            $body.append([
                "<tr>",
                "<td>", escapeHtml(record.EmployeeCode), "</td>",
                "<td>", escapeHtml(record.EmployeeName), "</td>",
                "<td>", escapeHtml(record.Position || "—"), "</td>",
                "<td>", formatCurrency(record.GrossPay), "</td>",
                "<td>", formatCurrency(record.TotalDeductions), "</td>",
                "<td><strong>", formatCurrency(record.NetPay), "</strong></td>",
                '<td><a href="' + api.payslipPdf(record.EmployeeId, selectedPeriodId) +
                '" class="btn btn-sm btn-outline-primary" target="_blank">PDF</a></td>',
                "</tr>"
            ].join(""));
        });
    }

    function generatePayslips() {
        if (!selectedPeriodId) {
            showError("Select a saved payroll period.");
            return;
        }

        hideError();
        setGenerateEnabled(false);

        $.getJSON(api.summary, { payrollPeriodId: selectedPeriodId })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to generate payslips.");
                    return;
                }

                renderSummary(response.Data);
            })
            .fail(function (xhr) {
                var message = "Failed to generate payslips.";
                if (xhr.responseJSON && xhr.responseJSON.Message) {
                    message = xhr.responseJSON.Message;
                }

                showError(message);
            })
            .always(function () {
                setGenerateEnabled(!!selectedPeriodId);
            });
    }

    $(function () {
        resetSummary();
        loadSavedPeriods();

        $("#payslips-period-select").on("change", function () {
            selectedPeriodId = $(this).val() || null;
            setGenerateEnabled(!!selectedPeriodId);
            resetSummary();
            $("#payslips-body").html(
                '<tr><td colspan="7" class="text-center text-muted py-4">Select a saved payroll period and generate payslips.</td></tr>'
            );
        });

        $("#payslips-generate-btn").on("click", generatePayslips);
    });
})(jQuery);
