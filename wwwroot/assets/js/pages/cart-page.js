import { Cart, showNotification } from '../cart.js';
import { formatPrice } from '../format.js';
import { renderHeader } from '../header.js';
import { renderFooter } from '../footer.js';
import { html } from '../utils/html.js';

renderHeader();
renderFooter();

const root = document.getElementById('cart-root');

function lineItemHTML(item) {
  return html`
    <div class="cart-row" data-id="${item.productId}">
      <img class="cart-row-image" src="${item.image}" alt="${item.name}" loading="lazy" decoding="async" fetchpriority="low" />

      <div class="cart-row-info">
        <p class="cart-row-name">${item.name}</p>
        <p class="cart-row-price">${formatPrice(item.price, item.currency)} cad.</p>
      </div>

      <div class="qty-control qty-control-compact">
        <button class="qty-btn" data-action="dec">−</button>
        <span class="qty-value">${item.qty}</span>
        <button class="qty-btn" data-action="inc">+</button>
      </div>

      <p class="cart-row-total">${formatPrice(item.price * item.qty, item.currency)}</p>

      <button class="remove-btn" data-action="remove" aria-label="Rimuovi ${item.name}">✕</button>
    </div>
  `;
}

// Replaces the old client-side mailto: link — the server now actually sends
// the inquiry via SMTP, rather than just handing a pre-filled draft to
// whatever mail client happens to be configured on the visitor's device.
async function sendInquiry(items, subtotal, btn) {
  const originalLabel = btn.textContent;
  btn.disabled = true;
  btn.textContent = 'Invio in corso…';

  try {
    const res = await fetch('/api/inquiry', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        items: items.map(item => ({
          name: item.name,
          qty: item.qty,
          price: item.price,
          currency: item.currency,
        })),
        subtotal,
      }),
    });

    if (!res.ok) {
      const problem = await res.json().catch(() => null);
      throw new Error(problem?.detail || problem?.error || `Richiesta fallita (${res.status})`);
    }

    showNotification('Richiesta inviata! Ti risponderemo il prima possibile.');
  } catch (err) {
    showNotification(`Invio non riuscito: ${err.message}`);
  } finally {
    btn.disabled = false;
    btn.textContent = originalLabel;
  }
}

function render() {
  const items = Cart.get();
  const subtotal = Cart.subtotal();

  if (items.length === 0) {
    root.innerHTML = html`
      <div class="state-panel">
        <p class="state-title">Il carrello è vuoto.</p>
        <a href="index.html" class="button">Sfoglia i prodotti</a>
      </div>
    `;
    return;
  }

  root.innerHTML = html`
    <h1 class="page-title">Il tuo carrello</h1>

    <div class="cart-list">
      ${items.map(lineItemHTML).join('')}
    </div>

    <div class="cart-summary">
      <div class="summary-row summary-total">
        <span>Subtotale</span>
        <span>${formatPrice(subtotal)}</span>
      </div>
      <p class="summary-note">Spese di spedizione e imposte da confermare in fase di preventivo.</p>
      <p class="summary-note">Non è richiesto alcun pagamento online. Ti contatteremo per confermare l'ordine e concordare il pagamento </p>
      <a href="checkout.html" class="button button-primary button-block">Procedi al checkout</a>
      <button class="button button-block" id="send-inquiry-btn" style="margin-top:10px; display:none">Invia lista via email</button>
    </div>
  `;

  document.getElementById('send-inquiry-btn').addEventListener('click', (e) => {
    sendInquiry(items, subtotal, e.currentTarget);
  });

  root.querySelectorAll('.cart-row').forEach(row => {
    const id = row.dataset.id;
    row.querySelector('[data-action="dec"]').addEventListener('click', () => {
      const item = Cart.get().find(i => i.productId === id);
      Cart.setQty(id, item.qty - 1);
    });
    row.querySelector('[data-action="inc"]').addEventListener('click', () => {
      const item = Cart.get().find(i => i.productId === id);
      Cart.setQty(id, item.qty + 1);
    });
    row.querySelector('[data-action="remove"]').addEventListener('click', () => {
      Cart.remove(id);
    });
  });
}

window.addEventListener('cartChange', render);
render();
