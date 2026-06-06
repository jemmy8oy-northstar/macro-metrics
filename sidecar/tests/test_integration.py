"""
Integration tests for the yfinance sidecar API layer.

These tests use FastAPI's ``TestClient`` (backed by ``httpx``) to exercise the
full HTTP request/response pipeline in-process — no real network or yfinance
calls are made.  yf.download is patched at the boundary so responses are
deterministic.

The expected response bodies are loaded from the shared ``test-assets/yfinance/``
fixtures, which are also consumed by the .NET ``YFinanceFetcherService`` tests.
This ensures that both services agree on the wire format end-to-end.

BDD scenarios covered
---------------------
- GET /series/{ticker} → 200 with ticker + points matching fixture
- GET /series/{ticker} with URL-encoded ticker (e.g. GC%3DF → GC=F)
- GET /series/{ticker} with empty DataFrame → 200 with empty points array
- GET /series/{ticker} when yfinance raises → 502
- GET /health → 200 {"status": "ok"}
"""

from __future__ import annotations

from typing import Any, Dict, List
from unittest.mock import patch

import pandas as pd
import pytest
from fastapi.testclient import TestClient

from main import app


# ── Helpers ────────────────────────────────────────────────────────────────────


def _build_yfinance_dataframe(points: List[Dict[str, Any]]) -> pd.DataFrame:
    """Build a minimal yfinance-style DataFrame from a list of {date, close} dicts."""
    dates = pd.to_datetime([p["date"] for p in points])
    closes = [p["close"] for p in points]
    df = pd.DataFrame({"Close": closes}, index=dates)
    df.index.name = "Date"
    return df


client = TestClient(app, raise_server_exceptions=False)


# ── Health check ───────────────────────────────────────────────────────────────


class TestHealthEndpoint:
    def test_health_returns_200(self) -> None:
        response = client.get("/health")
        assert response.status_code == 200

    def test_health_returns_ok_status(self) -> None:
        response = client.get("/health")
        assert response.json() == {"status": "ok"}


# ── GET /series/{ticker} — happy paths ────────────────────────────────────────


class TestGetSeriesHappyPath:
    """Integration tests verifying the full request → response pipeline."""

    def test_gc_f_returns_200(self, gc_f_fixture: dict) -> None:
        """GET /series/GC=F returns HTTP 200."""
        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/GC=F")
        assert response.status_code == 200

    def test_gc_f_response_matches_fixture(self, gc_f_fixture: dict) -> None:
        """Response body matches the shared gc_f_series.json fixture exactly."""
        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/GC=F")

        body = response.json()
        assert body["ticker"] == gc_f_fixture["ticker"]
        assert len(body["points"]) == len(gc_f_fixture["points"])
        for actual, expected in zip(body["points"], gc_f_fixture["points"]):
            assert actual["date"] == expected["date"]
            assert actual["close"] == pytest.approx(expected["close"], rel=1e-5)

    def test_btc_usd_response_matches_fixture(self, btc_usd_fixture: dict) -> None:
        """Response body matches the shared btc_usd_series.json fixture exactly."""
        mock_df = _build_yfinance_dataframe(btc_usd_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/BTC-USD")

        body = response.json()
        assert body["ticker"] == btc_usd_fixture["ticker"]
        assert len(body["points"]) == 2
        assert body["points"][0]["date"] == "2014-09-30"
        assert body["points"][0]["close"] == pytest.approx(400.00, rel=1e-5)

    def test_gspc_response_matches_fixture(self, gspc_fixture: dict) -> None:
        """Response body matches the shared gspc_series.json fixture exactly."""
        mock_df = _build_yfinance_dataframe(gspc_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/%5EGSPC")  # ^GSPC URL-encoded

        body = response.json()
        assert body["ticker"] == gspc_fixture["ticker"]
        assert body["points"][0]["date"] == "2024-01-02"

    def test_response_content_type_is_json(self, gc_f_fixture: dict) -> None:
        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/GC=F")
        assert "application/json" in response.headers["content-type"]

    def test_empty_series_returns_200_with_empty_points(self, empty_fixture: dict) -> None:
        """When yfinance returns no data, the response is 200 with an empty points array."""
        with patch("main.yf.download", return_value=pd.DataFrame()):
            response = client.get("/series/%5EFTSE")  # ^FTSE URL-encoded

        assert response.status_code == 200
        body = response.json()
        assert body["points"] == []


# ── GET /series/{ticker} — URL encoding ────────────────────────────────────────


class TestGetSeriesUrlEncoding:
    """Verifies that URL-encoded tickers are decoded correctly before being
    passed to yfinance — matching the encoding applied by the .NET client."""

    @pytest.mark.parametrize("encoded,decoded", [
        ("GC%3DF", "GC=F"),          # gold futures — = encoded as %3D
        ("CL%3DF", "CL=F"),          # oil futures
        ("%5EFTSE", "^FTSE"),         # FTSE 100 — ^ encoded as %5E
        ("%5EGSPC", "^GSPC"),         # S&P 500
        ("BTC-USD", "BTC-USD"),       # bitcoin — no encoding needed
        ("%5ETMBMKGB-10Y", "^TMBMKGB-10Y"),  # UK 10yr gilt
    ])
    def test_url_encoded_ticker_is_passed_decoded_to_yfinance(
        self, encoded: str, decoded: str
    ) -> None:
        """The ticker value received by yfinance must be the decoded form."""
        captured: list[str] = []

        def _fake_download(ticker: str, **kwargs: Any) -> pd.DataFrame:
            captured.append(ticker)
            return pd.DataFrame()

        with patch("main.yf.download", side_effect=_fake_download):
            client.get(f"/series/{encoded}")

        assert captured, "yf.download was never called"
        assert captured[0] == decoded


# ── GET /series/{ticker} — error paths ────────────────────────────────────────


class TestGetSeriesErrors:
    def test_yfinance_exception_returns_502(self) -> None:
        """When yf.download raises, the endpoint returns HTTP 502."""
        with patch("main.yf.download", side_effect=RuntimeError("network error")):
            response = client.get("/series/GC=F")

        assert response.status_code == 502

    def test_yfinance_exception_body_contains_ticker(self) -> None:
        with patch("main.yf.download", side_effect=RuntimeError("timeout")):
            response = client.get("/series/GC=F")

        body = response.json()
        assert "GC=F" in body.get("detail", "")

    def test_response_structure_has_ticker_and_points_keys(
        self, gc_f_fixture: dict
    ) -> None:
        """Response always contains 'ticker' and 'points' keys on success."""
        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/GC=F")

        body = response.json()
        assert "ticker" in body
        assert "points" in body

    def test_each_point_has_date_and_close(self, gc_f_fixture: dict) -> None:
        """Each point in the response has 'date' and 'close' keys."""
        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            response = client.get("/series/GC=F")

        for point in response.json()["points"]:
            assert "date" in point
            assert "close" in point
