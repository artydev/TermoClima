export function formatPrice(price, currency = '€') {
  if (!price) return 'On request';
  // Use Italian number formatting: dot as thousands separator, comma as decimal separator
  const formatted = new Intl.NumberFormat('it-IT', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(Number(price));
  // In Italian convention the currency symbol follows the amount with a space
  return `${formatted} ${currency}`;
}

// The real catalog has three distinct availability values, not two:
// "Sì", "Sì (scorta limitata)", and "No". Treating the middle one as plain
// "in stock" would hide a real signal from shoppers, and treating it as
// "out of stock" would incorrectly block a purchase that's still possible.
export function getStockStatus(availability) {
  const value = (availability || '').trim();

  if (/limitat/i.test(value)) {
    return { code: 'low-stock', label: 'Scorta limitata', purchasable: true };
  }
  if (value.startsWith('S')) {
    return { code: 'in-stock', label: 'Disponibile', purchasable: true };
  }
  return { code: 'out-stock', label: 'Non disponibile', purchasable: false };
}
