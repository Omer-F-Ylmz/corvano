import puppeteer from 'puppeteer';

const [, , url = 'http://localhost:3000', mode = 'dev'] = process.argv;
const browser = await puppeteer.launch({ args: ['--ignore-certificate-errors'] });
const page = await browser.newPage();
await page.setViewport({ width: 1440, height: 900 });
await page.goto(url, { waitUntil: 'networkidle0', timeout: 30000 });
const r = await page.evaluate(() => {
  const q = s => [...document.querySelectorAll(s)];
  let css = '';
  for (const s of document.styleSheets) { try { css += [...s.cssRules].map(x => x.cssText).join('\n'); } catch {} }
  const cls = q('[class]').map(e => e.getAttribute('class')).join(' ');
  const text = document.body.innerText || '';
  const has = p => css.includes(p) || cls.includes(p.slice(1) + ':');
  return {
    lang: document.documentElement.lang || null,
    h1: q('h1').length,
    main: q('main').length,
    header: q('header').length,
    footer: q('footer').length,
    imgNoAlt: q('img').filter(i => !i.hasAttribute('alt')).length,
    imgNoDims: q('img').filter(i => !i.hasAttribute('width') || !i.hasAttribute('height')).length,
    interactive: q('a,button,[role=button],input,select,textarea').length,
    hover: has(':hover'),
    focusVisible: has(':focus-visible'),
    active: has(':active'),
    transitionAll: /transition(-property)?\s*:\s*all\b/.test(css) || /\btransition-all\b/.test(cls),
    animated: /@keyframes|animation\s*:/.test(css),
    reducedMotion: css.includes('prefers-reduced-motion'),
    fontDisplaySwap: !css.includes('@font-face') || css.includes('font-display: swap'),
    cdnTailwind: q('script[src*="cdn.tailwindcss.com"]').length > 0,
    slop: {
      emoji: (text.match(/\p{Extended_Pictographic}/gu) || []).length,
      backdropBlur: /backdrop-filter\s*:/.test(css) || /\bbackdrop-blur/.test(cls),
      loremIpsum: /lorem ipsum/i.test(text),
      purpleClasses: (cls.match(/\b\w+-(purple|indigo|violet|fuchsia)-\d+/g) || []).length
    }
  };
});
await browser.close();
const f = [];
if (!r.lang) f.push('html[lang] eksik');
if (r.h1 !== 1) f.push(`h1 sayısı ${r.h1}, 1 olmalı`);
if (r.main !== 1) f.push(`main sayısı ${r.main}, 1 olmalı`);
if (!r.header || !r.footer) f.push('header/footer landmark eksik');
if (r.imgNoAlt) f.push(`${r.imgNoAlt} img alt'sız`);
if (r.imgNoDims) f.push(`${r.imgNoDims} img width/height'sız`);
if (r.interactive && !(r.hover && r.focusVisible && r.active)) f.push('hover/focus-visible/active stillerinden biri yok');
if (r.transitionAll) f.push('transition-all kullanılmış');
if (r.animated && !r.reducedMotion) f.push('animasyon var, prefers-reduced-motion yok');
if (!r.fontDisplaySwap) f.push('@font-face var, font-display: swap yok');
if (mode === 'prod' && r.cdnTailwind) f.push('prod modda Tailwind CDN');
const s = [];
if (r.slop.emoji) s.push(`${r.slop.emoji} emoji`);
if (r.slop.backdropBlur) s.push('backdrop-filter blur');
if (r.slop.loremIpsum) s.push('lorem ipsum');
if (r.slop.purpleClasses) s.push(`${r.slop.purpleClasses} mor/indigo sınıfı`);
console.log(JSON.stringify(r, null, 2));
console.log('SLOP: ' + (s.length ? s.join(', ') : '-'));
console.log(f.length ? 'FAIL\n- ' + f.join('\n- ') : 'PASS');
process.exit(f.length ? 1 : 0);
