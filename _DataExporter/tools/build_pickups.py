# -*- coding: utf-8 -*-
# 먹는 아이템 생성기 v2 - SlayerDog 도트 (pixkit, 주인공과 같은 그림 규칙)
#
# - pickup_box     보물상자 (PartDropper 키 · 프리팹 그대로). 필드에 떨어져 무기 · 강화를 준다
# - pickup_kibble  경험치 개뼈다귀 (이름은 프리팹 호환으로 그대로)
# - 아이템마다 <이름>_glow.png: 실루엣을 1도트씩 키운 흰 도트 링(알파 단계). 가산 머티리얼로 몸 뒤에 깔려
#   모양 그대로 테두리가 빛난다. 색은 런타임이 등급으로 틴트한다 (PartPickup)
# - pickup_meat    회복 고기 (가끔 떨어진다, 체력 회복)
# - pickup_magnet  자석 (드물게 떨어진다, 쉬는 경험치를 모두 끌어온다)
# - hero_ring        주인공 발밑 링 (흰 도트 고리, 바닥에 눕혀 틴트)
# - 크기는 예전 그림과 같은 칸을 쓴다 (상자 64 중 가로 42 · 경험치 가로 14 — 적보다 작게) — 프리팹 배율을 안 바꾼다
#
# 사용: python build_pickups.py

from PIL import Image, ImageFilter
import os, sys
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\Sprites")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview")
UP = 4


def chest(f):
    wood = (150, 92, 50)
    band = (236, 190, 70)
    f.p(wood).r(17, 30, 47, 54)
    f.p(wood).e(17, 18, 47, 38)
    f.p(wood).r(17, 28, 47, 33)
    f.p(band, light=False).r(17, 31, 47, 33)
    f.p(band, light=False).r(21, 20, 24, 54)
    f.p(band, light=False).r(40, 20, 43, 54)
    f.p(band).r(29, 29, 35, 37)
    f.flat((60, 40, 30)).r(31, 33, 33, 35)
    f.flat((255, 250, 210)).d1((25, 22), (26, 21), (44, 40))


def bone(f):
    # 적보다 작게 (가로 14) — 수십 개 깔려도 위험보다 눈에 덜 띄게
    col = (246, 236, 214)
    f.p(col).r(28, 31, 36, 33)
    for x, y in ((26, 30), (26, 34), (38, 30), (38, 34)):
        f.p(col).e(x - 2, y - 2, x + 2, y + 2)


def glow_of(img, grow, strength=1.0):
    # 도트 링: 64 격자로 내려 1도트씩 키운 링을 알파 단계로 쌓고 최근접으로 다시 키운다 (가우시안 번짐 안 씀)
    a0 = img.split()[3].resize((W, H), Image.NEAREST).point(lambda v: 255 if v > 40 else 0)
    steps = [0.7, 0.4, 0.2]
    masks = [a0]
    for i in range(grow):
        masks.append(masks[-1].filter(ImageFilter.MaxFilter(3)))
    out = Image.new("L", (W, H), 0)
    for i in range(grow, 0, -1):
        out.paste(int(255 * steps[min(i - 1, 2)] * strength), (0, 0, W, H), masks[i])
    g = Image.new("RGBA", img.size, (255, 255, 255, 0))
    g.putalpha(out.resize(img.size, Image.NEAREST))
    return g


def meat(f):
    # 회복 고기 — 뼈 달린 고깃덩이 (먹으면 체력 회복)
    f.p((246, 236, 214)).r(38, 26, 41, 34)
    f.p((246, 236, 214)).e(39, 22, 45, 28)
    f.p((246, 236, 214)).e(39, 30, 45, 36)
    f.p((196, 92, 70)).e(18, 26, 40, 44)
    f.flat((236, 150, 110)).r(22, 30, 26, 32)


def magnet(f):
    # 자석 — 빨간 말굽 + 은빛 끝 (먹으면 화면의 경험치를 모두 끌어온다). 가운데는 비운 U 자
    red = (214, 60, 60)
    f.p(red).r(19, 22, 26, 40)
    f.p(red).r(38, 22, 45, 40)
    f.p(red).r(23, 38, 41, 46)
    f.p(red).e(19, 34, 29, 46)
    f.p(red).e(35, 34, 45, 46)
    f.p((220, 226, 236)).r(19, 17, 26, 21)
    f.p((220, 226, 236)).r(38, 17, 45, 21)


def make(fn):
    f = Fig()
    fn(f)
    return big(render(f, shadow_w=0), UP)


def hero_ring():
    """주인공 발밑 링 — 흰 도트 타원 고리(바깥 진하게 · 안쪽 옅게). 바닥에 눕혀 쓰고 색은 런타임 틴트 (Art2DBuilder FootRing)."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()
    cx, cy, r = 31.5, 31.5, 20.0
    for y in range(H):
        for x in range(W):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if r - 1.5 <= d < r + 1.5:
                px[x, y] = (255, 255, 255, 255)
            elif r - 3.5 <= d < r - 1.5:
                px[x, y] = (255, 255, 255, 90)
    return big(im, UP)


def main():
    # 뼈는 어두운 바깥선 — 밝은 바닥(라벤더 · 모래)에서도 보이게, 수가 많아도 위험(적)보다는 작고 어둡게
    items = {"pickup_meat": (make(meat), 2, 0.8), "pickup_magnet": (make(magnet), 2, 0.9),
             "pickup_box": (make(chest), 3, 1.0), "pickup_kibble": (rim(make(bone).resize((W, H), Image.NEAREST), (70, 58, 50, 255)).resize((W * UP, H * UP), Image.NEAREST), 1, 0.55)}
    T = W * UP
    sheet = Image.new("RGBA", (T * len(items), T), (50, 50, 70, 255))
    for i, (name, (img, grow, strength)) in enumerate(items.items()):
        g = glow_of(img, grow, strength)
        img.save(os.path.join(DST, name + ".png"))
        g.save(os.path.join(DST, name + "_glow.png"))
        tint = Image.new("RGBA", g.size, (255, 230, 90, 255) if i == 0 else (255, 240, 120, 255))
        tint.putalpha(g.split()[3])
        sheet.alpha_composite(tint, (i * T, 0))
        sheet.alpha_composite(img, (i * T, 0))
    hero_ring().save(os.path.join(DST, "hero_ring.png"))
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    sheet.save(os.path.join(PREVIEW_DIR, "pickups.png"))
    print("먹는 아이템 %d종 + 테두리 빛" % len(items))


main()
