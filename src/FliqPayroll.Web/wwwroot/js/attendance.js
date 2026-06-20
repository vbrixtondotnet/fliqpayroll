(function ($) {
    "use strict";

    const api = {
        list: "/api/attendance",
        upload: "/api/attendance/upload",
        update: function (id) { return "/api/attendance/" + id; }
    };

    function showError(message) {
        $("#attendance-success").addClass("d-none");
        $("#attendance-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#attendance-alert").addClass("d-none");
        $("#attendance-success").removeClass("d-none").text(message);
    }

    function formatDateInput(date) {
        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
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

    function renderRows(records) {
        const $body = $("#attendance-body");
        $body.empty();

        if (!records || records.length === 0) {
            $body.append('<tr><td colspan="10" class="text-center text-muted py-4">No attendance records for this date.</td></tr>');
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
        const date = $("#attendance-date").val();
        $.getJSON(api.list, { date: date })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load attendance.");
                    return;
                }
                renderRows(response.Data);
            })
            .fail(function () { showError("Failed to load attendance."); });
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
        $("#attendance-date").val(PhTime.todayKey());
        loadAttendance();

        $("#attendance-load-btn").on("click", loadAttendance);
        $("#attendance-upload-form").on("submit", uploadCsv);

        $("#attendance-body").on("click", ".attendance-save-btn", function () {
            saveRow($(this).closest("tr"));
        });
    });
})(jQuery);
