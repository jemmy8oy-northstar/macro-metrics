import { useState, type ReactNode } from "react";
import { CompareContext } from "./compare-context";

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
