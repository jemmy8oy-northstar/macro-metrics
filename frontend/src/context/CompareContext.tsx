import { createContext, useContext, useState, type ReactNode } from "react";

type CompareState = {
  numerator: string | null;
  denominator: string | null;
  setCompare: (numerator: string, denominator: string) => void;
  setNumerator: (v: string | null) => void;
  setDenominator: (v: string | null) => void;
};

const CompareContext = createContext<CompareState | null>(null);

export function CompareProvider({ children }: { children: ReactNode }) {
  const [numerator, setNumerator] = useState<string | null>(null);
  const [denominator, setDenominator] = useState<string | null>(null);

  function setCompare(num: string, den: string) {
    setNumerator(num);
    setDenominator(den);
  }

  return (
    <CompareContext.Provider value={{ numerator, denominator, setCompare, setNumerator, setDenominator }}>
      {children}
    </CompareContext.Provider>
  );
}

export function useCompare() {
  const ctx = useContext(CompareContext);
  if (!ctx) throw new Error("useCompare must be used inside CompareProvider");
  return ctx;
}
