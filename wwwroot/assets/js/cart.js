const STORAGE_KEY = 'termoclima:cart';
const shopChannel = new BroadcastChannel('termoclima_cart_sync');

export const Cart = {
  get() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      const data = raw ? JSON.parse(raw) : [];
    
      return data;
    } catch {
      return [];
    }
  },

  save(items) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    } catch {
      // storage unavailable (private browsing, quota, etc.) — fail silently
    }
    shopChannel.postMessage({ type: 'CART_UPDATED' });
    window.dispatchEvent(new CustomEvent('cartChange'));
  },

  add(product, qty = 1) {
    const items = this.get();
    const existing = items.find(item => item.productId === product.id);
    if (existing) {
      existing.qty += qty;
    } else {
      items.push({
        productId: product.id,
        name: product.name,
        price: product.price,
        currency: product.currency,
        image: product.image_url,
        qty
      });
    }

    this.save(items);
    shopChannel.postMessage({ type: 'SHOW_NOTIF', message: `${product.name} aggiunto al carrello` });
    showNotification(`${product.name} aggiunto al carrello`);
  },

  setQty(productId, qty) {
    if (qty <= 0) {
      this.remove(productId);
      return;
    }
    const items = this.get();
    const item = items.find(i => i.productId === productId);
    if (!item) return;
    item.qty = qty;
    this.save(items);
  },

  remove(productId) {
    const items = this.get().filter(item => item.productId !== productId);
    this.save(items);
  },

  clear() {
    this.save([]);
  },

  count() {
    return this.get().reduce((sum, item) => sum + item.qty, 0);
  },

  subtotal() {
    return this.get().reduce((sum, item) => sum + item.price * item.qty, 0);
  }
};

// Keep every open tab in sync
shopChannel.onmessage = (event) => {
  if (event.data.type === 'CART_UPDATED') {
    window.dispatchEvent(new CustomEvent('cartChange'));
  }
  if (event.data.type === 'SHOW_NOTIF') {
    showNotification(event.data.message);
  }
};

export function showNotification(message) {
  let container = document.getElementById('notification-container');
  if (!container) {
    container = document.createElement('div');
    container.id = 'notification-container';
    document.body.appendChild(container);
  }
  const toast = document.createElement('div');
  toast.className = 'toast';
  toast.textContent = message;
  container.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}
