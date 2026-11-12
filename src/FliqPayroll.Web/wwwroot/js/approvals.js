(function ($) {
    "use strict";

    const api = {
        pendingPeriods: "/api/payroll-approvals/pending-periods",
        periodChanges: function (periodId) {
            return "/api/payroll-approvals/period/" + periodId + "?pendingOnly=true";
        },
        approve: function (id) { return "/api/payroll-approvals/" + id + "/approve"; },
        reject: function (id) { return "/api/payroll-approvals/" + id + "/reject"; },
        approveAll: function (periodId) { return "/api/payroll-approvals/period/" + periodId + "/approve-all"; },
        rejectAll: function (periodId) { return "/api/payroll-approvals/period/" + periodId + "/reject-all"; }
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

    function formatAmount(change, value) {
        if (change.Field === "PaymentMethod") {
            return escapeHtml(value);
        }

        var number = parseFloat(value);
        return isNaN(number)
            ? escapeHtml(value)
            : number.toLocaleString("en-PH", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function showError(message) {
        window.FliqToast.error(message);
    }

    function showSuccess(message) {
        window.FliqToast.success(message);
    }

    function errorMessage(xhr, fallback) {
        if (xhr.status === 403) {
            return "Only a Finance Officer can approve or reject payroll changes.";
        }

        return (xhr.responseJSON && xhr.responseJSON.Message) || fallback;
    }

    // ---------------- List page ----------------
    function loadList() {
        var $body = $("#approvals-list-body");

        $.getJSON(api.pendingPeriods)
            .done(function (response) {
                var periods = (response && response.Data) || [];
                $body.empty();

                if (periods.length === 0) {
                    $body.append('<tr><td colspan="6" class="text-center text-muted py-4">Nothing is waiting for approval.</td></tr>');
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
            .fail(function (xhr) {
                showError(errorMessage(xhr, "Failed to load pending approvals."));
                $body.html('<tr><td colspan="6" class="text-center text-muted py-4">Unable to load.</td></tr>');
            });
    }

    // ---------------- Period detail page ----------------
    var periodId = null;

    function setBusy(isBusy) {
        $("#approval-detail button").prop("disabled", isBusy);
    }

    function renderRequests(requests) {
        var $container = $("#approval-requests").empty();

        if (requests.length === 0) {
            $("#approval-bulk-card").addClass("d-none");
            $container.append(
                '<div class="card"><div class="card-body text-center py-5">' +
                '<div class="fs-5 mb-1">All changes for this period have been reviewed.</div>' +
                '<a class="btn btn-primary mt-2" href="/Approvals">Back to approvals</a></div></div>');
            return;
        }

        $("#approval-bulk-card").removeClass("d-none");
        var fieldCount = requests.reduce(function (sum, r) { return sum + (r.Changes || []).length; }, 0);
        $("#approval-summary").text(
            requests.length + (requests.length === 1 ? " employee row" : " employee rows") + " with " +
            fieldCount + (fieldCount === 1 ? " changed field" : " changed fields") + " waiting for approval.");

        requests
            .slice()
            .sort(function (a, b) { return a.EmployeeName.localeCompare(b.EmployeeName); })
            .forEach(function (request) {
                var rows = (request.Changes || []).map(function (change) {
                    return "<tr><td>" + escapeHtml(change.Label) + "</td>" +
                        '<td class="text-end text-muted">' + formatAmount(change, change.OldValue) + "</td>" +
                        '<td class="text-end fw-semibold">' + formatAmount(change, change.NewValue) + "</td></tr>";
                }).join("");

                $container.append(
                    '<div class="card mb-3" data-request-id="' + request.Id + '">' +
                    '<div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2" style="background:#ffe8cc">' +
                    "<div><strong>" + escapeHtml(request.EmployeeCode) + " - " + escapeHtml(request.EmployeeName) + "</strong>" +
                    '<div class="small text-muted">Edited by ' + escapeHtml(request.RequestedBy) + " on " +
                    escapeHtml(formatTimestamp(request.RequestedAt)) + "</div></div>" +
                    '<div class="d-flex gap-2">' +
                    '<button type="button" class="btn btn-sm btn-success approval-approve-btn">Approve</button>' +
                    '<button type="button" class="btn btn-sm btn-outline-danger approval-reject-btn">Reject</button>' +
                    "</div></div>" +
                    '<div class="table-responsive"><table class="table table-sm table-bordered align-middle mb-0">' +
                    '<thead class="table-light"><tr><th>Field</th><th class="text-end">Original</th><th class="text-end">Revised</th></tr></thead>' +
                    "<tbody>" + rows + "</tbody></table></div></div>");
            });
    }

    function loadPeriod() {
        return $.getJSON(api.periodChanges(periodId))
            .done(function (response) {
                renderRequests((response && response.Data) || []);
            })
            .fail(function (xhr) {
                showError(errorMessage(xhr, "Failed to load the pending changes."));
                $("#approval-requests").empty();
            });
    }

    function review(url, successFallback) {
        setBusy(true);

        $.ajax({
            url: url,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({ Note: $("#approval-note").val() })
        })
            .done(function (response) {
                showSuccess((response && response.Message) || successFallback);
                $("#approval-note").val("");
                if (window.FliqNotifications) {
                    window.FliqNotifications.refresh();
                }
            })
            .fail(function (xhr) {
                showError(errorMessage(xhr, "Your decision could not be saved."));
            })
            .always(function () {
                loadPeriod().always(function () { setBusy(false); });
            });
    }

    function bindDetail() {
        $("#approval-requests").on("click", ".approval-approve-btn", function () {
            review(api.approve($(this).closest("[data-request-id]").attr("data-request-id")), "Changes approved.");
        });
        $("#approval-requests").on("click", ".approval-reject-btn", function () {
            review(api.reject($(this).closest("[data-request-id]").attr("data-request-id")), "Changes rejected.");
        });
        $("#approve-all-btn").on("click", function () {
            if (window.confirm("Approve ALL pending changes for this payroll period?")) {
                review(api.approveAll(periodId), "All changes approved.");
            }
        });
        $("#reject-all-btn").on("click", function () {
            if (window.confirm("Reject ALL pending changes for this payroll period?")) {
                review(api.rejectAll(periodId), "All changes rejected.");
            }
        });
    }

    $(function () {
        if ($("#approvals-list-body").length) {
            loadList();
        }

        var $detail = $("#approval-detail");
        if ($detail.length) {
            periodId = parseInt($detail.attr("data-period-id"), 10);
            bindDetail();
            loadPeriod();
        }
    });
})(jQuery);
