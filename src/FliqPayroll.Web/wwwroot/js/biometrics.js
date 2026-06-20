(function ($) {
    "use strict";

    const api = {
        upload: "/api/biometrics/upload",
        summary: "/api/biometrics/summary"
    };

    function showError(message) {
        $("#biometrics-success").addClass("d-none");
        $("#biometrics-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#biometrics-alert").addClass("d-none");
        $("#biometrics-success").removeClass("d-none").text(message);
    }

    function formatDateInput(date) {
        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
    }

    function setDefaultDates() {
        const today = new Date();
        const start = new Date(today.getFullYear(), today.getMonth(), 1);
        $("#biometrics-summary-start").val(formatDateInput(start));
        $("#biometrics-summary-end").val(formatDateInput(today));
    }

    function renderSummary(data) {
        $("#bio-total-employees").text(data.TotalEmployees);
        $("#bio-valid-days").text(data.ValidAttendanceDays);
        $("#bio-late-days").text(data.LateDays);
        $("#bio-incomplete-days").text(data.IncompleteDays);
        $("#bio-overtime-hours").text(data.TotalOvertimeHours);
    }

    function loadSummary() {
        const startDate = $("#biometrics-summary-start").val();
        const endDate = $("#biometrics-summary-end").val();

        $.getJSON(api.summary, { startDate: startDate, endDate: endDate })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load summary.");
                    return;
                }
                renderSummary(response.Data);
            })
            .fail(function () { showError("Failed to load attendance summary."); });
    }

    function renderUploadResult(result) {
        const $container = $("#biometrics-upload-result");
        const $details = $("#biometrics-upload-details");
        $details.empty();
        $details.append("<li>File: " + result.FileName + "</li>");
        $details.append("<li>Total rows: " + result.TotalRows + "</li>");
        $details.append("<li>Processed days: " + result.ProcessedDays + "</li>");
        $details.append("<li>Skipped incomplete: " + result.SkippedIncomplete + "</li>");
        $details.append("<li>Unmatched rows: " + result.UnmatchedRows + "</li>");
        if (result.Errors && result.Errors.length > 0) {
            $details.append("<li>Errors: " + result.Errors.length + "</li>");
            result.Errors.slice(0, 5).forEach(function (err) {
                $details.append('<li class="text-danger">' + err + "</li>");
            });
        }
        $container.removeClass("d-none");
    }

    function uploadFile(e) {
        e.preventDefault();

        const fileInput = document.getElementById("biometrics-file");
        if (!fileInput.files || fileInput.files.length === 0) {
            showError("Please select a file.");
            return;
        }

        const formData = new FormData();
        formData.append("file", fileInput.files[0]);

        $("#biometrics-upload-btn").prop("disabled", true).text("Uploading...");

        $.ajax({
            url: api.upload,
            method: "POST",
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Upload failed.");
                    return;
                }
                showSuccess(response.Message || "File processed.");
                renderUploadResult(response.Data);
                loadSummary();
            })
            .fail(function () { showError("Failed to upload biometric file."); })
            .always(function () {
                $("#biometrics-upload-btn").prop("disabled", false).text("Upload & Process");
            });
    }

    $(function () {
        setDefaultDates();
        loadSummary();

        $("#biometrics-upload-form").on("submit", uploadFile);
        $("#biometrics-summary-btn").on("click", loadSummary);
    });
})(jQuery);
