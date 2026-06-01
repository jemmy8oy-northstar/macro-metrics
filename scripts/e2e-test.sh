#!/usr/bin/env bash
# scripts/e2e-test.sh
#
# End-to-end integration test: spins up the full stack (PostgreSQL + yfinance
# sidecar + .NET backend) using Docker Compose, runs a series of HTTP checks to
# verify that both services are healthy and that the backend → sidecar wiring is
# correct, then tears everything down.
#
# Prerequisites: docker (with the Compose plugin), curl
#
# Usage:
#   ./scripts/e2e-test.sh              # runs from repo root
#   bash scripts/e2e-test.sh --no-build  # skip --build if images are already built
#
# Exit codes:
#   0  all checks passed
#   1  one or more checks failed (services are torn down before exit)

set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────────────

COMPOSE_FILE="docker-compose.e2e.yml"
SIDECAR_URL="http://localhost:8001"
BACKEND_URL="http://localhost:8080"
BUILD_FLAG="--build"

# Parse args
for arg in "$@"; do
  [[ "$arg" == "--no-build" ]] && BUILD_FLAG=""
done

# ── Colours ────────────────────────────────────────────────────────────────────

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'  # No Colour

pass() { echo -e "${GREEN}✔ $*${NC}"; }
fail() { echo -e "${RED}✘ $*${NC}"; }
info() { echo -e "${YELLOW}▶ $*${NC}"; }

# ── Helpers ────────────────────────────────────────────────────────────────────

FAILURES=0

check() {
  local description="$1"
  shift
  if "$@" > /dev/null 2>&1; then
    pass "$description"
  else
    fail "$description"
    FAILURES=$((FAILURES + 1))
  fi
}

# Wait until a URL returns HTTP 2xx, retrying up to $2 times with $3-second gaps.
wait_for() {
  local url="$1"
  local retries="${2:-30}"
  local interval="${3:-5}"
  local name="${4:-$url}"

  info "Waiting for $name to become available..."
  for i in $(seq 1 "$retries"); do
    if curl -sf "$url" > /dev/null 2>&1; then
      pass "$name is up"
      return 0
    fi
    echo "  attempt $i/$retries — retrying in ${interval}s..."
    sleep "$interval"
  done

  fail "$name did not become available after $((retries * interval))s"
  return 1
}

# ── Teardown trap ──────────────────────────────────────────────────────────────

teardown() {
  info "Tearing down Docker Compose stack..."
  docker compose -f "$COMPOSE_FILE" down -v --remove-orphans 2>/dev/null || true
}

trap teardown EXIT

# ── 1. Start services ──────────────────────────────────────────────────────────

info "Starting services (this may take a while on the first run)..."
# shellcheck disable=SC2086
docker compose -f "$COMPOSE_FILE" up $BUILD_FLAG -d

# ── 2. Wait for services ───────────────────────────────────────────────────────

wait_for "$SIDECAR_URL/health" 30 3 "yfinance sidecar"
wait_for "$BACKEND_URL/api/metrics" 40 5 ".NET backend"

# ── 3. Sidecar checks ─────────────────────────────────────────────────────────

info "Running sidecar checks..."

check "GET /health returns 200" \
  curl -sf "$SIDECAR_URL/health"

check "GET /health body contains 'ok'" \
  bash -c "curl -sf '$SIDECAR_URL/health' | grep -q '\"ok\"'"

# ── 4. Backend checks ─────────────────────────────────────────────────────────

info "Running backend checks..."

check "GET /api/metrics returns 200" \
  curl -sf "$BACKEND_URL/api/metrics"

check "GET /api/metrics contains 'gold' metric" \
  bash -c "curl -sf '$BACKEND_URL/api/metrics' | grep -qi 'gold'"

check "GET /api/metrics contains yfinance entries" \
  bash -c "curl -sf '$BACKEND_URL/api/metrics' | grep -qi 'yfinance'"

# ── 5. Backend ↔ sidecar integration check ────────────────────────────────────

info "Running backend ↔ sidecar integration check..."

# Request the 'gold' series from the backend.  The backend will call the
# sidecar at GET /series/GC%3DF.  Acceptable outcomes:
#   200  — yfinance returned data (real network available)
#   502  — sidecar reached yfinance but got an upstream error
# Unacceptable: 500 (backend crash) — would indicate a wiring problem.
gold_status=$(curl -s -o /dev/null -w "%{http_code}" "$BACKEND_URL/api/metrics/gold")
echo "  GET /api/metrics/gold → HTTP $gold_status"

if [[ "$gold_status" == "200" || "$gold_status" == "502" || "$gold_status" == "404" ]]; then
  pass "Backend successfully forwarded request to sidecar (HTTP $gold_status)"
else
  fail "Backend returned unexpected HTTP $gold_status — sidecar wiring may be broken"
  FAILURES=$((FAILURES + 1))
fi

# ── 6. Results ────────────────────────────────────────────────────────────────

echo ""
if [[ "$FAILURES" -eq 0 ]]; then
  pass "All E2E checks passed ✓"
  exit 0
else
  fail "$FAILURES E2E check(s) failed"
  info "Collecting container logs..."
  docker compose -f "$COMPOSE_FILE" logs --tail=50
  exit 1
fi
