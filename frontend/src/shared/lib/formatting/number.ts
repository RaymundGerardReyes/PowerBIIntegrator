export function formatCurrency(value: number, locale = "en-PH", currency = "PHP"): string {
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(value);
}
