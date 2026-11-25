"""Genera todos los sonidos y la música de Disaster Ready (síntesis propia, sin derechos de terceros)."""
import os, sys
import numpy as np
from scipy import signal
from scipy.io import wavfile

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)
SR = 44100      # efectos
SRM = 32000     # música y ambientes (archivos más livianos)


# ---------------------------------------------------------------- utilidades
def t_axis(dur, sr):
    return np.arange(int(dur * sr)) / sr

def save(name, x, sr, peak_db=-1.0):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) + 1e-9
    x = x / peak * (10 ** (peak_db / 20))
    wavfile.write(os.path.join(OUT, name + ".wav"), sr, (x * 32767).astype(np.int16))

def env_adsr(n, sr, a=0.01, d=0.1, s=0.7, r=0.2):
    a_n, d_n, r_n = int(a * sr), int(d * sr), int(r * sr)
    s_n = max(0, n - a_n - d_n - r_n)
    e = np.concatenate([np.linspace(0, 1, a_n, endpoint=False), np.linspace(1, s, d_n, endpoint=False),
                        np.full(s_n, s), np.linspace(s, 0, r_n)])
    return np.pad(e, (0, max(0, n - len(e))))[:n]

def exp_env(n, sr, decay):
    return np.exp(-np.arange(n) / sr / decay)

def lp(x, sr, f, order=2):
    b, a = signal.butter(order, min(f / (sr / 2), 0.99), "low")
    return signal.lfilter(b, a, x)

def hp(x, sr, f, order=2):
    b, a = signal.butter(order, min(f / (sr / 2), 0.99), "high")
    return signal.lfilter(b, a, x)

def bp(x, sr, lo, hi, order=2):
    b, a = signal.butter(order, [lo / (sr / 2), min(hi / (sr / 2), 0.99)], "band")
    return signal.lfilter(b, a, x)

def noise(n):
    return rng.uniform(-1, 1, n)

def brown(n):
    b = np.cumsum(rng.normal(0, 1, n))
    b = b - signal.lfilter([1], [1, -0.9995], b) * 0.0005
    b = hp(b, 44100, 15)
    return b / (np.max(np.abs(b)) + 1e-9)

def reverb(x, sr, t60=1.6, mix=0.25, tone=4000):
    n = int(t60 * sr)
    ir = noise(n) * np.exp(-6.9 * np.arange(n) / n)
    ir = lp(ir, sr, tone)
    ir[: int(0.012 * sr)] = 0
    wet = signal.fftconvolve(x, ir)[: len(x)]
    wet = wet / (np.max(np.abs(wet)) + 1e-9) * (np.max(np.abs(x)) + 1e-9)
    return x * (1 - mix) + wet * mix

def loopify(x, sr, fade=1.0):
    f = int(fade * sr)
    head, tail = x[:f].copy(), x[-f:]
    w = np.linspace(0, 1, f)
    y = x[:-f].copy()
    y[:f] = head * w + tail * (1 - w)
    return y

def place(buf, x, start):
    s = int(start)
    if s >= len(buf): return
    e = min(len(buf), s + len(x))
    buf[s:e] += x[: e - s]

def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


# ---------------------------------------------------------------- instrumentos
def pluck(freq, dur, sr, bright=0.6, decay=0.996):
    """Karplus-Strong: suena a charango / guitarra."""
    n = int(dur * sr)
    period = max(2, int(sr / freq))
    buf = noise(period) * 0.5
    buf = lp(buf, sr, 1500 + 6000 * bright)
    out = np.zeros(n)
    for i in range(n):
        v = buf[i % period]
        out[i] = v
        buf[i % period] = decay * 0.5 * (v + buf[(i + 1) % period])
    return out * env_adsr(n, sr, 0.002, 0.05, 0.9, 0.05)

def quena(freq, dur, sr, vib=5.2):
    """Flauta andina: seno con vibrato, armónicos suaves y soplido."""
    t = t_axis(dur, sr)
    v = 1 + 0.008 * np.sin(2 * np.pi * vib * t) * np.clip(t / 0.25, 0, 1)
    ph = 2 * np.pi * np.cumsum(freq * v) / sr
    tone = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.06 * np.sin(3 * ph)
    breath = bp(noise(len(t)), sr, freq * 1.5, min(freq * 6, sr / 2.2)) * 0.12
    return (tone + breath) * env_adsr(len(t), sr, 0.07, 0.1, 0.85, 0.12)

def pad(freqs, dur, sr, cutoff=1400, a=0.6, r=0.8):
    t = t_axis(dur, sr)
    x = np.zeros(len(t))
    for f in freqs:
        for det in (-0.12, 0.0, 0.11):
            ph = 2 * np.pi * f * (1 + det / 100) * t + rng.uniform(0, 6.28)
            x += signal.sawtooth(ph) * 0.3
    x = lp(x, sr, cutoff, 2)
    return x * env_adsr(len(t), sr, a, 0.2, 0.8, r)

def bass(freq, dur, sr):
    t = t_axis(dur, sr)
    x = np.sin(2 * np.pi * freq * t) + 0.3 * np.sin(4 * np.pi * freq * t)
    return x * env_adsr(len(t), sr, 0.01, 0.15, 0.6, 0.1)

def bombo(sr, big=1.0):
    """Bombo andino: golpe grave con piel."""
    n = int(0.6 * sr)
    t = np.arange(n) / sr
    f = 48 + 70 * np.exp(-t / 0.04)
    x = np.sin(2 * np.pi * np.cumsum(f) / sr) * np.exp(-t / (0.18 * big))
    x += lp(noise(n), sr, 900) * np.exp(-t / 0.03) * 0.4
    return x

def shaker(sr, amp=0.25):
    n = int(0.07 * sr)
    return hp(noise(n), sr, 6000) * np.exp(-np.arange(n) / sr / 0.02) * amp

def woodblock(sr, f=1800):
    n = int(0.08 * sr)
    t = np.arange(n) / sr
    return (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.7 * t)) * np.exp(-t / 0.015)

def tom(sr, f=90):
    n = int(0.5 * sr)
    t = np.arange(n) / sr
    fr = f * (1 + 0.5 * np.exp(-t / 0.05))
    return np.sin(2 * np.pi * np.cumsum(fr) / sr) * np.exp(-t / 0.2) + lp(noise(n), sr, 1500) * np.exp(-t / 0.02) * 0.3

def bell(freq, dur, sr):
    t = t_axis(dur, sr)
    mod = np.sin(2 * np.pi * freq * 3.5 * t) * 2.0 * np.exp(-t / 0.3)
    return np.sin(2 * np.pi * freq * t + mod) * np.exp(-t / (dur * 0.4))


# ---------------------------------------------------------------- música
CHORDS = {  # notas MIDI
    "Em": [52, 55, 59], "D": [50, 54, 57], "G": [55, 59, 62], "C": [48, 52, 55], "B7": [47, 51, 54, 57],
    "Am": [57, 60, 64], "F": [53, 57, 60], "Cmaj": [60, 64, 67], "Gmaj": [55, 59, 62],
    "Dm": [50, 53, 57], "Bb": [46, 50, 53], "Gm": [55, 58, 62], "A": [57, 61, 64],
}

def song(sr, bpm, bars, chords, melody, kit, pad_cut=1300, arp=True, arp_vol=0.35, quena_vol=0.5, extra=None):
    beat = 60 / bpm
    total = bars * 4 * beat
    out = np.zeros(int((total + 2) * sr))
    for bar in range(bars):
        ch = CHORDS[chords[bar % len(chords)]]
        start = bar * 4 * beat
        place(out, pad([midi(n) for n in ch], 4 * beat + 0.4, sr, pad_cut) * 0.16, start * sr)
        place(out, bass(midi(ch[0] - 12), beat * 1.8, sr) * 0.35, start * sr)
        place(out, bass(midi(ch[0] - 12), beat * 1.8, sr) * 0.3, (start + 2 * beat) * sr)
        if arp:
            pattern = [0, 1, 2, 1, 2, 1, 0, 2]
            for i, p in enumerate(pattern):
                note = ch[p % len(ch)] + 12
                accent = 1.0 if i % 2 == 0 else 0.7
                place(out, pluck(midi(note), beat * 0.9, sr, 0.75) * arp_vol * accent, (start + i * beat / 2) * sr)
        kit(out, start, beat, bar)
    # melodía: lista de (compás, tiempo, nota MIDI, duración en tiempos)
    for (bar, pos, note, length) in melody:
        place(out, quena(midi(note), length * beat, sr) * quena_vol, (bar * 4 + pos) * beat * sr)
    if extra: extra(out, beat)
    out = out[: int(total * sr) + int(1.0 * sr)]
    out = reverb(out, sr, 2.2, 0.3, 5000)
    return loopify(out, sr, 1.0)

def kit_huayno(out, start, beat, bar):
    sr = SRM
    # ritmo de huayno: largo-corto
    place(out, bombo(sr) * 0.55, start * sr)
    place(out, bombo(sr, 0.6) * 0.35, (start + 1.5 * beat) * sr)
    place(out, bombo(sr) * 0.5, (start + 2 * beat) * sr)
    place(out, bombo(sr, 0.6) * 0.35, (start + 3.5 * beat) * sr)
    for i in range(8):
        place(out, shaker(sr, 0.18 if i % 2 else 0.25), (start + i * beat / 2) * sr)

def kit_prep(out, start, beat, bar):
    sr = SRM
    for i in range(4):
        place(out, woodblock(sr, 1700 if i % 2 else 2200) * 0.15, (start + i * beat) * sr)  # reloj
    for i in range(8):
        place(out, shaker(sr, 0.12), (start + i * beat / 2 + beat / 4) * sr)
    if bar % 2 == 0: place(out, bombo(sr, 0.7) * 0.4, start * sr)

def kit_evac(out, start, beat, bar):
    sr = SRM
    place(out, tom(sr, 70) * 0.6, start * sr)
    place(out, tom(sr, 70) * 0.45, (start + 1.5 * beat) * sr)
    place(out, tom(sr, 95) * 0.4, (start + 2.5 * beat) * sr)
    place(out, tom(sr, 60) * 0.5, (start + 3 * beat) * sr)
    for i in range(16):
        place(out, shaker(sr, 0.08), (start + i * beat / 4) * sr)

def melody_menu():
    m = []
    phrase_a = [(0, 0, 71, 1.5), (0, 1.5, 74, 0.5), (0, 2, 76, 2), (1, 0, 74, 1), (1, 1, 71, 1), (1, 2, 69, 2),
                (2, 0, 67, 1.5), (2, 1.5, 69, 0.5), (2, 2, 71, 1), (2, 3, 74, 1), (3, 0, 71, 3.5)]
    phrase_b = [(4, 0, 76, 1), (4, 1, 79, 1), (4, 2, 76, 1), (4, 3, 74, 1), (5, 0, 71, 2), (5, 2, 74, 2),
                (6, 0, 71, 1), (6, 1, 69, 1), (6, 2, 67, 1), (6, 3, 69, 1), (7, 0, 64, 3.5)]
    for off in (0, 8):
        for (b, p, n, l) in phrase_a + phrase_b:
            m.append((b + off, p, n, l))
    return m

def music():
    # Menú: huayno instrumental en Mi menor
    menu = song(SRM, 84, 16, ["Em", "D", "G", "Em", "C", "D", "Em", "Em"], melody_menu(), kit_huayno, arp_vol=0.3)
    save("music_menu", menu, SRM, -3)

    # Preparación: charango y reloj, curioso y con prisa
    prep_mel = []
    for off in (0, 8):
        prep_mel += [(off + 1, 0, 76, 0.5), (off + 1, 0.5, 79, 0.5), (off + 1, 1, 81, 1), (off + 3, 0, 84, 0.5),
                     (off + 3, 0.5, 81, 0.5), (off + 3, 1, 79, 2), (off + 5, 0, 76, 1), (off + 5, 1, 79, 1),
                     (off + 7, 0, 81, 0.5), (off + 7, 0.5, 79, 0.5), (off + 7, 1, 76, 2)]
    prep = song(SRM, 100, 16, ["Am", "F", "Cmaj", "Gmaj"], prep_mel, kit_prep, pad_cut=1000, arp_vol=0.32, quena_vol=0.32)
    save("music_prep", prep, SRM, -4)

    # Evacuación: tensión con ostinato grave y tremolo
    def evac_extra(out, beat):
        sr = SRM
        notes = [38, 38, 41, 38, 43, 38, 41, 36]
        for bar in range(16):
            for i in range(8):
                n = notes[i] + (0 if bar % 4 < 2 else -2)
                t = t_axis(beat / 2 * 0.9, sr)
                x = signal.sawtooth(2 * np.pi * midi(n) * t) * env_adsr(len(t), sr, 0.005, 0.08, 0.5, 0.05)
                place(out, lp(x, sr, 500) * 0.35, (bar * 4 + i / 2) * beat * sr)
        # tremolo agudo
        total = len(out) / sr
        t = t_axis(total, sr)
        trem = (0.5 + 0.5 * np.sin(2 * np.pi * 7 * t)) * 0.05
        hi = pad([midi(74), midi(77)], total, sr, 2500, 2, 1) * trem
        out[: len(hi)] += hi
    evac = song(SRM, 112, 16, ["Dm", "Bb", "Gm", "A"], [], kit_evac, pad_cut=900, arp=False, extra=evac_extra)
    save("music_evac", evac, SRM, -4)

    # Jingle de victoria y de derrota
    win = np.zeros(int(3.2 * SR))
    for i, n in enumerate([64, 67, 71, 76, 79]):
        place(win, pluck(midi(n), 1.5, SR, 0.8) * 0.5, i * 0.11 * SR)
    place(win, pad([midi(55), midi(59), midi(62), midi(67)], 2.6, SR, 2500, 0.05, 1.2) * 0.25, 0.5 * SR)
    place(win, quena(midi(79), 1.6, SR) * 0.4, 0.6 * SR)
    place(win, bell(midi(91), 1.5, SR) * 0.2, 0.6 * SR)
    save("win", reverb(win, SR, 1.8, 0.3), SR, -2)

    lose = np.zeros(int(3.2 * SR))
    for i, n in enumerate([71, 69, 67, 64]):
        place(lose, quena(midi(n), 0.7, SR) * 0.45, i * 0.45 * SR)
    place(lose, pad([midi(40), midi(47), midi(52)], 2.8, SR, 700, 0.3, 1.0) * 0.3, 0)
    save("lose", reverb(lose, SR, 2.0, 0.35), SR, -3)


# ---------------------------------------------------------------- ambientes
def ambience():
    # Casa tranquila: tono de habitación, pájaros y ciudad lejana
    dur = 24
    n = int(dur * SRM)
    room = lp(noise(n), SRM, 400) * 0.05 + lp(brown(n), SRM, 200) * 0.08
    birds = np.zeros(n)
    for _ in range(26):
        s = rng.uniform(0, dur - 1)
        f0 = rng.uniform(2500, 4200)
        for k in range(rng.integers(2, 5)):
            d = rng.uniform(0.05, 0.12)
            t = t_axis(d, SRM)
            sweep = f0 * (1 + 0.25 * np.sin(np.pi * t / d)) * rng.uniform(0.9, 1.1)
            chirp = np.sin(2 * np.pi * np.cumsum(sweep) / SRM) * np.sin(np.pi * t / d) ** 2
            place(birds, chirp * rng.uniform(0.05, 0.12), (s + k * 0.13) * SRM)
    city = bp(noise(n), SRM, 150, 900) * 0.04
    for _ in range(3):  # auto lejano
        s = rng.uniform(0, dur - 5)
        t = t_axis(4, SRM)
        car = bp(noise(len(t)), SRM, 80, 600) * np.sin(np.pi * t / 4) ** 2 * 0.15
        place(city, car, s * SRM)
    amb = reverb(room + birds + city, SRM, 1.2, 0.2)
    save("amb_house", loopify(amb, SRM, 1.5), SRM, -10)

    # Después del sismo: viento, sirenas lejanas, perros y escombros
    n = int(dur * SRM)
    t = np.arange(n) / SRM
    wind = bp(noise(n), SRM, 200, 1200) * (0.5 + 0.5 * np.sin(2 * np.pi * 0.07 * t + 1) * np.sin(2 * np.pi * 0.13 * t)) * 0.25
    siren = np.zeros(n)
    for start in (2.0, 13.0):
        d = 9.0
        tt = t_axis(d, SRM)
        f = 700 + 350 * np.sin(2 * np.pi * 0.35 * tt)
        s = (np.sin(2 * np.pi * np.cumsum(f) / SRM) + 0.3 * np.sin(4 * np.pi * np.cumsum(f) / SRM))
        s = lp(s, SRM, 1800) * np.sin(np.pi * tt / d) ** 2 * 0.09
        place(siren, s, start * SRM)
    debris = np.zeros(n)
    for _ in range(20):
        k = hp(noise(int(0.03 * SRM)), SRM, 1500) * np.exp(-np.arange(int(0.03 * SRM)) / SRM / 0.008)
        place(debris, k * rng.uniform(0.03, 0.08), rng.uniform(0, dur - 0.1) * SRM)
    amb2 = reverb(wind + siren + debris, SRM, 2.5, 0.4)
    save("amb_after", loopify(amb2, SRM, 1.5), SRM, -9)


# ---------------------------------------------------------------- sismo y alarma
def quake_and_alarm():
    # Alarma sísmica: sirena que sube y baja (en bucle)
    dur = 4.0
    t = t_axis(dur, SR)
    f = 600 + 500 * (0.5 - 0.5 * np.cos(2 * np.pi * t / 2.0))
    ph = 2 * np.pi * np.cumsum(f) / SR
    x = signal.square(ph, 0.5) * 0.4 + signal.sawtooth(ph) * 0.4 + np.sin(ph) * 0.4
    x = lp(x, SR, 3500)
    x = reverb(x, SR, 1.0, 0.25)
    save("alarm_siren", loopify(x, SR, 0.25), SR, -2)

    # Retumbo: graves profundos, crujidos y golpes (en bucle)
    dur = 10.0
    n = int(dur * SR)
    t = np.arange(n) / SR
    rumble = lp(brown(n), SR, 110, 4) * 1.0
    sub = np.sin(2 * np.pi * (34 + 6 * np.sin(2 * np.pi * 0.3 * t)) * t) * 0.35
    mod = 0.65 + 0.35 * lp(noise(n), SR, 3) / 0.02
    mod = np.clip(mod, 0.3, 1.3)
    rattles = bp(noise(n), SR, 300, 2500) * (lp(np.abs(noise(n)), SR, 8) > 0.55) * 0.12
    thumps = np.zeros(n)
    for _ in range(18):
        k = bombo(SR, rng.uniform(0.5, 1.2)) * rng.uniform(0.2, 0.5)
        place(thumps, lp(k, SR, 200), rng.uniform(0, dur - 0.6) * SR)
    x = (rumble + sub) * mod + rattles + thumps
    save("quake_rumble", loopify(x, SR, 0.8), SR, -1)

    # Crujidos de madera / estructura
    for i in range(2):
        d = 1.4
        t = t_axis(d, SR)
        f = rng.uniform(90, 160) * (1 + 0.3 * np.sin(2 * np.pi * rng.uniform(0.6, 1.2) * t))
        stick = (lp(np.abs(noise(len(t))), SR, 30) > 0.45).astype(float)
        saw = signal.sawtooth(2 * np.pi * np.cumsum(f) / SR) * (0.4 + 0.6 * stick)
        x = bp(saw, SR, 200, 2200) * np.sin(np.pi * t / d) ** 0.7
        save(f"creak_{i + 1}", reverb(x, SR, 1.2, 0.25), SR, -3)


# ---------------------------------------------------------------- efectos
def sfx():
    # Vidrios rompiéndose
    for i in range(2):
        d = 1.6
        n = int(d * SR)
        x = np.zeros(n)
        crash = hp(noise(int(0.25 * SR)), SR, 2500) * np.exp(-np.arange(int(0.25 * SR)) / SR / 0.06)
        place(x, crash * 0.9, 0)
        for _ in range(45):
            f = rng.uniform(2500, 9000)
            dd = rng.uniform(0.05, 0.3)
            tt = t_axis(dd, SR)
            ping = np.sin(2 * np.pi * f * tt) * np.exp(-tt / (dd * 0.3))
            place(x, ping * rng.uniform(0.05, 0.25), rng.uniform(0, 1.0) ** 2 * SR)
        place(x, lp(bombo(SR, 0.4), SR, 300) * 0.3, 0)
        save(f"glass_{i + 1}", reverb(x, SR, 0.9, 0.2), SR, -1)

    # Impactos livianos (libros, platos)
    for i in range(3):
        n = int(0.3 * SR)
        t = np.arange(n) / SR
        f = rng.uniform(140, 260)
        body = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.05)
        knock = bp(noise(n), SR, 600, 3500) * np.exp(-t / 0.015)
        save(f"impact_light_{i + 1}", reverb(body * 0.6 + knock, SR, 0.5, 0.15), SR, -2)

    # Impactos pesados (muebles)
    for i in range(2):
        n = int(1.4 * SR)
        t = np.arange(n) / SR
        boom = np.sin(2 * np.pi * np.cumsum(45 + 60 * np.exp(-t / 0.05)) / SR) * np.exp(-t / 0.3)
        crash = lp(noise(n), SR, 2500) * np.exp(-t / 0.25) * 0.6
        wood = bp(noise(n), SR, 300, 1200) * np.exp(-t / 0.12) * 0.5
        save(f"impact_heavy_{i + 1}", reverb(boom + crash + wood, SR, 1.4, 0.25), SR, -1)

    # Pasos en madera y en césped
    for i in range(4):
        n = int(0.16 * SR)
        t = np.arange(n) / SR
        x = bp(noise(n), SR, 150, rng.uniform(900, 1400)) * np.exp(-t / 0.03)
        x += np.sin(2 * np.pi * rng.uniform(90, 130) * t) * np.exp(-t / 0.025) * 0.6
        save(f"step_wood_{i + 1}", reverb(x, SR, 0.3, 0.1), SR, -4)
    for i in range(3):
        n = int(0.22 * SR)
        t = np.arange(n) / SR
        grains = hp(noise(n), SR, 2500) * (lp(np.abs(noise(n)), SR, 120) * 4)
        x = grains * np.sin(np.pi * np.clip(t / 0.2, 0, 1)) * 0.8
        save(f"step_grass_{i + 1}", x, SR, -6)

    # Recoger: cierre de mochila + campanita
    n = int(0.7 * SR)
    x = np.zeros(n)
    zt = int(0.16 * SR)
    zip_ = hp(noise(zt), SR, 2000) * (0.5 + 0.5 * signal.square(2 * np.pi * 70 * np.arange(zt) / SR)) * np.linspace(0.3, 1, zt) * 0.4
    place(x, zip_, 0)
    place(x, bell(midi(88), 0.5, SR) * 0.5, 0.12 * SR)
    place(x, bell(midi(95), 0.45, SR) * 0.45, 0.2 * SR)
    save("pickup", reverb(x, SR, 0.8, 0.2), SR, -2)

    # Objeto no esencial
    n = int(0.5 * SR)
    x = np.zeros(n)
    for i, f in enumerate([330, 247]):
        tt = t_axis(0.2, SR)
        place(x, (signal.square(2 * np.pi * f * tt) * 0.3 + np.sin(2 * np.pi * f * tt)) * np.exp(-tt / 0.08), i * 0.12 * SR)
    save("pickup_bad", lp(x, SR, 2000), SR, -4)

    # Sacar de la mochila
    n = int(0.45 * SR)
    x = np.zeros(n)
    place(x, zip_[::-1] * 0.8, 0)
    tt = t_axis(0.2, SR)
    place(x, np.sin(2 * np.pi * 120 * tt) * np.exp(-tt / 0.04), 0.2 * SR)
    save("drop", x, SR, -3)

    # Acción no permitida
    n = int(0.3 * SR)
    x = np.zeros(n)
    for k in range(2):
        tt = t_axis(0.09, SR)
        place(x, signal.square(2 * np.pi * 150 * tt) * env_adsr(len(tt), SR, 0.005, 0.02, 0.8, 0.02), k * 0.12 * SR)
    save("denied", lp(x, SR, 1500), SR, -6)

    # Interruptor / palanca del tablero
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    x = bp(noise(n), SR, 1500, 6000) * np.exp(-t / 0.006)
    x2 = np.zeros(n)
    place(x2, bp(noise(int(0.1 * SR)), SR, 400, 2500) * np.exp(-np.arange(int(0.1 * SR)) / SR / 0.02), 0.05 * SR)
    save("switch", reverb(x + x2 * 0.8, SR, 0.4, 0.15), SR, -3)

    # Válvula de gas: chirrido metálico + golpe
    d = 0.8
    t = t_axis(d, SR)
    f = 900 - 250 * t / d
    sq = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.5 * np.sin(2 * np.pi * np.cumsum(f * 2.01) / SR)
    sq *= np.sin(np.pi * np.clip(t / 0.55, 0, 1)) * (0.6 + 0.4 * (lp(np.abs(noise(len(t))), SR, 40) > 0.4))
    clunk = np.zeros(len(t))
    place(clunk, bp(noise(int(0.08 * SR)), SR, 300, 3000) * np.exp(-np.arange(int(0.08 * SR)) / SR / 0.015), 0.58 * SR)
    save("valve", reverb(sq * 0.4 + clunk, SR, 0.5, 0.2), SR, -3)

    # Golpe recibido: impacto + zumbido en el oído
    for i in range(2):
        n = int(1.0 * SR)
        t = np.arange(n) / SR
        thud = np.sin(2 * np.pi * np.cumsum(60 + 80 * np.exp(-t / 0.03)) / SR) * np.exp(-t / 0.12)
        ring = np.sin(2 * np.pi * rng.uniform(3500, 4500) * t) * np.exp(-t / 0.4) * 0.12
        save(f"hurt_{i + 1}", thud + ring + lp(noise(n), SR, 800) * np.exp(-t / 0.03) * 0.4, SR, -1)

    # Transición de fase
    d = 0.7
    n = int(d * SR)
    t = np.arange(n) / SR
    x = noise(n)
    out = np.zeros(n)
    for k in range(0, n, 512):
        f = 300 + 3500 * (k / n)
        seg = bp(x[k:k + 1024], SR, f, f * 1.6)
        out[k:k + len(seg)] += seg[: len(out[k:k + len(seg)])]
    save("whoosh", out * np.sin(np.pi * t / d) ** 2, SR, -4)

    # Protegido: acorde suave
    x = pad([midi(64), midi(68), midi(71)], 0.9, SR, 3000, 0.08, 0.5)
    place(x, bell(midi(83), 0.8, SR) * 0.4, 0)
    save("protected", reverb(x, SR, 1.2, 0.3), SR, -4)

    # Interfaz
    tt = t_axis(0.05, SR)
    save("ui_click", np.sin(2 * np.pi * 1200 * tt) * np.exp(-tt / 0.01) + bp(noise(len(tt)), SR, 2000, 8000) * np.exp(-tt / 0.004) * 0.5, SR, -6)
    tt = t_axis(0.04, SR)
    save("ui_hover", np.sin(2 * np.pi * 2400 * tt) * np.exp(-tt / 0.008), SR, -14)

    # Latido (en bucle, 1 segundo)
    n = SR
    x = np.zeros(n)
    for start, amp in ((0.0, 1.0), (0.22, 0.7)):
        tt = t_axis(0.18, SR)
        place(x, np.sin(2 * np.pi * np.cumsum(45 + 25 * np.exp(-tt / 0.02)) / SR) * np.exp(-tt / 0.06) * amp, start * SR)
    save("heartbeat", lp(x, SR, 200), SR, -2)


if __name__ == "__main__":
    which = sys.argv[2] if len(sys.argv) > 2 else "all"
    if which in ("all", "sfx"): sfx()
    if which in ("all", "quake"): quake_and_alarm()
    if which in ("all", "amb"): ambience()
    if which in ("all", "music"): music()
    print("listo:", sorted(os.listdir(OUT)))
