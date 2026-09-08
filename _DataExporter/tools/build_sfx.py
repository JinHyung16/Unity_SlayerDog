# -*- coding: utf-8 -*-
"""
효과음 생성기 — 「개많이잡아」

사운드 에셋이 하나도 없어 타격감의 절반이 비어 있었다. 아트를 절차 생성한 것과 같은 방식으로
WAV 를 직접 만든다. 외부 의존 없이 표준 라이브러리(wave, struct, math)만 쓴다.

이건 **자리를 채우는 소리**다. 진짜 음원이 생기면 같은 파일명으로 갈아 끼우면 된다 —
그래서 파일명이 곧 용도이고, 코드는 파일명만 안다.

규칙
- 22050Hz 모노 16bit. 픽셀 아트와 같은 결이고 파일이 가볍다
- Resources/Sfx 로 낸다 — 8개를 어드레서블에 따로 등록하는 것보다 짧다
- 전부 0.5초 이내. 초당 수십 번 겹치는 게임이라 길면 소리가 뭉갠다
- 시작/끝에 짧은 페이드를 넣는다. 안 넣으면 딸깍(클릭) 노이즈가 난다
"""
import math
import os
import struct
import sys
import wave

sys.stdout.reconfigure(encoding="utf-8")

DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\Resources\Sfx"
RATE = 22050


def _fade(samples, fade_in=0.005, fade_out=0.02):
    """양 끝을 재운다. 파형이 0 이 아닌 곳에서 끊기면 딸깍 소리가 난다."""
    n = len(samples)
    fi = max(1, int(RATE * fade_in))
    fo = max(1, int(RATE * fade_out))
    for i in range(min(fi, n)):
        samples[i] *= i / fi
    for i in range(min(fo, n)):
        samples[n - 1 - i] *= i / fo
    return samples


def _write(name, samples):
    os.makedirs(DST, exist_ok=True)
    path = os.path.join(DST, name + ".wav")
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        frames = bytearray()
        for s in samples:
            v = int(max(-1.0, min(1.0, s)) * 32000)
            frames += struct.pack("<h", v)
        w.writeframes(bytes(frames))
    return path


def _noise(seed):
    """결정적 잡음. random 을 쓰면 돌릴 때마다 파일이 달라져 diff 가 지저분해진다."""
    x = seed
    while True:
        x = (x * 1103515245 + 12345) & 0x7FFFFFFF
        yield (x / 0x3FFFFFFF) - 1.0


def tone(freq_from, freq_to, dur, kind="sine", vol=0.7, seed=1):
    n = int(RATE * dur)
    out = []
    noise = _noise(seed)
    phase = 0.0
    for i in range(n):
        t = i / n
        f = freq_from + (freq_to - freq_from) * t
        phase += 2 * math.pi * f / RATE
        if kind == "sine":
            v = math.sin(phase)
        elif kind == "square":
            v = 1.0 if math.sin(phase) >= 0 else -1.0
        elif kind == "saw":
            v = 2.0 * ((phase / (2 * math.pi)) % 1.0) - 1.0
        else:  # noise
            v = next(noise)
        # 뒤로 갈수록 줄인다. 일정 음량이면 「띠—」 하고 붙어 타격으로 안 들린다
        out.append(v * vol * (1.0 - t) ** 1.5)
    return _fade(out)


def mix(*layers):
    n = max(len(l) for l in layers)
    out = [0.0] * n
    for l in layers:
        for i, v in enumerate(l):
            out[i] += v
    peak = max(1.0, max(abs(v) for v in out))
    return [v / peak for v in out]


def seq(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


SFX = {
    # 타격 — 짧고 탁한 소리. 초당 여러 번 나므로 가장 짧아야 한다
    "sfx_hit": lambda: mix(tone(420, 160, 0.09, "square", 0.5),
                           tone(0, 0, 0.06, "noise", 0.35, seed=7)),
    # 처치 — 아래로 떨어지는 톤. 타격과 구분돼야 「죽었다」가 들린다
    "sfx_kill": lambda: mix(tone(660, 220, 0.16, "saw", 0.5),
                            tone(0, 0, 0.08, "noise", 0.2, seed=11)),
    # 획득(경험치·파츠) — 위로 오르는 짧은 두 음
    "sfx_pickup": lambda: seq(tone(880, 880, 0.05, "sine", 0.5),
                              tone(1320, 1320, 0.07, "sine", 0.5)),
    # 레벨업 — 위로 오르는 세 음. 획득보다 길고 분명하게
    "sfx_levelup": lambda: seq(tone(660, 660, 0.07, "square", 0.4),
                               tone(880, 880, 0.07, "square", 0.4),
                               tone(1320, 1320, 0.14, "square", 0.45)),
    # 피격(플레이어) — 낮고 둔탁. 적 타격과 반대 방향으로 내려간다
    "sfx_hurt": lambda: mix(tone(260, 90, 0.18, "saw", 0.6),
                            tone(0, 0, 0.1, "noise", 0.3, seed=23)),
    # 클리어 — 팡파레
    "sfx_clear": lambda: seq(tone(523, 523, 0.10, "square", 0.4),
                             tone(659, 659, 0.10, "square", 0.4),
                             tone(784, 784, 0.10, "square", 0.4),
                             tone(1046, 1046, 0.30, "square", 0.5)),
    # 사망 — 길게 떨어진다
    "sfx_dead": lambda: mix(tone(400, 60, 0.55, "saw", 0.6),
                            tone(0, 0, 0.3, "noise", 0.18, seed=31)),
    # UI 클릭
    "sfx_click": lambda: tone(1200, 900, 0.05, "square", 0.35),
}


def main():
    for name, make in SFX.items():
        p = _write(name, make())
        size = os.path.getsize(p)
        print("%-14s %5.2fs  %6d bytes" % (name, os.path.getsize(p) / (RATE * 2.0), size))
    print("효과음 %d종" % len(SFX))


main()
