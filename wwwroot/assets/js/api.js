// Same-origin now that this frontend is served from wwwroot by our own API —
// no more external mockapi.io dependency, and no CORS involved on this path.
const API_URL = "/products";

// Simple client-side cache so repeat visits/navigations within a few minutes
// don't refetch the whole catalog every time. Uses localStorage (survives
// across page loads, since this is a multi-page app, not an SPA) with a
// short TTL so the data can't go stale for long.
const CACHE_PREFIX = 'termoclima:cache:';
const CACHE_TTL_MS = 3 * 60 * 1000; // 3 minutes

function readCache(key) {
  try {
    const raw = localStorage.getItem(CACHE_PREFIX + key);
    if (!raw) return null;
    const { data, timestamp } = JSON.parse(raw);
    if (Date.now() - timestamp > CACHE_TTL_MS) return null;
    return data;
  } catch {
    return null; // storage unavailable/corrupt — just skip the cache
  }
}

function writeCache(key, data) {
  try {
    localStorage.setItem(CACHE_PREFIX + key, JSON.stringify({ data, timestamp: Date.now() }));
  } catch {
    // storage unavailable (private browsing, quota, etc.) — caching just won't happen
  }
}

async function fetchJSON(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
  return res.json();
}

// Requests for the same cache key that come in while one is already in
// flight share the same promise instead of firing duplicate network calls.
const inFlight = new Map();

async function cachedFetch(cacheKey, url) {
  const cached = readCache(cacheKey);
  if (cached) return cached;

  if (inFlight.has(cacheKey)) return inFlight.get(cacheKey);

  const promise = fetchJSON(url)
    .then(data => {
      writeCache(cacheKey, data);
      return data;
    })
    .finally(() => inFlight.delete(cacheKey));

  inFlight.set(cacheKey, promise);
  return promise;
}

export async function getProducts() {
  return cachedFetch('products', API_URL);
}

export async function getProductById(id) {
  // If we already have the full product list cached, reuse it instead of
  // making a second network request — this is the common path when someone
  // clicks from the catalog grid into a product's detail page.
  const cachedList = readCache('products');
  if (cachedList) {
    const found = cachedList.find(p => String(p.id) === String(id));
    if (found) return found;
  }

  return cachedFetch(`product:${id}`, `${API_URL}/${id}`);
}