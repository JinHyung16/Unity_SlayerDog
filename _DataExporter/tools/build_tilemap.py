# -*- coding: utf-8 -*-
# Tiny Swords 타일셋 -> 타일맵용 개별 타일 스프라이트
#
# 반복 텍스처 쿼드 한 장이 아니라 **진짜 타일맵**을 깔기 위한 재료를 뽑는다.
# 같은 잔디라도 색 변형을 섞어야 반복 패턴이 눈에 안 띈다 (탕탕특공대 레퍼런스).
#
# 타일 = 64px, PPU 32 -> 월드 2유닛. 셀 크기와 반드시 같아야 이음매가 안 생긴다.

from PIL import Image
import os, sys
sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\wlsgu\Downloads\Tiny Swords (Free Pack)\Tiny Swords (Free Pack)\Terrain"
DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\Tiles"
CELL = 64

os.makedirs(DST, exist_ok=True)


def tileset(idx):
    return Image.open(os.path.join(SRC, "Tileset", "Tilemap_color%d.png" % idx)).convert("RGBA")


def save(im, name):
    im.save(os.path.join(DST, name + ".png"))


def main():
    n = 0

    # 잔디 내부 타일 — 초록 계열(1~3)만. 4(올리브)·5(청록)는 색이 너무 튀어
    # 나란히 깔면 바닥이 얼룩덜룩해진다. 변주는 색이 아니라 뒤집기로 준다.
    for out, src in enumerate((1, 2, 3), start=1):
        sheet = tileset(src)
        inner = sheet.crop((CELL, CELL, CELL * 2, CELL * 2))
        save(inner, "tile_grass_%d" % out)
        n += 1

    # 같은 타일을 뒤집어 변형을 늘린다. 픽셀아트라 뒤집어도 티가 안 난다.
    for src, out in ((1, 4), (2, 5), (3, 6)):
        b = Image.open(os.path.join(DST, "tile_grass_%d.png" % src)).convert("RGBA")
        save(b.transpose(Image.FLIP_LEFT_RIGHT), "tile_grass_%d" % out)
        n += 1

    # 장식 — 바위 4종(64px, 셀 하나에 딱 맞음)
    for i in range(1, 5):
        p = os.path.join(SRC, "Decorations", "Rocks", "Rock%d.png" % i)
        save(Image.open(p).convert("RGBA"), "deco_rock%d" % i)
        n += 1

    # 장식 — 덤불 (1024x128 = 8프레임 x 128px, 첫 프레임만)
    b = Image.open(os.path.join(SRC, "Decorations", "Bushes", "Bushe1.png")).convert("RGBA")
    save(b.crop((0, 0, 128, 128)), "deco_bush")
    n += 1

    # 장식 — 나무 (1536x256 = 8프레임 x 192px).
    #
    # [측정] 원본 그대로 쓰니 실루엣이 190px 이었다. 같은 PPU(32)에서 개가 61px 이므로
    # 나무가 캐릭터의 **3.1배**다. 레퍼런스 도트 시트는 캐릭터 · 이펙트 · 소품이 한 눈금인데
    # 나무만 튀어 화면이 어수선해졌다.
    # **정확히 절반**으로 줄인다 — 정수 축소라 픽셀 격자가 깨지지 않는다(NEAREST).
    # 결과 실루엣 95px ≒ 개의 1.5배. 나무로 읽히면서 화면을 지배하지 않는 크기다.
    t = Image.open(os.path.join(SRC, "Resources", "Wood", "Trees", "Tree1.png")).convert("RGBA")
    tree = t.crop((0, 0, 192, 256)).resize((96, 128), Image.NEAREST)
    save(tree, "deco_tree")
    n += 1

    print("타일/장식 %d장" % n)


main()
