import { getProductById } from '../api.js';
import { formatPrice, getStockStatus } from '../format.js';
import { Cart } from '../cart.js';
import { renderHeader } from '../header.js';
import { renderFooter } from '../footer.js';
import { html } from '../utils/html.js';

renderHeader('prodotti');
renderFooter();

const id = new URLSearchParams(window.location.search).get('id') || '';
const root = document.getElementById('product-root');

function cartQty(productId) {
  const item = Cart.get().find(i => i.productId === productId);
  return item ? item.qty : 0;
}

function addOrIncrement(product) {
  const qty = cartQty(product.id);
  if (qty === 0) {
    Cart.add(product, 1);
  } else {
    Cart.setQty(product.id, qty + 1);
  }
  render(product);
}

function decrement(product) {
  const qty = cartQty(product.id);
  if (qty > 0) Cart.setQty(product.id, qty - 1);
  render(product);
}

// "specifications" is a free-form object straight from the catalog (energy
// class, power output, color, etc.) — render whatever keys are actually
// present rather than assuming a fixed set.
function specRows(product) {
  const specs = product.specifications;
  if (!specs || typeof specs !== 'object') return [];
  return Object.entries(specs).filter(([, value]) => value !== '' && value != null);
}

// Certificate/delivery/payment/dimensions are each optional on a given
// product (a service listing like maintenance won't have dimensions; a
// pellet bag won't have delivery details on every entry) — only show rows
// that actually have something to say.
function infoRows(product) {
  const rows = [];
  if (product.certificate) rows.push(['Certificazione', product.certificate]);
  if (product.delivery) rows.push(['Consegna', product.delivery]);
  if (product.payment) rows.push(['Pagamento', product.payment]);

  const dims = product.dimensions;
  if (dims && (dims.length || dims.width || dims.height)) {
    const parts = [dims.length, dims.width, dims.height].filter(Boolean);
    rows.push(['Dimensioni (L × P × A)', parts.join(' × ')]);
  }

  return rows;
}

function specBlockHTML(title, rows) {
  if (rows.length === 0) return '';
  return html`
    <div class="product-extra-block">
      <h2 class="product-extra-title">${title}</h2>
      <dl class="spec-list">
        ${rows.map(([label, value]) => `
          <div class="spec-row">
            <dt>${label}</dt>
            <dd>${value}</dd>
          </div>
        `).join('')}
      </dl>
    </div>
  `;
}

function render(product) {
  const stock = getStockStatus(product.availability);
  const qty = cartQty(product.id);
  const inCart = qty > 0;
  const category = product.sub_category
    ? `${product.category} · ${product.sub_category}`
    : product.category;

  const extraHTML = [
    specBlockHTML('Caratteristiche', specRows(product)),
    specBlockHTML('Informazioni', infoRows(product)),
  ].join('');

  root.innerHTML = html`
    <a href="index.html" class="back-link">← Torna al catalogo</a>

    <div class="product-detail">
      <div class="product-media">
        <img src="${product.image_url}" alt="${product.name}" loading="eager" decoding="async" fetchpriority="high" />
      </div>

      <div class="product-info">
        <p class="eyebrow">${category}</p>
        <h1 class="product-title">${product.name}</h1>
        <p class="product-price">${formatPrice(product.price, product.currency)}</p>
        <p class="product-desc">${product.description}</p>

        <p class="availability ${stock.code}">
          ${stock.label}
        </p>

        <div class="qty-row">
          <span class="qty-label">Quantità</span>
          <div class="qty-control">
            <button class="qty-btn" id="qty-dec" ${!stock.purchasable ? 'disabled' : ''}>−</button>
            <input class="qty-input" type="number" min="0" value="${qty}" id="qty-input" ${!stock.purchasable ? 'disabled' : ''} />
            <button class="qty-btn" id="qty-inc" ${!stock.purchasable ? 'disabled' : ''}>+</button>
          </div>
          ${inCart ? `<button class="qty-clear" id="qty-clear" aria-label="Rimuovi dal carrello" title="Rimuovi dal carrello">✕</button>` : ''}
        </div>

        ${inCart
          ? `<a class="button button-primary" href="carrello.html">Vai al Carrello →</a>`
          : `<button class="button button-primary" id="add-btn" ${!stock.purchasable ? 'disabled' : ''}>Aggiungi al carrello</button>`
        }
      </div>
    </div>

    ${extraHTML ? `<div class="product-extra">${extraHTML}</div>` : ''}
  `;

  document.getElementById('qty-dec')?.addEventListener('click', () => decrement(product));
  document.getElementById('qty-inc')?.addEventListener('click', () => addOrIncrement(product));
  document.getElementById('qty-clear')?.addEventListener('click', () => { Cart.remove(product.id); render(product); });
  document.getElementById('add-btn')?.addEventListener('click', () => addOrIncrement(product));

  const qtyInput = document.getElementById('qty-input');
  qtyInput?.addEventListener('input', (e) => {
    const val = parseInt(e.target.value, 10);
    if (!isNaN(val) && val >= 0) {
      if (val === 0) {
        Cart.remove(product.id);
      } else if (qty === 0) {
        Cart.add(product, val);
      } else {
        Cart.setQty(product.id, val);
      }
      render(product);
    }
  });
  qtyInput?.addEventListener('blur', (e) => {
    const val = parseInt(e.target.value, 10);
    if (isNaN(val) || val < 0) {
      e.target.value = cartQty(product.id);
    }
  });
}

async function init() {
  if (!id) {
    root.innerHTML = html`
      <div class="state-panel">
        <p class="state-title">Prodotto non trovato.</p>
        <a href="index.html" class="button">Torna al catalogo</a>
      </div>
    `;
    return;
  }
  root.innerHTML = `<div class="state-panel"><span class="spinner"></span><p>Caricamento prodotto…</p></div>`;
  try {
    const product = await getProductById(id);
    render(product);
  } catch {
    root.innerHTML = html`
      <div class="state-panel">
        <p class="state-title">Prodotto non trovato.</p>
        <a href="index.html" class="button">Torna al catalogo</a>
      </div>
    `;
  }
}

init();
