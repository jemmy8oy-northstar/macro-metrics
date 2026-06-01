"""Shared fixtures for the yfinance sidecar test suite."""

from __future__ import annotations

import json
from pathlib import Path

import pytest

# Path to the shared test assets directory (relative to this file).
# Layout:
#   macro-metrics/
#     test-assets/yfinance/   ← shared JSON fixtures
#     sidecar/tests/          ← this file
FIXTURES_DIR = Path(__file__).parent.parent.parent / "test-assets" / "yfinance"


def load_fixture(filename: str) -> dict:
    """Load and parse a JSON fixture from the shared test-assets directory."""
    path = FIXTURES_DIR / filename
    if not path.exists():
        raise FileNotFoundError(f"Test fixture not found: {path}")
    return json.loads(path.read_text(encoding="utf-8"))


@pytest.fixture
def gc_f_fixture() -> dict:
    """Gold (GC=F) sidecar response fixture."""
    return load_fixture("gc_f_series.json")


@pytest.fixture
def btc_usd_fixture() -> dict:
    """Bitcoin (BTC-USD) sidecar response fixture."""
    return load_fixture("btc_usd_series.json")


@pytest.fixture
def gspc_fixture() -> dict:
    """S&P 500 (^GSPC) sidecar response fixture."""
    return load_fixture("gspc_series.json")


@pytest.fixture
def empty_fixture() -> dict:
    """Empty series fixture (^FTSE with no points)."""
    return load_fixture("empty_series.json")
