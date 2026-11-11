// FliqPayroll site scripts

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
