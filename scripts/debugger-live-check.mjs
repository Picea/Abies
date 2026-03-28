import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { chromium } from 'playwright';

const root = process.cwd();

const mime = (file) => {
    if (file.endsWith('.js')) return 'text/javascript; charset=utf-8';
    if (file.endsWith('.html')) return 'text/html; charset=utf-8';
    if (file.endsWith('.css')) return 'text/css; charset=utf-8';
    return 'application/octet-stream';
};

const server = http.createServer((req, res) => {
    const reqPath = decodeURIComponent((req.url || '/').split('?')[0]);
    const safePath = reqPath === '/' ? '/index.html' : reqPath;
    const fullPath = path.normalize(path.join(root, safePath));

    if (!fullPath.startsWith(root) || !fs.existsSync(fullPath) || fs.statSync(fullPath).isDirectory()) {
        res.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
        res.end('Not found');
        return;
    }

    res.writeHead(200, { 'content-type': mime(fullPath) });
    res.end(fs.readFileSync(fullPath));
});

await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
const port = typeof address === 'object' && address ? address.port : 0;
const baseUrl = `http://127.0.0.1:${port}`;

const browser = await chromium.launch({ headless: true });

async function assertToggle(page, shellSelector, panelSelector, label) {
    await page.waitForSelector(shellSelector, { state: 'visible', timeout: 10000 });
    await page.click(shellSelector);
    await page.waitForSelector(panelSelector, { state: 'visible', timeout: 10000 });
    await page.click(shellSelector);
    await page.waitForSelector(panelSelector, { state: 'hidden', timeout: 10000 });
    console.log(`PASS ${label}`);
}

try {
    {
        const page = await browser.newPage();
        await page.goto(`${baseUrl}/index.html`);
        await page.evaluate(async () => {
            const mount = document.createElement('div');
            mount.id = 'abies-debugger-timeline';
            document.body.appendChild(mount);

            window.__abiesDebugger = { enabled: true };
            const runtime = await import('/Picea.Abies.Browser/wwwroot/abies.js');
            runtime.setupEventDelegation();
            await import('/Picea.Abies.Browser/wwwroot/debugger.js');
        });

        await assertToggle(
            page,
            '[data-abies-debugger-shell="1"]',
            '[data-abies-debugger-panel="1"]',
            'browser-runtime badge toggles panel'
        );
        await page.close();
    }

    {
        const page = await browser.newPage();
        await page.goto(`${baseUrl}/index.html`);
        await page.evaluate(() => {
            const mount = document.createElement('div');
            mount.id = 'abies-debugger-timeline';
            document.body.appendChild(mount);

            const script = document.createElement('script');
            script.src = '/Picea.Abies.Server.Kestrel/wwwroot/_abies/abies-server.js';
            script.setAttribute('data-ws-path', '/noop');
            script.setAttribute('data-app-root', '#app');
            document.head.appendChild(script);
        });

        await assertToggle(
            page,
            '[data-abies-debugger-shell="1"]',
            '[data-abies-debugger-panel="1"]',
            'server-runtime badge toggles panel'
        );
        await page.close();
    }
} finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
}