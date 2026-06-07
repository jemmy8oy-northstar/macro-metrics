import { useMemo, useState } from "react";
import {
  LineChart, Line, ReferenceLine, ResponsiveContainer,
  Tooltip, XAxis, YAxis, CartesianGrid,
} from "recharts";
import { format, subYears, parseISO, isAfter } from "date-fns";
import type { DataPoint } from "../api/metricsApi";
import "./RatioChart.css";

type Range = "1y" | "2y" | "5y" | "10y" | "20y" | "Max";
const RANGES: Range[] = ["1y", "2y", "5y", "10y", "20y", "Max"];

function filterByRange(series: DataPoint[], range: Range): DataPoint[] {
  if (range === "Max") return series;
  const years = parseInt(range);
  const cutoff = subYears(new Date(), years);
  return series.filter((p) => isAfter(parseISO(p.date), cutoff));
}

type Props = { series: DataPoint[]; mode: "compact" | "full"; unit?: string };

export function RatioChart({ series, mode, unit = "×" }: Props) {
  const [range, setRange] = useState<Range>("Max");
  const visible = useMemo(() => filterByRange(series, range), [series, range]);
  const avg = useMemo(
    () => (visible.length ? visible.reduce((s, p) => s + p.value, 0) / visible.length : 0),
    [visible]
  );
  const isCompact = mode === "compact";

  return (
    <div className={`ratio-chart ratio-chart--${mode}`}>
      <ResponsiveContainer width="100%" height={isCompact ? 160 : 320}>
        <LineChart data={visible} margin={{ top: 8, right: isCompact ? 8 : 56, left: isCompact ? -32 : 0, bottom: 0 }}>
          {!isCompact && <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.06)" />}
          {!isCompact && (
            <XAxis dataKey="date" tickFormatter={(d) => format(parseISO(d), "MMM yy")}
              tick={{ fontSize: 11, fill: "#94a3b8" }} tickLine={false} axisLine={false} minTickGap={60} />
          )}
          {!isCompact && (
            <YAxis tick={{ fontSize: 11, fill: "#94a3b8" }} tickLine={false} axisLine={false}
              tickFormatter={(v) => v.toFixed(1) + unit} width={48} />
          )}
          <Tooltip
            contentStyle={{ background: "#1e1e24", border: "1px solid rgba(255,255,255,0.1)", borderRadius: "8px", fontSize: "0.8rem" }}
            formatter={(value) => [Number(value).toFixed(2) + unit, "Value"]}
            labelFormatter={(label) => format(parseISO(label as string), "dd MMM yyyy")}
          />
          <Line type="monotone" dataKey="value" stroke="#6366f1" strokeWidth={isCompact ? 1.5 : 2} dot={false} activeDot={{ r: 4 }} />
          <ReferenceLine y={avg} stroke="#a855f7" strokeDasharray="4 4" strokeWidth={1.5}
            label={!isCompact ? { value: `avg (${avg.toFixed(2)}${unit})`, position: "right", fontSize: 11, fill: "#a855f7" } : undefined} />
        </LineChart>
      </ResponsiveContainer>
      <div className="ratio-chart__ranges">
        {RANGES.map((r) => (
          <button key={r} className={r === range ? "active" : ""} onClick={() => setRange(r)}>{r}</button>
        ))}
      </div>
    </div>
  );
}
