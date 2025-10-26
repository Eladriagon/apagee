(() => {
    var __loaded = false;
    function apagee__init()
    {
        if (__loaded) return;

        __loaded = true;

        initFileUploads();
    }

    function initFileUploads()
    {
        const fileInput = document.querySelector("input.file-input[type=file]");
        fileInput.onchange = () => {
            if (fileInput.files.length > 0) {
                const fileName = fileInput.parentElement.querySelector(".file-name");
                const fileLabel = fileInput.parentElement.querySelector(".file-label");
                fileName.textContent = fileInput.files[0].name;
                fileLabel.textContent = "✅ Ready to Upload"
            }
        };
    }

    window.onload = apagee__init;
    window.document.onload = apagee__init;
})();