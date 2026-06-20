(function ($) {
    "use strict";

    const api = {
        getAll: "/api/holidays/getAll",
        getByDate: "/api/holidays/getByDate",
        add: "/api/holidays/add",
        update: function (id) { return "/api/holidays/update/" + id; },
        remove: function (id) { return "/api/holidays/delete/" + id; }
    };

    const holidayTypeLabels = {
        0: "Regular",
        1: "Special",
        Regular: "Regular",
        Special: "Special"
    };

    let currentMonth = PhTime.startOfMonth();
    let holidaysByDate = {};
    let addModal;
    let detailModal;

    function showError(message) {
        $("#holiday-success").addClass("d-none");
        $("#holiday-alert").removeClass("d-none").text(message);
    }

    function showSuccess(message) {
        $("#holiday-alert").addClass("d-none");
        $("#holiday-success").removeClass("d-none").text(message);
        window.setTimeout(function () {
            $("#holiday-success").addClass("d-none");
        }, 3000);
    }

    function holidayTypeLabel(type) {
        return holidayTypeLabels[type] || "Holiday";
    }

    function holidayEventClass(type) {
        const value = typeof type === "string" ? type : Number(type);
        return value === 1 ? "holiday-event holiday-event-special" : "holiday-event holiday-event-regular";
    }

    function updateMonthLabel() {
        const label = currentMonth.toLocaleDateString("en-PH", {
            month: "long",
            year: "numeric",
            timeZone: PhTime.TIME_ZONE
        });
        $("#holiday-month-label").text(label);
    }

    function buildHolidayMap(holidays) {
        holidaysByDate = {};
        (holidays || []).forEach(function (holiday) {
            holidaysByDate[PhTime.parseDateKey(holiday.Date)] = holiday;
        });
    }

    function renderCalendar() {
        updateMonthLabel();

        const year = currentMonth.getFullYear();
        const month = currentMonth.getMonth();
        const firstDay = new Date(year, month, 1);
        const startOffset = firstDay.getDay();
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const todayKey = PhTime.todayKey();

        const $grid = $("#holiday-calendar-grid");
        $grid.empty();

        for (let i = 0; i < startOffset; i++) {
            $grid.append('<div class="holiday-calendar-cell holiday-calendar-cell-empty"></div>');
        }

        for (let day = 1; day <= daysInMonth; day++) {
            const cellDate = new Date(year, month, day);
            const dateKey = PhTime.formatDateKey(cellDate);
            const holiday = holidaysByDate[dateKey];
            const isToday = dateKey === todayKey;
            const isSunday = cellDate.getDay() === 0;

            let eventHtml = "";
            if (holiday) {
                eventHtml = '<div class="' + holidayEventClass(holiday.HolidayType) + '">' +
                    '<span class="holiday-event-title">' + holiday.Description + "</span>" +
                    '<span class="badge badge-sm ' + (Number(holiday.HolidayType) === 1 ? "badge-orange" : "text-bg-danger") + '">' +
                    holidayTypeLabel(holiday.HolidayType) + " Holiday</span></div>";
            }

            $grid.append(
                '<button type="button" class="holiday-calendar-cell' +
                (isToday ? " holiday-calendar-cell-today" : "") +
                (isSunday ? " holiday-calendar-cell-sunday" : "") +
                '" data-date="' + dateKey + '">' +
                '<span class="holiday-calendar-day">' + day + "</span>" +
                eventHtml +
                "</button>"
            );
        }
    }

    function loadHolidays() {
        return $.getJSON(api.getAll)
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to load holidays.");
                    return;
                }

                buildHolidayMap(response.Data);
                renderCalendar();
            })
            .fail(function () {
                showError("Failed to load holidays.");
            });
    }

    function openAddModal(dateKey) {
        $("#holiday-add-date").val(dateKey);
        $("#holiday-add-date-label").text(PhTime.formatDisplayDate(dateKey));
        $("#holiday-add-description").val("");
        $("#holiday-add-type").val("0");
        addModal.show();
    }

    function openDetailModal(holiday) {
        const dateKey = PhTime.parseDateKey(holiday.Date);
        $("#holiday-detail-id").val(holiday.HolidayId);
        $("#holiday-detail-date-label").text(PhTime.formatDisplayDate(dateKey));
        $("#holiday-detail-description").val(holiday.Description);
        $("#holiday-detail-type").val(String(holiday.HolidayType));
        detailModal.show();
    }

    function saveHoliday() {
        const payload = {
            Date: $("#holiday-add-date").val(),
            Description: $("#holiday-add-description").val().trim(),
            HolidayType: Number($("#holiday-add-type").val())
        };

        if (!payload.Description) {
            showError("Description is required.");
            return;
        }

        $.ajax({
            url: api.add,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to save holiday.");
                    return;
                }

                addModal.hide();
                showSuccess(response.Message || "Holiday saved.");
                loadHolidays();
            })
            .fail(function () {
                showError("Failed to save holiday.");
            });
    }

    function updateHoliday() {
        const id = Number($("#holiday-detail-id").val());
        const payload = {
            Description: $("#holiday-detail-description").val().trim(),
            HolidayType: Number($("#holiday-detail-type").val())
        };

        if (!payload.Description) {
            showError("Description is required.");
            return;
        }

        $.ajax({
            url: api.update(id),
            method: "PUT",
            contentType: "application/json",
            data: JSON.stringify(payload)
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to update holiday.");
                    return;
                }

                detailModal.hide();
                showSuccess(response.Message || "Holiday updated.");
                loadHolidays();
            })
            .fail(function () {
                showError("Failed to update holiday.");
            });
    }

    function deleteHoliday() {
        const id = Number($("#holiday-detail-id").val());

        if (!window.confirm("Delete this holiday?")) {
            return;
        }

        $.ajax({
            url: api.remove(id),
            method: "DELETE"
        })
            .done(function (response) {
                if (!response || !response.Success) {
                    showError((response && response.Message) || "Unable to delete holiday.");
                    return;
                }

                detailModal.hide();
                showSuccess(response.Message || "Holiday deleted.");
                loadHolidays();
            })
            .fail(function () {
                showError("Failed to delete holiday.");
            });
    }

    $(function () {
        addModal = new bootstrap.Modal(document.getElementById("holiday-add-modal"));
        detailModal = new bootstrap.Modal(document.getElementById("holiday-detail-modal"));

        loadHolidays();

        $("#holiday-prev-month").on("click", function () {
            currentMonth.setMonth(currentMonth.getMonth() - 1);
            renderCalendar();
        });

        $("#holiday-next-month").on("click", function () {
            currentMonth.setMonth(currentMonth.getMonth() + 1);
            renderCalendar();
        });

        $("#holiday-today-btn").on("click", function () {
            currentMonth = PhTime.startOfMonth();
            renderCalendar();
        });

        $("#holiday-calendar-grid").on("click", ".holiday-calendar-cell:not(.holiday-calendar-cell-empty)", function () {
            const dateKey = $(this).data("date");
            const holiday = holidaysByDate[dateKey];

            if (holiday) {
                openDetailModal(holiday);
            } else {
                openAddModal(dateKey);
            }
        });

        $("#holiday-save-btn").on("click", saveHoliday);
        $("#holiday-update-btn").on("click", updateHoliday);
        $("#holiday-delete-btn").on("click", deleteHoliday);
    });
})(jQuery);
