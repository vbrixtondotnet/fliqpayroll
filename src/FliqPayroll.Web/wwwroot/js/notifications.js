// In-app notifications: the header bell lists payroll periods waiting for Finance Officer approval.
(function ($) {
    "use strict";

    if (!$ || $("#notification-bell").length === 0) {
        return;
    }

    const POLL_INTERVAL_MS = 60000;
    const pendingPeriodsUrl = "/api/payroll-approvals/pending-periods";
    let pollTimer = null;
    let inFlight = null;

    function escapeHtml(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function timeAgo(value) {
        if (!value) {
            return "";
        }

        var seconds = Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 1000));
        if (seconds < 60) { return "just now"; }
        var minutes = Math.floor(seconds / 60);
        if (minutes < 60) { return minutes + " min ago"; }
        var hours = Math.floor(minutes / 60);
        if (hours < 24) { return hours + " hr ago"; }
        var days = Math.floor(hours / 24);
        return days + (days === 1 ? " day ago" : " days ago");
    }

    function render(periods) {
        var totalRows = periods.reduce(function (sum, p) { return sum + p.PendingEmployeeCount; }, 0);
        var $badge = $("#notification-badge");

        if (totalRows > 0) {
            $badge.removeClass("d-none").text(totalRows > 99 ? "99+" : totalRows);
        } else {
            $badge.addClass("d-none").text("");
        }

        $("#notification-bell-btn").attr(
            "aria-label",
            totalRows > 0 ? "Notifications: " + totalRows + " pending approvals" : "Notifications");

        $("#notification-subtitle").text(
            totalRows > 0
                ? totalRows + (totalRows === 1 ? " change" : " changes") + " in " + periods.length +
                  (periods.length === 1 ? " payroll period" : " payroll periods")
                : "");

        var $list = $("#notification-list").empty();

        if (periods.length === 0) {
            $list.append('<div class="notification-empty">You\'re all caught up.<br />No payroll changes are waiting for approval.</div>');
            return;
        }

        periods.forEach(function (period) {
            var who = (period.RequestedBy || []).join(", ");
            $list.append(
                '<a class="notification-item" href="/Approvals/Period/' + period.PayrollPeriodId + '">' +
                '<div class="d-flex justify-content-between align-items-start gap-2">' +
                '<span class="fw-semibold">' + escapeHtml(period.PeriodName) + "</span>" +
                '<span class="badge bg-warning text-dark flex-shrink-0">' + period.PendingEmployeeCount +
                (period.PendingEmployeeCount === 1 ? " employee" : " employees") + "</span></div>" +
                '<div class="small text-muted">' + period.PendingChangeCount +
                (period.PendingChangeCount === 1 ? " field" : " fields") + " changed" +
                (who ? " by " + escapeHtml(who) : "") + " &middot; " + escapeHtml(timeAgo(period.LatestRequestedAt)) + "</div>" +
                "</a>");
        });
    }

    function refresh() {
        if (inFlight) {
            return inFlight;
        }

        inFlight = $.getJSON(pendingPeriodsUrl)
            .done(function (response) {
                render((response && response.Data) || []);
            })
            .always(function () {
                inFlight = null;
            });

        return inFlight;
    }

    function startPolling() {
        clearInterval(pollTimer);
        pollTimer = setInterval(function () {
            if (!document.hidden) {
                refresh();
            }
        }, POLL_INTERVAL_MS);
    }

    window.FliqNotifications = { refresh: refresh };

    $(function () {
        refresh();
        startPolling();

        // Fresh data whenever the list is opened or the tab comes back into focus.
        $("#notification-bell").on("show.bs.dropdown", refresh);
        document.addEventListener("visibilitychange", function () {
            if (!document.hidden) {
                refresh();
            }
        });
    });
})(window.jQuery);
