(function ($) {
    "use strict";

    const api = {
        list: "/api/leaves",
        employees: "/api/employees",
        delete: function (id) { return "/api/leaves/" + id; }
    };

    const leaveTypeLabels = {
        0: "Sick Leave",
        1: "Vacation Leave",
        SickLeave: "Sick Leave",
        VacationLeave: "Vacation Leave"
    };

    let employeeOptions = [];

    function showError(message) {
        $("#leaves-success").addClass("d-none");
        $("#leaves-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#leaves-alert").addClass("d-none");
        $("#leaves-success").removeClass("d-none").text(message);
    }

    function hideAlerts() {
        $("#leaves-alert, #leaves-success").addClass("d-none").text("");
    }

    function formatEmployeeLabel(employee) {
        const name = employee.FullName || [employee.LastName, employee.FirstName].filter(Boolean).join(", ");
        return employee.EmployeeCode + " - " + name;
    }

    function formatRecordDate(value) {
        if (!value) {
            return "—";
        }

        const key = PhTime.parseDateKey(value);
        const parts = PhTime.partsFromKey(key);
        const date = new Date(parts.year, parts.month, parts.day);
        return date.toLocaleDateString("en-PH");
    }

    function formatLeaveType(value) {
        return leaveTypeLabels[value] || "Unknown";
    }

    function getDateRange() {
        return {
            fromDate: PhTime.dateFromInput(document.getElementById("leaves-from-date")),
            toDate: PhTime.dateFromInput(document.getElementById("leaves-to-date"))
        };
    }

    function setDefaultDates() {
        $("#leaves-from-date").val(PhTime.formatDateKey(PhTime.startOfMonth()));
        $("#leaves-to-date").val(PhTime.todayKey());
    }

    function getSelectedEmployeeId() {
        const value = $("#leave-employee-value").val();
        return value ? parseInt(value, 10) : null;
    }

    function buildEmployeeOptions(employees) {
        return (employees || [])
            .slice()
            .sort(function (a, b) {
                return formatEmployeeLabel(a).localeCompare(formatEmployeeLabel(b));
            })
            .map(function (employee) {
                return {
                    value: String(employee.Id),
                    label: formatEmployeeLabel(employee),
                    searchText: [
                        employee.EmployeeCode,
                        employee.FirstName,
                        employee.LastName,
                        employee.MiddleName,
                        employee.FullName
                    ].filter(Boolean).join(" ").toLowerCase()
                };
            });
    }

    function openEmployeeMenu() {
        $("#leave-employee-menu").removeClass("d-none");
        $("#leave-employee-input").attr("aria-expanded", "true");
    }

    function closeEmployeeMenu() {
        $("#leave-employee-menu").addClass("d-none");
        $("#leave-employee-input").attr("aria-expanded", "false");
    }

    function renderEmployeeMenu(searchValue) {
        const $menu = $("#leave-employee-menu");
        const query = String(searchValue || "").trim().toLowerCase();
        $menu.empty();

        const matches = employeeOptions.filter(function (option) {
            return !query || option.searchText.indexOf(query) !== -1 || option.label.toLowerCase().indexOf(query) !== -1;
        });

        if (matches.length === 0) {
            $menu.append('<div class="searchable-select-empty">No employees found.</div>');
            return;
        }

        matches.forEach(function (option) {
            const $item = $("<button>", {
                type: "button",
                class: "dropdown-item searchable-select-item",
                text: option.label,
                "data-value": option.value,
                role: "option"
            });
            $menu.append($item);
        });
    }

    function selectEmployee(optionValue, optionLabel) {
        $("#leave-employee-value").val(optionValue || "");
        $("#leave-employee-input").val(optionLabel || "");
        closeEmployeeMenu();
    }

    function clearEmployeeSelection() {
        selectEmployee("", "");
        $("#leave-employee-input").val("");
    }

    function initEmployeeSelect() {
        const $input = $("#leave-employee-input");
        const $menu = $("#leave-employee-menu");

        $input.on("focus", function () {
            renderEmployeeMenu($input.val());
            openEmployeeMenu();
        });

        $input.on("input", function () {
            $("#leave-employee-value").val("");
            renderEmployeeMenu($input.val());
            openEmployeeMenu();
        });

        $menu.on("click", ".searchable-select-item", function () {
            const $item = $(this);
            selectEmployee($item.data("value"), $item.text());
        });

        $(document).on("click", function (event) {
            if (!$(event.target).closest("#leave-employee-select").length) {
                closeEmployeeMenu();
            }
        });
    }

    function loadEmployees() {
        return $.getJSON(api.employees)
            .done(function (response) {
                if (!response || !response.Success) {
                    return;
                }

                employeeOptions = buildEmployeeOptions(response.Data || []);
                renderEmployeeMenu($("#leave-employee-input").val());
            });
    }

    function renderRows(records) {
        const $body = $("#leaves-body");
        $body.empty();

        if (!records || records.length === 0) {
            $body.append('<tr><td colspan="6" class="text-center text-muted py-4">No leave records for this date range.</td></tr>');
            return;
        }

        records.forEach(function (record) {
            const typeBadgeClass = record.LeaveType === 1 || record.LeaveType === "VacationLeave"
                ? "text-bg-primary"
                : "text-bg-warning";

            $body.append([
                '<tr data-id="' + record.Id + '">',
                "<td>" + record.EmployeeCode + "</td>",
                "<td>" + record.EmployeeName + "</td>",
                "<td>" + formatRecordDate(record.FromDate) + "</td>",
                "<td>" + formatRecordDate(record.ToDate) + "</td>",
                '<td><span class="badge ' + typeBadgeClass + '">' + formatLeaveType(record.LeaveType) + "</span></td>",
                '<td><button type="button" class="btn btn-sm btn-outline-danger leaves-delete-btn">Delete</button></td>',
                "</tr>"
            ].join(""));
        });
    }

    function loadLeaves() {
        const range = getDateRange();

        if (!range.fromDate || !range.toDate) {
            showError("Select both From and To dates.");
            return;
        }

        if (range.toDate < range.fromDate) {
            showError("To date must be on or after From date.");
            return;
        }

        hideAlerts();
        $("#leaves-from-date").val(range.fromDate);
        $("#leaves-to-date").val(range.toDate);

        $.getJSON(api.list, { fromDate: range.fromDate, toDate: range.toDate })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load leave records.");
                    return;
                }

                renderRows(response.Data);
            })
            .fail(function (xhr) {
                let message = "Failed to load leave records.";
                if (xhr.responseJSON && xhr.responseJSON.Message) {
                    message = xhr.responseJSON.Message;
                }
                showError(message);
            });
    }

    function saveLeave(event) {
        event.preventDefault();
        hideAlerts();

        const employeeId = getSelectedEmployeeId();
        const fromDate = PhTime.dateFromInput(document.getElementById("leave-from-date"));
        const toDate = PhTime.dateFromInput(document.getElementById("leave-to-date"));
        const leaveType = parseInt($("#leave-type").val(), 10);

        if (!employeeId) {
            showError("Select an employee.");
            return;
        }

        if (!fromDate || !toDate) {
            showError("Enter both From and To dates for the leave.");
            return;
        }

        if (toDate < fromDate) {
            showError("Leave To date must be on or after From date.");
            return;
        }

        const payload = {
            EmployeeId: employeeId,
            FromDate: fromDate,
            ToDate: toDate,
            LeaveType: leaveType
        };

        $("#leaves-save-btn").prop("disabled", true);

        $.ajax({
            url: api.list,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to save leave record.");
                    return;
                }

                showSuccess(response.Message || "Leave record saved.");
                $("#leaves-form")[0].reset();
                clearEmployeeSelection();
                loadLeaves();
            })
            .fail(function (xhr) {
                let message = "Failed to save leave record.";
                if (xhr.responseJSON && xhr.responseJSON.Message) {
                    message = xhr.responseJSON.Message;
                }
                showError(message);
            })
            .always(function () {
                $("#leaves-save-btn").prop("disabled", false);
            });
    }

    function deleteLeave(id) {
        if (!window.confirm("Delete this leave record?")) {
            return;
        }

        hideAlerts();

        $.ajax({
            url: api.delete(id),
            method: "DELETE"
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to delete leave record.");
                    return;
                }

                showSuccess(response.Message || "Leave record deleted.");
                loadLeaves();
            })
            .fail(function () {
                showError("Failed to delete leave record.");
            });
    }

    $(function () {
        setDefaultDates();
        initEmployeeSelect();
        loadEmployees();

        $("#leaves-load-btn").on("click", loadLeaves);
        $("#leaves-form").on("submit", saveLeave);

        $("#leaves-body").on("click", ".leaves-delete-btn", function () {
            const id = $(this).closest("tr").data("id");
            deleteLeave(id);
        });
    });
})(jQuery);
