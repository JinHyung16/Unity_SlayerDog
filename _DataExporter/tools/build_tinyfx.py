# -*- coding: utf-8 -*-
"""
Tiny Swords 팩의 파티클을 이펙트로 반입한다.

[사고] 이 팩을 「타일용」으로만 알고 지형만 가져다 썼다. 실제로는 유닛·UI·파티클까지 410장이
들어 있었고, 그동안 나는 같은 그림을 직접 그리고 있었다.
가진 것을 먼저 세어 보지 않으면 없는 것을 만든다.

- 유닛은 사람 병사라 개 게임에 안 맞는다. 파티클과 UI 만 쓴다.
- 프레임 크기가 시트마다 다르다(64 / 192). 세로 높이로 판별한다.
- 색이 들어 있으므로 이 이펙트들은 **런타임 틴트를 끄고** 원본 색을 쓴다.
"""
import os
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\wlsgu\Downloads\Tiny Swords (Free Pack)\Tiny Swords (Free Pack)\Particle FX"
DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\FX"

# (출력명, 원본 파일, 용도)
SETS = [
    ("fx_ts_dust",      "Dust_01.png",      "흙먼지 — 뒹굴기/질주"),
    ("fx_ts_dust2",     "Dust_02.png",      "흙먼지(넓음)"),
    ("fx_ts_explosion", "Explosion_01.png", "폭발 — 사료 폭탄"),
    ("fx_ts_burst",     "Explosion_02.png", "큰 폭발"),
    ("fx_ts_fire",      "Fire_02.png",      "화염 — 화상 장판"),
    ("fx_ts_splash",    "Water Splash.png", "물보라 — 착지/충격"),
]


def main():
    total = 0
    for name, src_name, desc in SETS:
        src = os.path.join(SRC, src_name)
        if not os.path.exists(src):
            print("없음:", src_name)
            continue

        im = Image.open(src).convert("RGBA")
        cell = im.height          # 시트가 한 줄이라 높이가 곧 프레임 한 변이다
        frames = im.width // cell

        outdir = os.path.join(DST, name)
        os.makedirs(outdir, exist_ok=True)
        for old in os.listdir(outdir):
            if old.endswith(".png") or old.endswith(".meta"):
                os.remove(os.path.join(outdir, old))

        kept = 0
        for i in range(frames):
            fr = im.crop((i * cell, 0, (i + 1) * cell, cell))
            if fr.getbbox() is None:      # 빈 프레임은 버린다
                continue
            fr.save(os.path.join(outdir, "%02d.png" % kept))
            kept += 1

        print("%-16s %2d프레임 %3dpx  %s" % (name, kept, cell, desc))
        total += kept

    print("총 %d프레임 / %d종" % (total, len(SETS)))


main()
