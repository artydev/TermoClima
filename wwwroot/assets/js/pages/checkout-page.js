import { Cart, showNotification } from '../cart.js';
import { formatPrice } from '../format.js';
import { renderHeader } from '../header.js';
import { renderFooter } from '../footer.js';
import { html } from '../utils/html.js';

renderHeader();
renderFooter();

const root = document.getElementById('checkout-root');

function isValidEmail(value) {
  return /\S+@\S+\.\S+/.test(value);
}

function renderForm(items, subtotal) {
  root.innerHTML = html`
    <h1 class="page-title">Checkout</h1>

    <div class="checkout-layout">
      <form class="checkout-form" id="checkout-form">
        <h2 class="form-section-title">I tuoi dati</h2>

        <label class="field">
          <span>Nome completo *</span>
          <input type="text" name="customerName" placeholder="Mario Rossi" required />
        </label>

        <label class="field">
          <span>Email *</span>
          <input type="email" name="email" placeholder="mario@example.com" required />
        </label>

        <label class="field">
          <span>Telefono</span>
          <input type="tel" name="phone" placeholder="333 1234567" />
        </label>

        <h2 class="form-section-title">Consegna</h2>

        <label class="field">
          <span>Modalità</span>
          <select name="deliveryMethod">
            <option value="Ritiro in negozio">Ritiro in negozio</option>
            <option value="Consegna a domicilio">Consegna a domicilio</option>
          </select>
        </label>

        <label class="field">
          <span>Indirizzo</span>
          <input type="text" name="address" placeholder="Via Roma 1" />
        </label>

        <div class="field-row">
          <label class="field">
            <span>Città</span>
            <input type="text" name="city" />
          </label>
          <label class="field">
            <span>CAP</span>
            <input type="text" name="postalCode" />
          </label>
        </div>

        <label class="field">
          <span>Note (opzionale)</span>
          <textarea name="notes" placeholder="Orari preferiti, richieste particolari…"></textarea>
        </label>

        <p class="form-error" id="form-error" style="display:none"></p>

        <button type="submit" class="button button-primary button-block" id="submit-order-btn">
          Invia richiesta d'ordine — ${formatPrice(subtotal)}
        </button>
        <p class="summary-note" style="margin-top:4px">
          Non è richiesto alcun pagamento online. Ti contatteremo per confermare l'ordine e concordare
          il pagamento (contanti, bonifico, POS in negozio o Klarna).
        </p>
      </form>

      <aside class="order-summary">
        <h2 class="form-section-title">Riepilogo ordine</h2>
        ${items.map(item => `
          <div class="summary-line">
            <span>${item.name} × ${item.qty}</span>
            <span>${formatPrice(item.price * item.qty, item.currency)}</span>
          </div>
        `).join('')}
        <div class="summary-row summary-total">
          <span>Totale</span>
          <span>${formatPrice(subtotal)}</span>
        </div>
      </aside>
    </div>
  `;

  document.getElementById('checkout-form').addEventListener('submit', (e) => {
    e.preventDefault();
    submitOrder(items, subtotal);
  });
}

function renderConfirmation(order) {
  root.innerHTML = html`
    <div class="state-panel">
      <p class="state-title">Richiesta d'ordine inviata!</p>
      <p>
        Ordine <strong>#${order.id}</strong> ricevuto. Ti contatteremo a breve via email o telefono
        per confermare disponibilità e concordare il pagamento.
      </p>
      <a href="index.html" class="button button-primary">Continua gli acquisti</a>
    </div>
  `;
}

async function submitOrder(items, subtotal) {

    console.log(items);


  const form = document.getElementById('checkout-form');
  const btn = document.getElementById('submit-order-btn');
  const errorEl = document.getElementById('form-error');
  const data = Object.fromEntries(new FormData(form).entries());

  errorEl.style.display = 'none';

  if (!data.customerName?.trim()) {
    errorEl.textContent = 'Il nome è obbligatorio.';
    errorEl.style.display = 'block';
    return;
  }
  if (!isValidEmail(data.email || '')) {
    errorEl.textContent = 'Inserisci un indirizzo email valido.';
    errorEl.style.display = 'block';
    return;
  }

  const payload = {
    customerName: data.customerName.trim(),
    email: data.email.trim(),
    phone: (data.phone || '').trim(),
    address: (data.address || '').trim(),
    city: (data.city || '').trim(),
    postalCode: (data.postalCode || '').trim(),
    deliveryMethod: data.deliveryMethod || '',
    notes: (data.notes || '').trim(),
      items: items.map(item => ({
      productId: item.productId,
      name: item.name,
      qty: item.qty,
      price: item.price,
      currency: item.currency,
    })),
    subtotal,
  };

  btn.disabled = true;
  btn.textContent = 'Invio in corso…';

  try {
    const res = await fetch('/api/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    const body = await res.json().catch(() => null);

    if (!res.ok) {
      throw new Error(body?.error || body?.detail || `Richiesta fallita (${res.status})`);
    }

    Cart.clear();
    renderConfirmation(body);
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.style.display = 'block';
    showNotification(`Invio non riuscito: ${err.message}`);
    btn.disabled = false;
    btn.textContent = `Invia richiesta d'ordine — ${formatPrice(subtotal)}`;
  }
}

function init() {
    const items = Cart.get();

  const subtotal = Cart.subtotal();

  if (items.length === 0) {
    root.innerHTML = html`
      <div class="state-panel">
        <p class="state-title">Il carrello è vuoto.</p>
        <p>Aggiungi un prodotto prima di procedere al checkout.</p>
        <a href="index.html" class="button">Sfoglia i prodotti</a>
      </div>
    `;
    return;
  }

  renderForm(items, subtotal);
}

init();
