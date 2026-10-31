// Resident pages. Storage can be blocked (private mode, full disk); every page must still work without it.
const store = {
    get(key) { try { return JSON.parse(localStorage.getItem(key) || 'null'); } catch { return null; } },
    set(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* ignore */ } },
    remove(key) { try { localStorage.removeItem(key); } catch { /* ignore */ } }
};

const MY_REPORTS_KEY = 'marikinalert-my-reports';
const DRAFT_KEY = 'marikinalert-draft';
const OUTBOX_KEY = 'marikinalert-outbox';
const RETRY_MS = 15000;

// Random id per report so the server can spot one sent twice.
// Not crypto.randomUUID: that only exists on HTTPS, and phones reach the node over plain http://.
function newRequestId() {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    return [...bytes].map(b => b.toString(16).padStart(2, '0')).join('');
}

function rememberReport(code, ref, created) {
    if (!code) return;
    const mine = (store.get(MY_REPORTS_KEY) || []).filter(r => r.code !== code);
    mine.unshift({ code, ref, created });
    store.set(MY_REPORTS_KEY, mine.slice(0, 10));
}

function isConfirmation(res) {
    return res.redirected && new URL(res.url).pathname.toLowerCase().startsWith('/reports/submitted');
}

// ---------- Outbox: reports that couldn't be sent, kept on the phone until they go through ----------

const outbox = {
    list() { return store.get(OUTBOX_KEY) || []; },
    add(item) { store.set(OUTBOX_KEY, [...this.list().filter(i => i.id !== item.id), item]); },
    remove(id) { store.set(OUTBOX_KEY, this.list().filter(i => i.id !== id)); },
    has(id) { return this.list().some(i => i.id === id); }
};

// Sends one saved report. The token in an old page may no longer match the cookie, so get a fresh one first.
async function sendSaved(item) {
    const page = await fetch('/Reports/Create', { credentials: 'same-origin', cache: 'no-store' });
    if (!page.ok) throw new Error(String(page.status));
    const token = new DOMParser().parseFromString(await page.text(), 'text/html')
        .querySelector('input[name="__RequestVerificationToken"]');
    if (!token) throw new Error('no token');

    const body = new FormData();
    for (const [name, value] of Object.entries(item.fields)) body.append(name, value);
    body.set('ClientRequestId', item.id);
    body.set('__RequestVerificationToken', token.value);

    const res = await fetch('/Reports/Create', { method: 'POST', body, credentials: 'same-origin' });
    if (!res.ok) throw new Error(String(res.status));
    if (!isConfirmation(res)) return { invalid: true }; // the server sent the form back with errors

    const tracking = new DOMParser().parseFromString(await res.text(), 'text/html').getElementById('tracking');
    return { url: res.url, code: tracking?.dataset.code, ref: tracking?.dataset.ref, created: tracking?.dataset.created };
}

// Shared by the report form and the banner: tries every saved report, stops at the first network failure.
const sender = (function () {
    const listeners = [];
    let flushing = false;

    async function flush() {
        if (flushing || !outbox.list().length) return;
        flushing = true;
        listeners.forEach(fn => fn({ type: 'sending' }));
        try {
            for (const item of outbox.list()) {
                let result;
                try { result = await sendSaved(item); } catch { break; } // still offline, try again later
                outbox.remove(item.id);

                const draft = store.get(DRAFT_KEY);
                if (result.invalid) {
                    // Put it back in the form so the resident can fix it, unless they're already writing another one.
                    if (!draft || !draft.RawMessage || draft.ClientRequestId === item.id)
                        store.set(DRAFT_KEY, { ...item.fields, ClientRequestId: item.id });
                } else {
                    rememberReport(result.code, result.ref, result.created);
                    if (draft && draft.ClientRequestId === item.id) store.remove(DRAFT_KEY);
                }
                listeners.forEach(fn => fn({ type: result.invalid ? 'invalid' : 'sent', item, result }));
            }
        } finally {
            flushing = false;
            listeners.forEach(fn => fn({ type: 'done' }));
        }
    }

    window.addEventListener('online', flush);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) flush(); });
    setInterval(flush, RETRY_MS);
    setTimeout(flush, 500);

    return { flush, on: fn => listeners.push(fn), get busy() { return flushing; } };
})();

// ---------- Report form: counter, saved drafts, remembered barangay, sending without losing the message ----------
(function () {
    const form = document.getElementById('report-form');
    if (!form) return;

    const BARANGAY_KEY = 'marikinalert-barangay';
    const FIELDS = ['RawMessage', 'Barangay', 'Landmark', 'SenderName', 'ContactNumber', 'SelectedCategory', 'ClientRequestId'];
    const text = form.dataset; // translated button texts, rendered by the server

    const message = form.querySelector('textarea[name="RawMessage"]');
    const barangay = form.elements.Barangay;
    const requestId = form.elements.ClientRequestId;
    const counter = document.getElementById('msg-count');
    const button = document.getElementById('submit-btn');
    const label = button.querySelector('span');
    const sendError = document.getElementById('send-error');
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
        requestId.value = newRequestId(); // a report already waiting in the outbox still gets sent
        sendError.hidden = true;
        setSending(false);
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

    // Couldn't reach the server: keep the report in the outbox and keep trying in the background.
    function queue() {
        const fields = {};
        for (const [name, value] of new FormData(form)) {
            if (name !== '__RequestVerificationToken') fields[name] = value;
        }
        outbox.add({ id: requestId.value, fields, savedAt: new Date().toISOString() });
        showQueued();
    }

    function showQueued() {
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
            queue();
            return;
        }

        if (res.ok && isConfirmation(res)) {
            outbox.remove(requestId.value);
            store.remove(DRAFT_KEY);
            location.href = res.url;
        } else if (res.ok) {
            // Server-side validation failed; submit normally so the page shows the error messages.
            HTMLFormElement.prototype.submit.call(form);
        } else {
            queue();
        }
    });

    // The background sender got this report through: show its confirmation.
    sender.on(e => {
        if (e.item?.id !== requestId.value) return;
        if (e.type === 'sent') location.href = e.result.url;
        if (e.type === 'invalid') location.reload(); // reloads with the draft restored, ready to fix
    });

    // Coming back with the browser's Back button: re-enable the form.
    window.addEventListener('pageshow', () => { if (sendError.hidden) setSending(false); });

    if (!restoreDraft()) restoreBarangay();
    if (!requestId.value) requestId.value = newRequestId();
    if (outbox.has(requestId.value)) showQueued(); // reopened while this report is still waiting
    updateCount();
})();

// ---------- Banner on every resident page: saved reports waiting to send, and what happened to them ----------
(function () {
    const banner = document.getElementById('outbox-banner');
    if (!banner) return;

    const text = banner.dataset;
    const message = document.getElementById('outbox-text');
    const sendNow = document.getElementById('outbox-send');
    const link = document.getElementById('outbox-link');
    const form = document.getElementById('report-form');
    let notice = null; // last 'sent' or 'invalid' result, shown until the page changes

    // The form shows its own message for the report it holds, so the banner leaves that one out.
    function waiting() {
        const current = form?.elements.ClientRequestId.value;
        return outbox.list().filter(i => i.id !== current);
    }

    function render() {
        const pending = waiting();
        link.hidden = true;
        sendNow.hidden = !pending.length;
        sendNow.disabled = sender.busy;

        if (pending.length) {
            message.textContent = text.msgWaiting + (pending.length > 1 ? ` (${pending.length})` : '');
        } else if (notice?.type === 'sent') {
            message.textContent = notice.result.code ? `${text.msgSent} ${text.msgCode}: ${notice.result.code}` : text.msgSent;
            if (notice.result.code) {
                link.href = '/Reports/Status?code=' + encodeURIComponent(notice.result.code);
                link.textContent = text.msgOpen;
                link.hidden = false;
            }
        } else if (notice?.type === 'invalid') {
            message.textContent = text.msgInvalid;
            link.href = '/Reports/Create';
            link.textContent = text.msgOpen;
            link.hidden = !!form; // already on the form
        }
        banner.hidden = !pending.length && !notice;
    }

    sender.on(e => {
        if ((e.type === 'sent' || e.type === 'invalid') && e.item.id !== form?.elements.ClientRequestId.value) notice = e;
        render();
    });
    sendNow.addEventListener('click', () => sender.flush());
    render();
})();

// ---------- Confirmation page: remember this report on the phone ----------
(function () {
    const tracking = document.getElementById('tracking');
    if (!tracking) return;
    rememberReport(tracking.dataset.code, tracking.dataset.ref, tracking.dataset.created);
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
