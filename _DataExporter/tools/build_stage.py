# -*- coding: utf-8 -*-
# 무대 바닥 · 소품 생성기 - 「개많이잡아」 아트 리부트 (_Ref/ArtReboot/기획서.md 방향 표 배경 행)
#
# 핵심 규칙
# - 바닥은 네온 밤 무대: 진보라 패널 + 가장자리 줄눈 + 하프톤 점 / 스파클 낙서. 채도 ≤ 45% · 명도 30~45%
#   (레퍼 배경은 명도 85% 지만 동시 적 131 에서는 묻힌다 — 위계가 먼저다)
# - 타일 한 장 = 셀 하나 = 2 유닛. 64 좌표 × OUT 4 = 256px, PPU 128. 줄눈은 가장자리에 반씩 둬야 이어 붙여도 두께가 같다
# - 위험 색(레드오렌지) · 보상 색(레몬 · 시안)은 바닥에 쓰지 않는다. 바닥 장식은 마젠타 · 보라만
# - 소품은 빌보드로 서 있는다(TilemapStreamer). 캐릭터와 같은 외곽선 규칙 · 명암 2단. 색은 Deco_Tone 머티리얼이 한 번 더 누른다

from PIL import Image, ImageDraw, ImageFilter
import os, sys, json, random
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\Stage")
PREVIEW = os.path.join(ROOT, r"_Ref\ArtReboot\lineup_stage.png")
SS = 4
OUT = 4
S = SS * OUT
W = H = 64

FLOOR = (80, 62, 100, 255)         # 원색 채도 38% · 명도 39%. 캡처 실측은 ref_art.py 로 (10-03: 채도 45%→61%, 31%→32%, 명도 33%→29%)
FLOOR_ALT = (74, 57, 94, 255)
SEAM = (54, 42, 72, 255)
SEAM_HI = (100, 82, 124, 255)
DOT = (98, 74, 120, 255)
SCRIBBLE = (130, 80, 142, 255)
INK = (46, 20, 70, 255)
INK_PX = 1.6


def canvas(w=W, h=H):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def down(im, w=W, h=H):
    return im.resize((w * OUT, h * OUT), Image.BOX)


def star(d, cx, cy, r, c, k=0.38):
    pts = []
    for i in range(8):
        import math
        a = math.pi / 4 * i - math.pi / 2
        rr = r if i % 2 == 0 else r * k
        pts.append(((cx + math.cos(a) * rr) * S, (cy + math.sin(a) * rr) * S))
    d.polygon(pts, fill=c)


def tile(variant):
    rnd = random.Random(variant * 31 + 7)
    im, d = canvas()
    d.rectangle([0, 0, W * S, H * S], fill=FLOOR if variant % 2 else FLOOR_ALT)
    # 줄눈: 가장자리에 반씩. 위 · 왼쪽은 밝은 선, 아래 · 오른쪽은 어두운 선 — 패널이 살짝 솟아 보인다
    e = 0.8 * S
    d.rectangle([0, 0, W * S, e], fill=SEAM_HI)
    d.rectangle([0, 0, e, H * S], fill=SEAM_HI)
    d.rectangle([0, H * S - e, W * S, H * S], fill=SEAM)
    d.rectangle([W * S - e, 0, W * S, H * S], fill=SEAM)
    if variant == 2:
        # 하프톤 점: 한쪽 모서리에서 옅어진다
        for gy in range(4, 60, 5):
            for gx in range(4, 60, 5):
                t = 1 - (gx + gy) / 120.0
                r = 1.3 * t
                if r > 0.35:
                    d.ellipse([(gx - r) * S, (gy - r) * S, (gx + r) * S, (gy + r) * S], fill=DOT)
    elif variant == 3:
        star(d, 20 + rnd.uniform(-4, 4), 24, 7, SCRIBBLE)
        star(d, 40, 42, 3.5, SCRIBBLE)
    elif variant == 4:
        # 사선 줄무늬 (레퍼 1 의 쉐브론 기둥) — 아주 낮은 대비
        for k in range(-64, 64, 10):
            d.polygon([((k) * S, 64 * S), ((k + 4) * S, 64 * S), ((k + 4 + 64) * S, 0), ((k + 64) * S, 0)], fill=FLOOR)
        d.rectangle([0, 0, W * S, e], fill=SEAM_HI)
        d.rectangle([0, 0, e, H * S], fill=SEAM_HI)
        d.rectangle([0, H * S - e, W * S, H * S], fill=SEAM)
        d.rectangle([W * S - e, 0, W * S, H * S], fill=SEAM)
    return down(im).convert("RGB").convert("RGBA")


def fin(lay):
    small = down(lay)
    k = int(round(INK_PX * OUT)) * 2 + 1
    grown = small.split()[3].filter(ImageFilter.MaxFilter(k))
    out = Image.new("RGBA", small.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", small.size, INK), (0, 0), grown)
    out.alpha_composite(small)
    return out


def prop_lamp():
    """별 가로등. 마젠타 별 + 보라 기둥."""
    a, d = canvas()
    d.rounded_rectangle([30 * S, 22 * S, 34 * S, 60 * S], radius=1.5 * S, fill=(96, 70, 140, 255))
    d.ellipse([25 * S, 56 * S, 39 * S, 62 * S], fill=(96, 70, 140, 255))
    b, d2 = canvas()
    star(d2, 32, 16, 12, (255, 70, 180, 255), 0.45)
    c, d3 = canvas()
    star(d3, 31, 14, 5, (255, 200, 236, 255), 0.45)
    img = fin(a)
    img.alpha_composite(fin(b))
    img.alpha_composite(down(c))
    return img


def prop_speaker():
    """스피커 더미. 무대 소품."""
    a, d = canvas()
    d.rounded_rectangle([16 * S, 26 * S, 48 * S, 61 * S], radius=3 * S, fill=(70, 52, 104, 255))
    d.rectangle([40 * S, 28 * S, 47 * S, 60 * S], fill=(54, 38, 84, 255))
    b, d2 = canvas()
    for cy, r in ((36, 6.5), (52, 5)):
        d2.ellipse([(30 - r) * S, (cy - r) * S, (30 + r) * S, (cy + r) * S], fill=(150, 90, 200, 255))
        d2.ellipse([(30 - r * 0.45) * S, (cy - r * 0.45) * S, (30 + r * 0.45) * S, (cy + r * 0.45) * S], fill=(46, 20, 70, 255))
    img = fin(a)
    img.alpha_composite(down(b))
    return img


def prop_crystal():
    """보라 수정 무더기. 시안은 보상 색이라 쓰지 않는다."""
    a, d = canvas()
    col = (176, 120, 236, 255)
    sh = (124, 78, 190, 255)
    for x, h, w in ((24, 22, 6), (34, 32, 7), (43, 18, 5)):
        d.polygon([((x - w) * S, 60 * S), ((x - w) * S, (60 - h + 6) * S), (x * S, (60 - h) * S), ((x + w) * S, (60 - h + 6) * S), ((x + w) * S, 60 * S)], fill=col)
    b, d2 = canvas()
    for x, h, w in ((24, 22, 6), (34, 32, 7), (43, 18, 5)):
        d2.polygon([(x * S, (60 - h) * S), ((x + w) * S, (60 - h + 6) * S), ((x + w) * S, 60 * S), (x * S, 60 * S)], fill=sh)
    img = fin(a)
    img.alpha_composite(down(b))
    return img


def write_scale():
    path = os.path.join(ROOT, "_DataExporter", "_art_scale.json")
    table = {}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as fp:
            table = json.load(fp)
    table["Assets/_Art/Stage"] = OUT
    with open(path, "w", encoding="utf-8") as fp:
        json.dump(table, fp, ensure_ascii=False, indent=1, sort_keys=True)


def main():
    os.makedirs(DST, exist_ok=True)
    tiles = [tile(v) for v in (1, 2, 3, 4)]
    for i, t in enumerate(tiles, start=1):
        t.save(os.path.join(DST, "tile_stage_%d.png" % i))
    props = {"deco_lamp": prop_lamp(), "deco_speaker": prop_speaker(), "deco_crystal": prop_crystal()}
    for k, v in props.items():
        v.save(os.path.join(DST, k + ".png"))
    write_scale()

    # 미리보기: 4x3 바닥에 소품 · 주인공 한 장
    T = W * OUT
    prev = Image.new("RGBA", (T * 4, T * 3))
    rnd = random.Random(3)
    for y in range(3):
        for x in range(4):
            prev.alpha_composite(tiles[rnd.randrange(4)], (x * T, y * T))
    for i, p in enumerate(props.values()):
        prev.alpha_composite(p, (int(T * (0.4 + i * 1.2)), int(T * 0.3)))
    hero = os.path.join(ROOT, r"Assets\_Art\Sprites\dog_shiba_idle.png")
    if os.path.exists(hero):
        prev.alpha_composite(Image.open(hero).convert("RGBA"), (int(T * 1.5), int(T * 1.5)))
    prev.save(PREVIEW)
    print("바닥 4장 · 소품 %d장 · 미리보기 %s" % (len(props), PREVIEW))


main()
