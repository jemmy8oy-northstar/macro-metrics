import { createContext, useContext } from "react";

export type CompareState = {
  numerator: string | null;
  denominator: string | null;
  setCompare: (numerator: string, denominator: string) => void;
  setNumerator: (v: string | null) => void;
  setDenominator: (v: string | null) => void;
};

// Context object and hook live here so CompareContext.tsx exports only the
// provider component — same reason as theme-context.ts
// (react-refresh/only-export-components).
export const CompareContext = createContext<CompareState | null>(null);

export function useCompare() {
  const ctx = useContext(CompareContext);
  if (!ctx) throw new Error("useCompare must be used inside CompareProvider");
  return ctx;
}
