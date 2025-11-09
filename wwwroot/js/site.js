class Apagee
{
    constructor()
    {
        this.tagCodeLanguages();
    }

    tagCodeLanguages()
    {
        try
        {
            const findPrefix = 'language-';
            const className = 'lang-id';
            document.querySelectorAll(`pre > code[class^="${findPrefix}"]`).forEach(code =>
            {
                const pre = code.parentElement;

                const langClass = [...code.classList].find(c => c.startsWith(findPrefix));
                if (!langClass)
                {
                    return;
                }

                const lang = langClass.replace(findPrefix, '').trim();
                if (!lang || pre.querySelector('.' + className))
                {
                    return;
                }

                const span = document.createElement('span');
                span.className = className;
                span.textContent = lang;
                pre.appendChild(span);
            });
        } 
        catch (e)
        {
            console.error("Code language visual tag error.", e);
        }
    }
}
window.addEventListener('DOMContentLoaded', () => window._apagee = new Apagee());
