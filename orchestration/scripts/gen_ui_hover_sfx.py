"""Generate UI hover SFX (S-122 Phase 1).
- sfx_ui_hover.wav — subtle pointer-enter tap, ~60ms

Deterministic: seed 20122. 16-bit PCM mono 44.1 kHz. Subtle by design — 0.55 peak,
not 0.85, so users don't get overstimulated when sweeping the cursor across menus.
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


def chirp(start_hz, end_hz, ms, attack_ms=2, decay_curve=4.0):
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


def normalize(buf, peak):
    m = max(abs(x) for x in buf) or 1.0
    g = peak / m
    return [x * g for x in buf]


# ---------- sfx_ui_hover (60 ms) ----------
# Peak intentionally low (0.55) — hover SFX play frequently, must not fatigue.
buf = empty(60)

pluck = chirp(1800, 1500, 14, attack_ms=1, decay_curve=5.0)
add(buf, 0, pluck, gain=0.45)

noise = filtered_noise(45, lo_hz=1500, seed=20122)
noise_env = [math.exp(-4.5 * i / max(1, len(noise) - 1)) for i in range(len(noise))]
noise = [s * e for s, e in zip(noise, noise_env)]
add(buf, int(SR * 0.010), noise, gain=0.30)

buf = normalize(buf, 0.55)
write_wav(os.path.join(OUT_GEN, "sfx_ui_hover.wav"), buf)
write_wav(os.path.join(OUT_RES, "sfx_ui_hover.wav"), buf)

p = os.path.join(OUT_GEN, "sfx_ui_hover.wav")
print("sfx_ui_hover.wav", os.path.getsize(p), "bytes")
