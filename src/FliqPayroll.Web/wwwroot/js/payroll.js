(function ($) {

    "use strict";



    const api = {

        getByDateRange: "/api/payroll/getByDateRange",

        defaultPeriod: "/api/payroll/defaultPeriod"

    };



    let loadRequest = null;



    function formatCurrency(value) {

        return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(value || 0);

    }



    function formatDateInput(date) {

        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");

    }



    function showError(message) {

        $("#payroll-alert").removeClass("d-none").text(message);

    }



    function salaryTypeBadge(salaryType) {
        const config = {
            0: { label: "Monthly", css: "badge-green" },
            1: { label: "Daily", css: "badge-blue" },
            2: { label: "Fixed", css: "badge-gray" },
            Monthly: { label: "Monthly", css: "badge-green" },
            Daily: { label: "Daily", css: "badge-blue" },
            Fixed: { label: "Fixed", css: "badge-gray" }
        };
        const badge = config[salaryType] || { label: "Unknown", css: "badge-gray" };
        return ' <span class="badge badge-sm ' + badge.css + '">' + badge.label + "</span>";
    }



    function getDateRange() {

        return {

            fromDate: $("#payroll-from-date").val(),

            toDate: $("#payroll-to-date").val()

        };

    }



    function setLoading(isLoading) {

        $("#payroll-loading").toggleClass("d-none", !isLoading);

        $("#payroll-from-date, #payroll-to-date, #payroll-generate-btn").prop("disabled", isLoading);

    }



    function setDefaultDates(callback) {

        $.getJSON(api.defaultPeriod)

            .done(function (response) {

                if (response && response.Success && response.Data) {

                    const period = response.Data;

                    $("#payroll-from-date").val(PhTime.parseDateKey(period.StartDate));

                    $("#payroll-to-date").val(PhTime.parseDateKey(period.EndDate));

                } else {

                    const today = PhTime.now();

                    const start = PhTime.startOfMonth(today);

                    $("#payroll-from-date").val(formatDateInput(start));

                    $("#payroll-to-date").val(formatDateInput(today));

                }



                if (callback) callback();

            })

            .fail(function () {

                const today = PhTime.now();

                const start = PhTime.startOfMonth(today);

                $("#payroll-from-date").val(formatDateInput(start));

                $("#payroll-to-date").val(formatDateInput(today));

                if (callback) callback();

            });

    }



    function updatePeriodInfo(periodName) {

        if (!periodName) {

            $("#payroll-period-info").addClass("d-none");

            return;

        }



        $("#payroll-period-info")

            .removeClass("d-none alert-warning")

            .addClass("alert-light")

            .text("Showing calculated payroll for " + periodName + ".");

    }



    function renderRows(records) {

        const $body = $("#payroll-body");

        $body.empty();



        if (!records || records.length === 0) {

            $body.append('<tr><td colspan="10" class="text-center text-muted py-4">No payroll records for this period.</td></tr>');

            return;

        }



        records.forEach(function (record) {

            $body.append([

                "<tr>",

                "<td>" + record.EmployeeCode + "</td>",

                "<td>" + record.EmployeeName + salaryTypeBadge(record.SalaryType) + "</td>",

                "<td>" + formatCurrency(record.BasicSalary) + "</td>",

                "<td>" + record.WorkingDays + "</td>",

                "<td>" + record.AbsentDays + "</td>",

                "<td>" + formatCurrency(record.OvertimePay) + "</td>",

                "<td>" + formatCurrency(record.HolidayPay) + "</td>",

                "<td>" + formatCurrency(record.GrossPay) + "</td>",

                "<td>" + formatCurrency(record.TotalDeductions) + "</td>",

                "<td><strong>" + formatCurrency(record.NetPay) + "</strong></td>",

                "</tr>"

            ].join(""));

        });

    }



    function loadPayroll() {

        const range = getDateRange();

        if (!range.fromDate || !range.toDate) {

            showError("Select both From and To dates.");

            return;

        }



        if (range.toDate < range.fromDate) {

            showError("To date must be on or after From date.");

            return;

        }



        if (loadRequest) {

            loadRequest.abort();

        }



        setLoading(true);

        $("#payroll-alert").addClass("d-none");



        loadRequest = $.getJSON(api.getByDateRange, range)

            .done(function (response) {

                if (!response || !response.Success) {

                    showError((response && response.Message) || "Unable to load payroll.");

                    return;

                }



                updatePeriodInfo(response.Data.PeriodName);

                renderRows(response.Data.Records);

            })

            .fail(function (xhr) {

                if (xhr.statusText === "abort") {

                    return;

                }

                showError("Failed to load payroll.");

            })

            .always(function () {

                loadRequest = null;

                setLoading(false);

            });

    }



    $(function () {

        setDefaultDates();

        $("#payroll-generate-btn").on("click", loadPayroll);

    });

})(jQuery);


