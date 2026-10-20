# -*- coding: utf-8 -*-
# 맵 보스 10 생성기 - SlayerDog 도트 (볼트 SlayerDog/SlayerDog 기획서.md 맵 표 보스 · TASKS A-7)
#
# - 보스는 적 한 종(sprite 'boss')에 맵 스킨을 입힌다: mob_<맵키>_boss_walk{,up,side}{0..3}.png
#   (build_mobs.py 와 같은 이름 규칙 · SpriteSetLibrary 키 "<맵키>:boss")
# - 캔버스 배율 2.55 (= 1.5 × 보스 이벤트 1.7). 화면 크기는 예전과 같고 도트 크기만 캐릭터와 같아진다
# - 공용 거구 골격(다리 · 몸통 · 팔 · 머리) 위에 보스마다 등 장식 · 머리 · 얼굴 · 무기를 얹어 실루엣을 가른다
#
# 사용: python build_bosses.py [--preview]

from PIL import Image
import os, sys, math
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, "Assets", "_Art", "Sprites")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview", "bosses")
UP = 4
# 1.5 (보스 크기) × 1.7 (Tables/wave boss scaleMul) — 이 크기로 그리고 Enemy 가 화면에서 상쇄해 도트 크기를 맞춘다
K = 1.5 * 1.7
EYE = (30, 20, 30)


# ---------------------------------------------------------------- 등 장식 (몸보다 먼저)

def back(f, c, cx, b, view):
    k = c.get("back")
    if k == "tails":
        col, tip = c["tail"], c.get("tail_tip", WHITE)
        # 아홉 꼬리를 어깨 위로 부채꼴로 — 몸에 가리지 않게 바깥으로 길게
        for i in range(9):
            a = math.radians(-195 + i * 26.25)
            x, y = cx + math.cos(a) * 30, 34 + b + math.sin(a) * 28
            f.p(col).g((cx - 3, 40 + b), (x - 4, y + 2), (x, y - 4), (x + 4, y + 1), (cx + 3, 40 + b))
            f.p(tip, line=False).e(x - 3, y - 4, x + 3, y + 2)
    elif k == "cape":
        f.p(c["cape"]).g((cx - 14, 24 + b), (cx + 14, 24 + b), (cx + 20, 60), (cx - 20, 60))
    elif k == "halo":
        f.p(GOLD, shade=False).e(cx - 13, 0 + b, cx + 13, 22 + b)
        f.p(c.get("halo_in", (60, 50, 40)), line=False, shade=False, light=False).e(cx - 10, 3 + b, cx + 10, 19 + b)
    elif k == "branches":
        col = c["skin"]
        f.p(col).l((cx - 6, 20 + b), (cx - 20, 4 + b), w=3)
        f.p(col).l((cx + 6, 20 + b), (cx + 22, 2 + b), w=3)
        f.p(col).l((cx - 14, 12 + b), (cx - 24, 12 + b), w=2)
        f.p((70, 120, 60)).e(cx - 30, -2 + b, cx - 12, 14 + b)
        f.p((70, 120, 60)).e(cx + 12, -4 + b, cx + 30, 12 + b)
    elif k == "pages":
        for i, (dx, dy) in enumerate(((-24, 10), (22, 6), (-20, 34), (24, 30))):
            f.p((240, 230, 200)).g((cx + dx - 4, dy + b), (cx + dx + 4, dy - 2 + b), (cx + dx + 5, dy + 8 + b), (cx + dx - 3, dy + 10 + b))
    elif k == "mist":
        f.p(c["skin"], shade=False).e(cx - 22, 44 + b, cx + 22, 62)


# ---------------------------------------------------------------- 몸

def body(f, c, cx, ph, b, view):
    lift_l = max(0, int(round(2 * ph)))
    lift_r = max(0, int(round(-2 * ph)))
    leg, torso, skin = c.get("leg", c["skin"]), c.get("body", c["skin"]), c["skin"]
    if c.get("float"):
        f.p(c.get("robe", torso)).g((cx - 14, 26 + b), (cx + 14, 26 + b), (cx + 18, 56 + b), (cx, 61), (cx - 18, 56 + b))
    elif c.get("book"):
        f.p((120, 40, 50)).r(cx - 20, 20 + b, cx + 20, 54 + b)
        f.p((240, 230, 200), line=False).r(cx - 18, 22 + b, cx + 18, 26 + b)
        f.p(GOLD, line=False).r(cx - 20, 20 + b, cx - 17, 54 + b)
    else:
        for x0, lift in ((cx - 12, lift_l), (cx + 4, lift_r)):
            f.p(leg).r(x0, 48, x0 + 8, 60 - lift)
            f.p(c.get("boot", ink(leg))).r(x0 - 1, 57 - lift, x0 + 9, 60 - lift)
        f.p(torso).g((cx - 17, 24 + b), (cx + 17, 24 + b), (cx + 14, 52 + b), (cx - 14, 52 + b))
        if c.get("belt"):
            f.p(c["belt"], line=False).r(cx - 15, 44 + b, cx + 15, 47 + b)
        if c.get("plate"):
            f.p(c["plate"]).e(cx - 21, 20 + b, cx - 11, 30 + b)
            f.p(c["plate"]).e(cx + 11, 20 + b, cx + 21, 30 + b)
            f.p(c["plate"], line=False).r(cx - 10, 28 + b, cx + 10, 40 + b)
        if c.get("beads"):
            bp = f.p(c["beads"], light=False)
            for i in range(-4, 5):
                bp.e(cx + i * 3 - 1, 27 + b + abs(i), cx + i * 3 + 2, 30 + b + abs(i))


def arms(f, c, cx, b, view):
    if c.get("book") or view == "up":
        return
    skin = c.get("arm", c["skin"])
    if c.get("arms_forward"):
        f.p(skin).r(cx - 16, 30 + b, cx - 10, 36 + b)
        f.p(skin).r(cx + 10, 30 + b, cx + 16, 36 + b)
        return
    f.p(skin).e(cx - 25, 26 + b, cx - 15, 42 + b)
    f.p(skin).e(cx + 15, 26 + b, cx + 25, 42 + b)


def head(f, c, cx, b, view):
    top = 6 + b
    if c.get("book"):
        if view != "up":
            f.p((250, 240, 220)).e(cx - 7, 30 + b, cx + 7, 42 + b)
            f.flat(c.get("eye", (200, 30, 40))).e(cx - 3, 33 + b, cx + 3, 39 + b)
            f.flat(EYE).r(cx - 1, 35 + b, cx + 1, 37 + b)
            for x in range(cx - 16, cx + 17, 4):
                f.flat(WHITE).g((x, 48 + b), (x + 3, 48 + b), (x + 1, 52 + b))
        return
    f.p(c.get("head_col", c["skin"])).e(cx - 11, top, cx + 11, top + 20)
    for k in c.get("head", []):
        if k == "kabuto":
            f.p((40, 36, 44)).e(cx - 13, top - 3, cx + 13, top + 9)
            f.p((40, 36, 44)).r(cx - 15, top + 6, cx + 15, top + 9)
            f.p(GOLD).g((cx - 12, top - 2), (cx - 4, top - 12), (cx - 2, top - 2))
            f.p(GOLD).g((cx + 12, top - 2), (cx + 4, top - 12), (cx + 2, top - 2))
        elif k == "horns":
            f.p((240, 230, 200)).g((cx - 9, top + 3), (cx - 12, top - 8), (cx - 4, top + 1))
            f.p((240, 230, 200)).g((cx + 9, top + 3), (cx + 12, top - 8), (cx + 4, top + 1))
        elif k == "fox_ears":
            f.p(c["skin"]).g((cx - 11, top + 6), (cx - 9, top - 9), (cx - 2, top + 2))
            f.p(c["skin"]).g((cx + 11, top + 6), (cx + 9, top - 9), (cx + 2, top + 2))
            f.flat((240, 120, 140)).g((cx - 9, top + 3), (cx - 8, top - 5), (cx - 4, top + 1))
            f.flat((240, 120, 140)).g((cx + 9, top + 3), (cx + 8, top - 5), (cx + 4, top + 1))
        elif k == "qing":
            f.p((50, 46, 60)).e(cx - 14, top - 2, cx + 14, top + 6)
            f.p((50, 46, 60)).r(cx - 6, top - 9, cx + 6, top + 1)
            f.flat(RED).r(cx - 1, top - 12, cx + 1, top - 9)
        elif k == "knight_helm":
            f.p((60, 56, 70)).e(cx - 12, top - 2, cx + 12, top + 20)
            f.p((150, 30, 40)).g((cx, top - 2), (cx - 3, top - 14), (cx + 6, top - 10))
        elif k == "icicle_beard":
            for x in range(cx - 8, cx + 9, 4):
                f.p((200, 236, 250)).g((x - 2, top + 15), (x + 2, top + 15), (x, top + 24))
        elif k == "tricorne":
            f.p((40, 34, 46)).g((cx - 18, top + 4), (cx + 18, top + 4), (cx + 13, top - 6), (cx, top - 2), (cx - 13, top - 6))
            f.flat(GOLD).l((cx - 17, top + 4), (cx + 17, top + 4))
            f.flat(WHITE).r(cx - 2, top - 3, cx + 1, top)
        elif k == "bald_mark":
            f.flat((90, 60, 40)).d1((cx - 3, top + 3), (cx, top + 2), (cx + 3, top + 3))
        elif k == "mushrooms":
            f.p((200, 60, 60)).e(cx - 14, top - 2, cx - 6, top + 4)
            f.p((220, 180, 90)).e(cx + 5, top - 4, cx + 13, top + 2)
    if view == "up":
        return
    ey = top + 9
    sx = 3 if view == "side" else 0
    style = c.get("eyes", "angry")
    xs = (cx + sx + 2,) if view == "side" else (cx - 6, cx + 4)
    for x in xs:
        if style == "glow":
            f.flat(c.get("glow", (255, 80, 60))).r(x, ey, x + 2, ey + 1)
        else:
            f.flat(EYE).r(x, ey, x + 1, ey + 1)
            f.flat(WHITE).d1((x, ey))
    if style == "angry" and view == "down":
        f.flat(ink(c.get("head_col", c["skin"]))).l((cx - 8, ey - 3), (cx - 3, ey - 1))
        f.flat(ink(c.get("head_col", c["skin"]))).l((cx + 8, ey - 3), (cx + 3, ey - 1))
    for k in c.get("face", []):
        if k == "fangs":
            f.flat(EYE).l((cx - 4 + sx, ey + 6), (cx + 4 + sx, ey + 6))
            f.flat(WHITE).d1((cx - 3 + sx, ey + 7), (cx + 3 + sx, ey + 7))
        elif k == "menpo":
            f.p((40, 36, 44), line=False).r(cx - 9 + sx, ey + 4, cx + 9 + sx, ey + 10)
            f.flat((200, 40, 40)).l((cx - 6 + sx, ey + 7), (cx + 6 + sx, ey + 7))
        elif k == "talisman":
            f.p((250, 222, 90), line=False).r(cx - 3 + sx, ey - 10, cx + 3 + sx, ey + 6)
            f.flat(RED).l((cx + sx, ey - 9), (cx - 2 + sx, ey - 3), (cx + 2 + sx, ey + 3))
        elif k == "fox_marks":
            f.flat(RED).l((cx - 9, ey - 2), (cx - 6, ey + 3))
            f.flat(RED).l((cx + 9, ey - 2), (cx + 6, ey + 3))
        elif k == "beard":
            f.p((236, 236, 240)).g((cx - 7 + sx, ey + 5), (cx + 7 + sx, ey + 5), (cx + sx, ey + 16))
        elif k == "bark":
            f.flat(ink(c["skin"])).l((cx - 6, ey + 4), (cx - 4, ey + 9))
            f.flat(ink(c["skin"])).l((cx + 5, ey + 3), (cx + 3, ey + 10))


def weapon(f, c, cx, b, view):
    if view == "up":
        return
    k = c.get("weapon")
    x, y = cx + 22, 36 + b
    if k == "kanabo":
        f.p((50, 44, 52)).g((x - 2, y + 6), (x + 2, y + 6), (x + 6, y - 22), (x - 4, y - 24))
        st = f.flat(SILVER)
        for dy in range(-20, 4, 5):
            st.r(x - 2, y + dy, x - 1, y + dy + 1)
    elif k == "katana":
        f.p(SILVER).g((x - 1, y + 2), (x + 1, y + 2), (x + 6, y - 26), (x + 4, y - 26))
        f.p(GOLD).r(x - 3, y + 2, x + 3, y + 3)
        f.p((150, 30, 40)).r(x - 1, y + 3, x + 1, y + 8)
    elif k == "greatsword":
        f.p((90, 90, 110)).g((x - 3, y + 4), (x + 3, y + 4), (x + 3, y - 28), (x, y - 32), (x - 3, y - 28))
        f.p((150, 30, 40)).r(x - 6, y + 3, x + 6, y + 5)
    elif k == "ice_club":
        f.p((170, 220, 245)).g((x - 2, y + 6), (x + 2, y + 6), (x + 8, y - 20), (x - 5, y - 22))
    elif k == "hook":
        f.p(SILVER).l((x, y), (x, y - 6), (x - 4, y - 9), w=2)
        f.p(SILVER).g((cx - 26, y), (cx - 22, y - 20), (cx - 20, y - 20), (cx - 24, y))
    elif k == "staff":
        f.p(WOOD).l((x, y + 16), (x, y - 26), w=2)
        f.p(GOLD).e(x - 4, y - 32, x + 4, y - 24)
    elif k == "fire":
        for dx, dy in ((-30, -6), (30, -10), (-26, 18)):
            f.p((120, 180, 255), shade=False).g((cx + dx - 3, 30 + dy + b), (cx + dx, 22 + dy + b), (cx + dx + 3, 30 + dy + b))


# ---------------------------------------------------------------- 보스 10 (맵 키 = build_stage THEMES · gen_slayer MAPS)

BOSSES = {
    "snow_sakura": dict(name="오니 장군", skin=(200, 60, 50), body=(40, 36, 44), leg=(40, 36, 44), belt=GOLD, plate=(150, 40, 40),
                        head=["kabuto"], face=["fangs"], weapon="kanabo"),
    "palace_range": dict(name="구미호", skin=(250, 236, 214), body=(250, 236, 214), leg=(240, 220, 190), back="tails", belt=(200, 40, 50),
                         tail=(250, 236, 214), tail_tip=(255, 170, 80), head=["fox_ears"], face=["fox_marks"], eyes="glow",
                         glow=(255, 200, 80), weapon="fire"),
    "moon_roof": dict(name="그림자 쇼군", skin=(60, 50, 80), body=(50, 40, 70), leg=(40, 34, 56), plate=(80, 60, 110), belt=(150, 40, 50),
                      back="cape", cape=(30, 26, 40), head=["kabuto"], face=["menpo"], eyes="glow", glow=(180, 120, 255), weapon="katana"),
    "bamboo_temple": dict(name="철갑 나한", skin=(180, 140, 80), body=(170, 120, 60), leg=(150, 110, 60), belt=(110, 70, 40),
                          back="halo", beads=(110, 64, 40), head=["bald_mark"], eyes="angry", weapon="staff"),
    "mist_realm": dict(name="강시 대왕", skin=(150, 190, 150), body=(60, 50, 110), leg=(50, 40, 90), belt=GOLD, plate=(90, 60, 140),
                       head=["qing"], face=["talisman"], eyes="glow", glow=(255, 240, 120), arms_forward=True),
    "castle_square": dict(name="타락 기사단장", skin=(70, 66, 80), body=(70, 66, 80), leg=(56, 52, 64), plate=(110, 104, 120), belt=(150, 30, 40),
                          back="cape", cape=(140, 30, 40), head=["knight_helm"], eyes="glow", glow=(255, 60, 60), weapon="greatsword"),
    "mage_library": dict(name="미믹 대마도서", skin=(120, 40, 50), book=True, back="pages", eye=(220, 30, 60)),
    "glacier_fjord": dict(name="서리 거인", skin=(170, 210, 235), body=(130, 170, 210), leg=(110, 150, 190), belt=(90, 70, 60),
                          head=["horns", "icicle_beard"], eyes="glow", glow=(220, 250, 255), weapon="ice_club"),
    "giant_forest": dict(name="썩은 엔트", skin=(110, 80, 56), body=(100, 74, 52), leg=(90, 66, 46), back="branches",
                         head=["mushrooms"], face=["bark"], eyes="glow", glow=(200, 255, 100)),
    "skull_deck": dict(name="유령 선장", skin=(160, 220, 230), body=(70, 110, 140), robe=(90, 150, 170), float=True, belt=GOLD,
                       back="mist", head=["tricorne"], face=["beard"], eyes="glow", glow=(255, 255, 200), weapon="hook"),
}

WALK = [(-1.0, 0), (-0.4, -1), (1.0, 0), (0.4, -1)]
VIEWS = (("down", ""), ("up", "up"), ("side", "side"))


def draw(c, view, ph, b):
    f = Fig(K)
    cx = 32
    if c.get("float") or c.get("book"):
        b = b - 1 + int(round(ph))
    back(f, c, cx, b, view)
    body(f, c, cx, ph, b, view)
    arms(f, c, cx, b, view)
    head(f, c, cx, b, view)
    weapon(f, c, cx, b, view)
    return rim(render(f, shadow_cx=32, shadow_w=20))


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    n = 0
    cells = []
    for mkey, c in BOSSES.items():
        cells.append(draw(c, "down", 0, 0))
        for view, suffix in VIEWS:
            for i, (ph, b) in enumerate(WALK):
                if not preview_only:
                    big(draw(c, view, ph, b), UP).save(os.path.join(DST, "mob_%s_boss_walk%s%d.png" % (mkey, suffix, i)))
                n += 1
    cw = cells[0].width
    sheet = Image.new("RGBA", (cw * 5, cw * 2), (72, 82, 104, 255))
    for i, im in enumerate(cells):
        sheet.alpha_composite(im, ((i % 5) * cw, (i // 5) * cw))
    big(sheet, 3).save(os.path.join(PREVIEW_DIR, "bosses_all.png"))
    side = Image.new("RGBA", (cw * 5, cw), (72, 82, 104, 255))
    for i, c in enumerate(list(BOSSES.values())[:5]):
        side.alpha_composite(draw(c, "side", 0, 0), (i * cw, 0))
    big(side, 2).save(os.path.join(PREVIEW_DIR, "bosses_side.png"))
    print("보스 %d · 스프라이트 %d장%s" % (len(BOSSES), n, " (미리보기만)" if preview_only else ""))


main()
