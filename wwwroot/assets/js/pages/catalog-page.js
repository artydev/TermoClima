import { getProducts } from '../api.js';
import { formatPrice, getStockStatus } from '../format.js';
import { renderHeader } from '../header.js';
import { renderFooter } from '../footer.js';
import { html } from '../utils/html.js';

renderHeader('prodotti');
renderFooter();

let allProducts = [];
let activeCategory = 'ALL';

// Only the first row of cards is guaranteed to be above the fold on load;
// everything after that should stay lazy so we don't fetch images the
// visitor may never scroll to.
const EAGER_IMAGE_COUNT = 4;

function productCardHTML(p, index) {
  const stock = getStockStatus(p.availability);
  const eager = index < EAGER_IMAGE_COUNT;
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

function renderFilters() {
  const categories = ['ALL', ...new Set(allProducts.map(p => p.category).filter(Boolean))];
  const row = document.getElementById('filter-row');
  row.innerHTML = categories.map(c =>
    `<button class="pill ${c === activeCategory ? 'pill-active' : ''}" data-category="${c}">${c}</button>`
  ).join('');
  row.querySelectorAll('.pill').forEach(btn => {
    btn.addEventListener('click', () => {
      activeCategory = btn.dataset.category;
      renderFilters();
      renderGrid();
    });
  });
}

function renderGrid() {
  const filtered = activeCategory === 'ALL'
    ? allProducts
    : allProducts.filter(p => p.category === activeCategory);

  const grid = document.getElementById('product-grid');
  const state = document.getElementById('catalog-state');

  if (filtered.length === 0) {
    grid.innerHTML = '';
    state.innerHTML = `<div class="state-panel"><p class="state-title">Nessun prodotto in questa categoria.</p></div>`;
    return;
  }

  state.innerHTML = `<p class="result-count">${filtered.length} ${filtered.length === 1 ? 'prodotto' : 'prodotti'}</p>`;
  grid.innerHTML = filtered.map(productCardHTML).join('');
}

async function init() {
  const state = document.getElementById('catalog-state');
  state.innerHTML = `<div class="state-panel"><span class="spinner"></span><p>Caricamento prodotti…</p></div>`;
  try {
    allProducts = await getProducts();

    // Support deep-linking into a pre-filtered view, e.g. from the promo
    // page's category links (index.html?category=Stufe%20a%20pellet).
    const requestedCategory = new URLSearchParams(window.location.search).get('category');
    if (requestedCategory) {
      const match = allProducts.find(p => p.category?.toLowerCase() === requestedCategory.toLowerCase());
      if (match) activeCategory = match.category;
    }

    renderFilters();
    renderGrid();
  } catch (err) {
    state.innerHTML = html`
      <div class="state-panel state-error">
        <p class="state-title">Impossibile caricare il catalogo.</p>
        <p>${err.message}</p>
        <button class="button" id="retry-btn">Riprova</button>
      </div>
    `;
    document.getElementById('retry-btn').addEventListener('click', init);
  }
}

init();
