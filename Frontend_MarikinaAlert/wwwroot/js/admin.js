// Rescue Command dashboard: live polling, AJAX actions, relay dialog.
(function () {
    const POLL_MS = 5000;

    const feed = document.getElementById('feed');
    const filters = document.getElementById('filters');
    const liveStatus = document.getElementById('live-status');
    const liveDot = document.getElementById('live-dot');
    const toastEl = document.getElementById('toast');
    const baseTitle = document.title;

    let knownIds = currentIds();
    let soundOn = false;
    let toastTimer;

    // ---------- helpers ----------

    function currentIds() {
        return new Set([...feed.querySelectorAll('.incident')].map(el => el.dataset.id));
    }

    function toast(message, isError) {
        toastEl.textContent = message;
        toastEl.classList.toggle('bg-danger', !!isError);
        toastEl.classList.toggle('bg-ink', !isError);
        toastEl.classList.remove('opacity-0');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toastEl.classList.add('opacity-0'), 3000);
    }

    function feedUrl() {
        const params = new URLSearchParams(new FormData(filters));
        for (const [k, v] of [...params]) if (!v) params.delete(k);
        const qs = params.toString();
        return { feed: '/Admin/Feed' + (qs ? '?' + qs : ''), page: '/Admin/Dashboard' + (qs ? '?' + qs : '') };
    }

    function setOnline(online) {
        liveStatus.textContent = online
            ? 'Live · updated ' + new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', second: '2-digit' })
            : 'Connection lost · retrying…';
        liveDot.classList.toggle('bg-success', online);
        liveDot.classList.toggle('bg-danger', !online);
    }

    function beep() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = 'square';
            osc.frequency.value = 880;
            gain.gain.setValueAtTime(0.08, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.6);
            osc.connect(gain).connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.6);
        } catch { /* audio not available */ }
    }

    // ---------- live feed ----------

    let refreshing = false;
    let refreshAgain = false;
    let lastSignature = null;
    async function refresh() {
        // A filter change during a poll must not be dropped: run once more when the current poll ends.
        if (refreshing) { refreshAgain = true; return; }
        refreshing = true;
        try {
            const res = await fetch(feedUrl().feed, { headers: { 'X-Requested-With': 'fetch' }, cache: 'no-store' });
            if (res.redirected || res.status === 401) { location.href = '/Admin/Login'; return; }
            if (!res.ok) throw new Error(res.statusText);

            const html = await res.text();
            const template = document.createElement('template');
            template.innerHTML = html.trim();
            const data = template.content.getElementById('feed-data');

            // Only touch the DOM when something changed, so focus and hover aren't lost every 5s.
            // Antiforgery tokens differ on every render, so leave them out of the comparison.
            const signature = html.replace(/name="__RequestVerificationToken"[^>]*/g, '');
            if (signature !== lastSignature) {
                lastSignature = signature;
                feed.replaceChildren(template.content);
                const ids = currentIds();
                const fresh = knownIds ? [...ids].filter(id => !knownIds.has(id)) : [];
                fresh.forEach(id => feed.querySelector(`.incident[data-id="${id}"]`)?.classList.add('animate-flash'));

                if (fresh.length) {
                    const critical = fresh.some(id => feed.querySelector(`.incident[data-id="${id}"]`)?.dataset.priority === 'Critical');
                    toast(`${fresh.length} new report${fresh.length > 1 ? 's' : ''}${critical ? ' · CRITICAL' : ''}`);
                    if (critical && soundOn) beep();
                }
                knownIds = ids;
            }
            if (!knownIds) knownIds = currentIds(); // filter changed but the list didn't

            if (data) {
                document.getElementById('critical-count').textContent = data.dataset.critical;
                document.getElementById('open-count').textContent = data.dataset.open;
                const overdue = document.getElementById('overdue-count');
                overdue.textContent = data.dataset.overdue;
                overdue.classList.toggle('text-danger', data.dataset.overdue > 0);
                document.title = (data.dataset.critical > 0 ? `(${data.dataset.critical}) ` : '') + baseTitle;
            }
            setOnline(true);
        } catch {
            setOnline(false);
        } finally {
            refreshing = false;
            if (refreshAgain) { refreshAgain = false; refresh(); }
        }
    }

    setInterval(refresh, POLL_MS);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    setOnline(true);

    // Filters apply instantly without a page reload; URL stays shareable.
    filters.addEventListener('change', () => {
        history.replaceState(null, '', feedUrl().page);
        knownIds = null; // a filter change is not "new reports": no flash, no toast
        refresh();
    });

    // Barangay list and map: click a barangay to show only its reports; click it again or "Show all" to clear.
    feed.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-barangay]');
        if (!btn) return;
        filters.elements.barangay.value = btn.getAttribute('aria-pressed') === 'true' ? '' : btn.dataset.barangay;
        filters.dispatchEvent(new Event('change'));
    });

    document.getElementById('sound-toggle').addEventListener('click', function () {
        soundOn = !soundOn;
        this.setAttribute('aria-pressed', soundOn);
        this.querySelector('span').textContent = soundOn ? 'Sound alerts on' : 'Sound alerts off';
        this.classList.toggle('text-primary', soundOn);
        this.classList.toggle('border-primary', soundOn);
        if (soundOn) beep(); // also unlocks audio, browsers require a user gesture
    });

    // ---------- card actions (Verify / Resolve / Archive / teams) ----------

    async function post(form) {
        const res = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'fetch' }
        });
        if (!res.ok) throw new Error(String(res.status));
    }

    feed.addEventListener('submit', async function (e) {
        const form = e.target.closest('form[data-ajax]');
        if (!form) return;
        e.preventDefault();
        if (form.dataset.confirm && !confirm(form.dataset.confirm)) return;

        const button = form.querySelector('button');
        button.disabled = true;
        try {
            await post(form);
            toast(form.dataset.done || 'Saved');
            await refresh();
        } catch (err) {
            button.disabled = false;
            // 409: someone else changed it first (e.g. the team was just sent elsewhere). Show the latest state.
            if (err.message === '409') {
                toast(form.dataset.conflict || 'This was changed by someone else. The list has been updated.', true);
                refresh();
            } else {
                toast('Could not save. Check the connection and try again.', true);
            }
        }
    });

    // ---------- relay dialog ----------

    const dialog = document.getElementById('relay-dialog');
    const relayForm = document.getElementById('relay-form');
    const summary = document.getElementById('relay-summary');
    const copyBtn = document.getElementById('copy-summary');

    const callHotline = document.getElementById('call-hotline');
    const callBackup = document.getElementById('call-backup');
    const radio = document.getElementById('relay-radio');
    const noContact = document.getElementById('relay-no-contact');
    const numbers = document.getElementById('relay-numbers');
    const logButtons = document.getElementById('relay-log');
    let lastDialed = null; // number the dispatcher last tapped, logged with the outcome

    function telHref(number) {
        return 'tel:' + number.replace(/[^\d+]/g, '');
    }

    function setNumber(link, number) {
        link.hidden = !number;
        link.href = number ? telHref(number) : '';
        link.dataset.number = number || '';
        link.querySelector('span').textContent = number || '';
        link.classList.remove('ring-4', 'ring-[#05795B]/30');
    }

    // Show the numbers and radio channel of the agency picked in the dialog.
    function showContact() {
        const option = relayForm.elements.agency.selectedOptions[0];
        const hotline = option?.dataset.hotline || '';
        const backup = option?.dataset.backup || '';
        const channel = option?.dataset.radio || '';
        setNumber(callHotline, hotline);
        setNumber(callBackup, backup);
        radio.hidden = !channel;
        radio.querySelector('strong').textContent = channel;
        noContact.hidden = !!(hotline || channel);
        numbers.hidden = !(hotline || channel);
        logButtons.hidden = !(hotline || channel);
        logButtons.querySelectorAll('[data-method="Phone"]').forEach(b => b.hidden = !hotline);
        logButtons.querySelector('[data-method="Radio"]').hidden = !channel;
        lastDialed = hotline || null;
    }

    function openRelay(id, agency, text, dialed) {
        relayForm.elements.id.value = id;
        relayForm.elements.agency.value = agency;
        summary.value = text;
        copyBtn.querySelector('span').textContent = 'Copy details';
        showContact();
        if (dialed) lastDialed = dialed;
        dialog.showModal();
    }

    feed.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-relay]');
        if (btn) openRelay(btn.dataset.relay, btn.dataset.agency, btn.dataset.summary);

        // "Call" on a card: let the tel: link dial, and open the dialog so the result can be logged.
        const call = e.target.closest('[data-call]');
        if (call) openRelay(call.dataset.call, call.dataset.agency, call.dataset.summary, call.dataset.number);
    });

    relayForm.elements.agency.addEventListener('change', showContact);
    dialog.querySelectorAll('[data-dial]').forEach(link =>
        link.addEventListener('click', () => { lastDialed = link.dataset.number; }));

    logButtons.addEventListener('click', async function (e) {
        const btn = e.target.closest('[data-outcome]');
        if (!btn) return;
        const body = new FormData();
        body.append('__RequestVerificationToken', relayForm.elements.__RequestVerificationToken.value);
        body.append('id', relayForm.elements.id.value);
        body.append('agency', relayForm.elements.agency.value);
        body.append('method', btn.dataset.method);
        body.append('outcome', btn.dataset.outcome);
        if (btn.dataset.method === 'Phone' && lastDialed) body.append('number', lastDialed);

        btn.disabled = true;
        try {
            const res = await fetch('/Admin/LogContact/' + relayForm.elements.id.value, {
                method: 'POST', body, headers: { 'X-Requested-With': 'fetch' }
            });
            if (!res.ok) throw new Error(res.status);

            const failed = btn.dataset.outcome !== 'Answered';
            const tryBackup = failed && btn.dataset.method === 'Phone' && callBackup.dataset.number
                && lastDialed !== callBackup.dataset.number;
            if (tryBackup) callBackup.classList.add('ring-4', 'ring-[#05795B]/30');
            toast(tryBackup ? 'Logged. Try the backup number.' : failed ? 'Logged. Try again or use the radio.' : 'Logged. Mark it as relayed once the details are sent.');
            refresh();
        } catch {
            toast('Could not save. Check the connection and try again.', true);
        } finally {
            btn.disabled = false;
        }
    });

    dialog.querySelector('[data-close]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('click', e => { if (e.target === dialog) dialog.close(); });

    copyBtn.addEventListener('click', async function () {
        const text = summary.value;
        try {
            // navigator.clipboard only exists on HTTPS/localhost; fall back for plain-HTTP intranet nodes.
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
            } else {
                summary.select();
                document.execCommand('copy');
                summary.setSelectionRange(0, 0);
            }
            this.querySelector('span').textContent = 'Copied!';
        } catch {
            summary.select();
            this.querySelector('span').textContent = 'Press Ctrl+C';
        }
    });

    relayForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        relayForm.action = '/Admin/Relay/' + relayForm.elements.id.value;
        try {
            await post(relayForm);
            dialog.close();
            toast('Relayed to ' + relayForm.elements.agency.value);
            await refresh();
        } catch {
            toast('Could not save. Check the connection and try again.', true);
        }
    });
})();
