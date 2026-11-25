#!/usr/bin/env python3
"""Snapshot perp market data from Hyperliquid's public API into crypto/data.json.

Runs on a schedule (see .github/workflows/crypto-data.yml). Standard library only.
Hyperliquid has no open-interest history endpoint, so each run appends the current
OI to the history already in data.json; the OI chart fills in as runs accumulate.
"""
import json
import os
import time
import urllib.request
from datetime import datetime, timezone

API = "https://api.hyperliquid.xyz/info"
COINS = ["BTC", "ETH", "SOL"]
HOUR_MS = 3600 * 1000
CANDLE_DAYS = 14          # 1h candles kept for the price chart
FUNDING_DAYS = 14         # hourly funding prints
OI_KEEP_DAYS = 120        # rolling OI history kept in the file
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "crypto", "data.json")


def post(body):
    req = urllib.request.Request(
        API,
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json", "User-Agent": "m5ia-crypto-snapshot"},
    )
    for attempt in range(3):
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return json.load(r)
        except Exception:
            if attempt == 2:
                raise
            time.sleep(3 * (attempt + 1))


def load_previous():
    try:
        with open(OUT) as f:
            return json.load(f)
    except (FileNotFoundError, json.JSONDecodeError):
        return {}


def change_over(history, now_ms, hours, value_idx=1):
    """% change of history[-1] vs the closest point at least `hours` old."""
    target = now_ms - hours * HOUR_MS
    older = [p for p in history if p[0] <= target + 15 * 60 * 1000]
    if not older or not older[-1][value_idx]:
        return None
    return (history[-1][value_idx] / older[-1][value_idx] - 1) * 100


def main():
    now_ms = int(time.time() * 1000)
    prev = load_previous().get("coins", {})
    meta, ctxs = post({"type": "metaAndAssetCtxs"})
    by_name = {a["name"]: ctxs[i] for i, a in enumerate(meta["universe"])}

    coins = {}
    for coin in COINS:
        c = by_name[coin]
        mark = float(c["markPx"])
        prev_day = float(c["prevDayPx"])
        oi_coins = float(c["openInterest"])
        funding = float(c["funding"])  # hourly rate

        candles = post({"type": "candleSnapshot", "req": {
            "coin": coin, "interval": "1h",
            "startTime": now_ms - CANDLE_DAYS * 24 * HOUR_MS, "endTime": now_ms}})
        fund_hist = post({"type": "fundingHistory", "coin": coin,
                          "startTime": now_ms - FUNDING_DAYS * 24 * HOUR_MS})

        oi_hist = [p for p in prev.get(coin, {}).get("oiHistory", [])
                   if p[0] >= now_ms - OI_KEEP_DAYS * 24 * HOUR_MS]
        # one point per run, but never two within 20 minutes (manual re-runs)
        if oi_hist and now_ms - oi_hist[-1][0] < 20 * 60 * 1000:
            oi_hist.pop()
        oi_hist.append([now_ms, round(oi_coins * mark), mark])

        coins[coin] = {
            "mark": mark,
            "oracle": float(c["oraclePx"]),
            "prevDay": prev_day,
            "change24h": (mark / prev_day - 1) * 100 if prev_day else None,
            "fundingHourly": funding,
            "fundingAnnualPct": funding * 24 * 365 * 100,
            "premium": float(c["premium"]) if c.get("premium") else None,
            "oiCoins": oi_coins,
            "oiUsd": oi_coins * mark,
            "oiChange24h": change_over(oi_hist, now_ms, 24),
            "volume24hUsd": float(c["dayNtlVlm"]),
            "candles": [[k["t"], float(k["o"]), float(k["h"]), float(k["l"]), float(k["c"]), float(k["v"])]
                        for k in candles],
            "funding": [[f["time"], float(f["fundingRate"])] for f in fund_hist],
            "oiHistory": oi_hist,
        }

    out = {
        "updatedAt": datetime.fromtimestamp(now_ms / 1000, timezone.utc).isoformat(timespec="seconds"),
        "source": "Hyperliquid public API (perpetuals)",
        "coins": coins,
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(out, f, separators=(",", ":"))
    print(f"wrote {OUT}: " + ", ".join(f"{k} {v['mark']:,.2f}" for k, v in coins.items()))


if __name__ == "__main__":
    main()
