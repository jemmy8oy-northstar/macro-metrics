export type Preset = {
  id: string;
  title: string;
  numerator: string;
  denominator: string;
  category: "Affordability" | "Inflation-adjusted" | "Cross-asset";
};

export const PRESETS: Preset[] = [
  { id: "uk-house-prices-wages", title: "UK House Prices / Wages", numerator: "uk-house-prices", denominator: "uk-wages", category: "Affordability" },
  { id: "us-house-prices-wages", title: "US House Prices / Wages", numerator: "us-house-prices", denominator: "us-wages", category: "Affordability" },
  { id: "uk-real-hpi",           title: "UK Real House Prices",   numerator: "uk-house-prices", denominator: "uk-cpi",   category: "Affordability" },
  { id: "real-gold",   title: "Real Gold",    numerator: "gold",         denominator: "us-cpi", category: "Inflation-adjusted" },
  { id: "real-sp500",  title: "Real S&P 500", numerator: "sp500",        denominator: "us-cpi", category: "Inflation-adjusted" },
  { id: "real-oil",    title: "Real Oil",     numerator: "oil",          denominator: "us-cpi", category: "Inflation-adjusted" },
  { id: "gold-equities",    title: "Gold / Equities",    numerator: "gold",          denominator: "sp500", category: "Cross-asset" },
  { id: "uk-property-gold", title: "UK Property / Gold", numerator: "uk-house-prices", denominator: "gold", category: "Cross-asset" },
  { id: "btc-gold",         title: "BTC / Gold",         numerator: "bitcoin",       denominator: "gold",  category: "Cross-asset" },
];
