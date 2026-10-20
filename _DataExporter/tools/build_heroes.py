# -*- coding: utf-8 -*-
# 주인공 스프라이트 생성기 v3 - SlayerDog 도트 (볼트 SlayerDog/SlayerDog 기획서.md 캐릭터 10)
#
# 도트 규격 (TASKS A-1)
# - 논리 캔버스 64×64 를 픽셀 그대로 찍는다. 1 논리 좌표 = 1 도트. 발끝 y 61 (build_meta 발밑 피벗과 같다)
# - 출력은 최근접 ×4 확대 256px. Sprites 폴더 배율 4 · PPU 128 그대로라 프리팹 · 판정 반경이 안 바뀐다
#   (화면 배율이 720p 1.27배 · 1080p 1.9배로 정수가 아니어서 Point 대신 확대본 + Bilinear + 밉맵)
# - 부위마다 단색으로 칠하고 명암은 자동 3단: 오른쪽 아래 2도트 그늘, 왼쪽 위 1도트 하이라이트
# - 외곽선 1도트, 색은 이웃한 칠의 동색 어두운 톤. 겹친 부위 경계에도 위 부위의 동색 선
# - 정체는 견종 실루엣(귀 · 주둥이 · 꼬리 · 무늬) + 직업 소품 2~3개(모자 · 옷 · 무기)로 읽힌다
#
# 사용: python build_heroes.py [--preview] [--only <키>]
#   --preview  Assets 에 쓰지 않고 미리보기 라인업만 만든다

from PIL import Image
import os, sys
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, "Assets", "_Art", "Sprites")
LOBBY = os.path.join(ROOT, "Assets", "Resources", "Lobby")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview")

UP = 4
PORTRAIT_UP = 16
PORTRAIT_K = 2.0
HY = 24
NECK = 35
HEM = 52

# ---------------------------------------------------------------- 견종

def ears_back(f, c, hy, cx, view):
    """머리보다 먼저 — 머리 선이 귀 밑동을 가른다."""
    kind = c["ear"]
    fur = c["fur"]
    if c.get("ear_mode") == "hide":
        return
    low = c.get("ear_mode") == "low"
    if view == "side":
        if kind in ("prick", "round"):
            p = f.p(c.get("ear_col", fur))
            if low:
                p.g((cx - 7, hy - 3), (cx - 9, hy - 10), (cx - 2, hy - 7))
            elif kind == "round":
                p.g((cx - 7, hy - 6), (cx - 6, hy - 12), (cx - 1, hy - 8))
            else:
                p.g((cx - 7, hy - 5), (cx - 6, hy - 15), (cx - 1, hy - 8))
        return
    if kind == "prick":
        p = f.p(c.get("ear_col", fur))
        if low:
            p.g((cx - 12, hy - 1), (cx - 14, hy - 10), (cx - 5, hy - 7))
            p.g((cx + 12, hy - 1), (cx + 14, hy - 10), (cx + 5, hy - 7))
        else:
            p.g((cx - 11, hy - 4), (cx - 9, hy - 16), (cx - 2, hy - 8))
            p.g((cx + 11, hy - 4), (cx + 9, hy - 16), (cx + 2, hy - 8))
    elif kind == "round":
        p = f.p(c.get("ear_col", fur))
        p.g((cx - 11, hy - 5), (cx - 9, hy - 12), (cx - 4, hy - 8))
        p.g((cx + 11, hy - 5), (cx + 9, hy - 12), (cx + 4, hy - 8))


def ears_inner(f, c, hy, cx, view):
    if c.get("ear_mode") in ("hide", "low") or view != "down":
        return
    if c["ear"] == "prick":
        p = f.flat(c.get("ear_in", PINK))
        p.g((cx - 9, hy - 7), (cx - 9, hy - 13), (cx - 5, hy - 8))
        p.g((cx + 9, hy - 7), (cx + 9, hy - 13), (cx + 5, hy - 8))


def ears_front(f, c, hy, cx, view):
    """머리 위에 덮이는 귀 (늘어진 귀 · 곱슬 귀 · 접힌 귀)."""
    kind = c["ear"]
    if c.get("ear_mode") == "hide":
        return
    col = c.get("ear_col", c["fur"])
    if view == "side":
        if kind == "flop":
            f.p(col).e(cx - 7, hy - 5, cx, hy + 9 + c.get("ear_len", 0))
        elif kind == "curly":
            f.p(col).e(cx - 8, hy - 4, cx + 1, hy + 9)
        elif kind == "fold":
            f.p(col).g((cx - 5, hy - 8), (cx - 1, hy - 10), (cx, hy - 5))
        return
    if kind == "flop":
        ln = c.get("ear_len", 0)
        f.p(col).e(cx - 16, hy - 6, cx - 9, hy + 8 + ln)
        f.p(col).e(cx + 9, hy - 6, cx + 16, hy + 8 + ln)
    elif kind == "curly":
        f.p(col).e(cx - 17, hy - 5, cx - 8, hy + 10)
        f.p(col).e(cx + 8, hy - 5, cx + 17, hy + 10)
    elif kind == "fold":
        f.p(col).g((cx - 11, hy - 6), (cx - 6, hy - 10), (cx - 4, hy - 5))
        f.p(col).g((cx + 11, hy - 6), (cx + 6, hy - 10), (cx + 4, hy - 5))


def head(f, c, hy, cx, view):
    fur = c["fur"]
    if c.get("mane"):
        f.p(c.get("mane_col", fur)).e(cx - 15, hy - 11, cx + 15, hy + 13)
    ears_back(f, c, hy, cx, view)
    if c.get("puff") and c.get("ear_mode") != "hide":
        f.p(c.get("puff_col", fur)).e(cx - 9, hy - 16, cx + 9, hy - 4)

    if view == "side":
        hp = f.p(c.get("head_col", fur))
        hp.e(cx - 9, hy - 10, cx + 9, hy + 9)
        mz = {"short": 4, "mid": 8, "long": 11}[c["muzzle"]]
        mp = f.p(c.get("muzzle_col", c["cream"]))
        mp.e(cx + 2, hy + 1, cx + 6 + mz, hy + 8)
        mp.r(cx + 2, hy + 1, cx + 4 + mz, hy + 5)
        if c.get("side_mark"):
            f.flat(c["side_mark"]).e(cx - 2, hy - 2, cx + 6, hy + 6)
        tip = cx + 5 + mz
        f.flat(BLACK).r(tip - 1, hy + 1, tip, hy + 3)
        f.flat(BLACK).l((cx + 4, hy + 7), (tip - 2, hy + 6))
        if c.get("tongue"):
            f.flat(c["tongue"]).r(tip - 4, hy + 7, tip - 3, hy + 8)
        ec = c.get("eye", BLACK)
        f.flat(ec).r(cx + 2, hy - 2, cx + 3, hy + 1)
        f.flat(WHITE).d1((cx + 2, hy - 2))
        if c.get("brow"):
            f.flat(c["brow"]).r(cx + 1, hy - 5, cx + 3, hy - 4)
        f.flat(BLUSH).r(cx + 3, hy + 4, cx + 4, hy + 4)
        ears_front(f, c, hy, cx, view)
        return

    hp = f.p(c.get("head_col", fur))
    hp.e(cx - 11, hy - 10, cx + 11, hy + 10)
    hp.e(cx - 12, hy - 3, cx + 12, hy + 11)
    if view == "up":
        if c.get("back_mark"):
            f.flat(c["back_mark"]).e(cx - 6, hy - 9, cx + 6, hy + 2)
        ears_front(f, c, hy, cx, view)
        return

    if c.get("cheeks"):
        ck = f.p(c["cheeks"], line=False)
        ck.e(cx - 11, hy + 1, cx - 2, hy + 10)
        ck.e(cx + 2, hy + 1, cx + 11, hy + 10)
    if c.get("face_mask"):
        f.p(c["face_mask"], line=False).e(cx - 9, hy - 3, cx + 9, hy + 10)
        f.flat(c["face_mask"]).r(cx - 1, hy - 9, cx + 1, hy - 3)
    if c.get("blaze"):
        f.flat(c["blaze"]).r(cx - 1, hy - 10, cx + 1, hy + 1)
    mw = {"short": 6, "mid": 5, "long": 5}[c["muzzle"]]
    mh = {"short": 6, "mid": 7, "long": 8}[c["muzzle"]]
    f.p(c.get("muzzle_col", c["cream"]), line=False).e(cx - mw, hy + 2, cx + mw, hy + 2 + mh)
    f.flat(BLACK).r(cx - 1, hy + 3, cx + 1, hy + 4)
    f.flat((120, 110, 124)).d1((cx - 1, hy + 3))
    mc = c.get("mouth", ink(c.get("muzzle_col", c["cream"])))
    f.flat(mc).d1((cx, hy + 5), (cx - 1, hy + 6), (cx - 2, hy + 6), (cx + 1, hy + 6), (cx + 2, hy + 6))
    if c.get("tongue"):
        f.flat(c["tongue"]).r(cx - 1, hy + 7, cx + 1, hy + 8)
    ec = c.get("eye", BLACK)
    for ex in (cx - 6, cx + 5):
        f.flat(ec).r(ex, hy - 1, ex + 1, hy + 1)
        f.flat(WHITE).d1((ex, hy - 1))
    if c.get("brow"):
        f.flat(c["brow"]).r(cx - 7, hy - 4, cx - 5, hy - 4)
        f.flat(c["brow"]).r(cx + 5, hy - 4, cx + 7, hy - 4)
    f.flat(BLUSH).r(cx - 9, hy + 4, cx - 8, hy + 4)
    f.flat(BLUSH).r(cx + 8, hy + 4, cx + 9, hy + 4)
    ears_front(f, c, hy, cx, view)
    ears_inner(f, c, hy, cx, view)


def tail(f, c, b, cx, view):
    kind = c["tail"]
    col = c.get("tail_col", c["fur"])
    if view == "side":
        x = cx - 9
        y = HEM - 8 + b
        if kind == "curl":
            p = f.p(col)
            p.e(x - 8, y - 9, x, y - 1)
            f.flat(c.get("tail_tip", c["cream"])).e(x - 6, y - 7, x - 3, y - 4)
        elif kind == "pom":
            f.p(col).e(x - 8, y - 10, x - 1, y - 3)
            f.p(col).l((x - 1, y), (x - 4, y - 4), w=2)
        else:
            f.p(col).g((x, y - 2), (x - 9, y - 9), (x - 8, y - 11), (x + 1, y - 4))
            if c.get("tail_tip"):
                f.flat(c["tail_tip"]).r(x - 9, y - 11, x - 7, y - 9)
        return
    if view == "up":
        y = HEM - 8 + b
        if kind == "curl":
            f.p(col).e(cx - 5, y - 9, cx + 5, y + 1)
            f.flat(c.get("tail_tip", c["cream"])).e(cx - 2, y - 6, cx + 2, y - 2)
        elif kind == "pom":
            f.p(col).e(cx - 4, y - 6, cx + 4, y + 2)
        else:
            f.p(col).g((cx - 2, y + 2), (cx + 2, y + 2), (cx + 7, y - 10), (cx + 5, y - 11))
            if c.get("tail_tip"):
                f.flat(c["tail_tip"]).r(cx + 5, y - 11, cx + 7, y - 9)
        return
    # 정면 — 몸 뒤로 살짝 보인다
    y = HEM - 8 + b
    if kind == "curl":
        f.p(col).e(cx + 7, y - 10, cx + 15, y - 2)
    elif kind == "pom":
        f.p(col).e(cx + 9, y - 6, cx + 15, y)
    else:
        f.p(col).g((cx + 6, y), (cx + 15, y - 9), (cx + 16, y - 7), (cx + 8, y + 2))


def legs(f, c, b, cx, view, ph):
    fur = c.get("leg_col", c["fur"])
    paw = c.get("paw", c["cream"])
    top = HEM - 3 + c.get("leg_drop", 0)
    if view == "side":
        st = int(round(3.5 * ph))
        for x0, s in ((cx - 7, -st), (cx + 3, st)):
            lift = 2 if s < 0 and abs(st) > 1 else 0
            f.p(fur).r(x0 + s, top + b, x0 + s + 3, FOOT - 2 - lift)
            f.p(paw).e(x0 + s - 1, FOOT - 4 - lift, x0 + s + 5, FOOT - lift)
        return
    lift_l = max(0, int(round(3 * ph)))
    lift_r = max(0, int(round(-3 * ph)))
    for x0, lift in ((cx - 6, lift_l), (cx + 2, lift_r)):
        f.p(fur).r(x0, top + b, x0 + 4, FOOT - 2 - lift)
        f.p(paw).e(x0 - 1, FOOT - 4 - lift, x0 + 5, FOOT - lift)


def torso(f, c, b, cx, view):
    fur = c["fur"]
    if view == "side":
        f.p(fur).e(cx - 9, NECK - 1 + b, cx + 7, HEM + 1 + b + c.get("leg_drop", 0))
        return
    f.p(fur).e(cx - 8, NECK - 1 + b, cx + 8, HEM + 1 + b + c.get("leg_drop", 0))
    if view == "down":
        f.p(c["cream"], line=False).e(cx - 4, NECK + 3 + b, cx + 4, HEM + b)


# ---------------------------------------------------------------- 옷 (몸 위)

def robe_shape(p, cx, b, top, hem, flare, view):
    if view == "side":
        p.g((cx - 8, top + b), (cx + 6, top + b), (cx + 7 + flare // 2, hem), (cx - 10 - flare // 2, hem))
    else:
        p.g((cx - 8, top + b), (cx + 8, top + b), (cx + 8 + flare, hem), (cx - 8 - flare, hem))


def garment(f, c, b, cx, view):
    g = c["outfit"]
    kind = g["kind"]
    hem = g.get("hem", HEM) + b
    robe_shape(f.p(g["main"]), cx, b, NECK - 1, hem, g.get("flare", 1), view)
    if view == "down":
        if g.get("inner"):
            f.flat(g["inner"]).g((cx - 3, NECK - 1 + b), (cx + 3, NECK - 1 + b), (cx, NECK + 5 + b))
        if g.get("trim"):
            tr = f.flat(g["trim"])
            tr.r(cx - 9 - g.get("flare", 1), hem - 1, cx + 9 + g.get("flare", 1), hem)
            tr.l((cx - 4, NECK - 1 + b), (cx, NECK + 5 + b))
            tr.l((cx + 4, NECK - 1 + b), (cx, NECK + 5 + b))
    if g.get("belt"):
        y = NECK + 8 + b
        if view == "side":
            f.p(g["belt"], line=False).r(cx - 9, y, cx + 7, y + 1)
        else:
            f.p(g["belt"], line=False).r(cx - 9, y, cx + 9, y + 1)
            if g.get("buckle"):
                f.flat(g["buckle"]).r(cx - 1, y, cx + 1, y + 1)
            if kind == "gi" and view == "down":
                f.p(g["belt"]).g((cx + 3, y + 1), (cx + 6, y + 6), (cx + 4, y + 6), (cx + 2, y + 2))
    if kind == "kasaya" and view != "side":
        sx = 1 if view == "down" else -1
        f.p(g["sash"]).g((cx - 8 * sx, NECK - 1 + b), (cx - 4 * sx, NECK - 1 + b),
                         (cx + 9 * sx, hem - 3), (cx + 5 * sx, hem))
    if kind == "armor":
        f.p(g["plate"]).e(cx - 7, NECK + b, cx + 7, NECK + 9 + b)
        if view == "down":
            f.p(g["tabard"]).r(cx - 3, NECK + 6 + b, cx + 3, hem)
            f.flat(GOLD).r(cx - 1, NECK + 8 + b, cx, NECK + 14 + b)
            f.flat(GOLD).r(cx - 2, NECK + 10 + b, cx + 1, NECK + 10 + b)
        for sx in ((-1, 1) if view != "side" else (1,)):
            f.p(g["plate"]).e(cx + sx * 10 - 4, NECK - 3 + b, cx + sx * 10 + 3, NECK + 3 + b)
    if kind == "coat" and view == "down":
        for y in (NECK + 3, NECK + 6, NECK + 11, NECK + 14):
            f.flat(GOLD).d1((cx - 3, y + b), (cx + 3, y + b))
    if g.get("stars") and view != "up":
        f.flat(GOLD).d1((cx - 5, NECK + 12 + b), (cx + 4, NECK + 15 + b), (cx - 2, hem - 3))
    if g.get("scarf"):
        sc = f.p(g["scarf"])
        sc.r(cx - 8, NECK - 2 + b, cx + 8, NECK + 1 + b)
        if view == "side":
            sc.g((cx - 8, NECK - 1 + b), (cx - 17, NECK + 2 + b), (cx - 16, NECK + 4 + b), (cx - 7, NECK + 2 + b))
        elif view == "up":
            sc.g((cx - 2, NECK + b), (cx + 2, NECK + b), (cx + 3, NECK + 9 + b), (cx - 1, NECK + 8 + b))
        else:
            sc.g((cx - 7, NECK + b), (cx - 12, NECK + 6 + b), (cx - 10, NECK + 7 + b), (cx - 5, NECK + 2 + b))
    if g.get("beads") and view == "down":
        bp = f.p(g["beads"], light=False)
        for i in range(-3, 4):
            x = cx + i * 2
            y = NECK + 1 + abs(i) * -1 + 4 + b
            bp.e(x - 1, y - 1, x + 1, y + 1)
    if g.get("fur_collar"):
        fc = f.p(g["fur_collar"])
        fc.e(cx - 11, NECK - 3 + b, cx + 11, NECK + 4 + b)


def cape_back(f, c, b, cx, view):
    """몸 뒤 망토 (정면 · 옆에서 몸 뒤로 보인다)."""
    cp = c.get("cape")
    if not cp or view == "up":
        return
    p = f.p(cp["col"])
    hem = HEM + cp.get("drop", 4) + b
    if view == "side":
        p.g((cx - 7, NECK - 2 + b), (cx + 1, NECK - 2 + b), (cx - 6, hem), (cx - 17, hem + 1))
    else:
        p.g((cx - 10, NECK - 2 + b), (cx + 10, NECK - 2 + b), (cx + 14, hem), (cx - 14, hem))


def cape_front(f, c, b, cx, view):
    """어깨를 감싸는 망토 앞자락 · 뒷모습 전체."""
    cp = c.get("cape")
    if not cp:
        return
    hem = HEM + cp.get("drop", 4) + b
    if view == "up":
        f.p(cp["col"]).g((cx - 11, NECK - 3 + b), (cx + 11, NECK - 3 + b), (cx + 14, hem), (cx - 14, hem))
        if cp.get("edge"):
            f.flat(cp["edge"]).r(cx - 14, hem - 1, cx + 14, hem)
        return
    p = f.p(cp["col"])
    if view == "side":
        p.g((cx - 9, NECK - 3 + b), (cx + 5, NECK - 3 + b), (cx + 5, NECK + 3 + b), (cx - 9, NECK + 6 + b))
    else:
        p.g((cx - 12, NECK - 3 + b), (cx + 12, NECK - 3 + b), (cx + 11, NECK + 5 + b),
            (cx + 2, NECK + 2 + b), (cx - 2, NECK + 2 + b), (cx - 11, NECK + 5 + b))
    if cp.get("edge"):
        e = f.flat(cp["edge"])
        if view == "side":
            e.l((cx - 9, NECK + 6 + b), (cx + 5, NECK + 3 + b))
        else:
            e.l((cx - 11, NECK + 5 + b), (cx - 2, NECK + 2 + b))
            e.l((cx + 2, NECK + 2 + b), (cx + 11, NECK + 5 + b))


def paws(f, c, b, cx, view, ph):
    col = c.get("arm_col", c["fur"])
    y = NECK + 7 + b
    if view == "side":
        f.p(col).e(cx + 2, y - 1, cx + 7, y + 4)
        return []
    f.p(col).e(cx - 13, y - 1, cx - 8, y + 4)
    f.p(col).e(cx + 8, y - 1, cx + 13, y + 4)
    return [(cx - 11, y + 1), (cx + 10, y + 1)]


# ---------------------------------------------------------------- 모자

def hat(f, c, hy, cx, view):
    k = c.get("hat")
    if k == "kasa":
        f.p((208, 164, 88)).g((cx - 22, hy - 6), (cx, hy - 19), (cx + 22, hy - 6))
        f.p((168, 124, 60), light=False).r(cx - 19, hy - 7, cx + 19, hy - 5)
        tx = f.flat((176, 132, 66))
        for i in range(-3, 4):
            tx.l((cx + i * 5, hy - 8), (cx + i * 2, hy - 15))
    elif k == "gat":
        f.p((44, 40, 52)).e(cx - 21, hy - 10, cx + 21, hy - 5)
        f.p((40, 36, 48)).r(cx - 6, hy - 19, cx + 6, hy - 8)
        f.flat((90, 84, 104)).r(cx - 6, hy - 11, cx + 6, hy - 10)
    elif k == "hood":
        hp = f.p(c["hood_col"])
        if view == "side":
            hp.e(cx - 10, hy - 11, cx + 10, hy + 10)
            hp.g((cx - 7, hy - 6), (cx - 6, hy - 16), (cx - 1, hy - 9))
            f.p(c["fur"], line=False).r(cx + 1, hy - 3, cx + 10, hy + 1)
            f.flat(BLACK).r(cx + 3, hy - 2, cx + 4, hy)
            f.flat(WHITE).d1((cx + 3, hy - 2))
            f.p(c["hood_col"], line=False).e(cx + 1, hy + 1, cx + 14, hy + 8)
            f.flat(BLACK).r(cx + 12, hy + 2, cx + 13, hy + 3)
            return
        hp.e(cx - 13, hy - 12, cx + 13, hy + 12)
        hp.g((cx - 12, hy - 4), (cx - 10, hy - 17), (cx - 3, hy - 9))
        hp.g((cx + 12, hy - 4), (cx + 10, hy - 17), (cx + 3, hy - 9))
        if view == "down":
            f.p(c["fur"], line=False).r(cx - 10, hy - 4, cx + 10, hy + 2)
            for ex in (cx - 6, cx + 4):
                f.flat(BLACK).r(ex, hy - 3, ex + 2, hy + 1)
                f.flat(WHITE).d1((ex, hy - 3))
            f.flat(c["fur"]).r(cx - 7, hy - 5, cx - 5, hy - 4)
            f.flat(c["fur"]).r(cx + 5, hy - 5, cx + 7, hy - 4)
        if c.get("hood_band"):
            f.flat(c["hood_band"]).r(cx - 12, hy - 6, cx + 12, hy - 5)
    elif k == "daoguan":
        f.p((40, 36, 52)).r(cx - 6, hy - 14, cx + 6, hy - 8)
        f.p((40, 36, 52)).e(cx - 6, hy - 17, cx + 6, hy - 11)
        f.flat((90, 200, 150)).r(cx - 1, hy - 13, cx + 1, hy - 11)
        if view != "side":
            f.p((40, 36, 52)).r(cx - 11, hy - 11, cx - 7, hy - 10)
            f.p((40, 36, 52)).r(cx + 7, hy - 11, cx + 11, hy - 10)
    elif k == "circlet":
        f.p(GOLD, light=False).r(cx - 10, hy - 7, cx + 10, hy - 6)
        if view == "down":
            f.flat((70, 140, 230)).r(cx - 1, hy - 8, cx + 1, hy - 6)
    elif k == "wizard":
        col = c["hat_col"]
        f.p(col).e(cx - 16, hy - 12, cx + 16, hy - 6)
        if view == "side":
            f.p(col).g((cx - 9, hy - 10), (cx + 7, hy - 10), (cx + 4, hy - 19), (cx + 11, hy - 24), (cx - 2, hy - 20))
        else:
            f.p(col).g((cx - 10, hy - 10), (cx + 10, hy - 10), (cx + 5, hy - 19), (cx + 12, hy - 24), (cx - 1, hy - 21))
        f.p(GOLD, line=False).r(cx - 9, hy - 12, cx + 9, hy - 11)
        f.flat((255, 236, 120)).d1((cx - 3, hy - 16), (cx - 2, hy - 15), (cx - 4, hy - 15), (cx - 3, hy - 14), (cx + 3, hy - 19))
    elif k == "viking":
        horn = (236, 226, 196)
        if view != "side":
            f.p(horn).g((cx - 9, hy - 8), (cx - 15, hy - 10), (cx - 18, hy - 17), (cx - 16, hy - 18),
                        (cx - 13, hy - 13), (cx - 7, hy - 12))
            f.p(horn).g((cx + 9, hy - 8), (cx + 15, hy - 10), (cx + 18, hy - 17), (cx + 16, hy - 18),
                        (cx + 13, hy - 13), (cx + 7, hy - 12))
        else:
            f.p(horn).g((cx - 4, hy - 10), (cx - 9, hy - 14), (cx - 10, hy - 20), (cx - 7, hy - 19),
                        (cx - 5, hy - 15), (cx, hy - 12))
        f.p(IRON).e(cx - 11, hy - 15, cx + 11, hy - 1)
        f.p((120, 124, 140), light=False).r(cx - 12, hy - 5, cx + 12, hy - 3)
        if view == "down":
            f.p((120, 124, 140)).r(cx - 1, hy - 5, cx + 1, hy + 3)
            f.flat(GOLD).d1((cx - 8, hy - 4), (cx + 8, hy - 4), (cx - 4, hy - 4), (cx + 4, hy - 4))
    elif k == "ranger":
        col = (70, 130, 60)
        if view == "side":
            f.p(col).g((cx - 11, hy - 6), (cx + 10, hy - 6), (cx + 13, hy - 9), (cx - 2, hy - 15), (cx - 9, hy - 12))
        else:
            f.p(col).g((cx - 12, hy - 6), (cx + 12, hy - 6), (cx + 9, hy - 13), (cx - 1, hy - 16), (cx - 9, hy - 13))
        f.p((120, 80, 40), light=False).r(cx - 11, hy - 7, cx + 11, hy - 6)
        f.p((214, 50, 50)).g((cx + 4, hy - 11), (cx + 15, hy - 22), (cx + 16, hy - 20), (cx + 6, hy - 9))
    elif k == "tricorne":
        col = (40, 34, 46)
        if view == "side":
            f.p(col).g((cx - 12, hy - 6), (cx + 12, hy - 6), (cx + 9, hy - 12), (cx - 2, hy - 15), (cx - 10, hy - 12))
        else:
            f.p(col).g((cx - 16, hy - 6), (cx + 16, hy - 6), (cx + 13, hy - 14), (cx + 5, hy - 11),
                       (cx, hy - 16), (cx - 5, hy - 11), (cx - 13, hy - 14))
        f.flat(GOLD).l((cx - 15, hy - 6), (cx + 15, hy - 6))
        if view == "down":
            f.flat(WHITE).r(cx - 1, hy - 11, cx + 1, hy - 9)
            f.flat(BLACK).r(cx + 4, hy - 2, cx + 7, hy + 1)
            f.flat(BLACK).l((cx - 10, hy - 5), (cx + 4, hy - 1))


# ---------------------------------------------------------------- 무기

def weapon(f, c, b, cx, view, hands, front):
    """front=False 는 몸 뒤에 그리는 패스."""
    k = c["weapon"]
    y = NECK + 8 + b
    side = view == "side"
    if k == "katana":
        if view == "up" and front:
            f.p((50, 46, 62)).l((cx - 14, y + 2), (cx + 14, y - 2), w=2)
            f.p((232, 140, 60), light=False).l((cx + 14, y - 2), (cx + 19, y - 3), w=2)
            return
        if not front:
            return
        x0 = cx - 15 if not side else cx - 12
        f.p((50, 46, 62)).l((x0, y + 3), (cx + 13, y - 1), w=2)
        f.p(GOLD).r(cx + 13, y - 3, cx + 14, y + 1)
        f.p((232, 140, 60)).l((cx + 15, y - 1), (cx + 21, y - 2), w=2)
        f.flat((120, 40, 30)).d1((cx + 16, y - 1), (cx + 18, y - 2), (cx + 20, y - 2))
    elif k == "gakgung":
        if side:
            if not front:
                f.p((150, 60, 40)).l((cx - 10, NECK - 4 + b), (cx + 2, NECK + 15 + b), w=2)
            return
        if view == "up":
            if front:
                f.p((150, 60, 40)).l((cx - 8, NECK - 6 + b), (cx + 6, NECK + 14 + b), w=2)
            return
        if not front:
            return
        x = cx + 14 if not side else cx + 9
        p = f.p((150, 60, 40))
        p.l((x - 1, NECK - 9 + b), (x + 3, NECK - 3 + b), (x + 2, NECK + 7 + b), (x + 3, NECK + 17 + b), (x - 1, NECK + 22 + b), w=2)
        f.flat((240, 230, 200)).l((x - 1, NECK - 8 + b), (x - 1, NECK + 21 + b))
        f.flat((60, 50, 40)).r(x + 1, NECK + 5 + b, x + 3, NECK + 8 + b)
    elif k == "shuriken":
        if not front or view == "up":
            return
        hx, hy2 = (cx + 10, y) if not side else (cx + 8, y)
        p = f.p(SILVER)
        p.g((hx, hy2 - 5), (hx + 1, hy2 - 1), (hx + 5, hy2), (hx + 1, hy2 + 1), (hx, hy2 + 5),
            (hx - 1, hy2 + 1), (hx - 5, hy2), (hx - 1, hy2 - 1))
        f.flat(BLACK).d1((hx, hy2))
        if view == "down":
            f.p((60, 50, 70)).l((cx - 13, NECK - 4 + b), (cx - 18, NECK - 12 + b), w=2)
    elif k == "staff":
        if not front and view != "up":
            return
        if view == "up" and front:
            return
        x = cx + 14 if not side else cx + 6
        f.p(WOOD).l((x, 10 + b), (x, FOOT - 2), w=2)
        f.flat(RED).r(x - 1, 12 + b, x + 1, 13 + b)
        f.flat(RED).r(x - 1, FOOT - 6, x + 1, FOOT - 5)
    elif k == "talisman":
        if not front or view == "up":
            if view == "up" and front:
                f.p((200, 160, 90)).e(cx + 4, NECK + 8 + b, cx + 9, NECK + 15 + b)
            return
        hx = cx + 10 if not side else cx + 7
        f.p((250, 222, 90)).r(hx - 2, y - 9, hx + 3, y + 1)
        f.flat((200, 40, 40)).l((hx, y - 8), (hx + 1, y - 5), (hx - 1, y - 3), (hx + 1, y - 1))
        if view == "down":
            g = f.p((200, 160, 90))
            g.e(cx - 13, NECK + 9 + b, cx - 8, NECK + 15 + b)
            g.e(cx - 12, NECK + 6 + b, cx - 9, NECK + 10 + b)
            f.flat(RED).r(cx - 12, NECK + 9 + b, cx - 9, NECK + 9 + b)
    elif k == "paladin":
        if view == "up":
            if front:
                f.p((60, 90, 180)).e(cx - 9, NECK + 1 + b, cx + 9, NECK + 17 + b)
                f.p(GOLD, line=False, shade=False).r(cx - 1, NECK + 3 + b, cx + 1, NECK + 15 + b)
            return
        if not front:
            return
        x = cx + 14 if not side else cx + 9
        top = y - 16 if not side else y - 7
        f.p(WOOD).l((x, y + 8), (x + 1, top + 6), w=2)
        f.p(SILVER).r(x - 4, top, x + 5, top + 6)
        f.flat(GOLD).r(x - 4, top + 3, x + 5, top + 3)
        if view == "down":
            f.p((60, 90, 180)).e(cx - 20, NECK + 1 + b, cx - 8, NECK + 15 + b)
            f.p(GOLD, line=False, shade=False).r(cx - 15, NECK + 3 + b, cx - 13, NECK + 13 + b)
            f.p(GOLD, line=False, shade=False).r(cx - 18, NECK + 6 + b, cx - 10, NECK + 8 + b)
    elif k == "wand":
        if view == "up" and not front:
            return
        if view != "up" and not front:
            return
        x = cx + 14 if not side else cx + 7
        if view == "up":
            x = cx - 14
        f.p(WOOD).l((x, 18 + b), (x - 1, FOOT - 2), w=2)
        f.p((90, 220, 240)).e(x - 3, 12 + b, x + 3, 18 + b)
        f.flat(WHITE).d1((x - 1, 13 + b))
    elif k == "axe":
        if not front:
            return
        x = cx + 14 if not side else cx + 9
        if view == "up":
            x = cx - 14
        t = y - 13 if not side else y - 5
        f.p(WOOD).l((x, y + 10), (x + 1, t + 1), w=2)
        f.p(SILVER).g((x + 1, t), (x + 8, t - 4), (x + 9, t + 4), (x + 8, t + 9), (x + 1, t + 6))
    elif k == "longbow":
        if side:
            if not front:
                f.p((140, 96, 50)).l((cx - 11, NECK - 8 + b), (cx + 3, NECK + 18 + b), w=2)
                f.p((120, 80, 40)).r(cx - 9, NECK - 2 + b, cx - 5, NECK + 9 + b)
            return
        if view == "up":
            if front:
                f.p((120, 80, 40)).r(cx + 2, NECK + 1 + b, cx + 6, NECK + 14 + b)
                f.flat((230, 230, 230)).d1((cx + 3, NECK - 1 + b), (cx + 5, NECK + b), (cx + 4, NECK - 2 + b))
                f.p((140, 96, 50)).l((cx - 10, NECK - 10 + b), (cx + 10, NECK + 18 + b), w=2)
            return
        if not front:
            if view == "down":
                f.p((120, 80, 40)).r(cx - 13, NECK - 4 + b, cx - 9, NECK + 8 + b)
            return
        x = cx + 15 if not side else cx + 9
        p = f.p((140, 96, 50))
        p.l((x - 2, NECK - 14 + b), (x + 2, NECK - 6 + b), (x + 3, NECK + 7 + b), (x + 2, NECK + 19 + b), (x - 2, NECK + 25 + b), w=2)
        f.flat((240, 236, 220)).l((x - 2, NECK - 13 + b), (x - 2, NECK + 24 + b))
    elif k == "cannon":
        if not front or view == "up":
            if view == "up" and front:
                f.p(IRON).l((cx + 6, NECK + 2 + b), (cx + 12, NECK + 14 + b), w=3)
            return
        if side:
            f.p(IRON).r(cx + 2, y - 2, cx + 18, y + 2)
            f.p(GOLD).r(cx + 6, y - 3, cx + 7, y + 3)
            f.p(GOLD).r(cx + 17, y - 3, cx + 19, y + 3)
            f.p(WOOD).r(cx + 1, y, cx + 4, y + 5)
        else:
            f.p(IRON).r(cx + 6, y - 3, cx + 20, y + 1)
            f.p(GOLD).r(cx + 11, y - 4, cx + 12, y + 2)
            f.p(GOLD).r(cx + 19, y - 4, cx + 21, y + 2)
            f.p(WOOD).r(cx + 6, y, cx + 9, y + 5)
            f.flat(BLACK).r(cx + 20, y - 2, cx + 21, y)


# ---------------------------------------------------------------- 조립

def draw(c, view, ph, b, k=1.0):
    # k = 그림 배율. 게임 스프라이트는 1, 로비 초상화는 촘촘한 도트(PORTRAIT_K)로 같은 도안을 다시 그린다
    f = Fig(k)
    cx = 32 if view != "side" else 31
    hy = HY + b
    if view == "up":
        weapon(f, c, b, cx, view, [], False)
        legs(f, c, b, cx, view, ph)
        torso(f, c, b, cx, view)
        garment(f, c, b, cx, view)
        paws(f, c, b, cx, view, ph)
        cape_front(f, c, b, cx, view)
        tail(f, c, b, cx, view)
        weapon(f, c, b, cx, view, [], True)
        head(f, c, hy, cx, view)
        hat(f, c, hy, cx, view)
        return rim(render(f), HERO_RIM)

    cape_back(f, c, b, cx, view)
    tail(f, c, b, cx, view)
    weapon(f, c, b, cx, view, [], False)
    legs(f, c, b, cx, view, ph)
    torso(f, c, b, cx, view)
    garment(f, c, b, cx, view)
    cape_front(f, c, b, cx, view)
    head(f, c, hy, cx, view)
    hat(f, c, hy, cx, view)
    hands = paws(f, c, b, cx, view, ph)
    weapon(f, c, b, cx, view, hands, True)
    return rim(render(f, shadow_cx=cx), HERO_RIM)


# ---------------------------------------------------------------- 캐릭터 10 (기획서 캐릭터 표 순서)

CREAM = (252, 238, 214)

HEROES = {
    "shiba": dict(
        fur=(222, 124, 54), cream=CREAM, cheeks=CREAM, brow=CREAM, ear="prick", ear_mode="low",
        muzzle="mid", tail="curl", hat="kasa", weapon="katana",
        outfit=dict(kind="gi", main=(58, 60, 82), inner=CREAM, belt=RED),
        cape=dict(col=(88, 156, 56), drop=3, edge=(150, 200, 90))),
    "jindo": dict(
        fur=(246, 240, 228), cream=(255, 252, 244), ear="prick", ear_mode="low",
        muzzle="mid", tail="curl", hat="gat", weapon="gakgung", ear_in=(240, 170, 160),
        outfit=dict(kind="robe", main=(52, 72, 146), inner=WHITE, belt=RED, hem=56, flare=4)),
    "akita": dict(
        fur=(204, 102, 52), cream=CREAM, cheeks=CREAM, ear="prick", ear_mode="hide",
        muzzle="mid", tail="curl", hat="hood", hood_col=(58, 50, 78), weapon="shuriken",
        outfit=dict(kind="robe", main=(58, 50, 78), belt=(40, 34, 54), scarf=(206, 44, 56))),
    "chow": dict(
        fur=(214, 146, 72), cream=(244, 200, 140), mane=True, mane_col=(226, 160, 84), ear="round",
        muzzle="short", tail="curl", tongue=(110, 70, 150), weapon="staff",
        outfit=dict(kind="kasaya", main=(240, 156, 44), sash=(182, 52, 42), beads=(110, 64, 40), hem=56, flare=3)),
    "pug": dict(
        fur=(230, 196, 146), cream=(240, 214, 172), muzzle_col=(62, 52, 56), ear="fold", ear_col=(62, 52, 56),
        muzzle="short", tail="curl", hat="daoguan", weapon="talisman", mouth=(30, 24, 28),
        outfit=dict(kind="robe", main=(206, 220, 238), trim=(46, 44, 60), belt=(46, 44, 60), hem=57, flare=4)),
    "shepherd": dict(
        fur=(204, 146, 82), cream=(236, 200, 150), muzzle_col=(70, 56, 52), ear="prick",
        muzzle="long", tail="straight", tail_tip=(60, 48, 46), hat="circlet", weapon="paladin",
        back_mark=(70, 56, 52),
        outfit=dict(kind="armor", main=(188, 198, 214), plate=(214, 222, 234), tabard=(60, 92, 184), belt=GOLD)),
    "poodle": dict(
        fur=(248, 244, 252), cream=(255, 252, 255), puff=True, ear="curly", ear_mode="normal",
        muzzle="mid", tail="pom", hat="wizard", hat_col=(84, 66, 176), weapon="wand",
        outfit=dict(kind="robe", main=(84, 66, 176), belt=GOLD, buckle=(90, 220, 240), stars=True, hem=57, flare=4)),
    "husky": dict(
        fur=(118, 130, 152), cream=WHITE, face_mask=(246, 248, 252), ear="prick", ear_mode="hide",
        muzzle="mid", tail="curl", tail_tip=WHITE, eye=(70, 150, 230), hat="viking", weapon="axe",
        outfit=dict(kind="tunic", main=(124, 86, 56), belt=(80, 52, 36), buckle=GOLD, fur_collar=(160, 128, 96)),
        cape=dict(col=(150, 112, 74), drop=2)),
    "beagle": dict(
        fur=(250, 246, 238), cream=WHITE, head_col=(188, 116, 52), blaze=WHITE, ear="flop", ear_col=(160, 92, 40),
        ear_len=2, muzzle="mid", tail="straight", tail_tip=WHITE, hat="ranger", weapon="longbow",
        side_mark=(250, 246, 238),
        outfit=dict(kind="tunic", main=(72, 130, 62), belt=(110, 74, 40), buckle=GOLD),
        cape=dict(col=(124, 92, 54), drop=3)),
    "dachshund": dict(
        fur=(160, 74, 40), cream=(214, 140, 90), ear="flop", ear_len=5, muzzle="long", tail="straight",
        leg_drop=3, hat="tricorne", weapon="cannon",
        outfit=dict(kind="coat", main=(178, 40, 44), inner=WHITE, belt=(50, 40, 40), buckle=GOLD, trim=GOLD, hem=55, flare=2)),
}

WALK = [(-1.0, 0), (-0.4, -1), (1.0, 0), (0.4, -1)]
VIEWS = (("down", ""), ("up", "up"), ("side", "side"))



def portrait(c):
    """로비 초상화 — 같은 도안을 PORTRAIT_K 배 촘촘한 도트로 다시 그려 상반신을 자르고 밝은 바깥선 · 최근접 확대.
    게임 스프라이트(64 도트)를 그대로 키운 것보다 얼굴 · 무늬 계단이 곱다. 768px (카드 · 시작 화면 공용)"""
    k = PORTRAIT_K
    img = draw(c, "down", 0, 0, k)
    s = int(round(48 * k))
    x0 = int(round(8 * k))
    crop = img.crop((x0, 0, x0 + s, s))
    up = max(1, 768 // s)
    return big(crop, up).resize((768, 768), Image.NEAREST)


def main():
    preview_only = "--preview" in sys.argv
    keys = list(HEROES.keys())
    if "--only" in sys.argv:
        keys = [sys.argv[sys.argv.index("--only") + 1]]
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    if not preview_only:
        os.makedirs(DST, exist_ok=True)
        os.makedirs(LOBBY, exist_ok=True)

    T = W * UP
    sheet = Image.new("RGBA", (T * 5, T * 3 * len(keys)), (72, 82, 104, 255))
    n = 0
    for r, key in enumerate(keys):
        c = HEROES[key]
        for vi, (view, suffix) in enumerate(VIEWS):
            img = draw(c, view, 0, 0)
            sheet.alpha_composite(big(img, UP), (0, (r * 3 + vi) * T))
            if not preview_only:
                big(img, UP).save(os.path.join(DST, "dog_%s_idle%s.png" % (key, suffix)))
            n += 1
            for i, (ph, b) in enumerate(WALK):
                img = draw(c, view, ph, b)
                sheet.alpha_composite(big(img, UP), ((i + 1) * T, (r * 3 + vi) * T))
                if not preview_only:
                    big(img, UP).save(os.path.join(DST, "dog_%s_walk%s%d.png" % (key, suffix, i)))
                n += 1
        if not preview_only:
            portrait(c).save(os.path.join(LOBBY, "portrait_%s.png" % key))

    # 정면 대기 라인업 (원본 배율 · ×4) — 사람 확인용
    line = Image.new("RGBA", (W * len(keys), H), (72, 82, 104, 255))
    for i, key in enumerate(keys):
        line.alpha_composite(draw(HEROES[key], "down", 0, 0), (i * W, 0))
    big(line, UP).save(os.path.join(PREVIEW_DIR, "lineup_front.png"))
    sheet.save(os.path.join(PREVIEW_DIR, "lineup_sheet.png"))
    print("주인공 %d종 · 스프라이트 %d장%s · 미리보기 %s" % (len(keys), n, " (미리보기만)" if preview_only else "", PREVIEW_DIR))


main()
