"""Generate 3-tier coin drop SFX (S-117).
- sfx_coin_small.wav  (normal monster) — single pluck, ~150ms
- sfx_coin_pile.wav   (elite monster)  — 3 staggered plucks, ~280ms
- sfx_coin_burst.wav  (boss monster)   — 5 plucks + sparkle tail, ~500ms
"""
import math, struct, wave, os, random

SR = 44100
OUT = r"C:\sourcetree\Gen\Assets\Audio\Generated"

def write_wav(path, samples):
    samples = [max(-1.0, min(1.0, s)) for s in samples]
    pcm = b"".join(struct.pack("<h", int(s * 32760)) for s in samples)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm)

def empty(ms):
    return [0.0] * int(SR * ms / 1000)

def add(buf, off_samples, signal, gain=1.0):
    for i, v in enumerate(signal):
        idx = off_samples + i
        if 0 <= idx < len(buf):
            buf[idx] += v * gain

def chirp(start_hz, end_hz, ms, attack_ms=3, release_ms=None):
    """Exponential pitch sweep with attack/decay envelope (coin pluck)."""
    n = int(SR * ms / 1000)
    out = []
    if release_ms is None:
        release_ms = ms - attack_ms
    a_n = max(1, int(SR * attack_ms / 1000))
    for i in range(n):
        t = i / SR
        # exponential pitch sweep
        freq = start_hz * (end_hz / start_hz) ** (i / max(1, n - 1))
        phase = 2 * math.pi * freq * t
        # attack-release envelope
        if i < a_n:
            env = i / a_n
        else:
            env = math.exp(-3.5 * (i - a_n) / max(1, n - a_n))
        # Triangle-ish bright tone (square + sine blend = brighter)
        s = 0.65 * math.sin(phase) + 0.25 * math.sin(2 * phase) + 0.10 * math.sin(3 * phase)
        out.append(s * env)
    return out

def click_noise(ms, hz_filter=0.0):
    """Short noise burst for impact attack."""
    n = int(SR * ms / 1000)
    out = []
    rng = random.Random(1234)
    for i in range(n):
        env = math.exp(-12.0 * i / max(1, n - 1))
        out.append((rng.random() * 2 - 1) * env)
    return out

def normalize(buf, peak=0.85):
    m = max(abs(x) for x in buf) or 1.0
    g = peak / m
    return [x * g for x in buf]

# ---------- coin_small (normal) ~150ms ----------
small = empty(150)
add(small, 0, click_noise(20), gain=0.25)
add(small, 5, chirp(2400, 2900, 130, attack_ms=4), gain=0.85)
small = normalize(small, 0.82)
write_wav(os.path.join(OUT, "sfx_coin_small.wav"), small)

# ---------- coin_pile (elite) ~280ms ----------
pile = empty(280)
add(pile, 0, click_noise(15), gain=0.20)
add(pile, 0,   chirp(2200, 2600, 110, attack_ms=4), gain=0.70)
add(pile, int(SR*0.060), chirp(1900, 2300, 120, attack_ms=4), gain=0.60)
add(pile, int(SR*0.130), chirp(2500, 2800, 140, attack_ms=4), gain=0.65)
add(pile, int(SR*0.075), click_noise(10), gain=0.10)
pile = normalize(pile, 0.85)
write_wav(os.path.join(OUT, "sfx_coin_pile.wav"), pile)

# ---------- coin_burst (boss) ~500ms ----------
burst = empty(500)
add(burst, 0, click_noise(25), gain=0.30)
# Descending arpeggio of 5 chirps
arp = [3200, 2800, 2500, 2200, 1900]
for i, base in enumerate(arp):
    offset_ms = i * 55
    add(burst, int(SR * offset_ms / 1000),
        chirp(base, base * 1.18, 200, attack_ms=4),
        gain=0.55)
# Sparkle tail (high-freq shimmer)
sparkle_n = int(SR * 0.250)
sparkle = []
rng = random.Random(99)
for i in range(sparkle_n):
    t = i / SR
    env = math.exp(-3.0 * i / sparkle_n)
    f1 = 3800 + 400 * math.sin(2 * math.pi * 7 * t)
    f2 = 4500 + 300 * math.sin(2 * math.pi * 11 * t + 1.0)
    s = 0.5 * math.sin(2 * math.pi * f1 * t) + 0.4 * math.sin(2 * math.pi * f2 * t)
    s += (rng.random() * 2 - 1) * 0.15 * env
    sparkle.append(s * env * 0.45)
add(burst, int(SR * 0.230), sparkle, gain=1.0)
burst = normalize(burst, 0.88)
write_wav(os.path.join(OUT, "sfx_coin_burst.wav"), burst)

for fn in ("sfx_coin_small.wav", "sfx_coin_pile.wav", "sfx_coin_burst.wav"):
    p = os.path.join(OUT, fn)
    print(fn, os.path.getsize(p), "bytes")
