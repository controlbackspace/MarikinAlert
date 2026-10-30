// Resident report form: character counter, saved drafts, and sending without losing the message.
(function () {
    const form = document.getElementById('report-form');
    if (!form) return;

    const DRAFT_KEY = 'marikinalert-draft';
    const FIELDS = ['RawMessage', 'Barangay', 'Landmark', 'SenderName', 'ContactNumber', 'SelectedCategory'];

    const message = form.querySelector('textarea[name="RawMessage"]');
    const counter = document.getElementById('msg-count');
    const button = document.getElementById('submit-btn');
    const label = button.querySelector('span');
    const sendError = document.getElementById('send-error');
    const sendErrorDetail = document.getElementById('send-error-detail');
    const draftNotice = document.getElementById('draft-notice');

    // ---------- character counter ----------

    function updateCount() {
        counter.textContent = `${message.value.length}/${message.maxLength}`;
    }
    message.addEventListener('input', updateCount);

    // ---------- draft saved on this phone ----------
    // Storage can be blocked (private mode, full disk); the form must still work without it.

    function readDraft() {
        try { return JSON.parse(localStorage.getItem(DRAFT_KEY) || 'null'); } catch { return null; }
    }

    function saveDraft() {
        const data = {};
        for (const name of FIELDS) {
            const el = form.elements[name];
            if (el) data[name] = el.value || '';
        }
        try { localStorage.setItem(DRAFT_KEY, JSON.stringify(data)); } catch { /* ignore */ }
    }

    function clearDraft() {
        try { localStorage.removeItem(DRAFT_KEY); } catch { /* ignore */ }
    }

    function restoreDraft() {
        const draft = readDraft();
        // Only restore into an empty form; never overwrite what the server sent back (e.g. validation errors).
        if (!draft || !draft.RawMessage || message.value) return;

        for (const name of FIELDS) {
            const el = form.elements[name];
            if (!el || !draft[name]) continue;
            if (el instanceof RadioNodeList) {
                for (const radio of el) radio.checked = radio.value === draft[name];
            } else {
                el.value = draft[name];
            }
        }
        draftNotice.hidden = false;
        setTimeout(() => { draftNotice.hidden = true; }, 6000);
    }

    document.getElementById('draft-clear').addEventListener('click', function () {
        clearDraft();
        form.reset();
        updateCount();
        draftNotice.hidden = true;
    });

    form.addEventListener('input', saveDraft);
    form.addEventListener('change', saveDraft);

    // ---------- sending ----------

    function setSending(sending) {
        button.disabled = sending;
        label.textContent = sending ? 'Sending…' : 'Send emergency report';
    }

    function showError(detail) {
        sendErrorDetail.textContent = detail;
        sendError.hidden = false;
        label.textContent = 'Try again';
        button.disabled = false;
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        // jQuery unobtrusive validation shows field errors; stop here if anything is missing.
        if (window.jQuery && !window.jQuery(form).valid()) return;
        if (button.disabled) return;

        saveDraft();
        sendError.hidden = true;
        setSending(true);

        let res;
        try {
            res = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
        } catch {
            // Network dropped: keep everything on screen and let them retry.
            showError("Check that you're still connected to the Wi-Fi. Your message is saved on this phone.");
            return;
        }

        if (res.ok && res.redirected && new URL(res.url).pathname.toLowerCase().startsWith('/reports/submitted')) {
            clearDraft();
            location.href = res.url;
        } else if (res.ok) {
            // Server-side validation failed; submit normally so the page shows the error messages.
            HTMLFormElement.prototype.submit.call(form);
        } else {
            showError('The system had a problem. Reload the page and send again. Your message is saved on this phone.');
        }
    });

    // Coming back with the browser's Back button: re-enable the form.
    window.addEventListener('pageshow', () => setSending(false));

    restoreDraft();
    updateCount();
})();
