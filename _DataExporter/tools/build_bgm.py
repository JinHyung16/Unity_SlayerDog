# -*- coding: utf-8 -*-
# 배경음악 생성기 - SlayerDog 칩튠 루프 (로비 1 + 챕터 10)
#
# 진짜 음원이 생기기 전까지의 자리 음악이다. 같은 파일명으로 갈아 끼우면 된다 (MusicManager 는 파일명만 안다)
# 규칙
# - 22050Hz 모노 16bit WAV, 16마디 4/4 루프. 끝이 처음으로 이어지게 음은 마디 안에서 끝난다
# - 맵마다 음계 · 템포 · 음색이 다르다: 동양 맵은 5음계(요나누키 · 미야코부시), 서양은 도리안 · 에올리안 · 믹솔리디안
# - 리드(사각파 · 25% 듀티) · 베이스(삼각파) · 아르페지오(사각파 12.5%) · 드럼(킥 사인 하강 · 스네어 · 하이햇 노이즈)
# - 시드 고정 — 다시 돌려도 같은 곡
#
# 사용: python build_bgm.py [--preview]
#   출력 Assets/Resources/Bgm/bgm_<키>.wav (미리보기는 _DataExporter/preview/bgm)

import math, os, random, struct, sys, wave
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\Resources\Bgm")
PREVIEW = os.path.join(ROOT, r"_DataExporter\preview\bgm")
RATE = 22050

SCALES = {
    "yona": [0, 2, 4, 7, 9],          # 요나누키 장 5음계 — 밝은 동양
    "miyako": [0, 1, 5, 7, 8],        # 미야코부시 — 어두운 동양
    "korean": [0, 2, 5, 7, 9],        # 평조풍
    "dorian": [0, 2, 3, 5, 7, 9, 10],
    "aeolian": [0, 2, 3, 5, 7, 8, 10],
    "mixo": [0, 2, 4, 5, 7, 9, 10],
    "ionian": [0, 2, 4, 5, 7, 9, 11],
    "phrygian": [0, 1, 3, 5, 7, 8, 10],
}

# 키: (bpm, 으뜸음 MIDI, 음계, 드럼 세기 0~1, 아르페지오 여부, 시드)
TRACKS = {
    "lobby": (100, 57, "yona", 0.4, True, 1),
    "snow_sakura": (118, 57, "yona", 0.7, True, 11),
    "palace_range": (124, 55, "korean", 0.8, False, 12),
    "moon_roof": (132, 52, "miyako", 0.9, True, 13),
    "bamboo_temple": (110, 55, "korean", 0.6, False, 14),
    "mist_realm": (104, 60, "yona", 0.5, True, 15),
    "castle_square": (126, 55, "dorian", 0.9, False, 16),
    "mage_library": (116, 57, "aeolian", 0.6, True, 17),
    "glacier_fjord": (120, 52, "dorian", 0.8, True, 18),
    "giant_forest": (112, 55, "mixo", 0.7, False, 19),
    "skull_deck": (138, 50, "phrygian", 1.0, True, 20),
}


def freq(midi):
    return 440.0 * 2 ** ((midi - 69) / 12.0)


def square(ph, duty):
    return 1.0 if (ph % 1.0) < duty else -1.0


def triangle(ph):
    x = ph % 1.0
    return 4 * x - 1 if x < 0.5 else 3 - 4 * x


def env(i, n, a=0.01, r=0.12):
    t = i / RATE
    dur = n / RATE
    if t < a:
        return t / a
    if t > dur - r:
        return max(0.0, (dur - t) / r)
    return 1.0


def add_note(buf, start, length, midi, vol, kind, duty=0.25):
    f = freq(midi)
    n = max(1, int(length))
    for i in range(n):
        j = start + i
        if j >= len(buf):
            break
        ph = f * i / RATE
        if kind == "tri":
            s = triangle(ph)
        else:
            vib = 1 + 0.004 * math.sin(2 * math.pi * 5.5 * i / RATE) if i > RATE * 0.12 else 1
            s = square(f * vib * i / RATE, duty)
        buf[j] += s * vol * env(i, n)


def add_kick(buf, start, vol):
    n = int(RATE * 0.16)
    for i in range(n):
        j = start + i
        if j >= len(buf):
            break
        t = i / RATE
        f = 120 * math.exp(-t * 22) + 45
        buf[j] += math.sin(2 * math.pi * f * t) * vol * math.exp(-t * 18)


def add_noise(buf, start, vol, dur, rnd):
    n = int(RATE * dur)
    for i in range(n):
        j = start + i
        if j >= len(buf):
            break
        buf[j] += (rnd.random() * 2 - 1) * vol * math.exp(-i / RATE * (1 / dur) * 4)


def compose(key, bpm, root, scale_name, drums, arp, seed):
    rnd = random.Random(seed)
    scale = SCALES[scale_name]
    beat = 60.0 / bpm
    step = beat / 4                      # 16분
    bars = 16
    total = int(RATE * beat * 4 * bars)
    buf = [0.0] * total

    def deg(d, octave=0):
        n = len(scale)
        return root + scale[d % n] + 12 * (d // n + octave)

    # 화성: 4마디 단위 근음 진행 (음계 도수)
    prog = rnd.choice([[0, 5, 3, 4], [0, 3, 4, 0], [0, 4, 5, 3], [0, 2, 3, 4]])
    if len(scale) == 5:
        prog = [d % 5 for d in prog]

    # 모티프 2개 (A · B), 마디당 16칸 리듬
    def motif():
        rhythm = []
        pos = 0
        while pos < 16:
            ln = rnd.choice([2, 2, 4, 4, 1, 3]) if pos < 14 else 16 - pos
            ln = min(ln, 16 - pos)
            rhythm.append((pos, ln))
            pos += ln
        notes = []
        d = rnd.randint(2, 5)
        for p, ln in rhythm:
            d += rnd.choice([-2, -1, -1, 0, 1, 1, 2])
            d = max(0, min(len(scale) * 2, d))
            rest = rnd.random() < 0.15
            notes.append((p, ln, None if rest else d))
        return notes

    A, B = motif(), motif()
    form = [A, A, B, A]

    for bar in range(bars):
        t0 = int(RATE * bar * beat * 4)
        chord = prog[(bar // 4) % len(prog)] if bars >= 4 else 0
        chord = prog[bar % 4]
        section = form[(bar // 4) % 4]
        # 리드 (4마디 중 마지막 마디는 쉼표로 숨 쉬기)
        if bar % 4 != 3 or rnd.random() < 0.5:
            for p, ln, d in section:
                if d is None:
                    continue
                add_note(buf, t0 + int(RATE * p * step), RATE * ln * step * 0.92, deg(d + chord % 2, 1), 0.16, "sq", 0.25)
        # 베이스 8분
        for k in range(8):
            d = chord if k % 4 != 3 else chord + 2
            add_note(buf, t0 + int(RATE * k * step * 2), RATE * step * 1.7, deg(d, -1), 0.22, "tri")
        # 아르페지오
        if arp:
            for k in range(16):
                d = chord + [0, 2, 4, 2][k % 4]
                add_note(buf, t0 + int(RATE * k * step), RATE * step * 0.8, deg(d, 0), 0.06, "sq", 0.125)
        # 드럼
        if drums > 0:
            for k in range(16):
                at = t0 + int(RATE * k * step)
                if k in (0, 8) or (drums > 0.8 and k == 10):
                    add_kick(buf, at, 0.55 * drums)
                if k in (4, 12):
                    add_noise(buf, at, 0.22 * drums, 0.12, rnd)
                if k % 2 == 0:
                    add_noise(buf, at, 0.06 * drums, 0.03, rnd)

    peak = max(1e-6, max(abs(v) for v in buf))
    g = 0.85 / peak
    return [v * g for v in buf]


def write_wav(path, samples):
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples))


def main():
    out = PREVIEW if "--preview" in sys.argv else DST
    os.makedirs(out, exist_ok=True)
    for key, (bpm, root, scale, drums, arp, seed) in TRACKS.items():
        s = compose(key, bpm, root, scale, drums, arp, seed)
        write_wav(os.path.join(out, "bgm_%s.wav" % key), s)
        print("bgm_%s  %d bpm · %s · %.0f초" % (key, bpm, scale, len(s) / RATE))


main()
