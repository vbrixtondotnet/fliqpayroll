(function ($) {
    "use strict";

    const api = {
        getByDateRange: "/api/payroll/getByDateRange",
        defaultPeriod: "/api/payroll/defaultPeriod",
        savePeriod: "/api/payroll/savePeriod"
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

    function recalculateRow($row) {
        if ($row.length === 0 || $row.attr("id") === "payroll-v2-empty-row") {
            return;
        }

        var salaryType = parseInt($row.data("salary-type"), 10);
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
        var specialOtPay = roundMoney(
            getFieldValue($row, "specialOtHours") * hourlyRate * getFieldValue($row, "specialOtRate"));
        var holidayOtPay = roundMoney(
            getFieldValue($row, "holidayDays") * dailyRate * getFieldValue($row, "holidayOtRate"));
        var nightDiffOtPay = roundMoney(
            getFieldValue($row, "nightDiffHours") * hourlyRate * getFieldValue($row, "nightDiffOtRate"));
        var leavePay = roundMoney(getFieldValue($row, "leaveDays") * dailyRate);
        var lateUndertimeAmount = roundMoney(getFieldValue($row, "lateUndertimeHours") * hourlyRate);

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
            recalculateRow($(this).closest("tr"));
        });

        $("#payroll-v2-table").on("keyup input", ".payroll-v2-input", function () {
            recalculateRow($(this).closest("tr"));
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

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function getDateRange() {
        return {
            fromDate: $("#payroll-v2-from-date").val(),
            toDate: $("#payroll-v2-to-date").val()
        };
    }

    function showError(message) {
        $("#payroll-v2-alert").removeClass("d-none").text(message);
    }

    function hideError() {
        $("#payroll-v2-alert").addClass("d-none").text("");
    }

    function hideSaveSuccess() {
        $("#payroll-v2-save-success").addClass("d-none").text("");
    }

    function showSaveSuccess(message) {
        $("#payroll-v2-save-success").removeClass("d-none").text(message);
    }

    function setSaveButtonVisible(isVisible) {
        $("#payroll-v2-save-btn").toggleClass("d-none", !isVisible);
    }

    function setSaving(isSaving) {
        $("#payroll-v2-save-btn").prop("disabled", isSaving);
    }

    function setLoading(isLoading) {
        $("#payroll-v2-loading").toggleClass("d-none", !isLoading);
        $("#payroll-v2-from-date, #payroll-v2-to-date, #payroll-v2-generate-btn").prop("disabled", isLoading);
    }

    function updatePeriodInfo(periodName) {
        if (!periodName) {
            $("#payroll-v2-period-info").addClass("d-none");
            return;
        }

        $("#payroll-v2-period-info")
            .removeClass("d-none alert-warning")
            .addClass("alert-light")
            .text("Showing calculated payroll for " + periodName + ".");
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

    function renderRow(record) {
        var isDaily = record.SalaryType === 1;
        var dailyRate = isDaily ? record.BasicSalary : record.DailyRate;

        return [
            '<tr data-employee-id="', record.EmployeeId,
            '" data-employee-code="', escapeHtml(record.EmployeeCode),
            '" data-basic-salary="', record.BasicSalary,
            '" data-position="', escapeHtml(record.Position || ""),
            '" data-salary-type="', record.SalaryType,
            '" data-hourly-rate="', record.HourlyRate,
            '" data-daily-rate="', dailyRate,
            '" data-base-gross="', record.GrossSalary, '">',
            '<td class="payroll-v2-sticky-col payroll-v2-sticky-col-1">', escapeHtml(record.EmployeeName), "</td>",
            '<td class="payroll-v2-sticky-col payroll-v2-sticky-col-2 text-center ', getSalaryTypeClass(record.SalaryType), '">', escapeHtml(formatSalaryType(record.SalaryType)), "</td>",
            readonlyMoney(isDaily ? null : record.MonthlySalary, "", "payroll-v2-sticky-col-3", "monthlySalary"),
            readonlyMoney(isDaily ? null : record.BiMonthlySalary, "", "payroll-v2-sticky-col-4", "biMonthlySalary"),
            readonlyMoney(dailyRate, "", "payroll-v2-sticky-col-5", "dailyRate"),
            readonlyMoney(record.HourlyRate, "", "payroll-v2-sticky-col-6", "hourlyRate"),
            editableInput(record.WorkingDays, "number", "workingDays"),
            editableInput(record.AbsentDays, "number", "absentDays"),
            readonlyMoney(record.AbsentAmount, "", "", "absentAmount"),
            readonlyMoney(record.GrossSalary, "fw-semibold", "", "grossSalary"),
            editableInput(record.RegularOtRate, "rate", "regularOtRate"),
            editableInput(record.RegularOtHours, "number", "regularOtHours"),
            readonlyMoney(record.OvertimePay, "", "", "regularOtPay"),
            editableInput(record.SpecialOtRate, "rate", "specialOtRate"),
            editableInput(record.SpecialOtHours, "number", "specialOtHours"),
            readonlyMoney(record.SpecialOtPay, "", "", "specialOtPay"),
            editableInput(record.HolidayOtRate, "rate", "holidayOtRate"),
            editableInput(record.HolidayDays, "number", "holidayDays"),
            readonlyMoney(record.HolidayOtPay, "", "", "holidayOtPay"),
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
        var grossSalary = getFieldValue($row, "grossSalary");
        var regularOtPay = getFieldValue($row, "regularOtPay");
        var specialOtPay = getFieldValue($row, "specialOtPay");
        var holidayOtPay = getFieldValue($row, "holidayOtPay");
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
            EmployeeName: $row.find(".payroll-v2-sticky-col-1").text().trim(),
            EmployeeCode: getDataAttr($row, "employee-code"),
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
            SpecialOtHours: getFieldValue($row, "specialOtHours"),
            SpecialOtPay: specialOtPay,
            HolidayOtRate: getFieldValue($row, "holidayOtRate"),
            HolidayDays: getFieldValue($row, "holidayDays"),
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
            FromDate: $("#payroll-v2-from-date").val(),
            ToDate: $("#payroll-v2-to-date").val(),
            PeriodName: currentPeriodName || $("#payroll-v2-period-label").text(),
            Records: records
        };
    }

    function renderRows(records) {
        var $body = $("#payroll-v2-body");
        $body.empty();
        payrollGenerated = false;
        setSaveButtonVisible(false);

        if (!records || records.length === 0) {
            $body.append('<tr><td colspan="36" class="text-center text-muted py-4">No payroll records for this period.</td></tr>');
            return;
        }

        records.forEach(function (record) {
            $body.append(renderRow(record));
        });

        payrollGenerated = true;
        setSaveButtonVisible(true);
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

    function loadPayroll() {
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
        hideSaveSuccess();
        setLoading(true);
        setSaveButtonVisible(false);
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
                $("#payroll-v2-period-label").text(periodName);
                updatePeriodInfo(periodName);
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

    function savePayrollPeriod() {
        if (!payrollGenerated) {
            showError("Generate payroll before saving.");
            return;
        }

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

                showSaveSuccess(response.Message || "Payroll period saved successfully.");
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

    function bindPeriodControls() {
        setDefaultDates();
        $("#payroll-v2-generate-btn").on("click", loadPayroll);
        $("#payroll-v2-save-btn").on("click", savePayrollPeriod);
    }

    $(function () {
        bindValidation();
        bindPeriodControls();
    });
})(jQuery);
