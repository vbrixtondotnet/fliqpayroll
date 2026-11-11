(function ($) {
    "use strict";

    const api = {
        getSummary: "/api/dashboard/summary",
        pendingApprovals: "/api/payroll-approvals/pending-periods"
    };

    function escapeHtml(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function formatTimestamp(value) {
        return value
            ? new Date(value).toLocaleString("en-PH", {
                timeZone: "Asia/Manila", year: "numeric", month: "short", day: "numeric", hour: "numeric", minute: "2-digit"
            })
            : "";
    }

    function loadPendingApprovals() {
        var $body = $("#dashboard-approvals-body");
        if ($body.length === 0) {
            return;
        }

        $.getJSON(api.pendingApprovals)
            .done(function (response) {
                var periods = (response && response.Data) || [];
                $body.empty();

                $("#dashboard-approvals-count")
                    .toggleClass("d-none", periods.length === 0)
                    .text(periods.length + (periods.length === 1 ? " period" : " periods"));

                if (periods.length === 0) {
                    $body.append('<tr><td colspan="6" class="text-center text-muted py-3">Nothing is waiting for your approval.</td></tr>');
                    return;
                }

                periods.forEach(function (period) {
                    var href = "/Approvals/Period/" + period.PayrollPeriodId;
                    $body.append(
                        "<tr>" +
                        '<td><a class="fw-semibold text-decoration-none" href="' + href + '">' + escapeHtml(period.PeriodName) + "</a></td>" +
                        '<td class="text-center">' + period.PendingEmployeeCount + "</td>" +
                        '<td class="text-center">' + period.PendingChangeCount + "</td>" +
                        "<td>" + escapeHtml((period.RequestedBy || []).join(", ")) + "</td>" +
                        '<td class="text-nowrap">' + escapeHtml(formatTimestamp(period.LatestRequestedAt)) + "</td>" +
                        '<td class="text-end"><a class="btn btn-sm btn-warning" href="' + href + '">Review</a></td>' +
                        "</tr>");
                });
            })
            .fail(function () {
                $body.html('<tr><td colspan="6" class="text-center text-danger py-3">Failed to load pending approvals.</td></tr>');
            });
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat("en-PH", {
            style: "currency",
            currency: "PHP"
        }).format(value || 0);
    }

    function showError(message) {
        $("#dashboard-alert").removeClass("d-none").text(message);
    }

    function loadSummary() {
        $.getJSON(api.getSummary)
            .done(function (response) {
                if (!response || !response.Success || !response.Data) {
                    showError((response && response.Message) || "Unable to load dashboard summary.");
                    return;
                }

                const data = response.Data;
                $("#total-employees").text(data.TotalEmployees);
                $("#active-employees").text(data.ActiveEmployees);
                $("#present-today").text(data.PresentToday);
                $("#absent-today").text(data.AbsentToday);
                $("#monthly-payroll").text(formatCurrency(data.TotalMonthlyPayroll));
                $("#pending-payroll").text(data.PendingPayrollCount);
            })
            .fail(function () {
                showError("Failed to load dashboard summary.");
            });
    }

    $(function () {
        loadSummary();
        loadPendingApprovals();
    });
})(jQuery);
