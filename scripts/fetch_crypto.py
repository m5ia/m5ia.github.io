#!/usr/bin/env python3
"""Build the data behind m5ia.com/crypto. Standard library only.

Sources
- Hyperliquid public API: live price, funding, OI, hourly and daily candles.
- Binance public data archive (data.binance.vision): daily files of 5-minute
  BTCUSDT-style perp metrics (OI, long/short ratios, taker flow). Files land
  the day after, so these series lag by up to ~1 day.

Outputs
- crypto/data.json     what the page reads
- crypto/history.json  cache of slow-moving history (Binance archive, daily
                       Hyperliquid funding), so each hourly run only fetches
                       what is new

Run hourly by .github/workflows/crypto-data.yml.
"""
import csv
import io
import json
import math
import os
import time
import urllib.error
import urllib.request
import zipfile
from concurrent.futures import ThreadPoolExecutor
from datetime import date, datetime, timedelta, timezone

HL_API = "https://api.hyperliquid.xyz/info"
BV = "https://data.binance.vision/data/futures/um/daily/metrics/{s}/{s}-metrics-{d}.zip"
COINS = ["BTC", "ETH", "SOL"]
HOUR = 3600 * 1000
DAY = 24 * HOUR
HIST_DAYS = 365           # Binance daily + HL funding history kept
HOURLY_KEEP_DAYS = 45     # Binance hourly OI kept (liquidation model input)
PAGE_DAYS = 14            # hourly candles / funding sent to the page
LIQ_LOOKBACK_DAYS = 30    # positions opened in this window feed the liq map
OI_KEEP_DAYS = 120

# Liquidation model assumptions (shown on the page)
LEV_TIERS = [(5, 0.15), (10, 0.30), (25, 0.25), (50, 0.20), (100, 0.10)]
MMR = 0.005
BIN_PCT = 0.005           # 0.5% price bins
MAP_RANGE = 0.30          # +-30% around price

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "crypto")
OUT = os.path.join(ROOT, "data.json")
HIST = os.path.join(ROOT, "history.json")


# ---------------------------------------------------------------- fetching
def hl(body):
    req = urllib.request.Request(HL_API, data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json", "User-Agent": "m5ia-crypto"})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return json.load(r)
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))


def bv_day(symbol, d):
    """Rows of one day's Binance metrics file, or None if not published / unreachable."""
    url = BV.format(s=symbol, d=d.isoformat())
    for attempt in range(3):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "m5ia-crypto"}), timeout=30) as r:
                z = zipfile.ZipFile(io.BytesIO(r.read()))
            with z.open(z.namelist()[0]) as f:
                return list(csv.DictReader(io.TextIOWrapper(f, "utf-8")))
        except urllib.error.HTTPError as e:
            if e.code == 404:
                return None
        except Exception:
            pass
        time.sleep(2 * (attempt + 1))
    return None


def load_json(path):
    try:
        with open(path) as f:
            return json.load(f)
    except (FileNotFoundError, json.JSONDecodeError):
        return {}


# ---------------------------------------------------------------- math helpers
def pct_rank(values, v):
    """Percentile 0-100 of v within values (average rank for ties)."""
    vals = [x for x in values if x is not None]
    if not vals or v is None:
        return None
    below = sum(1 for x in vals if x < v)
    equal = sum(1 for x in vals if x == v)
    return 100.0 * (below + 0.5 * equal) / len(vals)


def blend(a, b, wa=0.6):
    if a is None:
        return b
    if b is None:
        return a
    return wa * a + (1 - wa) * b


def ema(xs, n):
    k, out, e = 2 / (n + 1), [], None
    for x in xs:
        e = x if e is None else x * k + e * (1 - k)
        out.append(e)
    return out


def sma(xs, n):
    out, s = [], 0.0
    for i, x in enumerate(xs):
        s += x
        if i >= n:
            s -= xs[i - n]
        out.append(s / n if i >= n - 1 else None)
    return out


def clamp(x, lo=-1.0, hi=1.0):
    return max(lo, min(hi, x))


def r(x, nd=2):
    return None if x is None else round(x, nd)


# ---------------------------------------------------------------- Binance archive cache
def update_binance(coin, cache):
    """Append any newly published days for coin. Returns the coin's cache entry."""
    symbol = coin + "USDT"
    entry = cache.setdefault(coin, {"daily": [], "hourly": []})
    today = datetime.now(timezone.utc).date()
    last = date.fromisoformat(entry["daily"][-1]["d"]) if entry["daily"] else today - timedelta(days=HIST_DAYS + 1)
    days = [last + timedelta(days=i) for i in range(1, (today - last).days)]  # up to yesterday
    if not days:
        return entry
    with ThreadPoolExecutor(max_workers=12) as ex:
        results = list(ex.map(lambda d: (d, bv_day(symbol, d)), days))
    for d, rows in results:
        if not rows:
            break  # not published yet (or unreachable); keep the series contiguous
        parsed = []
        for row in rows:
            try:
                t = int(datetime.strptime(row["create_time"], "%Y-%m-%d %H:%M:%S").replace(tzinfo=timezone.utc).timestamp() * 1000)
                oi = float(row["sum_open_interest"])
                oiv = float(row["sum_open_interest_value"])
                if oi <= 0 or oiv <= 0:
                    continue  # occasional empty rows in the archive
                parsed.append({
                    "t": t, "oi": oi, "px": oiv / oi,
                    "taker": float(row["sum_taker_long_short_vol_ratio"] or 0) or None,
                    "lsTop": float(row["sum_toptrader_long_short_ratio"] or 0) or None,
                    "lsTopAcc": float(row["count_toptrader_long_short_ratio"] or 0) or None,
                    "lsGlobal": float(row["count_long_short_ratio"] or 0) or None,
                })
            except (ValueError, KeyError, ZeroDivisionError):
                continue
        if not parsed:
            break
        parsed.sort(key=lambda x: x["t"])
        lastrow = parsed[-1]
        takers = [p["taker"] for p in parsed if p["taker"]]
        entry["daily"].append({
            "d": d.isoformat(), "oi": lastrow["oi"], "px": lastrow["px"],
            "lsTop": lastrow["lsTop"], "lsTopAcc": lastrow["lsTopAcc"], "lsGlobal": lastrow["lsGlobal"],
            "taker": sum(takers) / len(takers) if takers else None,
        })
        # hourly: value at each hour mark, taker averaged over the hour
        by_hour = {}
        for p in parsed:
            by_hour.setdefault(p["t"] // HOUR, []).append(p)
        for h in sorted(by_hour):
            ps = by_hour[h]
            tk = [p["taker"] for p in ps if p["taker"]]
            entry["hourly"].append([h * HOUR, ps[-1]["oi"], round(ps[-1]["px"], 6), round(sum(tk) / len(tk), 4) if tk else None])
    entry["daily"] = entry["daily"][-HIST_DAYS:]
    cutoff = int(time.time() * 1000) - HOURLY_KEEP_DAYS * DAY
    entry["hourly"] = [h for h in entry["hourly"] if h[0] >= cutoff]
    return entry


def update_hl_funding(coin, cache):
    """Daily mean of Hyperliquid hourly funding, last HIST_DAYS days (incremental)."""
    entry = cache.setdefault(coin, [])  # [[YYYY-MM-DD, mean, n]]
    now = int(time.time() * 1000)
    start = (int(datetime.fromisoformat(entry[-1][0]).replace(tzinfo=timezone.utc).timestamp() * 1000) if entry
             else now - HIST_DAYS * DAY)
    rows = []
    cursor = start
    for _ in range(40):
        batch = hl({"type": "fundingHistory", "coin": coin, "startTime": cursor})
        if not batch:
            break
        rows += batch
        if len(batch) < 500:
            break
        cursor = batch[-1]["time"] + 1
    days = {d[0]: [d[1] * d[2], d[2]] for d in entry}
    if entry:  # the last stored day is re-fetched in full, so drop its partial sum
        days.pop(entry[-1][0], None)
    seen = set()
    for f in rows:
        if f["time"] in seen:
            continue
        seen.add(f["time"])
        d = datetime.fromtimestamp(f["time"] / 1000, timezone.utc).date().isoformat()
        s = days.setdefault(d, [0.0, 0])
        s[0] += float(f["fundingRate"])
        s[1] += 1
    # completed UTC days only, so the cache file changes once a day, not every hour
    today = datetime.now(timezone.utc).date().isoformat()
    out = [[d, v[0] / v[1], v[1]] for d, v in sorted(days.items()) if v[1] and d < today]
    cache[coin] = out[-HIST_DAYS:]
    return cache[coin]


# ---------------------------------------------------------------- indicators
def trend_scores(daily_closes):
    """Mid- and long-term trend scores in [-100, 100] from daily closes."""
    c = daily_closes
    if len(c) < 60:
        return None
    p = c[-1]
    e21, e55 = ema(c, 21), ema(c, 55)
    mid_parts = {
        "Price vs EMA21": clamp((p / e21[-1] - 1) / 0.05),
        "EMA21 vs EMA55": clamp((e21[-1] / e55[-1] - 1) / 0.04),
        "EMA21 slope (10d)": clamp((e21[-1] / e21[-11] - 1) / 0.04),
        "30d momentum": clamp((p / c[-31] - 1) / 0.15),
    }
    out = {"mid": {"score": round(100 * sum(mid_parts.values()) / len(mid_parts)),
                   "parts": {k: round(v * 100) for k, v in mid_parts.items()}}}
    if len(c) >= 230:
        s50, s200 = sma(c, 50), sma(c, 200)
        long_parts = {
            "Price vs SMA200": clamp((p / s200[-1] - 1) / 0.15),
            "SMA50 vs SMA200": clamp((s50[-1] / s200[-1] - 1) / 0.10),
            "SMA200 slope (30d)": clamp((s200[-1] / s200[-31] - 1) / 0.05),
            "180d momentum": clamp((p / c[-181] - 1) / 0.50),
        }
        out["long"] = {"score": round(100 * sum(long_parts.values()) / len(long_parts)),
                       "parts": {k: round(v * 100) for k, v in long_parts.items()}}
    return out


def trend_history(daily, days=120):
    """Daily trend scores for the last `days` days (for the sparkline)."""
    closes = [k[4] for k in daily]
    out = []
    for i in range(max(60, len(closes) - days), len(closes) + 1):
        s = trend_scores(closes[:i])
        if s:
            out.append([daily[i - 1][0], s["mid"]["score"], s.get("long", {}).get("score")])
    return out


def gauges(bn_daily, hl_fund_daily, last24_funding, change24h, daily_closes):
    g = {}
    # Funding bias: where the last 24h of HL funding sits in its own 1Y / 6M history
    fvals = [d[1] for d in hl_fund_daily]
    fb = blend(pct_rank(fvals, last24_funding), pct_rank(fvals[-180:], last24_funding))
    # OI pressure: Binance OI (coin units) vs its 1Y / 6M history
    ois = [d["oi"] for d in bn_daily]
    oip = blend(pct_rank(ois, ois[-1]), pct_rank(ois[-180:], ois[-1])) if ois else None
    # size of the latest daily OI change vs history
    dois = [abs(ois[i] / ois[i - 1] - 1) for i in range(1, len(ois)) if ois[i - 1]]
    pdoi = pct_rank(dois, dois[-1]) if dois else None
    tension = None
    if None not in (oip, fb, pdoi):
        tension = 0.55 * oip + 0.30 * (2 * abs(fb - 50)) + 0.15 * pdoi
    # squeeze risk
    rets = [abs(daily_closes[i] / daily_closes[i - 1] - 1) for i in range(1, len(daily_closes))][-365:]
    move = pct_rank(rets, abs(change24h) / 100) if change24h is not None and rets else None
    sq_long = sq_short = None
    if None not in (fb, oip, move):
        f_long, f_short = max(0.0, 2 * (fb - 50)), max(0.0, 2 * (50 - fb))
        m_long = move if change24h < 0 else 0.0
        m_short = move if change24h > 0 else 0.0
        sq_long = {"score": 0.40 * f_long + 0.35 * oip + 0.25 * m_long, "funding": f_long, "oi": oip, "move": m_long}
        sq_short = {"score": 0.40 * f_short + 0.35 * oip + 0.25 * m_short, "funding": f_short, "oi": oip, "move": m_short}
    g["fundingBias"] = r(fb, 1)
    g["oiPressure"] = r(oip, 1)
    g["tension"] = r(tension, 1)
    g["oiAsOf"] = bn_daily[-1]["d"] if bn_daily else None
    g["squeezeLong"] = {k: r(v, 1) for k, v in sq_long.items()} if sq_long else None
    g["squeezeShort"] = {k: r(v, 1) for k, v in sq_short.items()} if sq_short else None
    return g


def flows(bn_daily):
    """Price / OI classification over 1D, 3D, 7D closed days (Binance)."""
    out = []
    for n, label in [(1, "1D"), (3, "3D"), (7, "7D")]:
        if len(bn_daily) <= n:
            continue
        a, b = bn_daily[-1 - n], bn_daily[-1]
        dp, doi = (b["px"] / a["px"] - 1) * 100, (b["oi"] / a["oi"] - 1) * 100
        if abs(dp) < 0.5 and abs(doi) < 0.5:
            cls = "Neutral"
        elif dp >= 0 and doi >= 0:
            cls = "Longs adding"
        elif dp >= 0:
            cls = "Short covering"
        elif doi >= 0:
            cls = "Shorts adding"
        else:
            cls = "Long liquidation"
        out.append({"window": label, "class": cls, "price": r(dp), "oi": r(doi)})
    return out


def liquidation_map(bn_hourly, hl_candles, mark):
    """Estimate where leveraged positions opened in the lookback window would be liquidated.

    Each hourly OI increase is split long/short by taker buy/sell volume, spread across
    leverage tiers and placed at its liquidation price. OI decreases shrink every open
    level proportionally. A level is removed once price trades through it.
    """
    now = int(time.time() * 1000)
    hrs = [h for h in bn_hourly if h[0] >= now - LIQ_LOOKBACK_DAYS * DAY and h[2]]
    if len(hrs) < 48:
        return None
    candle_by_hour = {k[0] // HOUR: k for k in hl_candles}
    longs, shorts = [], []  # [liq_price, usd]

    def sweep(hour_key):
        k = candle_by_hour.get(hour_key)
        if not k:
            return
        lo, hi = k[3], k[2]
        longs[:] = [l for l in longs if l[0] < lo]
        shorts[:] = [s for s in shorts if s[0] > hi]

    for i in range(1, len(hrs)):
        t, oi, px, taker = hrs[i]
        prev = hrs[i - 1][1]
        d = oi - prev
        if d > 0:
            usd = d * px
            ls = clamp((taker or 1) / (1 + (taker or 1)), 0.3, 0.7)
            for lev, w in LEV_TIERS:
                longs.append([px * (1 - 1 / lev + MMR), usd * ls * w])
                shorts.append([px * (1 + 1 / lev - MMR), usd * (1 - ls) * w])
        elif d < 0 and prev:
            f = oi / prev
            for l in longs:
                l[1] *= f
            for s in shorts:
                s[1] *= f
        sweep(t // HOUR)
    # carry the sweep forward through hours after the archive ends, using live candles
    last_hour = hrs[-1][0] // HOUR
    for hk in sorted(h for h in candle_by_hour if h > last_hour):
        sweep(hk)

    step = mark * BIN_PCT
    nbins = int(MAP_RANGE / BIN_PCT)
    bins = {}
    for side, levels in (("long", longs), ("short", shorts)):
        for p, usd in levels:
            k = math.floor((p - mark) / step)
            if -nbins <= k < nbins and usd > 0:
                b = bins.setdefault(k, [0.0, 0.0])
                b[0 if side == "long" else 1] += usd
    rows = [[round(mark + k * step, 6), round(mark + (k + 1) * step, 6), round(v[0]), round(v[1])]
            for k, v in sorted(bins.items()) if v[0] + v[1] >= 1]

    def top(side_idx, above):
        cand = [b for b in rows if (b[0] >= mark if above else b[1] <= mark) and b[2 + side_idx] > 0]
        cand.sort(key=lambda b: -b[2 + side_idx])
        picked = sorted(cand[:4], key=lambda b: abs((b[0] + b[1]) / 2 - mark))
        return [{"low": b[0], "high": b[1], "usd": b[2 + side_idx],
                 "dist": r(((b[0] + b[1]) / 2 / mark - 1) * 100)} for b in picked]

    return {
        "bins": rows,
        "longTotal": round(sum(b[2] for b in rows)),
        "shortTotal": round(sum(b[3] for b in rows)),
        "clustersBelow": top(0, False),
        "clustersAbove": top(1, True),
        "oiThrough": datetime.fromtimestamp(hrs[-1][0] / 1000, timezone.utc).isoformat(timespec="minutes"),
        "lookbackDays": LIQ_LOOKBACK_DAYS,
        "binPct": BIN_PCT * 100,
        "tiers": [[lev, w] for lev, w in LEV_TIERS],
    }


# ---------------------------------------------------------------- main
def main():
    now = int(time.time() * 1000)
    prev = load_json(OUT).get("coins", {})
    hist = load_json(HIST)
    hist.setdefault("binance", {})
    hist.setdefault("hlFunding", {})

    meta, ctxs = hl({"type": "metaAndAssetCtxs"})
    by_name = {a["name"]: ctxs[i] for i, a in enumerate(meta["universe"])}

    coins = {}
    for coin in COINS:
        c = by_name[coin]
        mark = float(c["markPx"])
        prev_day = float(c["prevDayPx"])
        oi_coins = float(c["openInterest"])
        funding = float(c["funding"])
        change24h = (mark / prev_day - 1) * 100 if prev_day else None

        h_candles = hl({"type": "candleSnapshot", "req": {"coin": coin, "interval": "1h",
                        "startTime": now - (LIQ_LOOKBACK_DAYS + 1) * DAY, "endTime": now}})
        h_candles = [[k["t"], float(k["o"]), float(k["h"]), float(k["l"]), float(k["c"]), float(k["v"])] for k in h_candles]
        d_candles = hl({"type": "candleSnapshot", "req": {"coin": coin, "interval": "1d",
                        "startTime": now - 1200 * DAY, "endTime": now}})
        d_candles = [[k["t"], float(k["o"]), float(k["h"]), float(k["l"]), float(k["c"]), float(k["v"])] for k in d_candles]
        fund_hist = hl({"type": "fundingHistory", "coin": coin, "startTime": now - PAGE_DAYS * DAY})
        funding_series = [[f["time"], float(f["fundingRate"])] for f in fund_hist]

        try:
            bn = update_binance(coin, hist["binance"])
        except Exception as e:  # archive unreachable: keep going with what is cached
            print(f"binance archive {coin}: {e}")
            bn = hist["binance"].get(coin, {"daily": [], "hourly": []})
        try:
            hlf = update_hl_funding(coin, hist["hlFunding"])
        except Exception as e:
            print(f"hl funding history {coin}: {e}")
            hlf = hist["hlFunding"].get(coin, [])

        oi_hist = [p for p in prev.get(coin, {}).get("oiHistory", []) if p[0] >= now - OI_KEEP_DAYS * DAY]
        if oi_hist and now - oi_hist[-1][0] < 20 * 60 * 1000:
            oi_hist.pop()
        oi_hist.append([now, round(oi_coins * mark), mark])

        last24 = [f[1] for f in funding_series if f[0] >= now - DAY]
        last24_mean = sum(last24) / len(last24) if last24 else funding
        # closed daily candles only (the last one is still forming)
        closed = d_candles[:-1] if d_candles and d_candles[-1][0] + DAY > now else d_candles
        closes = [k[4] for k in closed]

        def oi_change(hist_pts, hours):
            target = now - hours * HOUR
            older = [p for p in hist_pts if p[0] <= target + 15 * 60 * 1000]
            return (hist_pts[-1][1] / older[-1][1] - 1) * 100 if older and older[-1][1] else None

        bd = bn["daily"]
        coins[coin] = {
            "mark": mark,
            "oracle": float(c["oraclePx"]),
            "prevDay": prev_day,
            "change24h": change24h,
            "fundingHourly": funding,
            "fundingAnnualPct": funding * 24 * 365 * 100,
            "funding24hAnnualPct": last24_mean * 24 * 365 * 100,
            "premium": float(c["premium"]) if c.get("premium") else None,
            "oiCoins": oi_coins,
            "oiUsd": oi_coins * mark,
            "oiChange24h": oi_change(oi_hist, 24),
            "volume24hUsd": float(c["dayNtlVlm"]),
            "candles": [k for k in h_candles if k[0] >= now - PAGE_DAYS * DAY],
            "funding": funding_series,
            "oiHistory": oi_hist,
            "trend": trend_scores(closes),
            "trendHistory": trend_history(closed),
            "gauges": gauges(bd, hlf, last24_mean, change24h, closes) if bd and hlf else None,
            "flows": flows(bd),
            "binance": {
                "asOf": bd[-1]["d"] if bd else None,
                "oi": bd[-1]["oi"] if bd else None,
                "oiUsd": bd[-1]["oi"] * bd[-1]["px"] if bd else None,
                "lsTop": bd[-1]["lsTop"] if bd else None,
                "lsTopAcc": bd[-1]["lsTopAcc"] if bd else None,
                "lsGlobal": bd[-1]["lsGlobal"] if bd else None,
                "taker": bd[-1]["taker"] if bd else None,
                "oiHourly": [[h[0], round(h[1] * h[2])] for h in bn["hourly"] if h[0] >= now - PAGE_DAYS * DAY],
                "daily": [[d["d"], round(d["oi"]), r(d["lsTop"], 3), r(d["lsGlobal"], 3)] for d in bd[-90:]],
            },
            "liquidations": liquidation_map(bn["hourly"], h_candles, mark),
        }

    out = {
        "updatedAt": datetime.fromtimestamp(now / 1000, timezone.utc).isoformat(timespec="seconds"),
        "source": "Hyperliquid public API; Binance public data archive (data.binance.vision)",
        "coins": coins,
    }
    os.makedirs(ROOT, exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(out, f, separators=(",", ":"))
    with open(HIST, "w") as f:
        json.dump(hist, f, separators=(",", ":"))
    for k, v in coins.items():
        t = v["trend"] or {}
        g = v["gauges"] or {}
        print(f"{k} {v['mark']:,.2f} | trend mid {t.get('mid', {}).get('score')} long {t.get('long', {}).get('score')}"
              f" | FB {g.get('fundingBias')} OIP {g.get('oiPressure')} T {g.get('tension')}"
              f" | binance thru {v['binance']['asOf']} | liq bins {len((v['liquidations'] or {}).get('bins', []))}")


if __name__ == "__main__":
    main()
