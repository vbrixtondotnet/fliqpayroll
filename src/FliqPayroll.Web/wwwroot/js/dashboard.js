(function ($) {
    "use strict";

    const api = {
        getSummary: "/api/dashboard/summary"
    };

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
    });
})(jQuery);
