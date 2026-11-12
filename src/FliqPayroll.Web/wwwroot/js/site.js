// FliqPayroll site scripts

// Toast notifications. Errors use FliqToast.error(message), confirmations FliqToast.success(message); the toast stacks at the top right, below the header,
// dismisses itself after a few seconds (longer for errors) (or with its close button) and never shows the same message twice at once.
window.FliqToast = (function () {
    "use strict";

    var MAX_VISIBLE = 4;
    var DELAY_MS = { error: 8000, success: 4500 };

    function container() {
        var el = document.getElementById("fliq-toast-container");
        if (!el) {
            el = document.createElement("div");
            el.id = "fliq-toast-container";
            el.className = "toast-container position-fixed end-0 p-3 fliq-toast-container";
            el.setAttribute("aria-live", "assertive");
            el.setAttribute("aria-atomic", "true");
            document.body.appendChild(el);
        }

        return el;
    }

    function show(message, kind) {
        message = String(message == null ? "" : message).trim();
        if (!message || !window.bootstrap || !bootstrap.Toast) {
            return;
        }

        var host = container();
        var existing = host.querySelectorAll(".toast");

        for (var i = 0; i < existing.length; i++) {
            if (existing[i].getAttribute("data-message") === message) {
                return; // already on screen
            }
        }

        // Keep the stack short: drop the oldest when there are too many.
        while (existing.length >= MAX_VISIBLE) {
            existing[0].remove();
            existing = host.querySelectorAll(".toast");
        }

        var toast = document.createElement("div");
        toast.className = "toast align-items-center border-0 fliq-toast fliq-toast-" + kind;
        toast.setAttribute("role", kind === "error" ? "alert" : "status");
        toast.setAttribute("data-message", message);

        var row = document.createElement("div");
        row.className = "d-flex";

        var body = document.createElement("div");
        body.className = "toast-body";
        body.textContent = message; // textContent: messages can contain user-entered text

        var close = document.createElement("button");
        close.type = "button";
        close.className = "btn-close btn-close-white me-2 m-auto";
        close.setAttribute("data-bs-dismiss", "toast");
        close.setAttribute("aria-label", "Close");

        row.appendChild(body);
        row.appendChild(close);
        toast.appendChild(row);
        host.appendChild(toast);

        toast.addEventListener("hidden.bs.toast", function () { toast.remove(); });
        new bootstrap.Toast(toast, { delay: DELAY_MS[kind] || DELAY_MS.error, autohide: true }).show();
    }

    return {
        error: function (message) { show(message, "error"); },
        success: function (message) { show(message, "success"); }
    };
})();

// If the session is gone (expired or signed out elsewhere), any API call returns 401: send the user to the login page.
(function ($) {
    "use strict";

    if (!$) {
        return;
    }

    $(document).ajaxError(function (event, xhr) {
        if (xhr && xhr.status === 401) {
            window.location.href = "/Account/Login?returnUrl=" +
                encodeURIComponent(window.location.pathname + window.location.search);
        }
    });
})(window.jQuery);
