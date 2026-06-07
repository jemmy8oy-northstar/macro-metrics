import * as Select from "@radix-ui/react-select";
import { useCompare } from "../context/CompareContext";
import { METRICS } from "../data/metrics";
import "./MetricPicker.css";

export function MetricPicker() {
  const { numerator, denominator, setNumerator, setDenominator } = useCompare();

  function swap() {
    if (numerator && denominator) {
      const tmp = numerator;
      setNumerator(denominator);
      setDenominator(tmp);
    }
  }

  return (
    <div className="metric-picker">
      <MetricSelect value={numerator} onChange={setNumerator}
        options={METRICS.filter((m) => m.id !== denominator)} placeholder="Numerator" />
      <button className="metric-picker__swap" onClick={swap} aria-label="Swap"
        disabled={!numerator || !denominator}>⇄</button>
      <MetricSelect value={denominator} onChange={setDenominator}
        options={METRICS.filter((m) => m.id !== numerator)} placeholder="Denominator" />
    </div>
  );
}

function MetricSelect({ value, onChange, options, placeholder }: {
  value: string | null;
  onChange: (v: string | null) => void;
  options: { id: string; label: string }[];
  placeholder: string;
}) {
  return (
    <Select.Root value={value ?? ""} onValueChange={(v) => onChange(v || null)}>
      <Select.Trigger className="metric-select__trigger">
        <Select.Value placeholder={placeholder} />
        <Select.Icon className="metric-select__icon">▾</Select.Icon>
      </Select.Trigger>
      <Select.Portal>
        <Select.Content className="metric-select__content" position="popper" sideOffset={6}>
          <Select.Viewport>
            {options.map((m) => (
              <Select.Item key={m.id} value={m.id} className="metric-select__item">
                <Select.ItemText>{m.label}</Select.ItemText>
              </Select.Item>
            ))}
          </Select.Viewport>
        </Select.Content>
      </Select.Portal>
    </Select.Root>
  );
}
