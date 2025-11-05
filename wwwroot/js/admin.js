(() => {
    var __loaded = false;
    function apagee__init()
    {
        if (__loaded) return;

        __loaded = true;

        initFileUploads();
        initMediaPasteArea();
    }

    function initFileUploads()
    {
        const fileInputs = document.querySelectorAll("input.file-input[type=file]");
        fileInputs.forEach(fileInput =>
        {
            fileInput.onchange = () =>
            {
                if (fileInput.files.length > 0)
                {
                    const fileName = fileInput.parentElement.querySelector(".file-name");
                    const fileLabel = fileInput.parentElement.querySelector(".file-label");
                    fileName.textContent = fileInput.files[0].name;
                    fileLabel.textContent = "✅ Ready to Upload"
                }
            };
        });
    }

    function initMediaPasteArea()
    {
        document.addEventListener('paste', async (ev) =>
        {
            const target = ev.target;
            if (!(target instanceof HTMLTextAreaElement) || !target.classList.contains('media-paste-target')) return;

            ev.preventDefault();
            target.focus();
            selectHttpUrlBeforeCaret(target);

            let cbData = ev.clipboardData;
            let textFallback = cbData?.getData('text/plain') ?? '';

            const images = await extractImagesFromClipboard(cbData);
            if (!images.length)
            {
                requestAnimationFrame(() => {
                    if (textFallback)
                    {
                        target.focus();
                        selectHttpUrlBeforeCaret(target);
                        appendAtCaret(target, textFallback);
                    }
                });
                return;
            }

            for (const img of images)
            {
                try
                {
                    const dataURL = await fileToDataURL(img.file);
                    const [prefix, _] = dataURL.split(',');
                    const mimeMatch = prefix.match(/data:(image\/[a-z0-9.+-]+);base64/);
                    const mime = mimeMatch ? mimeMatch[1] : img.file.type || 'application/octet-stream';
                    const filename = img.name || `pasted.${mime.split('/')[1] || 'bin'}`;

                    const formData = new FormData();
                    formData.append('file', img.file, filename);
                    formData.append('mimeType', mime);

                    const resp = await fetch(`/admin/articles/edit/${idFromUrlPath()}/addImage`, {
                        method: 'POST',
                        headers: { 'Accept': 'application/json' },
                        body: formData,
                        credentials: 'same-origin'
                    });

                    if (!resp.ok)
                    {
                        throw new Error(`Upload failed: ${resp.status} ${resp.statusText}`);
                    }

                    const respMessage = await resp.json();

                    if (respMessage.md && respMessage.md.length > 0)
                    {
                        selectHttpUrlBeforeCaret(target);
                        appendAtCaret(target, respMessage.md);
                    }
                }
                catch (err)
                {
                    console.error(`Error during image paste/upload.`, err);
                }
            }
        });
    }

    async function extractImagesFromClipboard(clipboardData)
    {
        function dataURLtoFile(dataUrl, filename)
        {
            const [meta, data] = dataUrl.split(',');
            const mime = /data:([^;]+);/i.exec(meta)?.[1] || 'application/octet-stream';
            const bin = atob(data);
            const bytes = new Uint8Array(bin.length);
            for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
            return new File([bytes], filename, { type: mime });
        }

        function extractFirstImgSrcFromHtml(html)
        {
            const doc = new DOMParser().parseFromString(html, 'text/html');
            return doc.querySelector('img[src]')?.getAttribute('src') || null;
        }

        function detectImageType(bytes)
        {
            // GIF87a/GIF89a
            if (bytes.length >= 6 &&
                bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x38 &&
                (bytes[4] === 0x37 || bytes[4] === 0x39) && bytes[5] === 0x61) return 'image/gif';

            // PNG/APNG
            if (bytes.length >= 8 &&
                bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4E && bytes[3] === 0x47 &&
                bytes[4] === 0x0D && bytes[5] === 0x0A && bytes[6] === 0x1A && bytes[7] === 0x0A) return 'image/png';

            // JPEG
            if (bytes.length >= 3 &&
                bytes[0] === 0xFF && bytes[1] === 0xD8 && bytes[2] === 0xFF) return 'image/jpeg';

            // WebP
            if (bytes.length >= 12 &&
                bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x46 &&
                bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50) return 'image/webp';

            return '';
        }
        
        async function proxyFetchAsFile(url, nameHint = 'clipboard')
        {
            const formData = new FormData();
            formData.append('url', url);

            const res = await fetch(`/admin/articles/media/proxyGet`, {
                method: 'POST',
                headers: { 'Accept': 'application/json' },
                body: formData,
                credentials: 'same-origin'
            });
            const json = await res.json();
            if (!res.ok || json.error || !json.file) throw new Error('Proxy fetch failed');

            const b64 = json.file;
            const bin = atob(b64);
            const bytes = new Uint8Array(bin.length);
            for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);

            const type = detectImageType(bytes) || 'application/octet-stream';
            const ext = type.startsWith('image/') ? type.split('/')[1] : 'bin';

            return new File([bytes], `${nameHint}.${ext}`, { type });
        }

        const results = [];
        const preferredOrder = ['image/gif', 'image/webp', 'image/apng', 'image/png', 'image/jpeg', 'image/jpg'];

        try
        {
            // Special case: Paste a GIF URL (browsers block gif pasting - dumb).
            const plain = clipboardData?.getData?.('text/plain') || '';
            const gifUrl = plain && /^https?:\/\/\S+\.gif(\?|#|$)/i.test(plain) ? plain.trim() : null;
            if (gifUrl)
            {
                const file = await proxyFetchAsFile(gifUrl, 'pasted-url');
                results.unshift({ file, name: file.name, type: file.type, size: file.size });
                return results;
            }
            
            const hasHtml = clipboardData?.types?.includes?.('text/html');
            if (hasHtml)
            {
                const html = clipboardData.getData?.('text/html') || '';
                const src = html && extractFirstImgSrcFromHtml(html);

                if (src)
                {
                    if (src.startsWith('data:image/'))
                    {
                        const ext = pickExtFrom(/^data:([^;]+)/i.exec(src)?.[1] || 'image/bin');
                        const file = dataURLtoFile(src, `pasted-dataurl.${ext}`);
                        results.push(file);
                    }
                    else if (/^https?:\/\//i.test(src))
                    {
                        const file = await proxyFetchAsFile(text, 'pasted-text-url');
                        results.push(file);
                    }
                }
            }
        }
        catch (e)
        {
            // Skip and fall through, likely not a proper image URL.
        }

        try
        {
            const items = await navigator.clipboard.read();
            for (const item of items)
            {
                const bestType = preferredOrder.find(t => item.types.includes(t));
                if (!bestType) continue;

                const blob = await item.getType(bestType);
                const ext = bestType.split('/')[1];
                const file = new File([blob], `clipboard.${ext}`, { type: bestType });

                if (bestType === 'image/gif' || bestType === 'image/webp' || bestType === 'image/apng')
                {
                    results.unshift(file);
                }
                else
                {
                    results.push(file);
                }
            }
        }
        catch (err)
        {
            console.warn('navigator.clipboard.read() failed or unavailable:', err);
        }

        try
        {
            if (!results.length && clipboardData?.types?.includes?.('text/plain'))
            {
                const text = clipboardData.getData?.('text/plain') || '';
                if (/^https?:\/\//i.test(text))
                {
                    const file = await fetchAsFile(text, 'pasted-text-url');
                    if (/\.gif(\?|#|$)/i.test(text) || /^image\/gif$/i.test(file.type))
                    {
                        results.unshift(file);
                    }
                    else
                    {
                        results.push(file);
                    }
                }
            }
        }
        catch (e)
        {
            console.warn('Text URL fetch failed:', e);
        }

        return results.map(f => ({ file: f, name: f.name, type: f.type, size: f.size }));
    }

    function appendAtCaret(textarea, text)
    {
        if (!(textarea instanceof HTMLTextAreaElement)) return;

        const { selectionStart, selectionEnd } = textarea;
        textarea.setRangeText(text, selectionStart, selectionEnd, 'end');
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function selectHttpUrlBeforeCaret(textarea)
    {
        if (!(textarea instanceof HTMLTextAreaElement)) return false;

        const caret = textarea.selectionEnd;
        const before = textarea.value.slice(0, caret);

        const m = before.match(/(?:^|\s)(https?:\/\/\S+)$/i);
        if (!m) return false;

        const urlPart = m[1];
        const start = caret - urlPart.length;

        textarea.setSelectionRange(start, caret);
        return true;
    }

    function fileToDataURL(file)
    {
        return new Promise((resolve, reject) =>
        {
            const fr = new FileReader();
            fr.onerror = () => reject(fr.error);
            fr.onload = () => resolve(fr.result);
            fr.readAsDataURL(file);
        });
    }

    function idFromUrlPath()
    {
        const match = window.location.pathname.match(/[0-9A-HJKMNP-TV-Z]{26}/i);
        return match ? match[0] : null;
    }

    window.onload = apagee__init;
    window.document.onload = apagee__init;
})();