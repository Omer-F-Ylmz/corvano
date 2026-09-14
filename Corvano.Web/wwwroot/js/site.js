// Vitrin davranışları: mobil menü, görünür olunca bir kez açılan bloklar/eğriler ve galeri küçük görselleri.
(() => {
  const toggle = document.querySelector('[data-menu-toggle]');
  const menu = document.querySelector('[data-site-menu]');
  if (toggle && menu) {
    toggle.addEventListener('click', () => {
      const open = toggle.getAttribute('aria-expanded') !== 'true';
      toggle.setAttribute('aria-expanded', String(open));
      menu.classList.toggle('is-open', open);
    });
  }

  const revealables = document.querySelectorAll('[data-reveal], [data-seam]');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  if (reduceMotion || !('IntersectionObserver' in window)) {
    revealables.forEach((element) => element.classList.add('is-in'));
  } else {
    const observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-in');
          observer.unobserve(entry.target);
        }
      }
    }, { rootMargin: '0px 0px -8% 0px' });
    revealables.forEach((element) => observer.observe(element));
  }

  for (const gallery of document.querySelectorAll('[data-gallery]')) {
    const main = gallery.querySelector('.gallery__main');
    const thumbs = gallery.querySelectorAll('[data-gallery-thumb]');
    gallery.addEventListener('click', (event) => {
      const thumb = event.target.closest('[data-gallery-thumb]');
      if (!thumb || !main || main.tagName !== 'IMG') {
        return;
      }
      main.src = thumb.dataset.src;
      main.alt = thumb.dataset.alt;
      thumbs.forEach((other) => other.setAttribute('aria-pressed', String(other === thumb)));
    });
  }
})();
