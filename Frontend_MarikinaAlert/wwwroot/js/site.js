// Resident report form: character counter and double-submit protection.
(function () {
    const form = document.getElementById('report-form');
    if (!form) return;

    const message = form.querySelector('textarea[name="RawMessage"]');
    const counter = document.getElementById('msg-count');
    const button = document.getElementById('submit-btn');

    function updateCount() {
        counter.textContent = `${message.value.length}/${message.maxLength}`;
    }
    message.addEventListener('input', updateCount);
    updateCount();

    form.addEventListener('submit', function (e) {
        // jQuery unobtrusive validation blocks invalid submits; don't lock the button in that case.
        if (window.jQuery && !window.jQuery(form).valid()) return;

        if (button.disabled) {
            e.preventDefault();
            return;
        }
        button.disabled = true;
        button.querySelector('span').textContent = 'Sending…';
    });

    // If the user comes back with the browser's Back button, re-enable the form.
    window.addEventListener('pageshow', function () {
        button.disabled = false;
        button.querySelector('span').textContent = 'Send emergency report';
    });
})();
