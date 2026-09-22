/**
 * Shared number formatting utilities for currency, percentage, and compact notations.
 * Robustly handles edge cases: null/undefined, NaN, zero, negative values, and very large numbers.
 */

export function formatCurrency(
  value: number | null | undefined,
  locale = "en-PH",
  currency = "PHP"
): string {
  if (value === null || value === undefined || isNaN(value)) {
    return new Intl.NumberFormat(locale, { style: "currency", currency }).format(0);
  }
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(value);
}

export function formatPercent(
  value: number | null | undefined,
  locale = "en-US",
  decimals = 1
): string {
  if (value === null || value === undefined || isNaN(value)) {
    return new Intl.NumberFormat(locale, {
      style: "percent",
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals
    }).format(0);
  }
  return new Intl.NumberFormat(locale, {
    style: "percent",
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  }).format(value);
}

export function formatCompactNumber(
  value: number | null | undefined,
  locale = "en-US"
): string {
  if (value === null || value === undefined || isNaN(value)) {
    return "0";
  }
  return new Intl.NumberFormat(locale, {
    notation: "compact",
    maximumFractionDigits: 1
  }).format(value);
}
