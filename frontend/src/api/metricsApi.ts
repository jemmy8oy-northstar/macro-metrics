import { emptySplitApi as api } from "./emptyApi";

export type DataPoint = { date: string; value: number };

export type RatioResult = {
  numerator: string;
  denominator: string;
  longRunAverage: number;
  series: DataPoint[];
};

export type IndicatorResult = {
  id: string;
  label: string;
  unit: string;
  longRunAverage: number;
  series: DataPoint[];
};

const metricsApi = api.injectEndpoints({
  endpoints: (build) => ({
    getRatio: build.query<RatioResult, { numerator: string; denominator: string }>({
      query: ({ numerator, denominator }) => ({
        url: `/api/metrics/ratio`,
        params: { numerator, denominator },
      }),
    }),
    getIndicator: build.query<IndicatorResult, string>({
      query: (id) => ({ url: `/api/metrics/indicator/${id}` }),
    }),
  }),
  overrideExisting: false,
});

export const { useGetRatioQuery, useGetIndicatorQuery } = metricsApi;
