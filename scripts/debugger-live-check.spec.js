const { test, expect } = require('playwright/test');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');

let server;
let baseUrl;

function mime(file) {
  if (file.endsWith('.js')) return 'text/javascript; charset=utf-8';
  if (file.endsWith('.html')) return 'text/html; charset=utf-8';
  if (file.endsWith('.css')) return 'text/css; charset=utf-8';
  return 'application/octet-stream';
}

test.beforeAll(async () => {
  const root = process.cwd();

  server = http.createServer((req, res) => {
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
  baseUrl = `http://127.0.0.1:${port}`;
});

test.afterAll(async () => {
  if (!server) {
    return;
  }

  await new Promise((resolve) => server.close(resolve));
});

async function assertToggle(page) {
  const shell = page.locator('[data-abies-debugger-shell="1"]');
  const panel = page.locator('[data-abies-debugger-panel="1"]');

  await expect(shell).toBeVisible({ timeout: 10000 });
  await shell.click();
  await expect(panel).toBeVisible({ timeout: 10000 });
  await shell.click();
  await expect(panel).toBeHidden({ timeout: 10000 });
}

test('browser runtime badge toggles panel', async ({ page }) => {
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

  await assertToggle(page);
});

test('server runtime badge toggles panel', async ({ page }) => {
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

  await assertToggle(page);
});