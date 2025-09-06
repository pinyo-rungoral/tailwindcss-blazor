const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
const host = window.location.host;
const ws = new WebSocket(`${protocol}//${host}/ws`);
let cssfile = "tailwind.css";
ws.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log('ws.onmessage', data);
    switch(data.type) {
        case 'tailwind-output':
            console.log('Tailwind Output:', data.message);
            if(data.message.startsWith('Done in')) {
                updateCssByPath({isFirstLoad: false});
            }
            break;
        case 'tailwind-error':
            console.error('Tailwind Error:', data.message);
            break;
        case 'tailwind-started':
            console.log('Tailwind Started:', data.message);
            break;
        case 'tailwind-stopped':
            console.log('Tailwind Stopped:', data.message);
            break;
        case '__CSS_FILE__':
            console.log('__CSS_FILE__', data.message);
            cssfile = data.message;
            updateCssByPath({isFirstLoad: true});
            break;

    }
};

function removeLinkTag(){
    // Need to remove every time because dotnet hot reload will re-create
    console.log('Remove link tag...');
    const nameWithOutExt = cssfile.split('.')[0];
    const regex = new RegExp(`^${document.baseURI}${nameWithOutExt}.*\\.css$`);
    const elements = [...document.querySelectorAll('link[href]')]
        .filter((link) => regex.test(link.href));
    if (elements.length === 1) {
        elements[0].remove();
        console.log('Remove link tag completed');
    }
}

function updateCssByPath({isFirstLoad = false}) {
    fetch(cssfile)
        .then(response => response.text())
        .then(css => {
            let styleElement = document.getElementById(cssfile);
            if (!styleElement) {
                console.log('styleElement is null')
                styleElement = document.getElementById(cssfile);
                styleElement = document.createElement('style');
                styleElement.id = cssfile;
                styleElement.innerHTML = css;
                document.body.appendChild(styleElement);
            }else{
                styleElement.innerHTML = css;
            }
            console.log('Tailwind hot reload completed.', styleElement);
            removeLinkTag()
        });
}