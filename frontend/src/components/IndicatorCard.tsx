import { useGetIndicatorQuery } from "../api/metricsApi";
import { LineChart, Line, ReferenceLine, ResponsiveContainer, Tooltip } from "recharts";
import { format, parseISO } from "date-fns";
import type { IndicatorConfig } from "../data/indicators";
import "./IndicatorCard.css";

function pctFromAvg(value: number, avg: number) {
  if (!avg) return "";
  const pct = ((value - avg) / avg) * 100;
  return (pct >= 0 ? `+${pct.toFixed(0)}%` : `${pct.toFixed(0)}%`) + " vs long-run avg";
}

export function IndicatorCard({ indicator }: { indicator: IndicatorConfig }) {
  const { data, isLoading, isError } = useGetIndicatorQuery(indicator.id);

  if (isLoading) {
    return (
      <div className="indicator-card indicator-card--skeleton">
        <div className="ind-skeleton ind-skeleton--title" />
        <div className="ind-skeleton ind-skeleton--value" />
        <div className="ind-skeleton ind-skeleton--chart" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="indicator-card indicator-card--error">
        <h3 className="indicator-card__title">{indicator.title}</h3>
        <p className="indicator-card__error-msg">⚠ Unavailable</p>
      </div>
    );
  }

  const current = data.series.at(-1)?.value ?? 0;

  return (
    <div className="indicator-card">
      <h3 className="indicator-card__title">{indicator.title}</h3>
      <p className="indicator-card__value">{current.toFixed(2)}{data.unit}</p>
      <p className="indicator-card__pct">{pctFromAvg(current, data.longRunAverage)}</p>
      <ResponsiveContainer width="100%" height={90}>
        <LineChart data={data.series}>
          <Tooltip
            contentStyle={{ background: "#1e1e24", border: "1px solid rgba(255,255,255,0.1)", borderRadius: "8px", fontSize: "0.75rem" }}
            formatter={(v) => [Number(v).toFixed(2) + data.unit, "Value"]}
            labelFormatter={(l) => format(parseISO(l as string), "dd MMM yyyy")}
          />
          <Line type="monotone" dataKey="value" stroke="#6366f1" strokeWidth={1.5} dot={false} />
          <ReferenceLine y={data.longRunAverage} stroke="#a855f7" strokeDasharray="4 4" strokeWidth={1}
            label={{ value: `avg (${data.longRunAverage.toFixed(2)}${data.unit})`, position: "right", fontSize: 10, fill: "#a855f7" }} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
