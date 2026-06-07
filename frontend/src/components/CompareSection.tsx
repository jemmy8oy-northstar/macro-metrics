import { useCompare } from "../context/CompareContext";
import { useGetRatioQuery } from "../api/metricsApi";
import { MetricPicker } from "./MetricPicker";
import { RatioChart } from "./RatioChart";
import { METRICS } from "../data/metrics";
import "./CompareSection.css";

export function CompareSection() {
  const { numerator, denominator } = useCompare();
  const skip = !numerator || !denominator;

  const { data, isLoading, isError, refetch } = useGetRatioQuery(
    { numerator: numerator!, denominator: denominator! },
    { skip }
  );

  const numLabel = METRICS.find((m) => m.id === numerator)?.label ?? numerator;
  const denLabel = METRICS.find((m) => m.id === denominator)?.label ?? denominator;

  return (
    <section className="compare-section">
      <div className="compare-section__picker">
        <MetricPicker />
      </div>

      <div className="compare-section__chart-area">
        {skip && !numerator && (
          <p className="compare-section__prompt">Select two metrics above to see a custom comparison</p>
        )}
        {skip && numerator && !denominator && (
          <p className="compare-section__prompt">Now select a denominator</p>
        )}
        {!skip && isLoading && <div className="compare-section__skeleton" />}
        {!skip && isError && (
          <div className="compare-section__error">
            <p>⚠ Could not load data.</p>
            <button onClick={() => refetch()}>Try again</button>
          </div>
        )}
        {!skip && data && (
          <div>
            <div className="compare-section__chart-header">
              <h3>{numLabel} / {denLabel}</h3>
              <span className="compare-section__current">{data.series.at(-1)?.value.toFixed(2)}×</span>
            </div>
            <RatioChart series={data.series} mode="full" />
          </div>
        )}
      </div>
    </section>
  );
}
