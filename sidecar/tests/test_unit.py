"""
Unit tests for the yfinance sidecar business logic.

These tests exercise the response-shaping logic without making real HTTP calls or
invoking yfinance.  They use shared JSON fixtures from ``test-assets/yfinance/``
which are also consumed by the .NET ``YFinanceFetcherService`` tests, ensuring that
both services agree on the wire format.

BDD scenarios covered
---------------------
- Sidecar response shape matches expected JSON contract
  (ticker + points array of {date, close}).
- Each fixture date is preserved as ISO-8601 (yyyy-MM-dd).
- Empty points array is accepted.
"""

from __future__ import annotations

import json
from typing import Any, Dict, List
from unittest.mock import MagicMock, patch

import pandas as pd
import pytest


# ── Helpers ────────────────────────────────────────────────────────────────────


def _build_yfinance_dataframe(points: List[Dict[str, Any]]) -> pd.DataFrame:
    """Build a minimal yfinance-style DataFrame from a list of {date, close} dicts."""
    dates = pd.to_datetime([p["date"] for p in points])
    closes = [p["close"] for p in points]
    df = pd.DataFrame({"Close": closes}, index=dates)
    df.index.name = "Date"
    return df


# ── Fixture shape tests ────────────────────────────────────────────────────────


class TestFixtureShape:
    """Verify the shared test fixtures conform to the expected wire format."""

    def test_gc_f_fixture_has_required_keys(self, gc_f_fixture: dict) -> None:
        assert "ticker" in gc_f_fixture
        assert "points" in gc_f_fixture

    def test_gc_f_fixture_ticker(self, gc_f_fixture: dict) -> None:
        assert gc_f_fixture["ticker"] == "GC=F"

    def test_gc_f_fixture_points_count(self, gc_f_fixture: dict) -> None:
        assert len(gc_f_fixture["points"]) == 2

    def test_gc_f_fixture_point_shape(self, gc_f_fixture: dict) -> None:
        for point in gc_f_fixture["points"]:
            assert "date" in point
            assert "close" in point

    def test_gc_f_fixture_dates_are_iso8601(self, gc_f_fixture: dict) -> None:
        import re
        iso_pattern = re.compile(r"^\d{4}-\d{2}-\d{2}$")
        for point in gc_f_fixture["points"]:
            assert iso_pattern.match(point["date"]), f"Not ISO-8601: {point['date']}"

    def test_gc_f_fixture_values(self, gc_f_fixture: dict) -> None:
        points = gc_f_fixture["points"]
        assert points[0] == {"date": "2024-01-15", "close": 2023.50}
        assert points[1] == {"date": "2024-01-16", "close": 2031.10}

    def test_btc_usd_fixture_values(self, btc_usd_fixture: dict) -> None:
        points = btc_usd_fixture["points"]
        assert points[0] == {"date": "2014-09-30", "close": 400.00}
        assert points[1] == {"date": "2024-01-01", "close": 42000.00}

    def test_gspc_fixture_values(self, gspc_fixture: dict) -> None:
        points = gspc_fixture["points"]
        assert points[0] == {"date": "2024-01-02", "close": 4742.83}
        assert points[1] == {"date": "2024-01-03", "close": 4704.81}

    def test_empty_fixture_has_empty_points(self, empty_fixture: dict) -> None:
        assert empty_fixture["points"] == []

    def test_empty_fixture_ticker(self, empty_fixture: dict) -> None:
        assert empty_fixture["ticker"] == "^FTSE"


# ── _fetch_series unit tests (patching yf.download) ──────────────────────────


class TestFetchSeries:
    """Unit tests for the _fetch_series helper, patching yf.download."""

    def test_returns_points_matching_gc_f_fixture(self, gc_f_fixture: dict) -> None:
        """_fetch_series maps yfinance DataFrame rows to SeriesPoint objects."""
        from main import _fetch_series

        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            result = _fetch_series("GC=F")

        assert len(result) == 2
        assert result[0].date == "2024-01-15"
        assert result[0].close == pytest.approx(2023.50, rel=1e-5)
        assert result[1].date == "2024-01-16"
        assert result[1].close == pytest.approx(2031.10, rel=1e-5)

    def test_returns_points_matching_btc_usd_fixture(self, btc_usd_fixture: dict) -> None:
        from main import _fetch_series

        mock_df = _build_yfinance_dataframe(btc_usd_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            result = _fetch_series("BTC-USD")

        assert len(result) == 2
        assert result[0].date == "2014-09-30"
        assert result[0].close == pytest.approx(400.00, rel=1e-5)
        assert result[1].date == "2024-01-01"
        assert result[1].close == pytest.approx(42000.00, rel=1e-5)

    def test_returns_empty_list_when_no_data(self) -> None:
        """When yfinance returns an empty DataFrame, _fetch_series returns []."""
        from main import _fetch_series

        with patch("main.yf.download", return_value=pd.DataFrame()):
            result = _fetch_series("^FTSE")

        assert result == []

    def test_returns_empty_list_when_none_returned(self) -> None:
        from main import _fetch_series

        with patch("main.yf.download", return_value=None):
            result = _fetch_series("^FTSE")

        assert result == []

    def test_date_format_is_iso8601(self, gc_f_fixture: dict) -> None:
        """Dates are formatted as yyyy-MM-dd regardless of yfinance Timestamp precision."""
        import re
        from main import _fetch_series

        mock_df = _build_yfinance_dataframe(gc_f_fixture["points"])
        with patch("main.yf.download", return_value=mock_df):
            result = _fetch_series("GC=F")

        iso_pattern = re.compile(r"^\d{4}-\d{2}-\d{2}$")
        for point in result:
            assert iso_pattern.match(point.date), f"Not ISO-8601: {point.date}"
