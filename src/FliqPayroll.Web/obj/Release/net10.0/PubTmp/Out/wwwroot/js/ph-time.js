(function (window) {
    "use strict";

    var TIME_ZONE = "Asia/Manila";

    function formatDateKey(date) {
        return new Intl.DateTimeFormat("en-CA", { timeZone: TIME_ZONE }).format(date);
    }

    function parseDateKey(value) {
        if (!value) {
            return todayKey();
        }

        if (typeof value === "string") {
            var match = value.match(/^(\d{4}-\d{2}-\d{2})/);
            if (match) {
                return match[1];
            }
        }

        return formatDateKey(new Date(value));
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
        parseDateKey: parseDateKey,
        todayKey: todayKey,
        now: now,
        startOfMonth: startOfMonth,
        partsFromKey: partsFromKey,
        formatDisplayDate: formatDisplayDate
    };
})(window);
