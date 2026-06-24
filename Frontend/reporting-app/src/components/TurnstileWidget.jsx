import React, { useEffect, useImperativeHandle, useRef, forwardRef } from 'react';

// Cloudflare Turnstile universal TEST site key — always passes, safe for localhost/dev.
// Replace with the real public site key for production (it is not a secret).
const DEFAULT_SITE_KEY = '1x00000000000000000000AA';
const SCRIPT_SRC = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

// Load the Turnstile script exactly once for the whole app (idempotent across widgets).
let scriptPromise = null;
const loadTurnstileScript = () => {
    if (window.turnstile) return Promise.resolve();
    if (scriptPromise) return scriptPromise;

    scriptPromise = new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src^="${SCRIPT_SRC}"]`);
        if (existing) {
            existing.addEventListener('load', () => resolve());
            existing.addEventListener('error', reject);
            return;
        }
        const script = document.createElement('script');
        script.src = SCRIPT_SRC;
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = reject;
        document.head.appendChild(script);
    });
    return scriptPromise;
};

// Reusable Cloudflare Turnstile widget.
//   onVerify(token) — fires with the solved token, or '' when it expires/errors.
//   ref.reset()     — fetch a fresh token (Turnstile tokens are single-use; call after a failed submit).
const TurnstileWidget = forwardRef(({ onVerify, onError, siteKey = DEFAULT_SITE_KEY }, ref) => {
    const containerRef = useRef(null);
    const widgetIdRef = useRef(null);

    useImperativeHandle(ref, () => ({
        reset: () => {
            if (widgetIdRef.current !== null && window.turnstile) {
                window.turnstile.reset(widgetIdRef.current);
            }
            // reset() clears the widget but fires no callback — push the empty token to the
            // parent so its state can never desync from the (now unsolved) widget.
            onVerify?.('');
        },
    }), [onVerify]);

    useEffect(() => {
        let cancelled = false;

        loadTurnstileScript()
            .then(() => {
                // Bail if this effect was cleaned up (StrictMode double-invoke) or already rendered.
                if (cancelled || widgetIdRef.current !== null) return;
                if (!containerRef.current || !window.turnstile) return;

                widgetIdRef.current = window.turnstile.render(containerRef.current, {
                    sitekey: siteKey,
                    callback: (token) => onVerify?.(token),
                    'expired-callback': () => onVerify?.(''),
                    'error-callback': () => onVerify?.(''),
                });
            })
            .catch(() => {
                // Script failed to load (blocked by an extension, offline, etc.). Surface it so the
                // user isn't stuck behind a permanently-disabled button with no explanation. Still
                // fail-closed: no token is ever produced.
                if (!cancelled) onError?.();
            });

        return () => {
            cancelled = true;
            if (widgetIdRef.current !== null && window.turnstile) {
                try {
                    window.turnstile.remove(widgetIdRef.current);
                } catch {
                    /* widget already gone */
                }
                widgetIdRef.current = null;
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <div ref={containerRef} style={{ display: 'flex', justifyContent: 'center', margin: '0.25rem 0' }} />;
});

export default TurnstileWidget;
