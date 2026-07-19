const year = new Date().getFullYear();

export async function renderFooter() {
  const root = document.getElementById('footer-root');
  if (!root) return;

  root.innerHTML = `
    <footer class="app-footer" id="contatti" aria-label="Piè di pagina del sito">
      <div class="footer-loading" aria-live="polite">Caricamento…</div>
    </footer>
  `;

  let data;
  try {
    const res = await fetch('/footer.json');
    if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
    data = await res.json();
  } catch {
    root.innerHTML = `
      <footer class="app-footer" id="contatti" aria-label="Piè di pagina del sito">
        <div class="footer-loading footer-error" aria-live="assertive">
          Impossibile caricare le informazioni del footer.
        </div>
      </footer>
    `;
    return;
  }

  const { legal, stores } = data;

  const storesHTML = stores.map(store => `
    <div class="footer-store">
      <img src="/assets/logo.png" alt="TermoClima logo" class="footer-logo" width="499" height="132" loading="lazy" decoding="async" />

      <h2 class="footer-company">${store.name}</h2>
      <hr class="footer-divider" />

      <address class="footer-address">
        <p>${store.addressLine1}</p>
        <p>${store.addressLine2}</p>
        <p><a href="${store.telHref}" class="footer-phone">Tel: ${store.tel}</a></p>
        <p><a href="${store.cellHref}" class="footer-phone">Cell: ${store.cell}</a></p>
      </address>

      <h3 class="footer-hours-title">${store.hoursTitle}</h3>
      <dl class="footer-hours">
        ${store.hours.map(h => `
          <div>
            <dt>${h.day}</dt>
            <dd>${h.time}</dd>
          </div>
        `).join('')}
      </dl>
    </div>
  `).join('');

  root.innerHTML = `
    <footer class="app-footer" id="contatti" aria-label="Piè di pagina del sito">
      <div class="footer-inner">
        ${storesHTML}
      </div>

      <div class="footer-legal" style="display:none">
        <span>© ${year} ${legal.company} — ${legal.vatNumber}</span>
        <div class="footer-legal-links">
          <a href="${legal.privacyPolicyUrl}" class="footer-legal-link">Privacy Policy</a>
          <a href="${legal.cookiePolicyUrl}" class="footer-legal-link">Cookie Policy</a>
        </div>
      </div>
    </footer>
  `;
}
