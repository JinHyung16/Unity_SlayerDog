# -*- coding: utf-8 -*-
"""이펙트 팩 → Assets/_Art/FX 반입. 흰색 변형(row 5)만 뽑고 런타임에 틴트한다."""
from PIL import Image
import os, sys
sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\wlsgu\Downloads\Effect and FX Pixel All Free\Free"
DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\FX"
CELL = 64
WHITE_ROW = 5   # 0 red / 1 magenta / 2 cyan / 3 green / 4 tan / 5 white / 6 brown / 7 red-orange / 8 navy

# (출력명, Part, 파일, 용도)
FX = [
    ("fx_hit",        9, "427", "범용 타격 — 링 달린 둥근 폭발"),
    ("fx_hit_small", 7, "313", "약한 타격 — 작은 퍼프"),
    ("fx_explosion",  7, "315", "폭발 — 방사형 스파이크"),
    ("fx_burst_big",  4, "176", "대형 폭발 — 조밀한 방사"),
    ("fx_slash",     10, "466", "베기 — 넓은 호"),
    ("fx_slash_cross",6, "274", "교차 베기 — X"),
    ("fx_arc_wave",   5, "221", "충격파 — 부채꼴 호"),
    ("fx_ring",      12, "568", "지면 링 — 광역 표시"),
    ("fx_pillar",    10, "464", "기둥/빔"),
    ("fx_bolt",      14, "653", "번개 — 꺾인 섬광"),
    ("fx_vortex",    12, "566", "소용돌이 — 유도/추적"),
    ("fx_drop",      15, "701", "낙하 — 수직 선"),
    ("fx_ground",    15, "702", "지면 화염 — 지속 장판"),
    ("fx_buff",       4, "174", "버프 — 방사형 꽃"),
    ("fx_heal",      13, "614", "회복 — 방사 링"),
    ("fx_trail",      5, "222", "투사체 궤적 — 사선"),

    # 2차 반입. 내가 절차로 그린 것보다 팩 쪽이 확실히 나은 것들만 골랐다.
    # 개 정체성이 필요한 것(이빨·뼈다귀·슬리퍼 등)은 팩에 없어 직접 그린 것을 그대로 쓴다.
    ("fx_claw3",     11, "509", "발톱 자국 3줄 — 진짜 할퀸 자국"),
    ("fx_ring_spike",12, "586", "가시 달린 원 — 지면 광역 표시"),
    ("fx_crescent",   6, "284", "초승달 베기 — 꼬리 후리기"),
    ("fx_swirl",      9, "436", "소용돌이 별 — 각성"),
    ("fx_ring_dense", 4, "186", "촘촘한 고리 — 보호막"),
    ("fx_sparkle",   10, "474", "반짝임 — 획득/강화"),
]

def main():
    total = 0
    for name, part, num, desc in FX:
        src = os.path.join(SRC, "Part %d" % part, num + ".png")
        if not os.path.exists(src):
            print("MISSING", src); continue
        im = Image.open(src).convert("RGBA")
        frames = im.width // CELL
        y = WHITE_ROW * CELL
        outdir = os.path.join(DST, name)
        os.makedirs(outdir, exist_ok=True)
        kept = 0
        for i in range(frames):
            fr = im.crop((i * CELL, y, (i + 1) * CELL, y + CELL))
            if fr.getbbox() is None:      # 완전 투명 프레임은 버린다
                continue
            fr.save(os.path.join(outdir, "%02d.png" % kept))
            kept += 1
        print("%-16s %2d프레임  %s" % (name, kept, desc))
        total += kept
    print("총", total, "프레임")

main()
