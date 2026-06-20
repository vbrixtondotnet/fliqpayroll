(function ($) {
    "use strict";

    const api = {
        list: "/api/employees",
        departments: "/api/employees/departments",
        get: function (id) { return "/api/employees/" + id; },
        create: "/api/employees",
        update: function (id) { return "/api/employees/" + id; },
        remove: function (id) { return "/api/employees/" + id; }
    };

    let modal;

    function formatCurrency(value) {
        return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(value || 0);
    }

    function showError(message) {
        $("#employees-success").addClass("d-none");
        $("#employees-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#employees-alert").addClass("d-none");
        $("#employees-success").removeClass("d-none").text(message);
    }

    function formatDateInput(dateValue) {
        if (!dateValue) return "";
        const date = new Date(dateValue);
        return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
    }

    function valOrNull(selector) {
        const v = $(selector).val();
        return v === "" || v === null ? null : v;
    }

    function numVal(selector) {
        const v = $(selector).val();
        return v === "" ? 0 : Number(v);
    }

    function enumVal(selector) {
        const v = $(selector).val();
        return v === "" ? null : Number(v);
    }

    function getFilters() {
        const filters = {};
        const search = $("#employee-filter-search").val();
        const dept = $("#employee-filter-department").val();
        const active = $("#employee-filter-status").val();
        if (search) filters.Search = search;
        if (dept) filters.Department = dept;
        if (active !== "") filters.IsActive = active === "true";
        return filters;
    }

    function renderRows(employees) {
        const $body = $("#employees-body");
        $body.empty();

        if (!employees || employees.length === 0) {
            $body.append('<tr><td colspan="7" class="text-center text-muted py-4">No employees found.</td></tr>');
            return;
        }

        employees.forEach(function (employee) {
            const statusBadge = employee.IsActive
                ? '<span class="badge text-bg-success">Active</span>'
                : '<span class="badge text-bg-secondary">Inactive</span>';

            $body.append([
                "<tr>",
                "<td>" + employee.EmployeeCode + "</td>",
                "<td>" + employee.FullName + "</td>",
                "<td>" + (employee.Department || "—") + "</td>",
                "<td>" + (employee.Position || "—") + "</td>",
                "<td>" + formatCurrency(employee.BasicSalary) + "</td>",
                "<td>" + statusBadge + "</td>",
                '<td class="text-end">',
                '<button type="button" class="btn btn-sm btn-outline-primary me-1 employee-edit-btn" data-id="' + employee.Id + '">Edit</button>',
                '<button type="button" class="btn btn-sm btn-outline-danger employee-delete-btn" data-id="' + employee.Id + '">Delete</button>',
                "</td>",
                "</tr>"
            ].join(""));
        });
    }

    function loadDepartments() {
        $.getJSON(api.departments)
            .done(function (response) {
                if (!response || !response.Success) return;
                const $sel = $("#employee-filter-department");
                $sel.find("option:not(:first)").remove();
                (response.Data || []).forEach(function (dept) {
                    $sel.append('<option value="' + dept + '">' + dept + "</option>");
                });
            });
    }

    function loadEmployees() {
        $.getJSON(api.list, getFilters())
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load employees.");
                    return;
                }
                renderRows(response.Data);
            })
            .fail(function () { showError("Failed to load employees."); });
    }

    function resetForm() {
        $("#employee-id").val("");
        $("#employee-form")[0].reset();
        $("#employee-active").prop("checked", true);
        $("#employee-employment-status").val("0");
        $("#employee-salary-type").val("0");
        $("#employee-payroll-frequency").val("0");
        $("#employee-hire-date").val(formatDateInput(new Date()));
        $("#employee-modal-label").text("Add Employee");
        $("#tab-personal").tab("show");
    }

    function fillForm(employee) {
        $("#employee-id").val(employee.Id);
        $("#employee-code").val(employee.EmployeeCode);
        $("#employee-last-name").val(employee.LastName);
        $("#employee-first-name").val(employee.FirstName);
        $("#employee-middle-name").val(employee.MiddleName || "");
        $("#employee-gender").val(employee.Gender != null ? employee.Gender : "");
        $("#employee-civil-status").val(employee.CivilStatus != null ? employee.CivilStatus : "");
        $("#employee-dob").val(formatDateInput(employee.DateOfBirth));
        $("#employee-nationality").val(employee.Nationality || "");
        $("#employee-religion").val(employee.Religion || "");
        $("#employee-mobile").val(employee.MobileNumber || "");
        $("#employee-email").val(employee.Email || "");
        $("#employee-address").val(employee.HomeAddress || "");
        $("#employee-emergency-person").val(employee.EmergencyContactPerson || "");
        $("#employee-emergency-number").val(employee.EmergencyContactNumber || "");
        $("#employee-emergency-relationship").val(employee.EmergencyContactRelationship || "");
        $("#employee-employment-status").val(employee.EmploymentStatus);
        $("#employee-department").val(employee.Department || "");
        $("#employee-position").val(employee.Position || "");
        $("#employee-supervisor").val(employee.Supervisor || "");
        $("#employee-hire-date").val(formatDateInput(employee.HireDate));
        $("#employee-regularized").val(formatDateInput(employee.DateRegularized));
        $("#employee-salary-type").val(employee.SalaryType);
        $("#employee-basic-salary").val(employee.BasicSalary);
        $("#employee-payroll-frequency").val(employee.PayrollFrequency);
        $("#employee-bank-name").val(employee.BankName || "");
        $("#employee-bank-account").val(employee.BankAccountNumber || "");
        $("#employee-tin").val(employee.TinNumber || "");
        $("#employee-sss-number").val(employee.SssNumber || "");
        $("#employee-philhealth-number").val(employee.PhilHealthNumber || "");
        $("#employee-pagibig-number").val(employee.PagIbigNumber || "");
        $("#employee-sss-er").val(employee.SssErShare);
        $("#employee-sss-ee").val(employee.SssEeShare);
        $("#employee-sss-loan").val(employee.SssLoan);
        $("#employee-philhealth-er").val(employee.PhilHealthErShare);
        $("#employee-philhealth-ee").val(employee.PhilHealthEeShare);
        $("#employee-pagibig-er").val(employee.PagIbigErShare);
        $("#employee-pagibig-ee").val(employee.PagIbigEeShare);
        $("#employee-pagibig-loan").val(employee.PagIbigLoan);
        $("#employee-active").prop("checked", employee.IsActive);
    }

    function buildPayload() {
        return {
            EmployeeCode: $("#employee-code").val(),
            LastName: $("#employee-last-name").val(),
            FirstName: $("#employee-first-name").val(),
            MiddleName: valOrNull("#employee-middle-name"),
            Gender: enumVal("#employee-gender"),
            CivilStatus: enumVal("#employee-civil-status"),
            DateOfBirth: valOrNull("#employee-dob") || null,
            Nationality: valOrNull("#employee-nationality"),
            Religion: valOrNull("#employee-religion"),
            MobileNumber: valOrNull("#employee-mobile"),
            Email: valOrNull("#employee-email"),
            HomeAddress: valOrNull("#employee-address"),
            EmergencyContactPerson: valOrNull("#employee-emergency-person"),
            EmergencyContactNumber: valOrNull("#employee-emergency-number"),
            EmergencyContactRelationship: valOrNull("#employee-emergency-relationship"),
            EmploymentStatus: Number($("#employee-employment-status").val()),
            Department: valOrNull("#employee-department"),
            Position: valOrNull("#employee-position"),
            Supervisor: valOrNull("#employee-supervisor"),
            HireDate: $("#employee-hire-date").val(),
            DateRegularized: valOrNull("#employee-regularized") || null,
            SalaryType: Number($("#employee-salary-type").val()),
            BasicSalary: numVal("#employee-basic-salary"),
            PayrollFrequency: Number($("#employee-payroll-frequency").val()),
            BankName: valOrNull("#employee-bank-name"),
            BankAccountNumber: valOrNull("#employee-bank-account"),
            TinNumber: valOrNull("#employee-tin"),
            SssNumber: valOrNull("#employee-sss-number"),
            SssErShare: numVal("#employee-sss-er"),
            SssEeShare: numVal("#employee-sss-ee"),
            SssLoan: numVal("#employee-sss-loan"),
            PhilHealthNumber: valOrNull("#employee-philhealth-number"),
            PhilHealthErShare: numVal("#employee-philhealth-er"),
            PhilHealthEeShare: numVal("#employee-philhealth-ee"),
            PagIbigNumber: valOrNull("#employee-pagibig-number"),
            PagIbigErShare: numVal("#employee-pagibig-er"),
            PagIbigEeShare: numVal("#employee-pagibig-ee"),
            PagIbigLoan: numVal("#employee-pagibig-loan"),
            IsActive: $("#employee-active").is(":checked")
        };
    }

    function openCreateModal() {
        resetForm();
        modal.show();
    }

    function openEditModal(id) {
        $.getJSON(api.get(id))
            .done(function (response) {
                if (!response || !response.Success || !response.Data) {
                    showError((response && response.Message) || "Unable to load employee.");
                    return;
                }
                fillForm(response.Data);
                $("#employee-modal-label").text("Edit Employee");
                modal.show();
            })
            .fail(function () { showError("Failed to load employee."); });
    }

    function saveEmployee() {
        const id = $("#employee-id").val();
        const payload = buildPayload();
        const isEdit = Boolean(id);

        $.ajax({
            url: isEdit ? api.update(id) : api.create,
            method: isEdit ? "PUT" : "POST",
            contentType: "application/json",
            data: JSON.stringify(isEdit ? Object.assign({ Id: Number(id) }, payload) : payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to save employee.");
                    return;
                }
                modal.hide();
                showSuccess(response.Message || "Employee saved.");
                loadEmployees();
                loadDepartments();
            })
            .fail(function () { showError("Failed to save employee."); });
    }

    function deleteEmployee(id) {
        if (!window.confirm("Delete this employee?")) return;

        $.ajax({ url: api.remove(id), method: "DELETE" })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to delete employee.");
                    return;
                }
                showSuccess(response.Message || "Employee deleted.");
                loadEmployees();
            })
            .fail(function () { showError("Failed to delete employee."); });
    }

    $(function () {
        modal = new bootstrap.Modal(document.getElementById("employee-modal"));
        loadDepartments();
        loadEmployees();

        $("#employee-add-btn").on("click", openCreateModal);
        $("#employee-save-btn").on("click", saveEmployee);
        $("#employee-filter-btn").on("click", loadEmployees);
        $("#employee-filter-reset-btn").on("click", function () {
            $("#employee-filter-search").val("");
            $("#employee-filter-department").val("");
            $("#employee-filter-status").val("");
            loadEmployees();
        });

        $("#employees-body").on("click", ".employee-edit-btn", function () {
            openEditModal($(this).data("id"));
        });

        $("#employees-body").on("click", ".employee-delete-btn", function () {
            deleteEmployee($(this).data("id"));
        });
    });
})(jQuery);
