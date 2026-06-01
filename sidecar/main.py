"""
yfinance HTTP sidecar for Macro Metrics.

Exposes a single endpoint:
    GET /series/{ticker}

Returns time-series data in the format expected by the .NET YFinanceFetcherService:
    { "ticker": "GC=F", "points": [{ "date": "yyyy-MM-dd", "close": 0.0 }] }

The ticker is URL-decoded automatically by FastAPI before it is passed to yfinance,
so the caller may send either raw or percent-encoded tickers (e.g. GC%3DF or GC=F).
"""

from __future__ import annotations

import os
from typing import List

import yfinance as yf
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI(
    title="YFinance Sidecar",
    description="Thin HTTP wrapper around the yfinance library for the Macro Metrics backend.",
    version="1.0.0",
)

# ── Response models ────────────────────────────────────────────────────────────


class SeriesPoint(BaseModel):
    date: str
    close: float


class SeriesResponse(BaseModel):
    ticker: str
    points: List[SeriesPoint]


# ── Helpers ────────────────────────────────────────────────────────────────────


def _fetch_series(ticker: str) -> List[SeriesPoint]:
    """Download historical daily close prices via yfinance.

    Returns an ordered list of (date, close) pairs.  An empty list is returned
    when yfinance finds no data (e.g. for an out-of-range or delisted ticker).

    Raises:
        HTTPException(502): if yfinance raises an unexpected error.
    """
    try:
        data = yf.download(ticker, progress=False, auto_adjust=True)
    except Exception as exc:
        raise HTTPException(
            status_code=502,
            detail=f"Failed to fetch data from yfinance for ticker '{ticker}': {exc}",
        ) from exc

    if data is None or data.empty:
        return []

    # yfinance returns a DataFrame indexed by date; 'Close' is the adjusted close.
    close_col = data.get("Close")
    if close_col is None:
        return []

    points: List[SeriesPoint] = []
    for date_idx, close_val in close_col.items():
        # date_idx may be a Timestamp or a tuple when multi-level columns are present.
        if isinstance(date_idx, tuple):
            date_idx = date_idx[0]
        date_str = date_idx.strftime("%Y-%m-%d")
        points.append(SeriesPoint(date=date_str, close=float(close_val)))

    return points


# ── Routes ─────────────────────────────────────────────────────────────────────


@app.get("/series/{ticker:path}", response_model=SeriesResponse)
async def get_series(ticker: str) -> SeriesResponse:
    """Return the full historical daily close-price series for *ticker*.

    The ticker is passed directly to yfinance, so any symbol accepted by Yahoo
    Finance can be requested (e.g. ``GC=F``, ``^FTSE``, ``BTC-USD``).
    """
    points = _fetch_series(ticker)
    return SeriesResponse(ticker=ticker, points=points)


@app.get("/health")
async def health() -> dict:
    """Liveness probe."""
    return {"status": "ok"}
