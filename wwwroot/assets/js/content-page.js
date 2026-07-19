/**
 * Renders a simple eyebrow/title/body page from pages.json into #page-root.
 * @param {'chiSiamo' | 'assistenza'} key
 */
export async function renderContentPage(key) {
  const root = document.getElementById('page-root');
  if (!root) return;

  root.innerHTML = `<div class="state-panel"><span class="spinner"></span></div>`;

  try {
    const res = await fetch('/pages.json');
    if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
    const data = await res.json();
    const { eyebrow, title, body } = data[key];

    root.innerHTML = `
      <p class="eyebrow">${eyebrow}</p>
      <h1 class="page-title">${title}</h1>
      ${body.map(p => `<p class="hero-copy">${p}</p>`).join('')}
    `;
  } catch {
    root.innerHTML = `
      <div class="state-panel state-error">
        <p class="state-title">Impossibile caricare la pagina.</p>
      </div>
    `;
  }
}
