import { getProducts } from '../api.js';
import { formatPrice, getStockStatus } from '../format.js';
import { renderHeader } from '../header.js';
import { renderFooter } from '../footer.js';
import { html } from '../utils/html.js';

renderHeader('promozione');
renderFooter();

// How many catalog products to feature on the landing page.
const FEATURED_COUNT = 6;

// Pre-season campaign deadline: buy the stove over summer, get it installed
// before the autumn rush and the first cold snap.
const CAMPAIGN_DEADLINE = new Date('2026-09-30T23:59:59');

// Picks one representative product per category (preferring one that's
// actually purchasable) so the section shows the catalog's real breadth —
// heating, outdoor, climate control, maintenance — instead of whatever
// happens to come first in raw API order, which would otherwise just be a
// run of pellet bags.
function pickDiverseFeatured(products, count) {
  const byCategory = new Map();
  for (const p of products) {
    const cat = p.category || '';
    if (!byCategory.has(cat)) byCategory.set(cat, []);
    byCategory.get(cat).push(p);
  }

  const picked = [];
  for (const items of byCategory.values()) {
    if (picked.length >= count) break;
    const bestInCategory = items.find(p => getStockStatus(p.availability).purchasable) ?? items[0];
    picked.push(bestInCategory);
  }

  if (picked.length < count) {
    for (const p of products) {
      if (picked.length >= count) break;
      if (!picked.includes(p)) picked.push(p);
    }
  }

  return picked;
}

function featuredCardHTML(p, index) {
  const stock = getStockStatus(p.availability);
  const eager = index < 3;
  return html`
    <a class="card" href="prodotto.html?id=${p.id}">
      <div class="card-media">
        <img
          src="${p.image_url}"
          alt="${p.name}"
          loading="${eager ? 'eager' : 'lazy'}"
          decoding="async"
          fetchpriority="${eager ? 'high' : 'low'}"
        />
        <span class="stock-dot ${stock.code}" role="img" aria-label="${stock.label}" title="${stock.label}"></span>
      </div>
      <div class="card-body">
        <p class="eyebrow">${p.category}</p>
        <h3 class="card-title">${p.name}</h3>
        <p class="card-desc">${p.description}</p>
      </div>
      <div class="card-tag">
        <span class="tag-price">${formatPrice(p.price, p.currency)}</span>
      </div>
    </a>
  `;
}

async function renderFeatured() {
  const state = document.getElementById('lp-featured-state');
  const grid = document.getElementById('lp-featured-grid');
  const chipRow = document.getElementById('lp-category-chips');

  state.innerHTML = `<div class="state-panel"><span class="spinner"></span><p>Carichiamo qualche idea dal catalogo…</p></div>`;

  try {
    const products = await getProducts();

    state.innerHTML = '';
    grid.innerHTML = pickDiverseFeatured(products, FEATURED_COUNT).map(featuredCardHTML).join('');

    const categories = [...new Set(products.map(p => p.category).filter(Boolean))];
    chipRow.innerHTML = categories
      .map(c => `<a class="lp-chip" href="index.html?category=${encodeURIComponent(c)}">${c}</a>`)
      .join('') + `<a class="lp-chip" href="index.html">Tutto il catalogo</a>`;
  } catch (err) {
    state.innerHTML = html`
      <div class="state-panel state-error">
        <p class="state-title">Non riusciamo a caricare i prodotti in evidenza.</p>
        <p>${err.message}</p>
        <button class="button" id="lp-retry-btn">Riprova</button>
      </div>
    `;
    document.getElementById('lp-retry-btn').addEventListener('click', renderFeatured);
  }
}

function initCountdown() {
  const pad = n => String(n).padStart(2, '0');

  function tick() {
    const diff = CAMPAIGN_DEADLINE - Date.now();
    const unitsEl = document.getElementById('lp-countdown-units');
    if (diff <= 0) {
      unitsEl.innerHTML = `<p class="lp-countdown-title" style="margin:0">Offerta scaduta</p>`;
      return;
    }
    const d = Math.floor(diff / 86400000);
    const h = Math.floor((diff % 86400000) / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    const s = Math.floor((diff % 60000) / 1000);
    document.getElementById('lp-cd-days').textContent = pad(d);
    document.getElementById('lp-cd-hours').textContent = pad(h);
    document.getElementById('lp-cd-mins').textContent = pad(m);
    document.getElementById('lp-cd-secs').textContent = pad(s);
  }

  tick();
  setInterval(tick, 1000);
}

renderFeatured();
initCountdown();
