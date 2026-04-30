"""Generate NPC dialogue start/end SFX (S-121).
- sfx_dialogue_open.wav  — page-flip / book-open feel, ~200ms
- sfx_dialogue_close.wav — page-fold / book-close feel, ~180ms

Deterministic: seeds 20121 / 20122. 16-bit PCM mono 44.1 kHz.
"""
import math, struct, wave, os, random

SR = 44100
OUT_GEN = r"C:\sourcetree\Gen\Assets\Audio\Generated"
OUT_RES = r"C:\sourcetree\Gen\Assets\Resources\Audio\SFX"


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


def filtered_noise(ms, lo_hz=0.0, hi_hz=0.0, seed=0):
    """Single-pole high/low-pass on white noise. lo_hz=high-pass cutoff, hi_hz=low-pass cutoff."""
    n = int(SR * ms / 1000)
    rng = random.Random(seed)
    raw = [(rng.random() * 2 - 1) for _ in range(n)]
    out = list(raw)
    if hi_hz > 0:
        rc = 1.0 / (2 * math.pi * hi_hz)
        a = (1.0 / SR) / (rc + 1.0 / SR)
        prev = 0.0
        for i in range(n):
            prev = prev + a * (out[i] - prev)
            out[i] = prev
    if lo_hz > 0:
        rc = 1.0 / (2 * math.pi * lo_hz)
        a = rc / (rc + 1.0 / SR)
        prev_in = out[0]
        prev_out = out[0]
        for i in range(n):
            new_out = a * (prev_out + out[i] - prev_in)
            prev_in = out[i]
            prev_out = new_out
            out[i] = new_out
    return out


def env_attack_decay(n, attack_n, decay_curve=3.5):
    """Linear attack, exponential decay."""
    out = []
    a_n = max(1, attack_n)
    for i in range(n):
        if i < a_n:
            out.append(i / a_n)
        else:
            out.append(math.exp(-decay_curve * (i - a_n) / max(1, n - a_n)))
    return out


def chirp(start_hz, end_hz, ms, attack_ms=3, decay_curve=3.5):
    n = int(SR * ms / 1000)
    a_n = max(1, int(SR * attack_ms / 1000))
    out = []
    for i in range(n):
        t = i / SR
        freq = start_hz * (end_hz / start_hz) ** (i / max(1, n - 1))
        phase = 2 * math.pi * freq * t
        if i < a_n:
            env = i / a_n
        else:
            env = math.exp(-decay_curve * (i - a_n) / max(1, n - a_n))
        s = 0.7 * math.sin(phase) + 0.2 * math.sin(2 * phase)
        out.append(s * env)
    return out


def normalize(buf, peak=0.85):
    m = max(abs(x) for x in buf) or 1.0
    g = peak / m
    return [x * g for x in buf]


# ---------- sfx_dialogue_open (200 ms) ----------
# 0~50ms: high-pass noise burst (paper rustle), 800Hz HP, sharp attack
# 50~150ms: pluck chirp 1200→600 Hz
# 150~200ms: low-passed tail (1600 Hz LP)
op = empty(200)

burst = filtered_noise(50, lo_hz=800, seed=20121)
burst_env = env_attack_decay(len(burst), int(SR * 0.005), decay_curve=4.0)
burst = [s * e for s, e in zip(burst, burst_env)]
add(op, 0, burst, gain=0.55)

pluck = chirp(1200, 600, 100, attack_ms=4, decay_curve=3.0)
add(op, int(SR * 0.050), pluck, gain=0.70)

tail = filtered_noise(50, hi_hz=1600, seed=20121 + 1)
tail_env = [math.exp(-4.0 * i / max(1, len(tail) - 1)) for i in range(len(tail))]
tail = [s * e for s, e in zip(tail, tail_env)]
add(op, int(SR * 0.150), tail, gain=0.30)

op = normalize(op, 0.85)
write_wav(os.path.join(OUT_GEN, "sfx_dialogue_open.wav"), op)
write_wav(os.path.join(OUT_RES, "sfx_dialogue_open.wav"), op)

# ---------- sfx_dialogue_close (180 ms) ----------
# 0~30ms: muted attack 600→300 Hz (book closing thud)
# 30~140ms: low-pass noise tail decrescendo (600 Hz LP)
# 140~180ms: silence ramp
cl = empty(180)

thud = chirp(600, 300, 30, attack_ms=2, decay_curve=2.0)
add(cl, 0, thud, gain=0.50)

decay = filtered_noise(110, hi_hz=600, seed=20122)
decay_env = [math.exp(-3.0 * i / max(1, len(decay) - 1)) for i in range(len(decay))]
decay = [s * e for s, e in zip(decay, decay_env)]
add(cl, int(SR * 0.030), decay, gain=0.45)

# 140~180 ramp: pre-existing tail samples already taper; explicit silence not required.

cl = normalize(cl, 0.78)
write_wav(os.path.join(OUT_GEN, "sfx_dialogue_close.wav"), cl)
write_wav(os.path.join(OUT_RES, "sfx_dialogue_close.wav"), cl)

for fn in ("sfx_dialogue_open.wav", "sfx_dialogue_close.wav"):
    p = os.path.join(OUT_GEN, fn)
    print(fn, os.path.getsize(p), "bytes")
