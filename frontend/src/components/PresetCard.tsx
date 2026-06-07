import { useGetRatioQuery } from "../api/metricsApi";
import { RatioChart } from "./RatioChart";
import { useCompare } from "../context/CompareContext";
import type { Preset } from "../data/presets";
import "./PresetCard.css";

function pctFromAvg(value: number, avg: number) {
  if (!avg) return "";
  const pct = ((value - avg) / avg) * 100;
  return (pct >= 0 ? `▲ ${pct.toFixed(0)}%` : `▼ ${Math.abs(pct).toFixed(0)}%`) + " avg";
}

export function PresetCard({ preset }: { preset: Preset }) {
  const { data, isLoading, isError, refetch } = useGetRatioQuery({
    numerator: preset.numerator,
    denominator: preset.denominator,
  });
  const { setCompare } = useCompare();

  function handleClick() {
    setCompare(preset.numerator, preset.denominator);
    setTimeout(() => document.getElementById("compare")?.scrollIntoView({ behavior: "smooth" }), 50);
  }

  if (isLoading) {
    return (
      <div className="preset-card preset-card--skeleton">
        <div className="skeleton skeleton--title" />
        <div className="skeleton skeleton--value" />
        <div className="skeleton skeleton--chart" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="preset-card preset-card--error">
        <h3 className="preset-card__title">{preset.title}</h3>
        <p className="preset-card__error-msg">⚠ Could not load data.</p>
        <button className="preset-card__retry" onClick={() => refetch()}>Try again</button>
      </div>
    );
  }

  const current = data.series.at(-1)?.value ?? 0;

  return (
    <div className="preset-card" onClick={handleClick} role="button" tabIndex={0}
      onKeyDown={(e) => e.key === "Enter" && handleClick()}>
      <div className="preset-card__header">
        <h3 className="preset-card__title">{preset.title}</h3>
        <div className="preset-card__meta">
          <span className="preset-card__value">{current.toFixed(2)}×</span>
          <span className="preset-card__pct">{pctFromAvg(current, data.longRunAverage)}</span>
        </div>
      </div>
      <RatioChart series={data.series} mode="compact" />
    </div>
  );
}
