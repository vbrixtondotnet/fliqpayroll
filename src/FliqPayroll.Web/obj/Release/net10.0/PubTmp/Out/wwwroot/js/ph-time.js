(function (window) {
    "use strict";

    var TIME_ZONE = "Asia/Manila";

    function formatDateKeyFromParts(year, month, day) {
        return year + "-" + String(month).padStart(2, "0") + "-" + String(day).padStart(2, "0");
    }

    function formatDateKey(date) {
        return new Intl.DateTimeFormat("en-CA", { timeZone: TIME_ZONE }).format(date);
    }

    function parseSlashDate(value) {
        var match = String(value || "").trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
        if (!match) {
            return null;
        }

        var day = parseInt(match[1], 10);
        var month = parseInt(match[2], 10);
        var year = parseInt(match[3], 10);

        if (month < 1 || month > 12 || day < 1 || day > 31) {
            return null;
        }

        return formatDateKeyFromParts(year, month, day);
    }

    function parseDateKey(value) {
        if (!value) {
            return todayKey();
        }

        if (typeof value === "string") {
            var trimmed = value.trim();
            var isoMatch = trimmed.match(/^(\d{4}-\d{2}-\d{2})/);
            if (isoMatch) {
                return isoMatch[1];
            }

            var slashDate = parseSlashDate(trimmed);
            if (slashDate) {
                return slashDate;
            }
        }

        return formatDateKey(new Date(value));
    }

    function dateFromInput(input) {
        if (!input) {
            return todayKey();
        }

        if (input.type === "date" && input.value) {
            var isoMatch = input.value.match(/^(\d{4}-\d{2}-\d{2})/);
            if (isoMatch) {
                return isoMatch[1];
            }
        }

        return parseDateKey(input.value);
    }

    function todayKey() {
        return formatDateKey(new Date());
    }

    function now() {
        return new Date(new Date().toLocaleString("en-US", { timeZone: TIME_ZONE }));
    }

    function startOfMonth(reference) {
        var ref = reference || now();
        return new Date(ref.getFullYear(), ref.getMonth(), 1);
    }

    function partsFromKey(dateKey) {
        var segments = dateKey.split("-");
        return {
            year: parseInt(segments[0], 10),
            month: parseInt(segments[1], 10) - 1,
            day: parseInt(segments[2], 10)
        };
    }

    function formatDisplayDate(dateKey) {
        var parts = partsFromKey(dateKey);
        var date = new Date(parts.year, parts.month, parts.day);
        return date.toLocaleDateString("en-PH", {
            weekday: "long",
            year: "numeric",
            month: "long",
            day: "numeric"
        });
    }

    window.PhTime = {
        TIME_ZONE: TIME_ZONE,
        formatDateKey: formatDateKey,
        formatDateKeyFromParts: formatDateKeyFromParts,
        parseDateKey: parseDateKey,
        dateFromInput: dateFromInput,
        todayKey: todayKey,
        now: now,
        startOfMonth: startOfMonth,
        partsFromKey: partsFromKey,
        formatDisplayDate: formatDisplayDate
    };
})(window);
