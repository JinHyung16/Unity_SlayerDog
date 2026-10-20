# -*- coding: utf-8 -*-
# 맵 적 스킨 생성기 - SlayerDog 도트 (볼트 SlayerDog/SlayerDog 기획서.md 적 절 · TASKS A-5 · A-6)
#
# 공용 골격 5 × 맵 10. 골격(역할)은 Tables/enemy 의 sprite 키와 같다:
#   grunt 물량 · runner 고속 · brute 거구 · spitter 원거리 · swarm 떼
# 행동 · 수치는 공용이고, 그림만 맵 세계의 요괴 · 몬스터로 갈아입는다 (피부색 · 머리 · 소품)
# 크기 차이(Tables/enemy scale)는 캔버스 배율로 그린다 — 도트 크기를 캐릭터와 같게 (Enemy.SetArtScale 이 루트 배율을 상쇄)
#
# 출력: Assets/_Art/Sprites/mob_<맵키>_<역할>_walk{,up,side}{0..3}.png
#   Art2DBuilder 가 SpriteSetLibrary 에 "<맵키>:<역할>" 로 묶고, Enemy.ApplyType 이 지금 맵의 스킨을 먼저 찾는다
#
# 사용: python build_mobs.py [--preview]

from PIL import Image
import os, sys
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, "Assets", "_Art", "Sprites")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview", "mobs")
UP = 4

EYE = (30, 20, 30)
FANG = (250, 250, 240)


# ---------------------------------------------------------------- 머리 장식

def head_bits(f, c, cx, top, w, view):
    """top = 머리 윗선, w = 머리 반폭."""
    for k in c.get("head", []):
        if k == "horn1":
            f.p((240, 230, 200)).g((cx - 2, top + 2), (cx, top - 6), (cx + 2, top + 2))
        elif k == "horn2":
            f.p((240, 230, 200)).g((cx - w + 2, top + 4), (cx - w + 1, top - 5), (cx - w + 6, top + 2))
            f.p((240, 230, 200)).g((cx + w - 2, top + 4), (cx + w - 1, top - 5), (cx + w - 6, top + 2))
        elif k == "fox_ears":
            col = c.get("ear", c["skin"])
            f.p(col).g((cx - w + 1, top + 5), (cx - w + 2, top - 6), (cx - 2, top + 2))
            f.p(col).g((cx + w - 1, top + 5), (cx + w - 2, top - 6), (cx + 2, top + 2))
        elif k == "round_ears":
            col = c.get("ear", c["skin"])
            f.p(col).e(cx - w - 1, top - 2, cx - w + 6, top + 6)
            f.p(col).e(cx + w - 6, top - 2, cx + w + 1, top + 6)
        elif k == "kasa":
            f.p((200, 160, 90)).g((cx - w - 6, top + 5), (cx, top - 7), (cx + w + 6, top + 5))
        elif k == "jingasa":
            f.p((60, 50, 60)).g((cx - w - 4, top + 5), (cx, top - 3), (cx + w + 4, top + 5))
        elif k == "qing_hat":
            f.p((50, 46, 60)).e(cx - w - 2, top - 1, cx + w + 2, top + 5)
            f.p((50, 46, 60)).r(cx - 5, top - 6, cx + 5, top + 1)
            f.flat((200, 50, 50)).r(cx - 1, top - 8, cx + 1, top - 6)
        elif k == "helm":
            f.p(c.get("helm", (150, 154, 168))).e(cx - w - 1, top - 3, cx + w + 1, top + 8)
            f.p(c.get("helm", (150, 154, 168))).r(cx - w - 1, top + 4, cx + w + 1, top + 7)
        elif k == "horned_helm":
            f.p((236, 226, 196)).g((cx - w, top + 3), (cx - w - 5, top - 5), (cx - w + 3, top))
            f.p((236, 226, 196)).g((cx + w, top + 3), (cx + w + 5, top - 5), (cx + w - 3, top))
            f.p((110, 130, 160)).e(cx - w, top - 3, cx + w, top + 8)
        elif k == "hood":
            f.p(c.get("robe", (90, 70, 120))).e(cx - w - 2, top - 3, cx + w + 2, top + 2 * w)
        elif k == "witch":
            col = c.get("robe", (80, 110, 150))
            f.p(col).e(cx - w - 5, top + 1, cx + w + 5, top + 6)
            f.p(col).g((cx - w + 2, top + 3), (cx + w - 2, top + 3), (cx + 3, top - 12))
        elif k == "bandana":
            f.p((200, 40, 40)).e(cx - w, top - 2, cx + w, top + 6)
            f.p((200, 40, 40)).g((cx - w, top + 3), (cx - w - 5, top + 9), (cx - w - 2, top + 9))
            f.flat(WHITE).d1((cx - 4, top + 1), (cx + 3, top + 2), (cx, top + 3))
        elif k == "mush_cap":
            f.p(c.get("cap", (200, 60, 60))).e(cx - w - 5, top - 6, cx + w + 5, top + 8)
            dots = f.flat((250, 240, 230))
            dots.r(cx - 6, top - 2, cx - 4, top)
            dots.r(cx + 3, top - 3, cx + 5, top - 1)
            dots.r(cx - 1, top + 2, cx + 1, top + 4)
        elif k == "flame_top":
            f.p(c.get("flame", (120, 190, 255)), shade=False).g((cx - 5, top + 4), (cx - 2, top - 7), (cx, top - 2), (cx + 3, top - 9), (cx + 6, top + 4))
        elif k == "leaf":
            f.p((90, 170, 70)).g((cx - 1, top + 2), (cx + 6, top - 6), (cx + 2, top + 3))
        elif k == "tuft":
            f.p(c.get("ear", c["skin"])).g((cx - 4, top + 3), (cx - 1, top - 4), (cx + 1, top + 1), (cx + 4, top - 3), (cx + 5, top + 3))


def face_bits(f, c, cx, ey, view, small=False):
    if view == "up":
        return
    style = c.get("eyes", "angry")
    sx = 3 if view == "side" else 0
    gap = 2 if small else 4
    xs = (cx + sx + 1,) if view == "side" else (cx - gap - 1, cx + gap)
    ec = c.get("eye_col", EYE)
    for x in xs:
        if style == "socket":
            f.flat((40, 30, 40)).r(x - 1, ey - 1, x + 1, ey + 1)
            f.flat(c.get("glow", (255, 120, 80))).d1((x, ey))
        elif style == "glow":
            f.flat(c.get("glow", (255, 230, 120))).r(x, ey, x + 1, ey + 1)
        else:
            f.flat(ec).r(x, ey, x + 1, ey + 1)
            f.flat(WHITE).d1((x, ey))
    if style == "angry" and view == "down" and not small:
        f.flat(ink(c["skin"])).l((cx - gap - 2, ey - 2), (cx - gap + 1, ey - 1))
        f.flat(ink(c["skin"])).l((cx + gap + 2, ey - 2), (cx + gap - 1, ey - 1))
    for k in c.get("face", []):
        if k == "fang" and not small:
            mx = cx + sx
            f.flat(EYE).l((mx - 2, ey + 4), (mx + 2, ey + 4))
            f.flat(FANG).d1((mx - 2, ey + 5), (mx + 2, ey + 5))
        elif k == "long_nose":
            if view == "side":
                f.p(c["skin"]).r(cx + 8, ey + 1, cx + 14, ey + 3)
            else:
                f.p(c["skin"]).e(cx - 2, ey + 1, cx + 2, ey + 7)
        elif k == "talisman":
            f.p((250, 222, 90), line=False).r(cx - 2 + sx, ey - 8, cx + 2 + sx, ey + 4)
            f.flat(RED).l((cx + sx, ey - 7), (cx - 1 + sx, ey - 3), (cx + 1 + sx, ey + 1))
        elif k == "mask":
            f.p(c.get("robe", (50, 40, 60)), line=False).r(cx - 8 + sx, ey + 2, cx + 8 + sx, ey + 7)
        elif k == "paint":
            f.flat(RED).r(cx - 6, ey + 3, cx - 5, ey + 4)
            f.flat(RED).r(cx + 5, ey + 3, cx + 6, ey + 4)
        elif k == "beard":
            f.p((236, 236, 240)).g((cx - 5 + sx, ey + 4), (cx + 5 + sx, ey + 4), (cx + sx, ey + 12))
        elif k == "hair":
            f.p((30, 26, 34)).r(cx - 10, ey - 7, cx - 7, ey + 16)
            f.p((30, 26, 34)).r(cx + 7, ey - 7, cx + 10, ey + 16)
            f.p((30, 26, 34), line=False).r(cx - 8, ey - 9, cx + 8, ey - 4)


def item(f, c, x, y, view):
    k = c.get("item")
    if k is None:
        return
    if k == "club":
        f.p(WOOD).g((x - 1, y + 3), (x + 1, y + 3), (x + 4, y - 12), (x - 1, y - 13))
        f.flat((90, 60, 40)).d1((x + 1, y - 9), (x + 2, y - 6))
    elif k == "big_club":
        f.p(c.get("club", WOOD)).g((x - 2, y + 4), (x + 2, y + 4), (x + 7, y - 18), (x - 3, y - 20))
        f.flat((240, 230, 200)).d1((x + 2, y - 15), (x - 1, y - 12), (x + 4, y - 10))
    elif k == "fan":
        f.p((40, 36, 44)).g((x, y + 2), (x - 6, y - 9), (x + 6, y - 9))
        f.p((60, 140, 90), line=False).g((x, y), (x - 4, y - 8), (x + 4, y - 8))
    elif k == "blade":
        f.p(SILVER).l((x, y), (x + 5, y - 10), w=2)
        f.p((60, 50, 60)).l((x - 1, y + 1), (x, y + 3), w=2)
    elif k == "spear":
        f.p(WOOD).l((x, y + 6), (x, y - 18))
        f.p(SILVER).g((x - 2, y - 18), (x, y - 24), (x + 2, y - 18))
    elif k == "staff":
        f.p(c.get("staff", WOOD)).l((x, y + 10), (x, y - 16), w=2)
        f.p(c.get("orb", (150, 120, 240))).e(x - 3, y - 21, x + 3, y - 15)
    elif k == "orb":
        f.p(c.get("orb", (120, 220, 240))).e(x - 3, y - 4, x + 3, y + 2)
        f.flat(WHITE).d1((x - 1, y - 3))
    elif k == "lantern":
        f.flat((60, 50, 60)).l((x, y - 6), (x, y - 2))
        f.p((250, 210, 110)).r(x - 3, y - 2, x + 3, y + 5)
    elif k == "book":
        f.p((150, 60, 60)).r(x - 4, y - 5, x + 4, y + 3)
        f.flat((240, 230, 210)).r(x - 3, y - 4, x + 3, y - 3)
    elif k == "axe":
        f.p(WOOD).l((x, y + 6), (x + 1, y - 12), w=2)
        f.p(SILVER).g((x + 1, y - 13), (x + 7, y - 15), (x + 7, y - 6), (x + 1, y - 8))
    elif k == "cutlass":
        f.p(SILVER).g((x, y), (x + 3, y - 12), (x + 5, y - 11), (x + 2, y))
        f.p(GOLD).r(x - 1, y, x + 3, y + 1)
    elif k == "trident":
        f.p((60, 120, 120)).l((x, y + 10), (x, y - 16), w=2)
        tp = f.p(GOLD)
        tp.l((x - 4, y - 16), (x + 4, y - 16))
        tp.l((x - 4, y - 16), (x - 4, y - 22))
        tp.l((x, y - 16), (x, y - 24))
        tp.l((x + 4, y - 16), (x + 4, y - 22))
    elif k == "kunai":
        f.p(SILVER).g((x, y - 9), (x + 2, y - 2), (x - 2, y - 2))
        f.p((60, 50, 60)).r(x - 1, y - 2, x, y + 2)
    elif k == "sword":
        f.p(SILVER).l((x, y), (x + 1, y - 13), w=2)
        f.p(GOLD).r(x - 2, y - 1, x + 3, y)
    elif k == "petal":
        f.p((250, 190, 200)).e(x - 3, y - 3, x + 3, y + 3)


# ---------------------------------------------------------------- 골격 5

def g_grunt(f, c, view, ph, b):
    cx = 32 if view != "side" else 31
    lift_l = max(0, int(round(2 * ph)))
    lift_r = max(0, int(round(-2 * ph)))
    body = c.get("body", c["skin"])
    leg = c.get("leg", c["skin"])
    for x0, lift in ((cx - 6, lift_l), (cx + 2, lift_r)):
        f.p(leg).r(x0, 52, x0 + 4, 60 - lift)
    f.p(body).e(cx - 10, 36 + b, cx + 10, 57 + b)
    if c.get("belt"):
        f.p(c["belt"], line=False).r(cx - 10, 47 + b, cx + 10, 49 + b)
    top = 17 + b
    f.p(c.get("head_col", c["skin"])).e(cx - 11, top, cx + 11, top + 22)
    head_bits(f, c, cx, top, 11, view)
    face_bits(f, c, cx, top + 10, view)
    if view != "up":
        f.p(c["skin"]).e(cx - 14, 40 + b, cx - 9, 46 + b)
        f.p(c["skin"]).e(cx + 9, 40 + b, cx + 14, 46 + b)
        item(f, c, cx + 12, 43 + b, view)


def g_runner(f, c, view, ph, b):
    cx = 32 if view != "side" else 33
    bob = int(round(2 * ph))
    y0 = 32 + bob
    col = c["skin"]
    if c.get("tail", True):
        tc = c.get("tail_col", col)
        if view == "side":
            f.p(tc, shade=False).g((cx - 6, y0 + 8), (cx - 18, y0 + 2 - bob), (cx - 15, y0 + 12), (cx - 5, y0 + 14))
        elif view == "up":
            f.p(tc, shade=False).g((cx - 3, y0 + 12), (cx + 3, y0 + 12), (cx, y0 + 24))
        else:
            f.p(tc, shade=False).g((cx + 4, y0 + 10), (cx + 15, y0 + 2 - bob), (cx + 12, y0 + 14))
    if c.get("legs4"):
        lc = c.get("leg", col)
        for x in (cx - 7, cx - 3, cx + 2, cx + 6):
            lift = 2 if (x + int(ph * 10)) % 2 else 0
            f.p(lc).r(x, y0 + 14, x + 1, 60 - lift)
    if c.get("wings"):
        wc = c.get("wing_col", ink(col))
        flap = -4 if ph < 0 else 2
        f.p(wc).g((cx - 6, y0 + 6), (cx - 18, y0 + flap), (cx - 14, y0 + 12))
        f.p(wc).g((cx + 6, y0 + 6), (cx + 18, y0 + flap), (cx + 14, y0 + 12))
    f.p(col).e(cx - 9, y0, cx + 9, y0 + 17)
    head_bits(f, c, cx, y0 + 1, 8, view)
    face_bits(f, c, cx, y0 + 7, view, small=True)


def g_brute(f, c, view, ph, b):
    cx = 32 if view != "side" else 31
    lift_l = max(0, int(round(2 * ph)))
    lift_r = max(0, int(round(-2 * ph)))
    body = c.get("body", c["skin"])
    leg = c.get("leg", c["skin"])
    for x0, lift in ((cx - 11, lift_l), (cx + 4, lift_r)):
        f.p(leg).r(x0, 50, x0 + 7, 60 - lift)
    f.p(body).e(cx - 18, 24 + b, cx + 18, 56 + b)
    if c.get("belt"):
        f.p(c["belt"], line=False).r(cx - 17, 44 + b, cx + 17, 47 + b)
    top = 10 + b
    f.p(c.get("head_col", c["skin"])).e(cx - 10, top, cx + 10, top + 19)
    head_bits(f, c, cx, top, 10, view)
    face_bits(f, c, cx, top + 9, view)
    if view != "up":
        f.p(c["skin"]).e(cx - 24, 30 + b, cx - 15, 44 + b)
        f.p(c["skin"]).e(cx + 15, 30 + b, cx + 24, 44 + b)
        item(f, c, cx + 21, 40 + b, view)


def g_spitter(f, c, view, ph, b):
    cx = 32 if view != "side" else 31
    bob = int(round(1.5 * ph))
    robe = c.get("robe", (90, 70, 120))
    f.p(robe).g((cx - 9, 30 + bob), (cx + 9, 30 + bob), (cx + 13, 59), (cx - 13, 59))
    if c.get("trim"):
        f.flat(c["trim"]).r(cx - 13, 57, cx + 13, 58)
    top = 13 + bob
    f.p(c.get("head_col", c["skin"])).e(cx - 9, top, cx + 9, top + 18)
    head_bits(f, c, cx, top, 9, view)
    face_bits(f, c, cx, top + 9, view)
    if view != "up":
        f.p(robe).e(cx + 7, 37 + bob, cx + 13, 43 + bob)
        item(f, c, cx + 12, 38 + bob, view)


def g_swarm(f, c, view, ph, b):
    cx = 32
    sq = 1 if ph > 0 else 0
    col = c["skin"]
    if c.get("wings"):
        wc = c.get("wing_col", ink(col))
        flap = -3 if ph < 0 else 1
        f.p(wc).g((cx - 4, 50), (cx - 13, 44 + flap), (cx - 10, 53))
        f.p(wc).g((cx + 4, 50), (cx + 13, 44 + flap), (cx + 10, 53))
    f.p(col).e(cx - 8 - sq, 45 + sq, cx + 8 + sq, 60)
    head_bits(f, c, cx, 46, 7, view)
    face_bits(f, c, cx, 51, view, small=True)


ROLES = {"grunt": g_grunt, "runner": g_runner, "brute": g_brute, "spitter": g_spitter, "swarm": g_swarm}


# ---------------------------------------------------------------- 맵 10 × 골격 5 (기획서 맵 표 순서 · build_stage THEMES key 와 같다)

MOBS = {
    "snow_sakura": {
        "grunt": dict(name="오니졸", skin=(214, 84, 70), body=(220, 180, 70), belt=(60, 50, 50), head=["horn1"], face=["fang"], item="club"),
        "runner": dict(name="여우불", skin=(120, 180, 255), tail_col=(180, 220, 255), head=["fox_ears"], ear=(120, 180, 255), eyes="glow", glow=(255, 255, 255)),
        "brute": dict(name="청오니", skin=(90, 112, 200), body=(220, 180, 70), belt=(60, 50, 50), head=["horn2"], face=["fang"], item="big_club", club=(60, 56, 64)),
        "spitter": dict(name="텐구", skin=(210, 70, 60), robe=(60, 60, 80), trim=(220, 220, 230), head=["kasa"], face=["long_nose"], item="fan"),
        "swarm": dict(name="눈꼬마", skin=(236, 240, 250), eyes="round", head=["tuft"], ear=(200, 210, 236)),
    },
    "palace_range": {
        "grunt": dict(name="도깨비", skin=(110, 170, 90), body=(150, 110, 70), head=["horn1", "tuft"], ear=(60, 50, 40), face=["fang"], item="club"),
        "runner": dict(name="불여우", skin=(240, 140, 60), tail_col=(255, 200, 90), head=["fox_ears"], legs4=True, eyes="glow", glow=(255, 250, 200)),
        "brute": dict(name="장승", skin=(176, 124, 74), body=(176, 124, 74), head=["horn2"], face=["paint", "fang"], eyes="round", item="spear"),
        "spitter": dict(name="처녀귀신", skin=(236, 236, 240), robe=(236, 236, 240), face=["hair"], eyes="glow", glow=(200, 40, 40), item=None),
        "swarm": dict(name="도깨비불", skin=(110, 220, 230), head=["flame_top"], flame=(170, 240, 250), eyes="glow", glow=(255, 255, 255)),
    },
    "moon_roof": {
        "grunt": dict(name="닌자졸", skin=(220, 190, 150), head_col=(54, 48, 72), body=(54, 48, 72), belt=(180, 40, 50), robe=(54, 48, 72), face=["mask"], item="kunai"),
        "runner": dict(name="그림자 고양이", skin=(50, 44, 60), head=["fox_ears"], legs4=True, eyes="glow", glow=(250, 230, 90)),
        "brute": dict(name="갑주 망령", skin=(80, 70, 90), body=(170, 50, 50), belt=(40, 36, 44), head=["helm"], helm=(170, 50, 50), eyes="socket", glow=(255, 150, 60), item="sword"),
        "spitter": dict(name="너구리 요술사", skin=(150, 110, 80), robe=(110, 80, 60), head=["round_ears", "leaf"], ear=(90, 64, 50), face=["mask"], item="orb", orb=(120, 220, 140)),
        "swarm": dict(name="박쥐", skin=(70, 56, 90), wings=True, wing_col=(50, 40, 64), head=["fox_ears"], eyes="glow", glow=(255, 90, 90)),
    },
    "bamboo_temple": {
        "grunt": dict(name="원숭이 행자", skin=(170, 120, 80), head_col=(170, 120, 80), body=(230, 150, 50), head=["round_ears"], ear=(220, 170, 140), item="spear"),
        "runner": dict(name="대숲 정령", skin=(130, 200, 110), tail_col=(180, 230, 150), head=["leaf"], eyes="glow", glow=(255, 255, 220)),
        "brute": dict(name="청동 나한졸", skin=(176, 140, 80), body=(150, 110, 60), belt=(110, 70, 40), head=[], eyes="round", face=["fang"], item="big_club", club=(150, 110, 60)),
        "spitter": dict(name="요괴 승려", skin=(150, 160, 170), robe=(120, 120, 130), trim=(200, 80, 60), head=[], face=["fang"], item="staff", orb=(240, 200, 90)),
        "swarm": dict(name="풀벌레", skin=(120, 180, 70), wings=True, wing_col=(200, 230, 200), head=["horn2"], eyes="round"),
    },
    "mist_realm": {
        "grunt": dict(name="강시", skin=(150, 180, 150), body=(50, 60, 100), belt=(200, 160, 60), head=["qing_hat"], face=["talisman"], eyes="round", item=None),
        "runner": dict(name="구름 정령", skin=(230, 240, 245), tail_col=(200, 220, 230), head=["tuft"], ear=(210, 225, 235), eyes="round"),
        "brute": dict(name="석사자", skin=(96, 170, 146), body=(96, 170, 146), head=["round_ears", "tuft"], ear=(60, 120, 104), face=["fang"], eyes="round"),
        "spitter": dict(name="여우 도사", skin=(230, 160, 80), robe=(80, 130, 120), trim=(240, 220, 120), head=["fox_ears"], face=["beard"], item="staff", orb=(120, 230, 200)),
        "swarm": dict(name="복숭아 요정", skin=(250, 180, 170), head=["leaf"], eyes="round"),
    },
    "castle_square": {
        "grunt": dict(name="해골 병사", skin=(236, 230, 210), body=(120, 110, 100), head=["helm"], eyes="socket", glow=(255, 90, 60), item="sword"),
        "runner": dict(name="잿빛 늑대", skin=(130, 130, 140), head=["fox_ears"], legs4=True, eyes="glow", glow=(255, 220, 90)),
        "brute": dict(name="오우거", skin=(120, 160, 90), body=(120, 90, 60), belt=(70, 50, 40), head=["tuft"], ear=(70, 60, 50), face=["fang"], item="big_club"),
        "spitter": dict(name="사령술사", skin=(200, 200, 210), robe=(80, 50, 110), trim=(200, 170, 80), head=["hood"], eyes="glow", glow=(150, 255, 150), item="staff", orb=(130, 255, 140)),
        "swarm": dict(name="시궁쥐", skin=(130, 110, 100), head=["round_ears"], ear=(220, 150, 150), eyes="round"),
    },
    "mage_library": {
        "grunt": dict(name="책벌레", skin=(150, 60, 70), head_col=(236, 226, 200), body=(150, 60, 70), head=[], eyes="angry", face=["fang"], item="book"),
        "runner": dict(name="도깨비 촛불", skin=(250, 220, 150), tail_col=(255, 160, 80), head=["flame_top"], flame=(255, 180, 80), eyes="round"),
        "brute": dict(name="자수정 골렘", skin=(130, 100, 180), body=(110, 84, 160), head=[], eyes="glow", glow=(220, 200, 255)),
        "spitter": dict(name="임프", skin=(210, 70, 80), robe=(60, 40, 80), head=["horn2"], face=["fang"], item="orb", orb=(250, 150, 60)),
        "swarm": dict(name="잉크 방울", skin=(50, 50, 80), eyes="glow", glow=(150, 200, 255)),
    },
    "glacier_fjord": {
        "grunt": dict(name="얼음 고블린", skin=(130, 180, 220), body=(120, 90, 70), head=["horned_helm"], face=["fang"], item="axe"),
        "runner": dict(name="서리 늑대", skin=(220, 236, 248), head=["fox_ears"], ear=(180, 210, 236), legs4=True, eyes="glow", glow=(90, 200, 255)),
        "brute": dict(name="서리 트롤", skin=(170, 200, 220), body=(140, 170, 200), head=["tuft"], ear=(236, 244, 250), face=["fang"], item="big_club", club=(110, 160, 200)),
        "spitter": dict(name="얼음 마녀", skin=(220, 230, 240), robe=(80, 110, 150), trim=(200, 230, 250), head=["witch"], item="staff", orb=(170, 230, 255)),
        "swarm": dict(name="눈송이", skin=(200, 230, 250), head=["horn2"], eyes="round"),
    },
    "giant_forest": {
        "grunt": dict(name="버섯 병사", skin=(236, 220, 190), body=(110, 90, 60), head=["mush_cap"], cap=(200, 60, 60), item="spear"),
        "runner": dict(name="왕벌", skin=(250, 200, 60), wings=True, wing_col=(220, 240, 250), head=["horn2"], eyes="glow", glow=(40, 30, 30), tail=False),
        "brute": dict(name="썩은 나무꾼", skin=(110, 84, 60), body=(90, 120, 60), head=["leaf", "tuft"], ear=(70, 130, 60), face=["fang"], eyes="glow", glow=(220, 255, 120)),
        "spitter": dict(name="독꽃", skin=(220, 90, 160), head_col=(220, 90, 160), robe=(70, 140, 70), head=["tuft"], ear=(250, 220, 90), item=None, eyes="angry"),
        "swarm": dict(name="포자", skin=(180, 210, 120), head=["mush_cap"], cap=(150, 110, 200), eyes="round"),
    },
    "skull_deck": {
        "grunt": dict(name="해골 해적", skin=(236, 230, 210), body=(60, 70, 120), belt=(110, 80, 50), head=["bandana"], eyes="socket", glow=(255, 200, 80), item="cutlass"),
        "runner": dict(name="꼬마 문어", skin=(220, 90, 70), head=[], legs4=True, eyes="round", tail=False),
        "brute": dict(name="어인", skin=(90, 160, 150), body=(70, 130, 130), head=["tuft"], ear=(60, 120, 160), face=["fang"], eyes="round", item="trident"),
        "spitter": dict(name="유령 선원", skin=(170, 220, 230), robe=(110, 170, 190), head=["bandana"], eyes="glow", glow=(255, 255, 200), item="lantern"),
        "swarm": dict(name="바다 슬라임", skin=(90, 190, 200), head=[], eyes="round"),
    },
}

WALK = [(-1.0, 0), (-0.4, -1), (1.0, 0), (0.4, -1)]
VIEWS = (("down", ""), ("up", "up"), ("side", "side"))


# 역할 배율 = Tables/enemy scale. 이 크기 캔버스에 같은 도트 밀도로 그리고 Enemy.SetArtScale 이 화면에서 상쇄한다
ROLE_K = {"grunt": 1.0, "runner": 0.95, "brute": 1.35, "spitter": 1.05, "swarm": 0.75}


def draw(c, role, view, ph, b):
    f = Fig(ROLE_K[role])
    ROLES[role](f, c, view, ph, b)
    return rim(render(f, shadow_cx=32, shadow_w=10 if role != "brute" else 14))


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    n = 0
    rows = []
    for mkey, roles in MOBS.items():
        row = Image.new("RGBA", (W * 5, H), (0, 0, 0, 0))
        for i, role in enumerate(ROLES):
            c = roles[role]
            row.alpha_composite(draw(c, role, "down", 0, 0), (i * W, 0))
            for view, suffix in VIEWS:
                for k, (ph, b) in enumerate(WALK):
                    if not preview_only:
                        big(draw(c, role, view, ph, b), UP).save(
                            os.path.join(DST, "mob_%s_%s_walk%s%d.png" % (mkey, role, suffix, k)))
                    n += 1
        rows.append(row)
    sheet = Image.new("RGBA", (W * 5, H * len(rows)), (72, 82, 104, 255))
    for r, row in enumerate(rows):
        sheet.alpha_composite(row, (0, r * H))
    big(sheet, 3).save(os.path.join(PREVIEW_DIR, "mobs_all.png"))
    print("맵 %d × 골격 5 · 스프라이트 %d장%s" % (len(MOBS), n, " (미리보기만)" if preview_only else ""))


main()
