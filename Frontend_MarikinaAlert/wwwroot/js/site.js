// Resident pages. Storage can be blocked (private mode, full disk); every page must still work without it.
const store = {
    get(key) { try { return JSON.parse(localStorage.getItem(key) || 'null'); } catch { return null; } },
    set(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* ignore */ } },
    remove(key) { try { localStorage.removeItem(key); } catch { /* ignore */ } }
};

const MY_REPORTS_KEY = 'marikinalert-my-reports';

// ---------- Report form: counter, saved drafts, remembered barangay, sending without losing the message ----------
(function () {
    const form = document.getElementById('report-form');
    if (!form) return;

    const DRAFT_KEY = 'marikinalert-draft';
    const BARANGAY_KEY = 'marikinalert-barangay';
    const FIELDS = ['RawMessage', 'Barangay', 'Landmark', 'SenderName', 'ContactNumber', 'SelectedCategory'];
    const text = form.dataset; // translated button and error texts, rendered by the server

    const message = form.querySelector('textarea[name="RawMessage"]');
    const barangay = form.elements.Barangay;
    const counter = document.getElementById('msg-count');
    const button = document.getElementById('submit-btn');
    const label = button.querySelector('span');
    const sendError = document.getElementById('send-error');
    const sendErrorDetail = document.getElementById('send-error-detail');
    const draftNotice = document.getElementById('draft-notice');

    function updateCount() {
        counter.textContent = `${message.value.length}/${message.maxLength}`;
    }
    message.addEventListener('input', updateCount);

    // ----- draft -----

    function saveDraft() {
        const data = {};
        for (const name of FIELDS) {
            const el = form.elements[name];
            if (el) data[name] = el.value || '';
        }
        store.set(DRAFT_KEY, data);
    }

    function restoreDraft() {
        const draft = store.get(DRAFT_KEY);
        // Only restore into an empty form; never overwrite what the server sent back (e.g. validation errors).
        if (!draft || !draft.RawMessage || message.value) return false;

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
        return true;
    }

    document.getElementById('draft-clear').addEventListener('click', function () {
        store.remove(DRAFT_KEY);
        form.reset();
        restoreBarangay();
        updateCount();
        draftNotice.hidden = true;
    });

    form.addEventListener('input', saveDraft);
    form.addEventListener('change', saveDraft);

    // ----- remembered barangay -----
    // Most people report from home, so the next report from this phone starts with the same barangay.

    function restoreBarangay() {
        const saved = store.get(BARANGAY_KEY);
        if (saved && !barangay.value && [...barangay.options].some(o => o.value === saved)) barangay.value = saved;
    }

    barangay.addEventListener('change', () => { if (barangay.value) store.set(BARANGAY_KEY, barangay.value); });

    // ----- sending -----

    function setSending(sending) {
        button.disabled = sending;
        label.textContent = sending ? text.msgSending : text.msgSend;
    }

    function showError(detail) {
        sendErrorDetail.textContent = detail;
        sendError.hidden = false;
        label.textContent = text.msgRetry;
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
            showError(text.msgNetwork);
            return;
        }

        if (res.ok && res.redirected && new URL(res.url).pathname.toLowerCase().startsWith('/reports/submitted')) {
            store.remove(DRAFT_KEY);
            location.href = res.url;
        } else if (res.ok) {
            // Server-side validation failed; submit normally so the page shows the error messages.
            HTMLFormElement.prototype.submit.call(form);
        } else {
            showError(text.msgServer);
        }
    });

    // Coming back with the browser's Back button: re-enable the form.
    window.addEventListener('pageshow', () => setSending(false));

    if (!restoreDraft()) restoreBarangay();
    updateCount();
})();

// ---------- Confirmation page: remember this report on the phone ----------
(function () {
    const tracking = document.getElementById('tracking');
    if (!tracking) return;

    const mine = (store.get(MY_REPORTS_KEY) || []).filter(r => r.code !== tracking.dataset.code);
    mine.unshift({ code: tracking.dataset.code, ref: tracking.dataset.ref, created: tracking.dataset.created });
    store.set(MY_REPORTS_KEY, mine.slice(0, 10));
})();

// ---------- Check my report: list reports sent from this phone ----------
(function () {
    const section = document.getElementById('my-reports');
    if (!section) return;

    const mine = store.get(MY_REPORTS_KEY) || [];
    if (!mine.length) return;

    const list = section.querySelector('ul');
    for (const r of mine) {
        const li = document.createElement('li');
        const a = document.createElement('a');
        a.href = '/Reports/Status?code=' + encodeURIComponent(r.code);
        a.className = 'flex items-baseline justify-between gap-4 py-3 hover:bg-surface';
        const code = document.createElement('span');
        code.className = 'font-mono tracking-wider text-primary underline underline-offset-2';
        code.textContent = r.code;
        const meta = document.createElement('span');
        meta.className = 'text-sm text-muted';
        meta.textContent = `${r.ref} · ${new Date(r.created).toLocaleString([], { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })}`;
        a.append(code, meta);
        li.append(a);
        list.append(li);
    }
    section.hidden = false;
})();
