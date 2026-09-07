// Helpers for the version of Icon Pack Builder that runs in a plain browser tab, bound from C# in Platforms/WebAssembly/Browser/BrowserHost.cs.
// The bootstrapper loads this file before the app starts. It is also loaded inside the VS Code webview, where only isVsCode() is used.
(function () {
    'use strict';

    const PYODIDE_VERSION = '0.27.7';
    const PYODIDE_URL = 'https://cdn.jsdelivr.net/pyodide/v' + PYODIDE_VERSION + '/full/';

    let pyodidePromise = null;

    function isVsCode() {
        return typeof globalThis.ipbHost !== 'undefined';
    }

    function setTitle(title) {
        document.title = title;
    }

    function lightSchemeQuery() {
        return window.matchMedia ? window.matchMedia('(prefers-color-scheme: light)') : null;
    }

    function getThemeKind() {
        const query = lightSchemeQuery();
        return query && query.matches ? 'light' : 'dark';
    }

    function onThemeChanged(callback) {
        const query = lightSchemeQuery();
        if (query) {
            query.addEventListener('change', function (e) { callback(e.matches ? 'light' : 'dark'); });
        }
    }

    function storageGet(key) {
        try { return localStorage.getItem(key); } catch (e) { return null; }
    }

    function storageSet(key, value) {
        localStorage.setItem(key, value);
    }

    function storageRemove(key) {
        try { localStorage.removeItem(key); } catch (e) { /* nothing to remove */ }
    }

    function storageKeys(prefix) {
        const keys = [];
        try {
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(prefix)) {
                    keys.push(key);
                }
            }
        } catch (e) { /* storage unavailable: behave as empty */ }
        return JSON.stringify(keys);
    }

    /** Opens the file picker and resolves with JSON { name, text } for the chosen file, or null when the user cancels. */
    function pickFile(accept) {
        return new Promise(function (resolve) {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = accept;
            input.style.display = 'none';
            document.body.appendChild(input);

            let done = false;
            function finish(value) {
                if (done) { return; }
                done = true;
                input.remove();
                resolve(value);
            }

            input.addEventListener('change', function () {
                const file = input.files && input.files[0];
                if (!file) { finish(null); return; }
                file.text().then(function (text) { finish(JSON.stringify({ name: file.name, text: text })); }, function () { finish(null); });
            });
            // Browsers that implement the cancel event report it directly; for the rest, a refocused window with no selection means cancel.
            input.addEventListener('cancel', function () { finish(null); });
            window.addEventListener('focus', function () {
                setTimeout(function () { if (!input.files || input.files.length === 0) { finish(null); } }, 1500);
            }, { once: true });

            input.click();
        });
    }

    function downloadFile(fileName, contentBase64, mimeType) {
        const binary = atob(contentBase64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        const url = URL.createObjectURL(new Blob([bytes], { type: mimeType }));
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 30000);
    }

    /** Loads Pyodide and the fontTools package from the CDN once; failures are not cached so a retry can succeed. */
    function loadSubsetter() {
        if (pyodidePromise === null) {
            pyodidePromise = (async function () {
                if (typeof globalThis.loadPyodide !== 'function') {
                    await new Promise(function (resolve, reject) {
                        const script = document.createElement('script');
                        script.src = PYODIDE_URL + 'pyodide.js';
                        script.onload = resolve;
                        script.onerror = function () { reject(new Error('Could not download the font subsetter (Pyodide) from ' + PYODIDE_URL + '. Check your network connection and try again.')); };
                        document.head.appendChild(script);
                    });
                }
                const pyodide = await globalThis.loadPyodide({ indexURL: PYODIDE_URL });
                await pyodide.loadPackage('fonttools');
                return pyodide;
            })().catch(function (error) {
                pyodidePromise = null;
                throw error;
            });
        }
        return pyodidePromise;
    }

    const SUBSET_SCRIPT = [
        'import base64, io',
        'from fontTools import subset',
        'from fontTools.ttLib import TTFont',
        'font = TTFont(io.BytesIO(base64.b64decode(ipb_font_b64)))',
        'options = subset.Options()',
        'subsetter = subset.Subsetter(options)',
        'subsetter.populate(unicodes=list(ipb_unicodes))',
        'subsetter.subset(font)',
        'out = io.BytesIO()',
        'font.save(out)',
        "base64.b64encode(out.getvalue()).decode('ascii')",
    ].join('\n');

    /** Subsets the base64 font to the JSON array of code points with the same defaults as pyftsubset; resolves with the base64 result. */
    async function subsetFont(fontBase64, codePointsJson) {
        const pyodide = await loadSubsetter();
        pyodide.globals.set('ipb_font_b64', fontBase64);
        pyodide.globals.set('ipb_unicodes', pyodide.toPy(JSON.parse(codePointsJson)));
        try {
            return await pyodide.runPythonAsync(SUBSET_SCRIPT);
        }
        finally {
            pyodide.globals.delete('ipb_font_b64');
            pyodide.globals.delete('ipb_unicodes');
        }
    }

    globalThis.ipbBrowser = {
        isVsCode: isVsCode,
        setTitle: setTitle,
        getThemeKind: getThemeKind,
        onThemeChanged: onThemeChanged,
        storageGet: storageGet,
        storageSet: storageSet,
        storageRemove: storageRemove,
        storageKeys: storageKeys,
        pickFile: pickFile,
        downloadFile: downloadFile,
        subsetFont: subsetFont,
    };
})();
