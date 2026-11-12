(function ($) {
    "use strict";

    const api = {
        getAll: "/api/shifts/getAll",
        employees: "/api/shifts/employees",
        update: function (id) { return "/api/shifts/update/" + id; },
        setEmployees: function (id) { return "/api/shifts/" + id + "/employees"; }
    };

    let shifts = [];
    let allEmployees = [];
    let activeShiftId = null;
    let editModal;
    let assignModal;

    function esc(value) {
        return $("<div>").text(value == null ? "" : String(value)).html();
    }

    function showError(message) {
        window.FliqToast.error(message);
    }

    function showSuccess(message) {
        window.FliqToast.success(message);
    }

    function formatTime(hhmm) {
        const parts = String(hhmm || "").split(":");
        const hours = Number(parts[0]);
        const minutes = parts[1] || "00";
        if (isNaN(hours)) { return ""; }
        const suffix = hours >= 12 ? "PM" : "AM";
        return ((hours % 12) || 12) + ":" + minutes + " " + suffix;
    }

    function timeRange(shift) {
        const overnight = shift.EndTime <= shift.StartTime;
        return formatTime(shift.StartTime) + " – " + formatTime(shift.EndTime) + (overnight ? " (next day)" : "");
    }

    function findShift(id) {
        return shifts.find(function (s) { return s.Id === id; });
    }

    function render() {
        const $tabs = $("#shift-tabs").empty();
        const $content = $("#shift-tab-content").empty();

        if (!shifts.length) {
            $content.html('<div class="text-muted">No shifts found.</div>');
            return;
        }

        if (!findShift(activeShiftId)) {
            activeShiftId = shifts[0].Id;
        }

        shifts.forEach(function (shift) {
            const active = shift.Id === activeShiftId;
            $tabs.append(
                '<li class="nav-item" role="presentation">' +
                '<button class="nav-link' + (active ? " active" : "") + '" type="button" role="tab" data-shift-id="' + shift.Id + '">' +
                esc(shift.Name) + ' <span class="badge text-bg-secondary ms-1">' + shift.Employees.length + "</span></button></li>"
            );

            const rows = shift.Employees.map(function (e) {
                return "<tr>" +
                    "<td>" + esc(e.EmployeeCode) + "</td>" +
                    "<td>" + esc(e.FullName) + "</td>" +
                    "<td>" + esc(e.Department || "—") + "</td>" +
                    "<td>" + esc(e.Position || "—") + "</td>" +
                    '<td class="text-end"><button type="button" class="btn btn-outline-danger btn-sm shift-remove" data-shift-id="' + shift.Id +
                    '" data-employee-id="' + e.Id + '">Remove</button></td>' +
                    "</tr>";
            }).join("");

            const table = shift.Employees.length
                ? '<div class="table-responsive"><table class="table table-hover align-middle mb-0">' +
                  "<thead><tr><th>Code</th><th>Employee</th><th>Department</th><th>Position</th><th></th></tr></thead>" +
                  "<tbody>" + rows + "</tbody></table></div>"
                : '<div class="text-muted py-4 text-center">No employees assigned to this shift yet.</div>';

            $content.append(
                '<div class="tab-pane fade' + (active ? " show active" : "") + '" role="tabpanel">' +
                '<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">' +
                "<div><h2 class=\"h5 mb-1\">" + esc(shift.Name) + "</h2>" +
                '<div class="text-muted">' + esc(timeRange(shift)) + " &middot; " + shift.Employees.length + " employee" + (shift.Employees.length === 1 ? "" : "s") + "</div></div>" +
                '<div class="d-flex gap-2">' +
                '<button type="button" class="btn btn-outline-secondary shift-edit" data-shift-id="' + shift.Id + '">Edit Shift</button>' +
                '<button type="button" class="btn btn-primary shift-assign" data-shift-id="' + shift.Id + '">Assign Employees</button>' +
                "</div></div>" + table + "</div>"
            );
        });
    }

    function load() {
        return $.when($.getJSON(api.getAll), $.getJSON(api.employees))
            .done(function (shiftRes, empRes) {
                const s = shiftRes[0];
                const e = empRes[0];
                if (!s || !s.Success || !e || !e.Success) {
                    showError((s && s.Message) || (e && e.Message) || "Unable to load shifts.");
                    return;
                }
                shifts = s.Data || [];
                allEmployees = e.Data || [];
                render();
            })
            .fail(function () {
                showError("Failed to load shifts.");
            });
    }

    function putJson(url, body) {
        return $.ajax({
            url: url,
            method: "PUT",
            contentType: "application/json",
            data: JSON.stringify(body)
        });
    }

    function failMessage(xhr, fallback) {
        return (xhr.responseJSON && xhr.responseJSON.Message) || fallback;
    }

    // ---- Edit shift ----
    function openEdit(id) {
        const shift = findShift(id);
        if (!shift) { return; }
        $("#shift-edit-id").val(shift.Id);
        $("#shift-edit-name").val(shift.Name);
        $("#shift-edit-start").val(shift.StartTime);
        $("#shift-edit-end").val(shift.EndTime);
        editModal.show();
    }

    function saveEdit() {
        const id = Number($("#shift-edit-id").val());
        const payload = {
            Name: $("#shift-edit-name").val().trim(),
            StartTime: $("#shift-edit-start").val(),
            EndTime: $("#shift-edit-end").val()
        };

        if (!payload.Name || !payload.StartTime || !payload.EndTime) {
            window.FliqToast.error("Name, start time and end time are required.");
            return;
        }

        putJson(api.update(id), payload)
            .done(function (res) {
                if (!res.Success) {
                    window.FliqToast.error(res.Message || "Unable to save shift.");
                    return;
                }
                editModal.hide();
                activeShiftId = id;
                load().done(function () { showSuccess("Shift updated."); });
            })
            .fail(function (xhr) {
                window.FliqToast.error(failMessage(xhr, "Unable to save shift."));
            });
    }

    // ---- Assign employees ----
    // The checkboxes are rebuilt when searching, so selection is tracked in a set.
    let selectedIds = new Set();

    function updateAssignCount() {
        $("#shift-assign-count").text(selectedIds.size + " selected");
    }

    function renderAssign() {
        const id = Number($("#shift-assign-id").val());
        const term = $("#shift-assign-search").val().trim().toLowerCase();
        const $list = $("#shift-assign-list").empty();

        allEmployees.forEach(function (emp) {
            const haystack = (emp.FullName + " " + emp.EmployeeCode + " " + (emp.Department || "")).toLowerCase();
            if (term && haystack.indexOf(term) === -1) { return; }

            const other = emp.ShiftId && emp.ShiftId !== id ? findShift(emp.ShiftId) : null;
            $list.append(
                '<label class="list-group-item d-flex align-items-center gap-2">' +
                '<input class="form-check-input mt-0" type="checkbox" value="' + emp.Id + '"' + (selectedIds.has(emp.Id) ? " checked" : "") + " />" +
                '<span class="flex-grow-1">' + esc(emp.FullName) + ' <small class="text-muted">' + esc(emp.EmployeeCode) +
                (emp.Department ? " &middot; " + esc(emp.Department) : "") + "</small></span>" +
                (other ? '<span class="badge text-bg-warning">' + esc(other.Name) + "</span>" : "") +
                "</label>"
            );
        });

        if (!$list.children().length) {
            $list.append('<div class="text-muted p-3 text-center">No employees found.</div>');
        }
        updateAssignCount();
    }

    function openAssign(id) {
        const shift = findShift(id);
        if (!shift) { return; }
        $("#shift-assign-id").val(id);
        $("#shift-assign-title").text("Assign Employees – " + shift.Name);
        $("#shift-assign-search").val("");
        selectedIds = new Set(shift.Employees.map(function (e) { return e.Id; }));
        renderAssign();
        assignModal.show();
    }

    function saveAssign() {
        const id = Number($("#shift-assign-id").val());
        putJson(api.setEmployees(id), { EmployeeIds: Array.from(selectedIds) })
            .done(function (res) {
                if (!res.Success) {
                    window.FliqToast.error(res.Message || "Unable to save assignments.");
                    return;
                }
                assignModal.hide();
                activeShiftId = id;
                load().done(function () { showSuccess("Shift assignments saved."); });
            })
            .fail(function (xhr) {
                window.FliqToast.error(failMessage(xhr, "Unable to save assignments."));
            });
    }

    function removeEmployee(shiftId, employeeId) {
        const shift = findShift(shiftId);
        if (!shift) { return; }
        const ids = shift.Employees.map(function (e) { return e.Id; }).filter(function (i) { return i !== employeeId; });
        putJson(api.setEmployees(shiftId), { EmployeeIds: ids })
            .done(function (res) {
                if (!res.Success) { showError(res.Message || "Unable to remove employee."); return; }
                activeShiftId = shiftId;
                load().done(function () { showSuccess("Employee removed from shift."); });
            })
            .fail(function (xhr) {
                showError(failMessage(xhr, "Unable to remove employee."));
            });
    }

    $(function () {
        editModal = new bootstrap.Modal(document.getElementById("shift-edit-modal"));
        assignModal = new bootstrap.Modal(document.getElementById("shift-assign-modal"));

        $("#shift-tabs").on("click", "[data-shift-id]", function () {
            activeShiftId = Number($(this).data("shift-id"));
            render();
        });
        $("#shift-tab-content").on("click", ".shift-edit", function () { openEdit(Number($(this).data("shift-id"))); });
        $("#shift-tab-content").on("click", ".shift-assign", function () { openAssign(Number($(this).data("shift-id"))); });
        $("#shift-tab-content").on("click", ".shift-remove", function () {
            removeEmployee(Number($(this).data("shift-id")), Number($(this).data("employee-id")));
        });

        $("#shift-edit-save").on("click", saveEdit);
        $("#shift-assign-save").on("click", saveAssign);
        $("#shift-assign-search").on("input", renderAssign);
        $("#shift-assign-list").on("change", "input[type=checkbox]", function () {
            const id = Number($(this).val());
            if (this.checked) { selectedIds.add(id); } else { selectedIds.delete(id); }
            updateAssignCount();
        });

        load();
    });
})(jQuery);
