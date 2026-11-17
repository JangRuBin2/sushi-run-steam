// 시제품 캐릭터 미리보기 캡처 (Unity 없이). 저장소 루트에서:
//   (cd tools/character-preview && npm install)
//   python3 -m http.server 8765        (다른 터미널, 저장소 루트)
//   node tools/character-preview/shot.js   → docs/renders/s1-placeholder-character.png
// index.html은 Runtime/Player/SushiToyBuilder.cs를 손으로 옮긴 것 — 빌더를 바꾸면 같이 고쳐요.
const { chromium } = require('playwright');
const path = require('path');

const port = process.env.PORT || '8765';

(async () => {
  const browser = await chromium.launch({
    args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'],
  });
  const page = await browser.newPage({ viewport: { width: 1500, height: 900 }, deviceScaleFactor: 1 });
  page.on('pageerror', (e) => console.log('pageerror:', e.message));
  await page.goto(`http://127.0.0.1:${port}/tools/character-preview/index.html`);
  await page.waitForSelector('body[data-done="1"]', { timeout: 60000 });
  await page.waitForTimeout(500);
  const out = path.join(__dirname, '../../docs/renders/s1-placeholder-character.png');
  await page.screenshot({ path: out, fullPage: true });
  console.log('saved', out);
  await browser.close();
})();
