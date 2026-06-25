(function ($) {
    "use strict";

    const api = {
        list: "/api/attendance",
        upload: "/api/attendance/upload",
        employees: "/api/employees",
        update: function (id) { return "/api/attendance/" + id; }
    };

    let allAttendanceRecords = [];
    let employeeFilterOptions = [];

    function showError(message) {
        $("#attendance-success").addClass("d-none");
        $("#attendance-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#attendance-alert").addClass("d-none");
        $("#attendance-success").removeClass("d-none").text(message);
    }

    function formatTime(value) {
        if (!value) {
            return "";
        }

        const parts = String(value).split(":");
        if (parts.length < 2) {
            return value;
        }

        let hours = parseInt(parts[0], 10);
        const minutes = parts[1].substring(0, 2);
        const suffix = hours >= 12 ? "PM" : "AM";
        hours = hours % 12;
        if (hours === 0) {
            hours = 12;
        }

        return hours + ":" + minutes + " " + suffix;
    }

    function formatRecordDate(value) {
        if (!value) {
            return "—";
        }

        var key = PhTime.parseDateKey(value);
        var parts = PhTime.partsFromKey(key);
        var date = new Date(parts.year, parts.month, parts.day);
        return date.toLocaleDateString("en-PH");
    }

    function parseTimeInput(value) {
        if (!value) {
            return null;
        }

        const trimmed = value.trim();
        const match = trimmed.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?\s*(AM|PM)?$/i);
        if (!match) {
            return trimmed;
        }

        let hours = parseInt(match[1], 10);
        const minutes = match[2];
        const seconds = match[3] || "00";
        const meridiem = match[4];

        if (meridiem) {
            if (meridiem.toUpperCase() === "PM" && hours < 12) {
                hours += 12;
            }
            if (meridiem.toUpperCase() === "AM" && hours === 12) {
                hours = 0;
            }
        }

        return String(hours).padStart(2, "0") + ":" + minutes + ":" + seconds;
    }

    function getDateRange() {
        var fromInput = document.getElementById("attendance-from-date");
        var toInput = document.getElementById("attendance-to-date");

        return {
            fromDate: PhTime.dateFromInput(fromInput),
            toDate: PhTime.dateFromInput(toInput)
        };
    }

    function setDefaultDates() {
        $("#attendance-from-date").val(PhTime.formatDateKey(PhTime.startOfMonth()));
        $("#attendance-to-date").val(PhTime.todayKey());
    }

    function getSelectedEmployeeId() {
        const value = $("#attendance-employee-filter-value").val();
        return value ? parseInt(value, 10) : null;
    }

    function formatEmployeeLabel(employee) {
        const name = employee.FullName || [employee.LastName, employee.FirstName].filter(Boolean).join(", ");
        return employee.EmployeeCode + " - " + name;
    }

    function buildEmployeeFilterOptions(employees) {
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

    function openEmployeeFilterMenu() {
        $("#attendance-employee-filter-menu").removeClass("d-none");
        $("#attendance-employee-filter-input").attr("aria-expanded", "true");
    }

    function closeEmployeeFilterMenu() {
        $("#attendance-employee-filter-menu").addClass("d-none");
        $("#attendance-employee-filter-input").attr("aria-expanded", "false");
    }

    function renderEmployeeFilterMenu(searchValue) {
        const $menu = $("#attendance-employee-filter-menu");
        const query = String(searchValue || "").trim().toLowerCase();
        $menu.empty();

        const allOption = {
            value: "",
            label: "All employees",
            searchText: "all employees"
        };

        const matches = [allOption].concat(
            employeeFilterOptions.filter(function (option) {
                return !query || option.searchText.indexOf(query) !== -1 || option.label.toLowerCase().indexOf(query) !== -1;
            })
        );

        if (matches.length === 1 && query) {
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

    function selectEmployeeFilter(optionValue, optionLabel) {
        $("#attendance-employee-filter-value").val(optionValue || "");
        $("#attendance-employee-filter-input").val(optionLabel || "");
        closeEmployeeFilterMenu();
        applyAttendanceFilter();
    }

    function clearEmployeeFilter() {
        selectEmployeeFilter("", "");
        $("#attendance-employee-filter-input").val("");
    }

    function applyAttendanceFilter() {
        const employeeId = getSelectedEmployeeId();
        const filtered = employeeId
            ? allAttendanceRecords.filter(function (record) { return record.EmployeeId === employeeId; })
            : allAttendanceRecords.slice();

        renderRows(filtered);
    }

    function loadEmployees() {
        return $.getJSON(api.employees)
            .done(function (response) {
                if (!response || !response.Success) {
                    return;
                }

                employeeFilterOptions = buildEmployeeFilterOptions(response.Data || []);
                renderEmployeeFilterMenu($("#attendance-employee-filter-input").val());
            });
    }

    function initEmployeeFilter() {
        const $input = $("#attendance-employee-filter-input");
        const $menu = $("#attendance-employee-filter-menu");

        $input.on("focus", function () {
            renderEmployeeFilterMenu($input.val());
            openEmployeeFilterMenu();
        });

        $input.on("input", function () {
            const hadSelection = !!$("#attendance-employee-filter-value").val();
            $("#attendance-employee-filter-value").val("");
            renderEmployeeFilterMenu($input.val());
            openEmployeeFilterMenu();
            if (hadSelection) {
                applyAttendanceFilter();
            }
        });

        $menu.on("click", ".searchable-select-item", function () {
            const $item = $(this);
            selectEmployeeFilter($item.data("value"), $item.text());
        });

        $("#attendance-employee-clear-btn").on("click", clearEmployeeFilter);

        $(document).on("click", function (event) {
            if (!$(event.target).closest("#attendance-employee-filter").length) {
                closeEmployeeFilterMenu();
            }
        });
    }

    function renderRows(records) {
        const $body = $("#attendance-body");
        $body.empty();

        if (!records || records.length === 0) {
            const employeeId = getSelectedEmployeeId();
            const message = employeeId
                ? "No attendance records for the selected employee in this date range."
                : "No attendance records for this date range.";
            $body.append('<tr><td colspan="11" class="text-center text-muted py-4">' + message + '</td></tr>');
            return;
        }

        records.forEach(function (record) {
            const bioBadge = record.IsFromBiometric ? ' <span class="badge text-bg-info">Bio</span>' : "";
            const lateBadge = record.IsLate
                ? '<span class="badge text-bg-warning">Late</span>'
                : '<span class="badge text-bg-success">On Time</span>';
            const otBadge = record.IsOvertimeValid
                ? '<span class="badge text-bg-success">Yes</span>'
                : '<span class="badge text-bg-secondary">No</span>';

            $body.append([
                '<tr data-id="' + record.Id + '">',
                "<td>" + record.EmployeeCode + "</td>",
                "<td>" + record.EmployeeName + bioBadge + "</td>",
                "<td>" + formatRecordDate(record.Date) + "</td>",
                '<td><input type="text" class="form-control form-control-sm attendance-time-in" value="' + formatTime(record.TimeIn) + '" placeholder="8:00 AM" /></td>',
                '<td><input type="text" class="form-control form-control-sm attendance-time-out" value="' + formatTime(record.TimeOut) + '" placeholder="5:00 PM" /></td>',
                '<td class="attendance-late-display">' + lateBadge + '</td>',
                '<td><input type="text" class="form-control form-control-sm attendance-ot-in" value="' + formatTime(record.OvertimeIn) + '" /></td>',
                '<td><input type="text" class="form-control form-control-sm attendance-ot-out" value="' + formatTime(record.OvertimeOut) + '" /></td>',
                '<td class="attendance-ot-valid-display">' + otBadge + '</td>',
                '<td><input type="text" class="form-control form-control-sm attendance-notes" maxlength="500" value="' + (record.Notes || "") + '" /></td>',
                '<td><button type="button" class="btn btn-sm btn-outline-primary attendance-save-btn">Save</button></td>',
                "</tr>"
            ].join(""));
        });
    }

    function loadAttendance() {
        var range = getDateRange();

        if (!range.fromDate || !range.toDate) {
            showError("Select both From and To dates.");
            return;
        }

        if (range.toDate < range.fromDate) {
            showError("To date must be on or after From date.");
            return;
        }

        hideError();
        $("#attendance-from-date").val(range.fromDate);
        $("#attendance-to-date").val(range.toDate);

        $.getJSON(api.list, { fromDate: range.fromDate, toDate: range.toDate })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load attendance.");
                    return;
                }
                allAttendanceRecords = response.Data || [];
                applyAttendanceFilter();
            })
            .fail(function (xhr) {
                var message = "Failed to load attendance.";
                if (xhr.responseJSON && xhr.responseJSON.Message) {
                    message = xhr.responseJSON.Message;
                }
                showError(message);
            });
    }

    function hideError() {
        $("#attendance-alert").addClass("d-none").text("");
    }

    function saveRow($row) {
        const id = $row.data("id");
        const payload = {
            TimeIn: parseTimeInput($row.find(".attendance-time-in").val()),
            TimeOut: parseTimeInput($row.find(".attendance-time-out").val()),
            IsLate: $row.find(".attendance-late-display .badge").text() === "Late",
            OvertimeIn: parseTimeInput($row.find(".attendance-ot-in").val()),
            OvertimeOut: parseTimeInput($row.find(".attendance-ot-out").val()),
            IsOvertimeValid: !!parseTimeInput($row.find(".attendance-ot-in").val()) && !!parseTimeInput($row.find(".attendance-ot-out").val()),
            Notes: $row.find(".attendance-notes").val()
        };

        $.ajax({
            url: api.update(id),
            method: "PUT",
            contentType: "application/json",
            data: JSON.stringify(payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to update attendance.");
                    return;
                }
                showSuccess("Attendance updated for " + response.Data.EmployeeName + ".");
                loadAttendance();
            })
            .fail(function () { showError("Failed to update attendance."); });
    }

    function renderUploadResult(result) {
        const $box = $("#attendance-upload-result");
        const errorList = (result.Errors || []).slice(0, 10).map(function (e) {
            return "<li>" + e + "</li>";
        }).join("");

        $box.removeClass("d-none").html([
            '<div class="alert alert-info mb-0">',
            "<strong>Upload complete.</strong> ",
            "Processed " + result.ProcessedDays + " day(s), ",
            "skipped " + result.SkippedIncomplete + " incomplete, ",
            result.UnmatchedRows + " unmatched row(s) out of " + result.TotalRows + " total.",
            errorList ? "<ul class=" + '"mb-0 mt-2 small"' + ">" + errorList + "</ul>" : "",
            "</div>"
        ].join(""));
    }

    function uploadCsv(event) {
        event.preventDefault();

        const fileInput = document.getElementById("attendance-csv-file");
        if (!fileInput.files || fileInput.files.length === 0) {
            showError("Please choose a CSV file.");
            return;
        }

        const formData = new FormData();
        formData.append("file", fileInput.files[0]);

        $("#attendance-upload-btn").prop("disabled", true);

        $.ajax({
            url: api.upload,
            method: "POST",
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to upload attendance file.");
                    return;
                }

                showSuccess(response.Message || "Attendance file uploaded.");
                renderUploadResult(response.Data);
                fileInput.value = "";
                loadAttendance();
            })
            .fail(function () { showError("Failed to upload attendance file."); })
            .always(function () { $("#attendance-upload-btn").prop("disabled", false); });
    }

    $(function () {
        setDefaultDates();
        initEmployeeFilter();
        loadEmployees();

        $("#attendance-load-btn").on("click", loadAttendance);
        $("#attendance-upload-form").on("submit", uploadCsv);

        $("#attendance-body").on("click", ".attendance-save-btn", function () {
            saveRow($(this).closest("tr"));
        });
    });
})(jQuery);
