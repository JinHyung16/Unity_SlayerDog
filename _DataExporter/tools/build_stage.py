# -*- coding: utf-8 -*-
# 맵 바닥 · 소품 생성기 v2 - SlayerDog 도트 (볼트 SlayerDog/SlayerDog 기획서.md 맵 10)
#
# 규칙
# - 바닥 타일 한 장 = 셀 하나 = 2 유닛. 64 도트를 ×4 확대 256px, PPU 128 (build_meta 배율 표 4)
# - 바닥은 이어 붙는다: 무늬는 64 주기로 맞추고 점 장식은 가장자리를 넘으면 반대편으로 감는다
# - 바닥 대비는 낮게, 외곽선 없음. 캐릭터 · 적이 외곽선으로 서고 바닥은 뒤로 물러난다
# - 위험 색(레드오렌지)은 바닥에 넓게 쓰지 않는다. 소품은 캐릭터와 같은 도트 엔진(pixkit) — 외곽선 · 3단 명암
# - 소품은 빌보드로 선다(TilemapStreamer). 바닥선 y 61
# - 맵마다 Assets/_Art/Stage/<key>/ 에 tile_stage_1~4 · deco_* 를 쓴다. Art2DBuilder 가 폴더마다 바닥 프리팹
#   obj_ground_<key> 를 굽고, Tables/map 의 groundKey 가 그 주소를 가리킨다 (key 는 gen_slayer.js MAPS 와 같다)
# - 하위 폴더도 ×4 배율이라 build_meta 배율 표(_art_scale.json)에 폴더마다 적는다
#
# 사용: python build_stage.py [--preview]

from PIL import Image, ImageDraw
import os, sys, json, random, math
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\Stage")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview", "maps")
UP = 4


# ---------------------------------------------------------------- 바닥

def wrap_px(d, x, y, c):
    d.point((x % W, y % H), fill=c)


def wrap_blob(d, x, y, pts, c):
    for dx, dy in pts:
        wrap_px(d, x + dx, y + dy, c)


def speckle(d, rnd, n, cols):
    for _ in range(n):
        x, y = rnd.randrange(W), rnd.randrange(H)
        c = cols[rnd.randrange(len(cols))]
        wrap_px(d, x, y, c)
        if rnd.random() < 0.5:
            wrap_px(d, x + 1, y, c)


PETAL = [(0, 0), (1, 0), (0, 1)]
PEBBLE = [(0, 0), (1, 0), (0, 1), (1, 1)]
TUFT = [(0, 0), (1, -1), (2, 0), (1, 0)]
LEAF = [(0, 0), (1, 0), (1, 1), (2, 1)]


def floor_pattern(d, t, variant, rnd):
    k = t["pattern"]
    a, b = t["floor"], t["alt"]
    det = t["detail"]
    if k == "roof":
        # 기와 줄: 16 도트마다 한 줄, 반 칸씩 엇갈린 둥근 끝
        for row in range(0, 64, 8):
            d.line([(0, row + 7), (63, row + 7)], fill=b)
            off = 0 if (row // 8) % 2 == 0 else 8
            for x in range(off, 64 + 16, 16):
                d.line([((x - 1) % 64, row), ((x - 1) % 64, row + 6)], fill=b)
    elif k == "cobble":
        for row in range(0, 64, 16):
            off = 0 if (row // 16) % 2 == 0 else 8
            d.line([(0, row), (63, row)], fill=b)
            for x in range(off, 64 + 16, 16):
                d.line([(x % 64, row), (x % 64, row + 15)], fill=b)
    elif k == "planks":
        for x in range(0, 64, 16):
            d.line([(x, 0), (x, 63)], fill=b)
            seam = (x * 7 + variant * 13) % 64
            d.line([(x, seam), (x + 15, seam)], fill=b)
            wrap_px(d, x + 3, seam + 4, det)
            wrap_px(d, x + 12, seam - 5, det)
    elif k == "carpet":
        for x in range(0, 64, 32):
            d.line([(x + 1, 0), (x + 1, 63)], fill=b)
            d.line([(0, x + 1), (63, x + 1)], fill=b)
        if variant in (2, 4):
            for i in range(-4, 5):
                wrap_px(d, 16 + i, 16 + abs(i) - 4, det)
                wrap_px(d, 16 + i, 16 - abs(i) + 4, det)
                wrap_px(d, 48 + i, 48 + abs(i) - 4, det)
                wrap_px(d, 48 + i, 48 - abs(i) + 4, det)
    elif k == "ice":
        if variant in (2, 3):
            x, y = rnd.randrange(64), rnd.randrange(64)
            for _ in range(22):
                wrap_px(d, x, y, b)
                x += rnd.choice((1, 1, 0))
                y += rnd.choice((1, 0, -1))
    elif k == "cloud":
        if variant in (2, 4):
            cx, cy = 20 + variant * 6, 30
            for i in range(14):
                ang = i * 0.45
                wrap_px(d, int(cx + math.cos(ang) * (3 + i * 0.5)), int(cy + math.sin(ang) * (3 + i * 0.5)), det)

    # 점 장식 — 변형마다 양이 다르다 (1 = 거의 없음)
    shape = {"petals": PETAL, "pebbles": PEBBLE, "tufts": TUFT, "leaves": LEAF}.get(t.get("dots"))
    if shape:
        n = {1: 1, 2: 4, 3: 7, 4: 2, 5: 3, 6: 0, 7: 5, 8: 2}[variant]
        for _ in range(n):
            wrap_blob(d, rnd.randrange(64), rnd.randrange(64), shape, det)


# 바닥 타일 변형 수 (Art2DBuilder.GrassTileNames 와 같게)
TILE_VARIANTS = 8


def tile(t, variant):
    rnd = random.Random(variant * 31 + sum(map(ord, t["key"])))
    im = Image.new("RGB", (W, H), t["floor"] if variant not in (3, 7) else t.get("floor3", t["floor"]))
    d = ImageDraw.Draw(im)
    # 결: 큰 얼룩 패치 몇 개 (가장자리를 넘으면 반대편으로 감아 이음매가 안 보인다)
    base = t["floor"] if variant not in (3, 7) else t.get("floor3", t["floor"])
    soft = lambda c: tuple((c[i] + base[i]) // 2 for i in range(3))
    # 얼룩 수를 변형마다 다르게 (2~4) — 8장이 섞여 반복 무늬가 덜 보이게
    for _ in range(2 + variant % 3):
        cx, cy = rnd.randrange(64), rnd.randrange(64)
        rx, ry = rnd.randrange(10, 20), rnd.randrange(6, 12)
        col = soft(t["patch"]) if rnd.random() < 0.5 else soft(t["alt"])
        for ox in (-64, 0, 64):
            for oy in (-64, 0, 64):
                d.ellipse([cx - rx + ox, cy - ry + oy, cx + rx + ox, cy + ry + oy], fill=col)
    speckle(d, rnd, 90, [t["alt"], t["speck"], t["patch"]])
    floor_pattern(d, t, variant, rnd)
    return im.convert("RGBA")


# ---------------------------------------------------------------- 소품 (바닥선 y 61)

def p_tree(f, trunk, leaf, blossom=None, shape="round"):
    f.p(trunk).r(30, 38, 34, 61)
    f.p(trunk).g((26, 61), (38, 61), (34, 56), (30, 56))
    if shape == "pine":
        for y0, w in ((14, 6), (22, 10), (30, 14), (38, 17)):
            f.p(leaf).g((32, y0 - 6), (32 + w, y0 + 8), (32 - w, y0 + 8))
        return
    if shape == "giant":
        f.p(trunk).r(26, 30, 38, 61)
        f.p(leaf).e(8, 4, 56, 36)
        f.p(leaf).e(4, 16, 30, 40)
        f.p(leaf).e(34, 16, 60, 40)
        return
    f.p(trunk).l((32, 42), (22, 30), w=3)
    f.p(trunk).l((33, 40), (43, 28), w=3)
    f.p(leaf).e(12, 10, 52, 40)
    f.p(leaf).e(6, 20, 28, 40)
    f.p(leaf).e(36, 18, 58, 40)
    if blossom:
        dots = f.flat(blossom)
        rnd = random.Random(5)
        for _ in range(26):
            x, y = rnd.randrange(10, 55), rnd.randrange(12, 38)
            dots.d1((x, y), (x + 1, y))


def p_stone_lantern(f, col):
    f.p(col).r(26, 57, 38, 61)
    f.p(col).r(30, 40, 34, 57)
    f.p(col).r(25, 33, 39, 41)
    f.flat((250, 210, 120)).r(29, 35, 35, 39)
    f.p(col).g((22, 33), (42, 33), (36, 26), (28, 26))
    f.p(col).e(30, 22, 34, 27)


def p_rock(f, col, cap=None):
    f.p(col).e(16, 42, 48, 62)
    f.p(col).e(24, 36, 42, 54)
    if cap:
        f.p(cap, shade=False).e(22, 35, 42, 44)


def p_target(f):
    f.p(WOOD).r(23, 46, 25, 61)
    f.p(WOOD).r(39, 46, 41, 61)
    f.p((236, 228, 210)).r(18, 18, 46, 48)
    f.p((40, 36, 44), line=False).e(24, 24, 40, 42)
    f.p(RED, line=False).e(28, 28, 36, 38)


def p_wall(f, wall, roof):
    f.p(wall).r(8, 38, 56, 61)
    f.p(roof).g((4, 38), (60, 38), (56, 30), (8, 30))
    tl = f.flat(ink(roof))
    for x in range(10, 56, 6):
        tl.l((x, 31), (x - 1, 37))


def p_paper_lantern(f, col):
    f.p(WOOD).r(31, 30, 33, 61)
    f.p(WOOD).r(24, 22, 40, 24)
    f.p(col).e(25, 24, 39, 42)
    f.p((40, 34, 40)).r(28, 23, 36, 25)
    f.p((40, 34, 40)).r(28, 41, 36, 43)
    f.flat((255, 220, 150)).r(30, 29, 33, 37)


def p_chimney(f):
    f.p((150, 80, 66)).r(24, 26, 40, 61)
    f.p((110, 60, 52)).r(22, 22, 42, 27)
    m = f.flat((120, 64, 54))
    for y in range(30, 60, 6):
        m.l((24, y), (40, y))
    f.p((190, 190, 200), shade=False).e(28, 8, 38, 18)
    f.p((210, 210, 220), shade=False).e(33, 2, 42, 11)


def p_bamboo(f, col):
    for x, top in ((22, 6), (30, 2), (38, 10), (44, 16)):
        f.p(col).r(x, top, x + 3, 61)
        nd = f.flat(ink(col))
        for y in range(top + 8, 61, 9):
            nd.l((x, y), (x + 3, y))
        f.p((110, 160, 70)).g((x + 3, top + 6), (x + 11, top + 3), (x + 4, top + 9))


def p_bell(f):
    f.p(WOOD).r(14, 14, 17, 61)
    f.p(WOOD).r(47, 14, 50, 61)
    f.p((120, 60, 40)).r(10, 10, 54, 15)
    f.p((90, 120, 110)).g((24, 20), (40, 20), (43, 46), (21, 46))
    f.p((90, 120, 110)).e(26, 16, 38, 24)
    f.flat((140, 170, 150)).r(30, 30, 34, 36)


def p_stupa(f, col):
    f.p(col).r(22, 54, 42, 61)
    for i, y in enumerate((44, 34, 24)):
        w = 12 - i * 2
        f.p(col).r(32 - w + 4, y + 4, 32 + w - 4, y + 10)
        f.p(col).g((32 - w, y + 4), (32 + w, y + 4), (32 + w - 3, y), (32 - w + 3, y))
    f.p(col).r(31, 12, 33, 24)


def p_talisman_post(f):
    f.p((120, 50, 40)).r(29, 10, 35, 61)
    f.p((120, 50, 40)).r(25, 8, 39, 11)
    for y in (16, 30, 44):
        f.p((250, 222, 90)).r(26, y, 38, y + 9)
        f.flat(RED).l((32, y + 1), (31, y + 4), (33, y + 7))


def p_peach_tree(f):
    p_tree(f, (120, 80, 60), (110, 170, 110), blossom=(255, 170, 160))


def p_float_rock(f):
    f.p((210, 230, 230), shade=False).e(12, 48, 52, 62)
    f.p((130, 140, 150)).g((18, 30), (46, 30), (40, 50), (26, 52))
    f.p((90, 160, 100)).e(18, 24, 46, 34)


def p_banner(f, col):
    f.p(IRON).r(30, 6, 32, 61)
    f.p(GOLD).e(29, 3, 33, 7)
    f.p(col).g((33, 9), (52, 9), (52, 34), (43, 29), (33, 34))
    f.flat(GOLD).r(41, 15, 44, 22)


def p_fountain(f):
    f.p((170, 170, 180)).e(6, 44, 58, 62)
    f.p((90, 150, 210), line=False).e(12, 46, 52, 56)
    f.p((170, 170, 180)).r(28, 26, 36, 50)
    f.p((170, 170, 180)).e(20, 22, 44, 30)
    f.p((150, 210, 240), shade=False).e(29, 10, 35, 24)


def p_barrel(f):
    f.p(WOOD).e(20, 30, 44, 61)
    f.p(WOOD).r(20, 34, 44, 57)
    f.p(IRON, line=False).r(20, 38, 44, 39)
    f.p(IRON, line=False).r(20, 52, 44, 53)


def p_crate(f):
    f.p((170, 120, 70)).r(16, 34, 48, 61)
    x = f.flat((120, 80, 44))
    x.l((16, 34), (48, 61))
    x.l((48, 34), (16, 61))
    x.r(16, 34, 48, 35)


def p_bookshelf(f):
    f.p((110, 70, 44)).r(12, 12, 52, 61)
    for y in (16, 31, 46):
        f.p((60, 36, 30), line=False).r(15, y, 49, y + 11)
        rnd = random.Random(y)
        x = 16
        while x < 48:
            w = rnd.choice((2, 3, 3, 4))
            c = rnd.choice(((180, 60, 60), (70, 90, 170), (60, 140, 90), (220, 180, 80), (150, 80, 170)))
            f.flat(c).r(x, y + rnd.randrange(0, 3), x + w - 1, y + 10)
            x += w + 1


def p_crystal(f, col):
    for x, h, w in ((24, 22, 6), (34, 32, 7), (43, 18, 5)):
        f.p(col).g((x - w, 61), (x - w, 61 - h + 6), (x, 61 - h), (x + w, 61 - h + 6), (x + w, 61))


def p_candle(f):
    f.p(GOLD).r(31, 22, 33, 58)
    f.p(GOLD).e(24, 55, 40, 61)
    f.p(GOLD).l((22, 30), (42, 30), w=2)
    for x in (22, 32, 42):
        f.p((240, 236, 220)).r(x - 1, 20 if x == 32 else 24, x + 1, 30)
        f.flat((255, 200, 80)).r(x - 1, 15 if x == 32 else 19, x + 1, 19 if x == 32 else 23)


def p_runestone(f):
    f.p((120, 126, 140)).g((22, 61), (42, 61), (44, 20), (34, 10), (24, 16))
    rn = f.flat((90, 200, 230))
    rn.l((30, 22), (30, 44))
    rn.l((30, 26), (36, 32))
    rn.l((30, 34), (36, 40))


def p_ice_spike(f):
    for x, h, w in ((22, 28, 5), (32, 44, 6), (42, 24, 5)):
        f.p((170, 220, 245)).g((x - w, 61), (x, 61 - h), (x + w, 61))


def p_mushroom(f):
    f.p((236, 226, 200)).r(28, 34, 36, 61)
    f.p((200, 60, 60)).e(12, 14, 52, 40)
    f.p((200, 60, 60)).r(12, 28, 52, 36)
    dots = f.flat((250, 240, 230))
    for x, y in ((20, 22), (32, 18), (42, 24), (26, 30), (38, 31)):
        dots.r(x, y, x + 2, y + 2)


def p_stump(f):
    f.p((120, 82, 50)).r(18, 42, 46, 61)
    f.p((170, 130, 80)).e(18, 37, 46, 47)
    f.flat((130, 90, 56)).e(26, 40, 38, 44)
    f.p((70, 130, 60)).e(40, 50, 50, 61)


def p_bush(f, col):
    f.p(col).e(10, 36, 34, 61)
    f.p(col).e(26, 30, 54, 61)
    f.flat((220, 90, 140)).r(20, 42, 21, 43)
    f.flat((220, 90, 140)).r(40, 38, 41, 39)


def p_cannon(f):
    f.p(WOOD).r(16, 48, 44, 56)
    f.p(IRON).e(14, 50, 26, 62)
    f.p(IRON).e(34, 50, 46, 62)
    f.p(IRON).g((20, 46), (48, 30), (52, 36), (26, 52))
    f.flat(BLACK).e(47, 30, 53, 36)


# ---------------------------------------------------------------- 맵 10 (기획서 맵 표 순서)

THEMES = [
    dict(key="snow_sakura", name="벚꽃 설산", floor=(112, 118, 150), alt=(102, 108, 142), patch=(128, 136, 168), speck=(140, 148, 180),
         pattern="none", dots="petals", detail=(226, 150, 184),
         props={"sakura": lambda f: p_tree(f, (110, 70, 60), (236, 160, 196), blossom=(255, 214, 230)),
                "stone_lantern": lambda f: p_stone_lantern(f, (150, 150, 160)),
                "snow_rock": lambda f: p_rock(f, (110, 116, 136), cap=(236, 240, 250))}),
    dict(key="palace_range", name="궁궐 활터", floor=(150, 122, 86), alt=(138, 112, 80), patch=(162, 134, 96), speck=(170, 142, 102),
         pattern="none", dots="pebbles", detail=(140, 114, 82),
         props={"target": p_target,
                "palace_wall": lambda f: p_wall(f, (196, 90, 70), (60, 64, 74)),
                "pine": lambda f: p_tree(f, (110, 70, 50), (70, 120, 80), shape="pine")}),
    dict(key="moon_roof", name="달밤 기와지붕", floor=(56, 62, 98), alt=(46, 52, 84), patch=(64, 70, 108), speck=(70, 80, 118),
         pattern="roof", dots=None, detail=(80, 90, 130),
         props={"red_lantern": lambda f: p_paper_lantern(f, (210, 60, 60)),
                "chimney": p_chimney,
                "blue_lantern": lambda f: p_paper_lantern(f, (90, 120, 200))}),
    dict(key="bamboo_temple", name="대나무 산사", floor=(74, 100, 66), alt=(66, 90, 60), patch=(84, 112, 72), speck=(92, 120, 78),
         pattern="none", dots="leaves", detail=(130, 156, 90),
         props={"bamboo": lambda f: p_bamboo(f, (110, 160, 80)),
                "temple_bell": p_bell,
                "stupa": lambda f: p_stupa(f, (150, 150, 140))}),
    dict(key="mist_realm", name="안개 선계", floor=(96, 140, 132), alt=(88, 130, 124), patch=(110, 154, 146), speck=(124, 168, 158),
         pattern="cloud", dots=None, detail=(170, 210, 200),
         props={"talisman_post": p_talisman_post, "peach_tree": p_peach_tree, "float_rock": p_float_rock}),
    dict(key="castle_square", name="성채 광장", floor=(108, 106, 116), alt=(94, 92, 102), patch=(118, 116, 126), speck=(124, 122, 132),
         pattern="cobble", dots=None, detail=(150, 148, 156),
         props={"banner": lambda f: p_banner(f, (60, 90, 180)), "fountain": p_fountain, "barrel": p_barrel}),
    dict(key="mage_library", name="마탑 서고", floor=(80, 58, 110), alt=(70, 50, 98), patch=(88, 66, 120), speck=(92, 70, 124),
         pattern="carpet", dots=None, detail=(160, 128, 66),
         props={"bookshelf": p_bookshelf, "crystal": lambda f: p_crystal(f, (150, 120, 230)), "candelabra": p_candle}),
    dict(key="glacier_fjord", name="빙하 피오르", floor=(96, 140, 170), alt=(88, 130, 160), patch=(112, 156, 186), speck=(124, 168, 196),
         pattern="ice", dots=None, detail=(170, 210, 230),
         props={"runestone": p_runestone, "ice_spike": p_ice_spike,
                "snow_pine": lambda f: p_tree(f, (90, 70, 60), (90, 130, 140), shape="pine")}),
    dict(key="giant_forest", name="거목 숲", floor=(52, 86, 48), alt=(46, 78, 44), patch=(60, 96, 54), speck=(66, 104, 58),
         pattern="none", dots="tufts", detail=(90, 136, 70),
         props={"mushroom": p_mushroom, "stump": p_stump, "vine_bush": lambda f: p_bush(f, (60, 120, 60))}),
    dict(key="skull_deck", name="해골섬 갑판", floor=(112, 78, 52), alt=(96, 66, 44), patch=(122, 86, 58), speck=(126, 90, 60),
         pattern="planks", dots=None, detail=(80, 60, 44),
         props={"cannon": p_cannon, "barrel": p_barrel, "crate": p_crate}),
]


# 소품 배율 — 나무는 캐릭터 1.7 배(두 배는 수관이 몹을 가렸다), 벽 · 책장 · 분수는 1.6 배, 나머지 1.25 배. 같은 도트 밀도로 크게 그린다
BIG = {"sakura": 1.7, "pine": 1.7, "snow_pine": 1.7, "peach_tree": 1.7, "bamboo": 1.7, "mushroom": 1.6,
       "palace_wall": 1.6, "bookshelf": 1.6, "fountain": 1.6, "temple_bell": 1.6, "stupa": 1.6, "chimney": 1.6,
       "banner": 1.6, "runestone": 1.6, "talisman_post": 1.6, "vine_bush": 1.4, "float_rock": 1.4, "cannon": 1.4}


# 소품은 배경이다 — 채도 35% · 명도 10% 낮춰 적 · 주인공 · 아이템보다 덜 튀게 (위험 > 주인공 > 아이템 > 소품)
DECO_SAT = 0.65
DECO_VAL = 0.9


def mute(im):
    import numpy as np
    a = np.array(im).astype(np.float32)
    rgb = a[..., :3]
    gray = rgb.mean(axis=2, keepdims=True)
    rgb = (gray + (rgb - gray) * DECO_SAT) * DECO_VAL
    a[..., :3] = np.clip(rgb, 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def prop_img(fn, k):
    f = Fig(k)
    fn(f)
    return mute(render(f, shadow_cx=32, shadow_w=13))


def lineup(t, tiles, props, hero):
    T = W * UP
    prev = Image.new("RGBA", (T * 4, T * 3))
    rnd = random.Random(3)
    for y in range(3):
        for x in range(4):
            prev.alpha_composite(big(tiles[rnd.randrange(len(tiles))], UP), (x * T, y * T))
    for i, p in enumerate(props.values()):
        pb = big(p, UP)
        prev.alpha_composite(pb, (int(T * (0.3 + i * 1.25)) - (pb.width - T) // 2, int(T * 1.0) - pb.height))
    if hero is not None:
        prev.alpha_composite(hero, (int(T * 1.5), int(T * 1.6)))
    return prev


def write_scale(keys):
    path = os.path.join(ROOT, "_DataExporter", "_art_scale.json")
    table = {}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as fp:
            table = json.load(fp)
    for k in keys:
        table["Assets/_Art/Stage/" + k] = UP
    with open(path, "w", encoding="utf-8") as fp:
        json.dump(table, fp, ensure_ascii=False, indent=1, sort_keys=True)


def clear_pngs(folder):
    if os.path.isdir(folder) is False:
        return
    for f in os.listdir(folder):
        if f.startswith(("deco_", "tile_stage_")) and (f.endswith(".png") or f.endswith(".png.meta")):
            os.remove(os.path.join(folder, f))


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    if not preview_only:
        clear_pngs(DST)

    heroes = ["shiba", "jindo", "akita", "chow", "pug", "shepherd", "poodle", "husky", "beagle", "dachshund"]
    thumbs = []
    for n, t in enumerate(THEMES, start=1):
        tiles = [tile(t, v) for v in range(1, TILE_VARIANTS + 1)]
        props = {k: prop_img(fn, BIG.get(k, 1.25)) for k, fn in t["props"].items()}
        hp = os.path.join(ROOT, r"Assets\_Art\Sprites", "dog_%s_idle.png" % heroes[n - 1])
        hero = Image.open(hp).convert("RGBA") if os.path.exists(hp) else None
        prev = lineup(t, tiles, props, hero)
        prev.save(os.path.join(PREVIEW_DIR, "map%02d_%s.png" % (n, t["key"])))
        thumbs.append(prev.resize((prev.width // 4, prev.height // 4), Image.NEAREST))

        if not preview_only:
            out = os.path.join(DST, t["key"])
            os.makedirs(out, exist_ok=True)
            clear_pngs(out)
            for i, im in enumerate(tiles, start=1):
                big(im, UP).save(os.path.join(out, "tile_stage_%d.png" % i))
            for k, im in props.items():
                big(im, UP).save(os.path.join(out, "deco_%s.png" % k))

    sheet = Image.new("RGBA", (thumbs[0].width * 5, thumbs[0].height * 2))
    for i, th in enumerate(thumbs):
        sheet.alpha_composite(th, ((i % 5) * th.width, (i // 5) * th.height))
    sheet.save(os.path.join(PREVIEW_DIR, "maps_all.png"))
    if not preview_only:
        write_scale([t["key"] for t in THEMES])
    print("맵 %d종 · 미리보기 %s%s" % (len(THEMES), PREVIEW_DIR, "" if preview_only else " · Assets 반영 %s/<key>" % DST))


main()
