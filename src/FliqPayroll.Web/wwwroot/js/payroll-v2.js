(function ($) {
    "use strict";

    const api = {
        getByDateRange: "/api/payroll/getByDateRange",
        defaultPeriod: "/api/payroll/defaultPeriod",
        savePeriod: "/api/payroll/savePeriod",
        employees: "/api/employees",
        auditTrail: function (periodId) {
            return "/api/payroll-approvals/period/" + periodId;
        },
        approveChange: function (id) {
            return "/api/payroll-approvals/" + id + "/approve";
        },
        rejectChange: function (id) {
            return "/api/payroll-approvals/" + id + "/reject";
        },
        exportExcel: function (from, to) {
            return "/api/payroll/export/excel?from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to);
        },
        exportCsv: function (from, to) {
            return "/api/payroll/export/csv?from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to);
        },
        exportPdf: function (from, to) {
            return "/api/payroll/export/pdf?from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to);
        }
    };

    const patterns = {
        money: /^\d+(\.\d{0,2})?$/,
        rate: /^\d+(\.\d{0,4})?$/,
        number: /^\d+(\.\d{0,2})?$/
    };

    let loadRequest = null;
    let saveRequest = null;
    let currentPeriodName = "";
    let payrollGenerated = false;
    let allPayrollRecords = [];
    let employeeFilterOptions = [];
    let currentPeriodId = null;
    let reviewingChange = null;
    const canApprove = $("#payroll-v2-config").data("can-approve") === true ||
        $("#payroll-v2-config").attr("data-can-approve") === "true";

    function getSelectedEmployeeId() {
        var value = $("#payroll-v2-employee-filter-value").val();
        return value ? parseInt(value, 10) : null;
    }

    function formatEmployeeLabel(employee) {
        var name = employee.FullName || [employee.LastName, employee.FirstName].filter(Boolean).join(", ");
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
        $("#payroll-v2-employee-filter-menu").removeClass("d-none");
        $("#payroll-v2-employee-filter-input").attr("aria-expanded", "true");
    }

    function closeEmployeeFilterMenu() {
        $("#payroll-v2-employee-filter-menu").addClass("d-none");
        $("#payroll-v2-employee-filter-input").attr("aria-expanded", "false");
    }

    function renderEmployeeFilterMenu(searchValue) {
        var $menu = $("#payroll-v2-employee-filter-menu");
        var query = String(searchValue || "").trim().toLowerCase();
        $menu.empty();

        var allOption = {
            value: "",
            label: "All employees",
            searchText: "all employees"
        };

        var matches = [allOption].concat(
            employeeFilterOptions.filter(function (option) {
                return !query || option.searchText.indexOf(query) !== -1 || option.label.toLowerCase().indexOf(query) !== -1;
            })
        );

        if (matches.length === 1 && query) {
            $menu.append('<div class="searchable-select-empty">No employees found.</div>');
            return;
        }

        matches.forEach(function (option) {
            var $item = $("<button>", {
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
        $("#payroll-v2-employee-filter-value").val(optionValue || "");
        $("#payroll-v2-employee-filter-input").val(optionLabel || "");
        closeEmployeeFilterMenu();
        applyPayrollFilter();
    }

    function clearEmployeeFilter() {
        selectEmployeeFilter("", "");
        $("#payroll-v2-employee-filter-input").val("");
    }

    function applyPayrollFilter() {
        var employeeId = getSelectedEmployeeId();
        var visibleCount = 0;

        $("#payroll-v2-body tr").each(function () {
            var $row = $(this);
            var rowEmployeeId = getEmployeeId($row);

            if (!rowEmployeeId) {
                return;
            }

            var show = !employeeId || rowEmployeeId === employeeId;
            $row.toggleClass("d-none", !show);

            if (show) {
                visibleCount++;
            }
        });

        $("#payroll-v2-filter-empty-row").remove();

        if (employeeId && payrollGenerated && visibleCount === 0) {
            $("#payroll-v2-body").append(
                '<tr id="payroll-v2-filter-empty-row"><td colspan="37" class="text-center text-muted py-4">No payroll records for the selected employee.</td></tr>'
            );
        }
    }

    function loadEmployees() {
        return $.getJSON(api.employees)
            .done(function (response) {
                if (!response || !response.Success) {
                    return;
                }

                employeeFilterOptions = buildEmployeeFilterOptions(response.Data || []);
                renderEmployeeFilterMenu($("#payroll-v2-employee-filter-input").val());
            });
    }

    function initEmployeeFilter() {
        var $input = $("#payroll-v2-employee-filter-input");
        var $menu = $("#payroll-v2-employee-filter-menu");

        $input.on("focus", function () {
            renderEmployeeFilterMenu($input.val());
            openEmployeeFilterMenu();
        });

        $input.on("input", function () {
            var hadSelection = !!$("#payroll-v2-employee-filter-value").val();
            $("#payroll-v2-employee-filter-value").val("");
            renderEmployeeFilterMenu($input.val());
            openEmployeeFilterMenu();

            if (hadSelection) {
                applyPayrollFilter();
            }
        });

        $menu.on("click", ".searchable-select-item", function () {
            var $item = $(this);
            selectEmployeeFilter($item.data("value"), $item.text());
        });

        $("#payroll-v2-employee-clear-btn").on("click", clearEmployeeFilter);

        $(document).on("click", function (event) {
            if (!$(event.target).closest("#payroll-v2-employee-filter").length) {
                closeEmployeeFilterMenu();
            }
        });
    }

    function sanitizeInput(value) {
        return String(value || "").trim().replace(/,/g, "");
    }

    function isValid(value, type) {
        var cleaned = sanitizeInput(value);
        if (cleaned === "") {
            return false;
        }

        return patterns[type].test(cleaned);
    }

    function formatValue(value, type) {
        var cleaned = sanitizeInput(value);
        if (!isValid(cleaned, type)) {
            return null;
        }

        var numeric = parseFloat(cleaned);

        if (type === "money") {
            return numeric.toFixed(2);
        }

        if (type === "rate") {
            return String(numeric);
        }

        if (Number.isInteger(numeric) || numeric % 1 === 0) {
            return String(Math.trunc(numeric));
        }

        return numeric.toFixed(2);
    }

    function validateField($input) {
        var type = $input.data("validate");
        var formatted = formatValue($input.val(), type);

        if (formatted === null) {
            $input.addClass("is-invalid");
            return false;
        }

        $input.removeClass("is-invalid");
        $input.val(formatted);
        return true;
    }

    function roundMoney(value) {
        return Math.round((value || 0) * 100) / 100;
    }

    function parseNumber(value) {
        var cleaned = sanitizeInput(value);
        if (cleaned === "" || isNaN(parseFloat(cleaned))) {
            return 0;
        }

        return parseFloat(cleaned);
    }

    function getFieldValue($row, fieldName) {
        var $field = $row.find('[data-field="' + fieldName + '"]');
        if ($field.length === 0) {
            return 0;
        }

        if ($field.is("input")) {
            return parseNumber($field.val());
        }

        var $link = $field.find(".payroll-v2-absent-link");
        if ($link.length) {
            return parseNumber($link.text());
        }

        return parseNumber($field.text());
    }

    function getFieldText($row, fieldName) {
        var $field = $row.find('[data-field="' + fieldName + '"]');
        if ($field.length === 0) {
            return "";
        }

        return String($field.text() || "").trim();
    }

    function getEmployeeId($row) {
        var raw = $row.attr("data-employee-id");
        var id = parseInt(raw, 10);
        return isNaN(id) ? 0 : id;
    }

    function getDataAttr($row, name) {
        var value = $row.attr("data-" + name);
        return value == null ? "" : String(value);
    }

    function getAjaxErrorMessage(xhr, fallback) {
        var message = fallback;
        var payload = xhr.responseJSON;

        if (!payload) {
            return message;
        }

        if (payload.Message) {
            return payload.Message;
        }

        if (payload.title) {
            message = payload.title;

            if (payload.errors) {
                var details = [];
                Object.keys(payload.errors).forEach(function (key) {
                    var messages = payload.errors[key];
                    if (Array.isArray(messages)) {
                        details = details.concat(messages);
                    }
                });

                if (details.length > 0) {
                    message += " " + details.join(" ");
                }
            }

            return message;
        }

        return message;
    }

    function setFieldValue($row, fieldName, value, format) {
        var $field = $row.find('[data-field="' + fieldName + '"]');
        if ($field.length === 0) {
            return;
        }

        var formatted = format === "money" ? formatMoney(value) : formatNumber(value);

        if ($field.is("input")) {
            $field.val(formatted);
        } else {
            $field.text(formatted);
        }
    }

    function calculateSpecialNonWorkingPay(dailyRate, hoursWorked, rate) {
        var fullDayPay = dailyRate * rate;
        if (hoursWorked >= 8) {
            return fullDayPay;
        }

        return ((dailyRate * rate) / 8) * hoursWorked;
    }

    function calculateRegularHolidayPay(dailyRate, holidayDays, rate, absentHolidayDays) {
        absentHolidayDays = absentHolidayDays || 0;
        var absentPay = dailyRate * absentHolidayDays;

        if (rate <= 1) {
            return absentPay;
        }

        var workedDays = Math.max(0, holidayDays - absentHolidayDays);
        var hours = workedDays * 8;
        if (hours <= 0) {
            return absentPay;
        }

        if (hours >= 8) {
            return absentPay + dailyRate * rate;
        }

        return absentPay + ((dailyRate * rate) / 8) * hours;
    }

    function recalculateRow($row) {
        if ($row.length === 0 || $row.attr("id") === "payroll-v2-empty-row") {
            return;
        }

        var salaryType = parseInt($row.data("salary-type"), 10);
        var isFixed = salaryType === 2;
        var hourlyRate = parseFloat($row.data("hourly-rate")) || 0;
        var dailyRate = parseFloat($row.data("daily-rate")) || 0;
        var baseGross = parseFloat($row.data("base-gross")) || 0;

        var workingDays = getFieldValue($row, "workingDays");
        var absentDays = getFieldValue($row, "absentDays");
        var grossSalary = salaryType === 1
            ? roundMoney(dailyRate * workingDays)
            : baseGross;
        var absentAmount = roundMoney(dailyRate * absentDays);

        setFieldValue($row, "grossSalary", grossSalary, "money");
        setFieldValue($row, "absentAmount", absentAmount, "money");

        var regularOtPay = roundMoney(
            getFieldValue($row, "regularOtHours") * hourlyRate * getFieldValue($row, "regularOtRate"));
        // Fixed employees: HolidayPay is always 0 — skip regular/special holiday pay.
        var specialOtPay = isFixed
            ? 0
            : roundMoney(calculateSpecialNonWorkingPay(
                dailyRate,
                getFieldValue($row, "specialOtHours"),
                getFieldValue($row, "specialOtRate")));
        var holidayOtPay = isFixed
            ? 0
            : roundMoney(calculateRegularHolidayPay(
                dailyRate,
                getFieldValue($row, "holidayDays"),
                getFieldValue($row, "holidayOtRate"),
                parseFloat($row.data("regular-holiday-absent-count")) || 0));
        // Night differential: Hourly Rate × 20% × hours between 10:00 PM and 6:00 AM.
        var nightDiffOtPay = roundMoney(
            getFieldValue($row, "nightDiffHours") * hourlyRate * getFieldValue($row, "nightDiffOtRate"));
        var leavePay = roundMoney(getFieldValue($row, "leaveDays") * dailyRate);
        var lateUndertimeAmount = roundMoney((getFieldValue($row, "lateUndertimeHours") / 60) * hourlyRate);

        setFieldValue($row, "regularOtPay", regularOtPay, "money");
        setFieldValue($row, "specialOtPay", specialOtPay, "money");
        setFieldValue($row, "holidayOtPay", holidayOtPay, "money");
        setFieldValue($row, "nightDiffOtPay", nightDiffOtPay, "money");
        setFieldValue($row, "leavePay", leavePay, "money");
        setFieldValue($row, "lateUndertimeAmount", lateUndertimeAmount, "money");

        var earnings = regularOtPay +
            specialOtPay +
            holidayOtPay +
            nightDiffOtPay +
            leavePay +
            getFieldValue($row, "toAdd");

        var deductions = getFieldValue($row, "sss") +
            getFieldValue($row, "philHealth") +
            getFieldValue($row, "pagIbig") +
            lateUndertimeAmount +
            getFieldValue($row, "sssLoan") +
            getFieldValue($row, "sssCalamity") +
            getFieldValue($row, "pagIbigLoan") +
            getFieldValue($row, "toDeduct");

        if (salaryType === 0) {
            deductions += absentAmount;
        }

        var netPay = roundMoney(grossSalary + earnings - deductions);
        setFieldValue($row, "netPay", netPay, "money");
    }

    function snapshotInputs($row) {
        var snapshot = {};
        $row.find(".payroll-v2-input").each(function () {
            snapshot[$(this).attr("data-field")] = parseNumber($(this).val());
        });
        return snapshot;
    }

    function differsFromBaseline($row) {
        var baseline = $row.data("baseline");
        if (!baseline) {
            return false;
        }

        var current = snapshotInputs($row);
        return Object.keys(current).some(function (key) {
            return Math.abs((current[key] || 0) - (baseline[key] || 0)) > 0.0001;
        });
    }

    // Marks a row as pending approval (with its View changes button) the moment it is edited,
    // then queues an auto-save. Saved periods only: the very first save of a period creates it.
    function refreshEditedState($row) {
        if (!currentPeriodId || !$row.data("baseline")) {
            return;
        }

        var wasDirty = !!$row.data("dirty");
        var edited = differsFromBaseline($row);
        $row.data("dirty", edited);

        // Rows the server already holds as pending stay highlighted; only locally-edited rows toggle.
        if (!$row.attr("data-change-id")) {
            $row.toggleClass("payroll-v2-row-pending", edited);

            // Add/remove only when the state flips; re-creating the button on every keystroke or blur
            // would swallow a click that is in progress.
            var hasWrap = $row.find(".payroll-v2-edited-wrap").length > 0;
            if (edited && !hasWrap) {
                $row.find(".payroll-v2-employee-name").append(
                    '<span class="payroll-v2-edited-wrap">' + pendingBadge({ HasPendingApproval: true }) + "</span>");
            } else if (!edited && hasWrap) {
                $row.find(".payroll-v2-edited-wrap").remove();
            }
        }

        if (edited || wasDirty) {
            scheduleAutoSave();
        }
    }

    // ---------- Auto-save ----------
    var autoSaveTimer = null;
    var autoSaving = false;
    var autoSaveQueued = false;
    var autoSaveCallbacks = [];
    var AUTOSAVE_DELAY_MS = 1000;

    function setAutoSaveStatus(kind, text) {
        var classes = { saving: "text-muted", saved: "text-success", error: "text-danger", dirty: "text-warning" };

        if (kind === "error") {
            window.FliqToast.error(text);
        }
        $("#payroll-v2-autosave-status")
            .removeClass("d-none text-muted text-success text-danger text-warning")
            .addClass(classes[kind] || "text-muted")
            .text(text);
    }

    function scheduleAutoSave() {
        if (!currentPeriodId || !payrollGenerated) {
            return;
        }

        setAutoSaveStatus("dirty", "Unsaved changes...");
        clearTimeout(autoSaveTimer);
        autoSaveTimer = setTimeout(function () { runAutoSave(); }, AUTOSAVE_DELAY_MS);
    }

    function collectDirtyPayload() {
        var records = [];
        var sentRows = [];

        $("#payroll-v2-body tr").each(function () {
            var $row = $(this);
            if (!getEmployeeId($row) || !$row.data("dirty")) {
                return;
            }

            records.push(collectRowPayload($row));
            sentRows.push({ $row: $row, snapshot: snapshotInputs($row) });
        });

        return { records: records, sentRows: sentRows };
    }

    function finishAutoSave() {
        var callbacks = autoSaveCallbacks;
        autoSaveCallbacks = [];
        callbacks.forEach(function (callback) { callback(); });
    }

    function runAutoSave(callback) {
        clearTimeout(autoSaveTimer);

        if (callback) {
            autoSaveCallbacks.push(callback);
        }

        if (!currentPeriodId || !payrollGenerated) {
            finishAutoSave();
            return;
        }

        if (autoSaving) {
            autoSaveQueued = true;
            return;
        }

        if ($("#payroll-v2-body .payroll-v2-input.is-invalid").length > 0) {
            setAutoSaveStatus("error", "Fix the highlighted values to auto-save.");
            finishAutoSave();
            return;
        }

        var dirty = collectDirtyPayload();
        if (dirty.records.length === 0) {
            // Edits were typed back to their original values: nothing to save.
            if (/^Unsaved/.test($("#payroll-v2-autosave-status").text())) {
                $("#payroll-v2-autosave-status").addClass("d-none").text("");
            }
            finishAutoSave();
            return;
        }

        var range = getDateRange();
        autoSaving = true;
        setAutoSaveStatus("saving", "Saving...");

        $.ajax({
            url: api.savePeriod,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                FromDate: range.fromDate,
                ToDate: range.toDate,
                PeriodName: currentPeriodName || $("#payroll-v2-period-label").text(),
                Records: dirty.records
            })
        })
            .then(function (response) {
                if (!response || !response.Success) {
                    setAutoSaveStatus("error", (response && response.Message) || "Auto-save failed.");
                    return;
                }

                // What was sent is now stored; further edits are compared against it.
                dirty.sentRows.forEach(function (sent) {
                    sent.$row.data("baseline", sent.snapshot);
                    if (differsFromBaseline(sent.$row)) {
                        sent.$row.data("dirty", true);
                        autoSaveQueued = true;
                    } else {
                        sent.$row.data("dirty", false);
                    }
                });

                return syncPendingState().done(function () {
                    setAutoSaveStatus("saved", "All changes saved" +
                        (response.Data && response.Data.PendingApprovalCount ? " \u2022 awaiting approval" : ""));
                });
            })
            .fail(function (xhr) {
                if (xhr.status === 401) {
                    return;
                }

                setAutoSaveStatus("error", getAjaxErrorMessage(xhr, "Auto-save failed. It will retry on your next change."));
            })
            .always(function () {
                autoSaving = false;

                if (autoSaveQueued) {
                    autoSaveQueued = false;
                    runAutoSave();
                    return;
                }

                finishAutoSave();
            });
    }

    // Pulls the server's pending requests and refreshes badges/buttons without re-rendering the table.
    function syncPendingState() {
        return $.getJSON(api.auditTrail(currentPeriodId), { pendingOnly: true })
            .done(function (response) {
                var latest = {};
                ((response && response.Data) || []).forEach(function (request) {
                    if (!latest[request.EmployeeId] || latest[request.EmployeeId] < request.Id) {
                        latest[request.EmployeeId] = request.Id;
                    }
                });

                var count = 0;
                $("#payroll-v2-body tr").each(function () {
                    var $row = $(this);
                    var employeeId = getEmployeeId($row);
                    if (!employeeId) {
                        return;
                    }

                    var changeId = latest[employeeId];

                    if (changeId) {
                        count++;
                        var alreadyRendered = String($row.attr("data-change-id")) === String(changeId) &&
                            $row.find(".payroll-v2-review-btn").length === 1 &&
                            $row.find(".payroll-v2-edited-wrap").length === 0;

                        if (!alreadyRendered) {
                            $row.find(".payroll-v2-edited-wrap, .payroll-v2-pending-badge, .payroll-v2-review-btn").remove();
                            $row.find(".payroll-v2-employee-name").append(pendingBadge({ HasPendingApproval: true }));
                        }

                        $row.addClass("payroll-v2-row-pending").attr("data-change-id", changeId);
                    } else {
                        $row.find(".payroll-v2-edited-wrap, .payroll-v2-pending-badge, .payroll-v2-review-btn").remove();
                        $row.removeClass("payroll-v2-row-pending").removeAttr("data-change-id");
                    }
                });

                updatePendingBanner(count);
            });
    }

    function bindValidation() {
        $("#payroll-v2-table").on("input", ".payroll-v2-input", function () {
            var $input = $(this);
            var cleaned = sanitizeInput($input.val());

            if (cleaned === "" || isValid(cleaned, $input.data("validate"))) {
                $input.removeClass("is-invalid");
            } else {
                $input.addClass("is-invalid");
            }
        });

        $("#payroll-v2-table").on("blur", ".payroll-v2-input", function () {
            validateField($(this));
            var $row = $(this).closest("tr");
            recalculateRow($row);
            refreshEditedState($row);
        });

        $("#payroll-v2-table").on("keyup input", ".payroll-v2-input", function () {
            var $row = $(this).closest("tr");
            recalculateRow($row);
            refreshEditedState($row);
        });

        $("#payroll-v2-table").on("keydown", ".payroll-v2-input", function (event) {
            var allowed = ["Backspace", "Delete", "Tab", "Enter", "ArrowLeft", "ArrowRight", "Home", "End"];
            if (allowed.indexOf(event.key) >= 0) {
                return;
            }

            if (event.key === "." && $(this).val().indexOf(".") === -1) {
                return;
            }

            if (/^\d$/.test(event.key)) {
                return;
            }

            event.preventDefault();
        });
    }

    function formatDateInput(date) {
        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
    }

    function formatShortDate(dateKey) {
        var parts = PhTime.partsFromKey(dateKey);
        var date = new Date(parts.year, parts.month, parts.day);
        return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
    }

    function formatPeriodLabel(fromDate, toDate) {
        var toParts = PhTime.partsFromKey(toDate);
        return formatShortDate(fromDate) + " - " + formatShortDate(toDate) + ", " + toParts.year;
    }

    function formatMoney(value) {
        return (value || 0).toFixed(2);
    }

    function formatNumber(value) {
        var numeric = value || 0;
        return numeric % 1 === 0 ? String(Math.trunc(numeric)) : numeric.toFixed(2);
    }

    function formatRate(value) {
        return String(value || 0);
    }

    function shiftRibbon(shiftName) {
        var name = shiftName || "Day Shift";
        var cssClass = /night/i.test(name) ? "shift-ribbon-night" : "shift-ribbon-day";
        return ' <span class="shift-ribbon ' + cssClass + '">' + escapeHtml(name) + "</span>";
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function getDateRange() {
        var fromInput = document.getElementById("payroll-v2-from-date");
        var toInput = document.getElementById("payroll-v2-to-date");

        return {
            fromDate: PhTime.dateFromInput(fromInput),
            toDate: PhTime.dateFromInput(toInput)
        };
    }

    function showError(message) {
        window.FliqToast.error(message);
    }

    function hideError() {
    }

    function hideSaveSuccess() {
        // Toasts dismiss themselves; nothing to clear.
    }

    function showSaveSuccess(message) {
        window.FliqToast.success(message);
    }

    function setSaveButtonVisible(isVisible) {
        $("#payroll-v2-save-btn").toggleClass("d-none", !isVisible);
    }

    function setExportVisible(isVisible) {
        $("#payroll-v2-export-toolbar").toggleClass("d-none", !isVisible);

        if (!isVisible) {
            $("#payroll-v2-export-excel, #payroll-v2-export-csv, #payroll-v2-export-pdf").attr("href", "#");
            return;
        }

        var range = getDateRange();
        $("#payroll-v2-export-excel").attr("href", api.exportExcel(range.fromDate, range.toDate));
        $("#payroll-v2-export-csv").attr("href", api.exportCsv(range.fromDate, range.toDate));
        $("#payroll-v2-export-pdf").attr("href", api.exportPdf(range.fromDate, range.toDate));
    }

    function setSaving(isSaving) {
        $("#payroll-v2-save-btn").prop("disabled", isSaving);
    }

    function setLoading(isLoading) {
        $("#payroll-v2-loading").toggleClass("d-none", !isLoading);
        $("#payroll-v2-from-date, #payroll-v2-to-date, #payroll-v2-generate-btn").prop("disabled", isLoading);
    }

    function updatePeriodInfo(periodName, isSaved) {
        if (!periodName) {
            $("#payroll-v2-period-info").addClass("d-none");
            return;
        }

        $("#payroll-v2-period-info")
            .removeClass("d-none alert-warning")
            .addClass("alert-light")
            .text(isSaved
                ? "Showing the saved payroll for " + periodName + ". Changes you make are saved automatically and need Finance Officer approval."
                : "Showing calculated payroll for " + periodName + ".");
    }

    function readonlyMoney(value, extraClass, stickyClass, fieldName) {
        var classes = ["text-end", "payroll-v2-readonly"];
        if (stickyClass) {
            classes.push("payroll-v2-sticky-col", stickyClass);
        }
        if (extraClass) {
            classes.push(extraClass);
        }

        var fieldAttr = fieldName ? ' data-field="' + fieldName + '"' : "";
        var display = value == null ? "" : formatMoney(value);
        return '<td class="' + classes.join(" ") + '"' + fieldAttr + ">" + display + "</td>";
    }

    function editableInput(value, type, fieldName) {
        var formatted = type === "money"
            ? formatMoney(value)
            : type === "rate"
                ? formatRate(value)
                : formatNumber(value);

        return [
            '<td class="payroll-v2-editable">',
            '<input type="text" class="form-control form-control-sm payroll-v2-input text-end" data-validate="',
            type,
            '" data-field="',
            fieldName,
            '" value="',
            formatted,
            '" inputmode="decimal" autocomplete="off" />',
            "</td>"
        ].join("");
    }

    function readonlyNumber(value, fieldName) {
        return [
            '<td class="text-end payroll-v2-readonly" data-field="',
            fieldName,
            '">',
            formatNumber(value),
            "</td>"
        ].join("");
    }

    function absentDaysCell(record, isFixed) {
        if (isFixed) {
            return readonlyNumber(0, "absentDays");
        }

        var days = record.AbsentDays || 0;
        var formatted = formatNumber(days);
        var dates = record.AbsentDates || [];

        return [
            '<td class="payroll-v2-editable payroll-v2-absent-days">',
            '<div class="d-flex align-items-center gap-1">',
            '<input type="text" class="form-control form-control-sm payroll-v2-input text-end" ',
            'data-validate="number" data-field="absentDays" value="',
            formatted,
            '" inputmode="decimal" autocomplete="off" aria-label="Absent days" />',
            dates.length > 0
                ? [
                    '<button type="button" class="btn btn-link btn-sm p-0 payroll-v2-absent-link flex-shrink-0" ',
                    'data-absent-dates=\'',
                    JSON.stringify(dates),
                    '\' title="View calculated absent dates" aria-label="View calculated absent dates">',
                    '<span aria-hidden="true">View</span>',
                    "</button>"
                ].join("")
                : "",
            "</div>",
            "</td>"
        ].join("");
    }

    function showAbsentDatesModal($row, dates) {
        var employeeCode = $row.find(".payroll-v2-employee-code").text().trim();
        var employeeName = $row.find(".payroll-v2-employee-name-text").text().trim();
        var $list = $("#payroll-v2-absent-dates-list").empty();

        $("#payroll-v2-absent-employee").text(employeeCode + " - " + employeeName);

        dates.forEach(function (dateKey) {
            $list.append("<li>" + escapeHtml(formatShortDate(dateKey)) + "</li>");
        });

        bootstrap.Modal.getOrCreateInstance(document.getElementById("payroll-v2-absent-modal")).show();
    }

    function bindAbsentDaysLinks() {
        $("#payroll-v2-table").on("click", ".payroll-v2-absent-link", function () {
            var dates = [];

            try {
                dates = JSON.parse($(this).attr("data-absent-dates") || "[]");
            } catch (error) {
                dates = [];
            }

            showAbsentDatesModal($(this).closest("tr"), dates);
        });
    }

    function formatSalaryType(salaryType) {
        var labels = {
            0: "Monthly",
            1: "Daily",
            2: "Fixed",
            Monthly: "Monthly",
            Daily: "Daily",
            Fixed: "Fixed"
        };

        return labels[salaryType] || "Unknown";
    }

    function getSalaryTypeClass(salaryType) {
        var classes = {
            0: "payroll-v2-salary-type-monthly",
            1: "payroll-v2-salary-type-daily",
            2: "payroll-v2-salary-type-fixed",
            Monthly: "payroll-v2-salary-type-monthly",
            Daily: "payroll-v2-salary-type-daily",
            Fixed: "payroll-v2-salary-type-fixed"
        };

        return classes[salaryType] || "";
    }

    function pendingBadge(record) {
        if (!record.HasPendingApproval) {
            return "";
        }

        return '<span class="payroll-v2-pending-badge" title="Edits to this row are waiting for Finance Officer approval">Pending approval</span>' +
            ' <button type="button" class="btn btn-link btn-sm p-0 payroll-v2-review-btn">' +
            (canApprove ? "Review" : "View changes") + "</button>";
    }

    function updatePendingBanner(pendingCount) {
        var $banner = $("#payroll-v2-pending-banner");

        if (!pendingCount) {
            $banner.addClass("d-none").empty();
            return;
        }

        var rows = pendingCount === 1 ? "1 row has" : pendingCount + " rows have";
        var tail = canApprove
            ? "Use Review on each highlighted row to approve or reject the changes."
            : "A Finance Officer must approve them before they take effect. Payslips for these employees stay on hold until then.";

        $banner
            .removeClass("d-none")
            .html("<strong>Approval needed:</strong> " + rows + " edits waiting for approval (highlighted). " + escapeHtml(tail));
    }

    function renderRow(record) {
        var isDaily = record.SalaryType === 1 || record.SalaryType === "Daily";
        var isFixed = record.SalaryType === 2 || record.SalaryType === "Fixed";
        var dailyRate = isDaily ? record.BasicSalary : record.DailyRate;
        var holidayOtPay = isFixed ? 0 : (record.HolidayOtPay || 0);
        var specialOtPay = isFixed ? 0 : (record.SpecialOtPay || 0);
        var holidayDays = isFixed ? 0 : (record.HolidayDays || 0);
        var specialOtHours = isFixed ? 0 : (record.SpecialOtHours || 0);
        var regularHolidayAbsentCount = isFixed ? 0 : (record.RegularHolidayAbsentCount || 0);

        var isPending = !!record.HasPendingApproval;
        var pendingAttrs = isPending
            ? ' class="payroll-v2-row-pending" data-change-id="' + record.PendingChangeRequestId + '"'
            : "";

        return [
            '<tr', pendingAttrs, ' data-employee-id="', record.EmployeeId,
            '" data-employee-code="', escapeHtml(record.EmployeeCode),
            '" data-basic-salary="', record.BasicSalary,
            '" data-position="', escapeHtml(record.Position || ""),
            '" data-salary-type="', record.SalaryType,
            '" data-hourly-rate="', record.HourlyRate,
            '" data-daily-rate="', dailyRate,
            '" data-base-gross="', record.GrossSalary,
            '" data-regular-holiday-absent-count="', regularHolidayAbsentCount, '">',
            '<td class="payroll-v2-sticky-col payroll-v2-sticky-col-0 payroll-v2-employee-code">', escapeHtml(record.EmployeeCode), "</td>",
            '<td class="payroll-v2-sticky-col payroll-v2-sticky-col-1 payroll-v2-employee-name"><span class="payroll-v2-employee-name-text">', escapeHtml(record.EmployeeName), "</span>", shiftRibbon(record.ShiftName), pendingBadge(record), "</td>",
            '<td class="text-center ', getSalaryTypeClass(record.SalaryType), '">', escapeHtml(formatSalaryType(record.SalaryType)), "</td>",
            readonlyMoney(isDaily ? null : record.MonthlySalary, "", "", "monthlySalary"),
            readonlyMoney(isDaily ? null : record.BiMonthlySalary, "", "", "biMonthlySalary"),
            readonlyMoney(dailyRate, "", "", "dailyRate"),
            readonlyMoney(record.HourlyRate, "", "", "hourlyRate"),
            isFixed
                ? readonlyNumber(0, "workingDays")
                : editableInput(record.WorkingDays, "number", "workingDays"),
            absentDaysCell(record, isFixed),
            readonlyMoney(isFixed ? 0 : record.AbsentAmount, "", "", "absentAmount"),
            readonlyMoney(record.GrossSalary, "fw-semibold", "", "grossSalary"),
            editableInput(record.RegularOtRate, "rate", "regularOtRate"),
            editableInput(record.RegularOtHours, "number", "regularOtHours"),
            readonlyMoney(record.OvertimePay, "", "", "regularOtPay"),
            editableInput(record.SpecialOtRate, "rate", "specialOtRate"),
            editableInput(specialOtHours, "number", "specialOtHours"),
            readonlyMoney(specialOtPay, "", "", "specialOtPay"),
            editableInput(record.HolidayOtRate, "rate", "holidayOtRate"),
            editableInput(holidayDays, "number", "holidayDays"),
            readonlyMoney(holidayOtPay, "", "", "holidayOtPay"),
            editableInput(record.NightDiffOtRate, "rate", "nightDiffOtRate"),
            editableInput(record.NightDiffHours, "number", "nightDiffHours"),
            readonlyMoney(record.NightDiffOtPay, "", "", "nightDiffOtPay"),
            editableInput(record.LeaveDays, "number", "leaveDays"),
            readonlyMoney(record.LeaveWithPay, "", "", "leavePay"),
            editableInput(record.SssDeduction, "money", "sss"),
            editableInput(record.PhilHealthDeduction, "money", "philHealth"),
            editableInput(record.PagIbigDeduction, "money", "pagIbig"),
            editableInput(record.LateUndertimeHours, "number", "lateUndertimeHours"),
            readonlyMoney(record.LateUndertimeAmount, "", "", "lateUndertimeAmount"),
            editableInput(record.SssLoanDeduction, "money", "sssLoan"),
            editableInput(record.SssCalamityDeduction, "money", "sssCalamity"),
            editableInput(record.PagIbigLoanDeduction, "money", "pagIbigLoan"),
            editableInput(record.ToAdd, "money", "toAdd"),
            editableInput(record.ToDeduct, "money", "toDeduct"),
            readonlyMoney(record.NetPay, "fw-semibold text-primary", "", "netPay"),
            '<td class="payroll-v2-readonly" data-field="paymentMethod">', escapeHtml(record.PaymentMethod || "Cash"), "</td>",
            "</tr>"
        ].join("");
    }

    function collectRowPayload($row) {
        var salaryType = parseInt($row.data("salary-type"), 10);
        var isFixed = salaryType === 2;
        var grossSalary = getFieldValue($row, "grossSalary");
        var regularOtPay = getFieldValue($row, "regularOtPay");
        var specialOtPay = isFixed ? 0 : getFieldValue($row, "specialOtPay");
        var holidayOtPay = isFixed ? 0 : getFieldValue($row, "holidayOtPay");
        var nightDiffOtPay = getFieldValue($row, "nightDiffOtPay");
        var leavePay = getFieldValue($row, "leavePay");
        var absentAmount = getFieldValue($row, "absentAmount");
        var lateUndertimeAmount = getFieldValue($row, "lateUndertimeAmount");
        var toAdd = getFieldValue($row, "toAdd");
        var toDeduct = getFieldValue($row, "toDeduct");
        var sss = getFieldValue($row, "sss");
        var philHealth = getFieldValue($row, "philHealth");
        var pagIbig = getFieldValue($row, "pagIbig");
        var sssLoan = getFieldValue($row, "sssLoan");
        var sssCalamity = getFieldValue($row, "sssCalamity");
        var pagIbigLoan = getFieldValue($row, "pagIbigLoan");
        var totalDeductions = sss + philHealth + pagIbig + lateUndertimeAmount + sssLoan + sssCalamity + pagIbigLoan + toDeduct;

        if (salaryType === 0) {
            totalDeductions += absentAmount;
        }

        return {
            EmployeeId: getEmployeeId($row),
            EmployeeName: $row.find(".payroll-v2-employee-name-text").text().trim(),
            EmployeeCode: $row.find(".payroll-v2-employee-code").text().trim() || getDataAttr($row, "employee-code"),
            Position: getDataAttr($row, "position"),
            SalaryType: salaryType,
            BasicSalary: parseFloat($row.data("basic-salary")) || 0,
            MonthlySalary: getFieldValue($row, "monthlySalary"),
            BiMonthlySalary: getFieldValue($row, "biMonthlySalary"),
            DailyRate: getFieldValue($row, "dailyRate"),
            HourlyRate: getFieldValue($row, "hourlyRate"),
            WorkingDays: getFieldValue($row, "workingDays"),
            AbsentDays: getFieldValue($row, "absentDays"),
            AbsentAmount: absentAmount,
            BasicPayAmount: grossSalary,
            GrossSalary: grossSalary,
            RegularOtRate: getFieldValue($row, "regularOtRate"),
            RegularOtHours: getFieldValue($row, "regularOtHours"),
            SpecialOtRate: getFieldValue($row, "specialOtRate"),
            SpecialOtHours: isFixed ? 0 : getFieldValue($row, "specialOtHours"),
            SpecialOtPay: specialOtPay,
            HolidayOtRate: getFieldValue($row, "holidayOtRate"),
            HolidayDays: isFixed ? 0 : getFieldValue($row, "holidayDays"),
            HolidayOtPay: holidayOtPay,
            NightDiffOtRate: getFieldValue($row, "nightDiffOtRate"),
            NightDiffHours: getFieldValue($row, "nightDiffHours"),
            NightDiffOtPay: nightDiffOtPay,
            LeaveDays: getFieldValue($row, "leaveDays"),
            OvertimePay: regularOtPay,
            HolidayPay: roundMoney(holidayOtPay + specialOtPay),
            RegularHolidayPay: holidayOtPay,
            SpecialNonWorkingPay: specialOtPay,
            LeaveWithPay: leavePay,
            Incentives: 0,
            Allowances: 0,
            Bonuses: 0,
            AdjustmentsEarnings: toAdd,
            GrossPay: roundMoney(grossSalary + regularOtPay + specialOtPay + holidayOtPay + nightDiffOtPay + leavePay + toAdd),
            AbsenceDeduction: salaryType === 0 ? absentAmount : 0,
            LateDeduction: lateUndertimeAmount,
            LateUndertimeHours: getFieldValue($row, "lateUndertimeHours"),
            LateUndertimeAmount: lateUndertimeAmount,
            UndertimeDeduction: 0,
            CashAdvance: 0,
            SssDeduction: sss,
            PhilHealthDeduction: philHealth,
            PagIbigDeduction: pagIbig,
            SssLoanDeduction: sssLoan,
            SssCalamityDeduction: sssCalamity,
            PagIbigLoanDeduction: pagIbigLoan,
            WithholdingTax: 0,
            OtherDeductions: 0,
            ToAdd: toAdd,
            ToDeduct: toDeduct,
            TotalDeductions: roundMoney(totalDeductions),
            NetPay: getFieldValue($row, "netPay"),
            PaymentMethod: getFieldText($row, "paymentMethod") || "Cash",
            Status: 1
        };
    }

    function collectTablePayload() {
        var records = [];

        $("#payroll-v2-body tr").each(function () {
            var $row = $(this);
            if (!getEmployeeId($row)) {
                return;
            }

            records.push(collectRowPayload($row));
        });

        return {
            FromDate: getDateRange().fromDate,
            ToDate: getDateRange().toDate,
            PeriodName: currentPeriodName || $("#payroll-v2-period-label").text(),
            Records: records
        };
    }

    function renderRows(records) {
        var $body = $("#payroll-v2-body");
        $body.empty();
        payrollGenerated = false;
        setSaveButtonVisible(false);
        updatePendingBanner(0);

        if (!records || records.length === 0) {
            $body.append('<tr><td colspan="37" class="text-center text-muted py-4">No payroll records for this period.</td></tr>');
            return;
        }

        records.forEach(function (record) {
            $body.append(renderRow(record));
        });

        $body.find("tr").each(function () {
            recalculateRow($(this));
            $(this).data("baseline", snapshotInputs($(this)));
        });

        applyPayrollFilter();

        payrollGenerated = true;
        setSaveButtonVisible(!currentPeriodId);
        updatePendingBanner(records.filter(function (r) { return r.HasPendingApproval; }).length);
    }

    function setDefaultDates(callback) {
        $.getJSON(api.defaultPeriod)
            .done(function (response) {
                if (response && response.Success && response.Data) {
                    var period = response.Data;
                    $("#payroll-v2-from-date").val(PhTime.parseDateKey(period.StartDate));
                    $("#payroll-v2-to-date").val(PhTime.parseDateKey(period.EndDate));
                } else {
                    var today = PhTime.now();
                    var start = PhTime.startOfMonth(today);
                    $("#payroll-v2-from-date").val(formatDateInput(start));
                    $("#payroll-v2-to-date").val(formatDateInput(today));
                }

                if (callback) {
                    callback();
                }
            })
            .fail(function () {
                var today = PhTime.now();
                var start = PhTime.startOfMonth(today);
                $("#payroll-v2-from-date").val(formatDateInput(start));
                $("#payroll-v2-to-date").val(formatDateInput(today));

                if (callback) {
                    callback();
                }
            });
    }

    function loadPayroll(keepMessage) {
        var range = getDateRange();

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

        hideError();
        if (keepMessage !== true) {
            hideSaveSuccess();
        }
        setLoading(true);
        setSaveButtonVisible(false);
        setExportVisible(false);
        payrollGenerated = false;

        var periodLabel = formatPeriodLabel(range.fromDate, range.toDate);
        $("#payroll-v2-period-label").text(periodLabel);

        loadRequest = $.getJSON(api.getByDateRange, range)
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load payroll.");
                    return;
                }

                var periodName = response.Data.PeriodName || periodLabel;
                currentPeriodName = periodName;
                currentPeriodId = response.Data.PayrollPeriodId || null;
                clearTimeout(autoSaveTimer);
                $("#payroll-v2-autosave-status").addClass("d-none").text("");
                $("#payroll-v2-audit-btn").toggleClass("d-none", !currentPeriodId);
                $("#payroll-v2-period-label").text(periodName);
                updatePeriodInfo(periodName, !!currentPeriodId);
                allPayrollRecords = response.Data.Records || [];
                renderRows(allPayrollRecords);

                if (keepMessage === true || currentPeriodId) {
                    setExportVisible(true);
                }

                // Generating a period that is not in the database yet saves it right away.
                // A period that already exists is only displayed (its edits go through approval).
                if (keepMessage !== true && !currentPeriodId && payrollGenerated) {
                    savePayrollPeriod(true);
                }
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

    function savePayrollPeriod(isGeneration) {
        if (!payrollGenerated) {
            showError("Generate payroll before saving.");
            return;
        }

        clearTimeout(autoSaveTimer);
        var payload = collectTablePayload();

        if (!payload.FromDate || !payload.ToDate) {
            showError("Select both From and To dates.");
            return;
        }

        if (!payload.Records.length) {
            showError("No payroll rows to save.");
            return;
        }

        if (saveRequest) {
            saveRequest.abort();
        }

        hideError();
        hideSaveSuccess();
        setSaving(true);

        saveRequest = $.ajax({
            url: api.savePeriod,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to save payroll period.");
                    return;
                }

                showSaveSuccess(isGeneration === true
                    ? "Payroll for " + (payload.PeriodName || "this period") + " was generated and saved."
                    : (response.Message || "Payroll period saved successfully."));
                setExportVisible(true);
                loadPayroll(true);
            })
            .fail(function (xhr) {
                if (xhr.statusText === "abort") {
                    return;
                }

                showError(getAjaxErrorMessage(xhr, "Failed to save payroll period."));
            })
            .always(function () {
                saveRequest = null;
                setSaving(false);
            });
    }

    function formatAmount(value) {
        var number = parseFloat(value);
        if (isNaN(number)) {
            return escapeHtml(value == null ? "" : value);
        }

        return number.toLocaleString("en-PH", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatTimestamp(value) {
        if (!value) {
            return "";
        }

        return new Date(value).toLocaleString("en-PH", {
            timeZone: "Asia/Manila",
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "numeric",
            minute: "2-digit"
        });
    }

    function openReviewModal(changeId) {
        if (!currentPeriodId) {
            return;
        }

        $("#payroll-v2-review-note").val("");
        reviewingChange = null;

        $.getJSON(api.auditTrail(currentPeriodId), { pendingOnly: true })
            .done(function (response) {
                var requests = (response && response.Data) || [];
                var request = requests.filter(function (r) { return r.Id === changeId; })[0];

                if (!request) {
                    showError("This change is no longer pending. Refreshing the payroll.");
                    loadPayroll(true);
                    return;
                }

                reviewingChange = request;
                $("#payroll-v2-review-summary").text(request.EmployeeCode + " - " + request.EmployeeName);
                $("#payroll-v2-review-meta").text(
                    "Submitted by " + request.RequestedBy + " on " + formatTimestamp(request.RequestedAt));

                var $tbody = $("#payroll-v2-review-changes").empty();
                (request.Changes || []).forEach(function (change) {
                    var isText = change.Field === "PaymentMethod";
                    $tbody.append(
                        "<tr><td>" + escapeHtml(change.Label) + "</td>" +
                        '<td class="text-end text-muted">' + (isText ? escapeHtml(change.OldValue) : formatAmount(change.OldValue)) + "</td>" +
                        '<td class="text-end fw-semibold">' + (isText ? escapeHtml(change.NewValue) : formatAmount(change.NewValue)) + "</td></tr>");
                });

                $("#payroll-v2-review-actions, #payroll-v2-reject-btn, #payroll-v2-approve-btn").toggleClass("d-none", !canApprove);
                $("#payroll-v2-review-readonly").toggleClass("d-none", canApprove);

                bootstrap.Modal.getOrCreateInstance(document.getElementById("payroll-v2-review-modal")).show();
            })
            .fail(function () {
                showError("Failed to load the pending changes.");
            });
    }

    function submitReview(isApprove) {
        if (!reviewingChange) {
            return;
        }

        var $buttons = $("#payroll-v2-approve-btn, #payroll-v2-reject-btn").prop("disabled", true);

        $.ajax({
            url: isApprove ? api.approveChange(reviewingChange.Id) : api.rejectChange(reviewingChange.Id),
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({ Note: $("#payroll-v2-review-note").val() })
        })
            .done(function (response) {
                bootstrap.Modal.getOrCreateInstance(document.getElementById("payroll-v2-review-modal")).hide();
                showSaveSuccess((response && response.Message) || "Done.");
                if (window.FliqNotifications) {
                    window.FliqNotifications.refresh();
                }
                loadPayroll(true);
            })
            .fail(function (xhr) {
                var message = getAjaxErrorMessage(xhr, "Failed to submit your decision.");

                if (xhr.status === 401 || xhr.status === 403 || xhr.status === 302 || xhr.status === 0) {
                    message = "Only a signed-in Finance Officer can approve or reject payroll changes.";
                }

                window.FliqToast.error(message);
            })
            .always(function () {
                $buttons.prop("disabled", false);
            });
    }

    function statusBadge(status) {
        var classes = {
            Pending: "bg-warning text-dark",
            Approved: "bg-success",
            Rejected: "bg-danger",
            Superseded: "bg-secondary"
        };

        return '<span class="badge ' + (classes[status] || "bg-secondary") + '">' + escapeHtml(status) + "</span>";
    }

    function openAuditModal() {
        if (!currentPeriodId) {
            return;
        }

        var $body = $("#payroll-v2-audit-body").html(
            '<tr><td colspan="6" class="text-center text-muted py-3">Loading...</td></tr>');
        bootstrap.Modal.getOrCreateInstance(document.getElementById("payroll-v2-audit-modal")).show();

        $.getJSON(api.auditTrail(currentPeriodId))
            .done(function (response) {
                var requests = (response && response.Data) || [];
                $body.empty();

                if (requests.length === 0) {
                    $body.append('<tr><td colspan="6" class="text-center text-muted py-3">No changes have been made to this payroll since it was generated.</td></tr>');
                    return;
                }

                requests.forEach(function (request) {
                    var changes = (request.Changes || []).map(function (change) {
                        var isText = change.Field === "PaymentMethod";
                        var from = isText ? escapeHtml(change.OldValue) : formatAmount(change.OldValue);
                        var to = isText ? escapeHtml(change.NewValue) : formatAmount(change.NewValue);
                        return "<li>" + escapeHtml(change.Label) + ": " + from + " &rarr; <strong>" + to + "</strong></li>";
                    }).join("");

                    var reviewed = request.ReviewedBy
                        ? escapeHtml(request.ReviewedBy) + "<br /><span class=\"text-muted small\">" +
                          escapeHtml(formatTimestamp(request.ReviewedAt)) + "</span>" +
                          (request.ReviewNote ? "<br /><em class=\"small\">" + escapeHtml(request.ReviewNote) + "</em>" : "")
                        : '<span class="text-muted">&mdash;</span>';

                    $body.append(
                        "<tr><td class=\"text-nowrap\">" + escapeHtml(formatTimestamp(request.RequestedAt)) + "</td>" +
                        "<td>" + escapeHtml(request.EmployeeCode) + "<br />" + escapeHtml(request.EmployeeName) + "</td>" +
                        '<td><ul class="audit-change-list">' + changes + "</ul></td>" +
                        "<td>" + escapeHtml(request.RequestedBy) + "</td>" +
                        "<td>" + statusBadge(request.StatusName) + "</td>" +
                        "<td>" + reviewed + "</td></tr>");
                });
            })
            .fail(function () {
                $body.empty();
                window.FliqToast.error("Failed to load the audit trail.");
            });
    }

    function bindApproval() {
        $("#payroll-v2-body").on("click", ".payroll-v2-review-btn", function () {
            var $row = $(this).closest("tr");
            var changeId = parseInt($row.attr("data-change-id"), 10);

            if (changeId) {
                openReviewModal(changeId);
                return;
            }

            // The edit has not been saved yet: save now, then show the change.
            runAutoSave(function () {
                var savedId = parseInt($row.attr("data-change-id"), 10);
                if (savedId) {
                    openReviewModal(savedId);
                }
            });
        });
        $("#payroll-v2-approve-btn").on("click", function () { submitReview(true); });
        $("#payroll-v2-reject-btn").on("click", function () { submitReview(false); });
        $("#payroll-v2-audit-btn").on("click", openAuditModal);
    }

    function bindPeriodControls() {
        setDefaultDates();
        $("#payroll-v2-generate-btn").on("click", function () { loadPayroll(false); });
        $("#payroll-v2-save-btn").on("click", function () { savePayrollPeriod(false); });
        $("#payroll-v2-from-date, #payroll-v2-to-date").on("change", function () {
            setExportVisible(false);
            hideSaveSuccess();
        });
    }

    $(function () {
        bindValidation();
        bindPeriodControls();
        bindAbsentDaysLinks();
        bindApproval();
        initEmployeeFilter();
        loadEmployees();
    });
})(jQuery);
