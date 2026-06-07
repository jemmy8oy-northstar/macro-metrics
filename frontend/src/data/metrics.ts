export type Metric = { id: string; label: string };

export const METRICS: Metric[] = [
  { id: "uk-house-prices", label: "UK House Prices" },
  { id: "us-house-prices", label: "US House Prices" },
  { id: "uk-wages",        label: "UK Wages" },
  { id: "us-wages",        label: "US Wages" },
  { id: "uk-cpi",          label: "UK CPI" },
  { id: "us-cpi",          label: "US CPI" },
  { id: "gold",            label: "Gold" },
  { id: "oil",             label: "Oil" },
  { id: "ftse100",         label: "FTSE 100" },
  { id: "sp500",           label: "S&P 500" },
  { id: "bitcoin",         label: "Bitcoin" },
];
