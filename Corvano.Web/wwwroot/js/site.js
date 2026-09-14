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

  // Sepet: beden seçilmeden gönderilirse odak seçiciye; JS varsa fetch + mini sepet, yoksa form /sepet'e gider.
  const miniCart = document.querySelector('[data-mini-cart]');
  const backdrop = document.querySelector('[data-mini-cart-backdrop]');
  let focusBeforeMiniCart = null;

  const closeMiniCart = () => {
    if (!miniCart || miniCart.hidden) {
      return;
    }
    miniCart.classList.remove('is-open');
    backdrop.classList.remove('is-open');
    window.setTimeout(() => {
      miniCart.hidden = true;
      backdrop.hidden = true;
    }, reduceMotion ? 0 : 400);
    if (focusBeforeMiniCart) {
      focusBeforeMiniCart.focus();
    }
  };

  const openMiniCart = (data) => {
    const item = miniCart.querySelector('[data-mini-cart-item]');
    item.replaceChildren();
    if (data.line.image) {
      const image = document.createElement('img');
      image.src = data.line.image;
      image.alt = data.line.imageAlt || '';
      image.width = 64;
      image.height = 80;
      item.append(image);
    }
    const text = document.createElement('p');
    const name = document.createElement('strong');
    name.textContent = data.line.name;
    text.append(name, document.createElement('br'), `Beden ${data.line.size} · ${data.line.quantity} adet · ${data.line.price}`);
    item.append(text);
    miniCart.querySelector('[data-mini-cart-message]').textContent = data.message;
    miniCart.querySelector('[data-mini-cart-subtotal]').textContent = data.subtotal;

    focusBeforeMiniCart = document.activeElement;
    miniCart.hidden = false;
    backdrop.hidden = false;
    window.requestAnimationFrame(() => {
      miniCart.classList.add('is-open');
      backdrop.classList.add('is-open');
    });
    miniCart.focus();
  };

  document.querySelectorAll('[data-mini-cart-close]').forEach((element) => element.addEventListener('click', closeMiniCart));
  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      closeMiniCart();
    }
  });

  for (const form of document.querySelectorAll('[data-add-to-cart]')) {
    const error = form.querySelector('[data-form-error]');
    const showError = (message) => {
      error.textContent = message;
      error.hidden = false;
    };

    form.addEventListener('change', () => { error.hidden = true; });
    form.addEventListener('submit', async (event) => {
      const radios = [...form.querySelectorAll('input[name="variantId"]')];
      if (radios.length > 0 && !radios.some((radio) => radio.checked)) {
        event.preventDefault();
        showError('Sepete eklemek için önce beden seçin.');
        (radios.find((radio) => !radio.disabled) || radios[0]).focus();
        return;
      }

      if (!miniCart || !window.fetch) {
        return;
      }

      event.preventDefault();
      const button = form.querySelector('[type="submit"]');
      button.setAttribute('aria-busy', 'true');
      try {
        const response = await fetch(form.action, {
          method: 'POST',
          body: new FormData(form),
          headers: { Accept: 'application/json' },
          credentials: 'same-origin'
        });
        const data = await response.json();
        if (!data.ok) {
          showError(data.message);
          return;
        }
        error.hidden = true;
        document.querySelectorAll('[data-cart-count]').forEach((count) => { count.textContent = data.itemCount; });
        document.querySelectorAll('[data-cart-link]').forEach((link) => link.setAttribute('aria-label', `Sepet: ${data.itemCount} ürün`));
        openMiniCart(data);
      } catch {
        form.submit();
      } finally {
        button.removeAttribute('aria-busy');
      }
    });
  }

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
