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
    let lastSignature = null;
    async function refresh() {
        if (refreshing) return;
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
                const fresh = [...ids].filter(id => !knownIds.has(id));
                fresh.forEach(id => feed.querySelector(`.incident[data-id="${id}"]`)?.classList.add('animate-flash'));

                if (fresh.length) {
                    const critical = fresh.some(id => feed.querySelector(`.incident[data-id="${id}"]`)?.dataset.priority === 'Critical');
                    toast(`${fresh.length} new report${fresh.length > 1 ? 's' : ''}${critical ? ' · CRITICAL' : ''}`);
                    if (critical && soundOn) beep();
                }
                knownIds = ids;
            }

            if (data) {
                document.getElementById('critical-count').textContent = data.dataset.critical;
                document.getElementById('open-count').textContent = data.dataset.open;
                document.title = (data.dataset.critical > 0 ? `(${data.dataset.critical}) ` : '') + baseTitle;
            }
            setOnline(true);
        } catch {
            setOnline(false);
        } finally {
            refreshing = false;
        }
    }

    setInterval(refresh, POLL_MS);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    setOnline(true);

    // Filters apply instantly without a page reload; URL stays shareable.
    filters.addEventListener('change', () => {
        history.replaceState(null, '', feedUrl().page);
        knownIds = new Set(); // don't flash everything as "new" after a filter change
        refresh().then(() => { knownIds = currentIds(); });
    });

    document.getElementById('sound-toggle').addEventListener('click', function () {
        soundOn = !soundOn;
        this.setAttribute('aria-pressed', soundOn);
        this.querySelector('i').className = 'ph text-base ' + (soundOn ? 'ph-speaker-high' : 'ph-speaker-slash');
        this.querySelector('span').textContent = soundOn ? 'Sound alerts on' : 'Sound alerts off';
        this.classList.toggle('text-primary', soundOn);
        this.classList.toggle('border-primary', soundOn);
        if (soundOn) beep(); // also unlocks audio, browsers require a user gesture
    });

    // ---------- card actions (Verify / Resolve / Archive) ----------

    async function post(form) {
        const res = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'fetch' }
        });
        if (!res.ok) throw new Error(res.status);
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
        } catch {
            button.disabled = false;
            toast('Could not save. Check the connection and try again.', true);
        }
    });

    // ---------- relay dialog ----------

    const dialog = document.getElementById('relay-dialog');
    const relayForm = document.getElementById('relay-form');
    const summary = document.getElementById('relay-summary');
    const copyBtn = document.getElementById('copy-summary');

    feed.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-relay]');
        if (!btn) return;
        relayForm.elements.id.value = btn.dataset.relay;
        relayForm.elements.agency.value = btn.dataset.agency;
        summary.value = btn.dataset.summary;
        copyBtn.querySelector('span').textContent = 'Copy details';
        dialog.showModal();
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
