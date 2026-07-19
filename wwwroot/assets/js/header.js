import { Cart } from './cart.js';

function cartBadgeHTML() {
  const count = Cart.count();
  return html`
    <a class="cart-badge" href="carrello.html" aria-label="Carrello${count > 0 ? `, ${count} articoli` : ''}">
      <span class="cart-icon-wrap">
        <svg class="cart-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z" />
          <line x1="3" y1="6" x2="21" y2="6" />
          <path d="M16 10a4 4 0 0 1-8 0" />
        </svg>
        ${count > 0 ? `<span class="cart-count">${count}</span>` : ''}
      </span>
      <span class="cart-label">Carrello</span>
    </a>
  `;
}

function scrollToContatti(e) {
  e.preventDefault();
  const menu = document.querySelector('.mobile-menu');
  if (menu) menu.classList.remove('mobile-menu-open');
  const el = document.getElementById('contatti');
  if (el) {
    const elementPosition = el.getBoundingClientRect().top + window.scrollY;
    const headerHeight = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--header-height')) || 93;
    window.scrollTo({ top: elementPosition - headerHeight, behavior: 'smooth' });
  }
}

function html(strings, ...values) {
  return strings.reduce((result, string, i) => result + string + (values[i] || ''), '');
}

/**
 * Renders the header + mobile menu into #header-root.
 * @param {'promozione' | 'prodotti' | 'chi-siamo' | 'assistenza' | undefined} activePage
 */
export function renderHeader(activePage) {
  const root = document.getElementById('header-root');
  if (!root) return;

  const active = id => (id === activePage ? 'nav-link active' : 'nav-link');
  const activeMobile = id => (id === activePage ? 'mobile-nav-link active' : 'mobile-nav-link');

  root.innerHTML = html`
    <header class="nav">
      <a href="index.html" class="brand">
        <img src="/assets/logo.png" alt="TermoClima" class="brand-logo" width="499" height="132" fetchpriority="high" />
      </a>

      <nav class="nav-menu" aria-label="Menu principale">
        <a class="nav-link nav-link-promo" href="promozione.html">🔥Promozione</a>
        <a class="${active('prodotti')}" href="index.html">Prodotti</a>
        <a class="${active('chi-siamo')}" href="chi-siamo.html">Chi Siamo</a>
        <a class="${active('assistenza')}" href="assistenza.html">Assistenza</a>
        <a href="#contatti" class="nav-link" id="nav-contatti">Contatti</a>
      </nav>

      <div class="nav-right" id="cart-badge-slot"></div>
    </header>

    <div class="mobile-menu" aria-hidden="true">
      <nav class="mobile-nav" aria-label="Menu mobile">
        <a href="promozione.html" class="mobile-nav-link mobile-nav-link-promo">🔥 Promozione</a>
        <a href="index.html" class="${activeMobile('prodotti')}">Prodotti</a>
        <a href="chi-siamo.html" class="${activeMobile('chi-siamo')}">Chi Siamo</a>
        <a href="assistenza.html" class="${activeMobile('assistenza')}">Assistenza</a>
        <a href="#contatti" class="mobile-nav-link" id="mobile-nav-contatti">Contatti</a>
      </nav>
    </div>
  `;

  // Insert the hamburger button + cart badge together (cart count needs live updates)
  const navRight = root.querySelector('.nav-right');
  const hamburger = document.createElement('button');
  hamburger.className = 'hamburger';
  hamburger.setAttribute('aria-label', 'Apri menu');
  hamburger.setAttribute('aria-expanded', 'false');
  hamburger.innerHTML = `
    <span class="hamburger-bar"></span>
    <span class="hamburger-bar"></span>
    <span class="hamburger-bar"></span>
  `;

  function refreshCartBadge() {
    const slot = document.getElementById('cart-badge-slot');
    if (!slot) return;
    slot.innerHTML = cartBadgeHTML();
  }
  refreshCartBadge();
  navRight.appendChild(hamburger);

  hamburger.addEventListener('click', () => {
    const menu = root.querySelector('.mobile-menu');
    const isOpen = menu.classList.toggle('mobile-menu-open');
    hamburger.classList.toggle('hamburger-open', isOpen);
    hamburger.setAttribute('aria-label', isOpen ? 'Chiudi menu' : 'Apri menu');
    hamburger.setAttribute('aria-expanded', String(isOpen));
    menu.setAttribute('aria-hidden', String(!isOpen));
  });

  root.querySelector('#nav-contatti').addEventListener('click', scrollToContatti);
  root.querySelector('#mobile-nav-contatti').addEventListener('click', scrollToContatti);

  window.addEventListener('cartChange', refreshCartBadge);

  // Keep --header-height in sync with the real, rendered header height.
  // The header is position:fixed (taken out of flow), so both the page
  // content (.main) and the mobile dropdown rely on this variable to sit
  // correctly below it — this avoids hardcoded pixel guesses that drift
  // whenever the logo size or padding changes across breakpoints.
  const navEl = root.querySelector('.nav');
  const setHeaderHeightVar = () => {
    document.documentElement.style.setProperty('--header-height', `${navEl.offsetHeight}px`);
  };
  setHeaderHeightVar();
  if (window.ResizeObserver) {
    new ResizeObserver(setHeaderHeightVar).observe(navEl);
  } else {
    window.addEventListener('resize', setHeaderHeightVar);
  }
  // Logo may finish loading after layout, changing the header's height.
  const logoImg = navEl.querySelector('.brand-logo');
  if (logoImg && !logoImg.complete) {
    logoImg.addEventListener('load', setHeaderHeightVar, { once: true });
  }
}
