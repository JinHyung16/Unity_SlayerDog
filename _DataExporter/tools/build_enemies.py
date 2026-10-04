# -*- coding: utf-8 -*-
# 적 스프라이트 생성기 - 「개많이잡아」 아트 리부트 적 (.claude/requests.md 1번)
#
# 레퍼 3종 + 창작 2종을 기존 적 키 자리에 넣는다 (데이터 · SpriteSetLibrary 키 그대로):
#   grunt   기본       → 솜구름 (찡그린 노란 구름)              레퍼 ①
#   brute   크고 단단  → 목마 카우보이 (나무 목마 + 모자)       레퍼 ②
#   spitter 원거리     → 베레모 물방울 (시안 물방울 + 보라 베레모) 레퍼 ③
#   swarm   작고 많다  → 슬라임 (말랑한 젤리, 창작)
#   runner  빠르다     → 고스트 (꼬리 휘날리는 유령, 창작)
#   ufo     보스       → 납치선도 같은 그림체로 다시 그린다
#
# 그림 규칙 (레퍼 공통): 굵은 진보라 외곽선 · 파스텔 평면 채색 + 그림자 1단 · 볼터치 · 발밑 그림자 타원 · 떠 있는 느낌
# 주인공(build_heroes)과 같은 외곽선 색 · 같은 캔버스(64 좌표, OUT 4). 적은 외곽선을 더 굵게(2.0) — 물량 속에서 덩어리로 읽히게
# 방향 3(정면 · 뒤 · 옆) × (대기 1 + 걷기 4). 뒤는 얼굴이 없고, 옆은 얼굴이 한쪽으로 몰린다
#
# 사용: python build_enemies.py [--preview]   (--preview 는 라인업만 _Ref 에 낸다)

from PIL import Image, ImageDraw, ImageFilter
import os, sys, math
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\Sprites")
PREVIEW = os.path.join(ROOT, r"_Ref\ArtReboot\lineup_enemies.png")
SS = 4
OUT = 4
S = SS * OUT
W = 64
INK = (64, 30, 104, 255)
INK_PX = 2.0
BLUSH = (255, 140, 180, 255)
WHITE = (255, 255, 255, 255)
EYE = (52, 24, 86, 255)


def layer(w=W):
    im = Image.new("RGBA", (w * S, w * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def ell(d, x, y, rx, ry, c):
    d.ellipse([(x - rx) * S, (y - ry) * S, (x + rx) * S, (y + ry) * S], fill=c)


def poly(d, pts, c):
    d.polygon([(x * S, y * S) for x, y in pts], fill=c)


def rrect(d, x0, y0, x1, y1, r, c):
    d.rounded_rectangle([min(x0, x1) * S, min(y0, y1) * S, max(x0, x1) * S, max(y0, y1) * S], radius=r * S, fill=c)


def seg(d, x0, y0, x1, y1, w, c):
    d.line([(x0 * S, y0 * S), (x1 * S, y1 * S)], fill=c, width=int(w * S))
    ell(d, x0, y0, w / 2, w / 2, c)
    ell(d, x1, y1, w / 2, w / 2, c)


def shrink(im, w=W):
    return im.resize((w * OUT, w * OUT), Image.BOX)


def fin(lay, w=W, px=INK_PX):
    small = shrink(lay, w)
    k = int(round(px * OUT)) * 2 + 1
    grown = small.split()[3].filter(ImageFilter.MaxFilter(k))
    out = Image.new("RGBA", small.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", small.size, INK), (0, 0), grown)
    out.alpha_composite(small)
    return out


def stack(layers, w=W):
    out = Image.new("RGBA", (w * OUT, w * OUT), (0, 0, 0, 0))
    for lay in layers:
        if isinstance(lay, tuple):
            out.alpha_composite(shrink(lay[0], w))
        else:
            out.alpha_composite(fin(lay, w))
    return out


def shade(c, k=0.82):
    return (int(c[0] * k), int(c[1] * k * 0.96), int(min(255, c[2] * k + 18)), 255)


def ground(rx, ry=3.6, y=60.5, a=150, w=W):
    im, d = layer(w)
    cx = w / 2
    d.ellipse([(cx - rx) * S, (y - ry) * S, (cx + rx) * S, (y + ry) * S], fill=(40, 14, 60, a))
    return shrink(im, w)


# ── 얼굴 ────────────────────────────────────────────────────────────────

def face_grumpy(d, cx, cy, side):
    """찡그린 얼굴 — 처진 눈썹 · 실눈 · 뒤집힌 입 (레퍼 ①)."""
    xs = [cx + 4] if side else [cx - 5, cx + 5]
    for x in xs:
        seg(d, x - 2.4, cy - 1.0, x + 2.4, cy - 1.0, 1.4, EYE)
        seg(d, x - 2.6, cy - 4.2, x + 2.0, cy - 2.8 if x < cx else cy - 4.2, 1.1, EYE)
    mx = cx + 5 if side else cx
    d.arc([(mx - 3) * S, (cy + 2.5) * S, (mx + 3) * S, (cy + 7) * S], 200, 340, fill=EYE, width=int(1.3 * S))
    for x in ([cx + 9] if side else [cx - 9, cx + 9]):
        ell(d, x, cy + 2.5, 2.4, 1.4, BLUSH)


def face_happy(d, cx, cy, side, big=False):
    """웃는 얼굴 — 반달 눈 · 크게 벌린 입 (레퍼 ②)."""
    xs = [cx + 4] if side else [cx - 4.5, cx + 4.5]
    for x in xs:
        d.arc([(x - 2.2) * S, (cy - 3) * S, (x + 2.2) * S, (cy + 1) * S], 200, 340, fill=EYE, width=int(1.4 * S))
    mx = cx + 4.5 if side else cx
    d.chord([(mx - 3.4) * S, (cy + 0.5) * S, (mx + 3.4) * S, (cy + 7) * S], 0, 180, fill=EYE)
    ell(d, mx, cy + 5.2, 1.8, 1.0, (255, 120, 150, 255))
    for x in ([cx + 8.5] if side else [cx - 8, cx + 8]):
        ell(d, x, cy + 2.4, 2.0, 1.2, BLUSH)


def face_oval(d, cx, cy, side):
    """흰 타원 눈 + 삼각 입 (레퍼 ③)."""
    xs = [cx + 4.5] if side else [cx - 5, cx + 5]
    for x in xs:
        ell(d, x, cy - 1, 2.6, 3.8, WHITE)
        ell(d, x + 0.4, cy - 0.4, 1.0, 1.6, EYE)
    mx = cx + 4 if side else cx
    poly(d, [(mx - 2.4, cy + 6), (mx + 2.4, cy + 6), (mx, cy + 2.6)], (60, 90, 200, 255))


def face_dot(d, cx, cy, side, mood="smile"):
    xs = [cx + 3.6] if side else [cx - 3.6, cx + 3.6]
    for x in xs:
        ell(d, x, cy, 1.4, 1.9, EYE)
        ell(d, x - 0.4, cy - 0.7, 0.5, 0.5, WHITE)
    mx = cx + 3.8 if side else cx
    if mood == "smile":
        d.arc([(mx - 1.8) * S, (cy + 0.6) * S, (mx + 1.8) * S, (cy + 3.6) * S], 20, 160, fill=EYE, width=int(1.1 * S))
    else:
        ell(d, mx, cy + 3, 1.4, 1.8, EYE)
    for x in ([cx + 7] if side else [cx - 6.5, cx + 6.5]):
        ell(d, x, cy + 2.2, 1.6, 1.0, BLUSH)


# ── 적 ──────────────────────────────────────────────────────────────────

def cloud(face, ph, bob):
    """솜구름 — 몽글 원 6개. 걸음 대신 몽글거림 (좌우 출렁)."""
    c = (255, 240, 150, 255)
    cy = 38 + bob * 0.6
    wob = ph * 1.2
    body, d = layer()
    for dx, dy, r in ((-9, 2, 8), (0, -3, 10), (9, 2, 8), (-4, 7, 8), (5, 7, 8), (0, 2, 10)):
        ell(d, 32 + dx + wob * (1 if dx > 0 else -1) * 0.3, cy + dy, r, r * 0.92, c)
    sh, d2 = layer()
    for dx, r in ((-6, 6), (4, 6)):
        ell(d2, 32 + dx, cy + 10, r, 3, shade(c))
    hl, d3 = layer()
    ell(d3, 25, cy - 5, 3.2, 2.2, (255, 255, 230, 255))
    fc, d4 = layer()
    if face != "up":
        face_grumpy(d4, 32 if face == "down" else 33, cy + 1, face == "side")
    return stack([body, (sh,), (hl,), (fc,)])


def cowboy(face, ph, bob):
    """목마 카우보이 — 나무 목마(몸 · 머리 · 다리) 위 동그란 얼굴 + 갈색 모자 + 막대 팔."""
    wood = (214, 140, 82, 255)
    wood_d = (176, 104, 60, 255)
    skin = (255, 236, 170, 255)
    hat = (172, 110, 66, 255)
    rock = math.sin(ph * math.pi / 2) * 2
    side = face == "side"
    legs, d = layer()
    for i, x in enumerate((20, 28, 38, 46)):
        lift = 2 * ph * (1 if i % 2 else -1)
        rrect(d, x - 1.6, 44 + bob, x + 1.6, 58 - max(0, lift), 1.2, wood_d)
    horse, d = layer()
    rrect(d, 16, 36 + bob + rock * 0.3, 50, 46 + bob, 5, wood)
    hx = 52 if side or face == "down" else 32
    if face != "up":
        rrect(d, hx - 5, 24 + bob - rock, hx + 4, 40 + bob, 4, wood)
        ell(d, hx + 2, 27 + bob - rock, 6, 4.5, wood)
    rider, d = layer()
    ell(d, 30, 27 + bob, 9.5, 9, skin)
    seg(d, 22, 30 + bob, 16, 25 + bob + rock, 1.6, (120, 70, 200, 255))
    seg(d, 38, 30 + bob, 44, 24 + bob - rock, 1.6, (120, 70, 200, 255))
    ell(d, 16, 25 + bob + rock, 1.8, 1.8, (120, 70, 200, 255))
    ell(d, 44, 24 + bob - rock, 1.8, 1.8, (120, 70, 200, 255))
    poly(d, [(26, 34 + bob), (34, 34 + bob), (30, 38 + bob)], (230, 40, 70, 255))
    hatl, d = layer()
    ell(d, 30, 17.5 + bob, 14, 3.0, hat)
    rrect(d, 23, 6 + bob, 37, 18 + bob, 5, hat)
    det, d = layer()
    if face != "up":
        face_happy(d, 30 if face == "down" else 31, 27.5 + bob, side)
        # 고삐 · 목마 눈
        if face != "up":
            seg(d, hx + 4, 31 + bob - rock, 38, 37 + bob, 0.8, (230, 40, 70, 255))
            ell(d, hx + 3, 26 + bob - rock, 1.0, 1.0, EYE)
    seg(d, 23, 15 + bob, 37, 15 + bob, 1.2, (110, 64, 40, 255))
    for x in (22, 34, 44):
        seg(d, x, 39 + bob, x + 4, 39 + bob, 0.7, wood_d)
    return stack([legs, horse, rider, hatl, (det,)])


def bubble(face, ph, bob):
    """베레모 물방울 — 시안 구슬 몸 · 보라 베레모 · 초록 왕관 잎 · 막대 팔."""
    c = (110, 228, 245, 255)
    cy = 38 + bob
    arms, d = layer()
    sw = ph * 2
    for s in (-1, 1):
        seg(d, 32 + s * 10, cy, 32 + s * 19, cy + 2 + sw * s, 1.3, (120, 70, 200, 255))
        ell(d, 32 + s * 19.5, cy + 2 + sw * s, 2.0, 2.0, (120, 70, 200, 255))
    body, d = layer()
    ell(d, 32, cy, 11.5, 11, c)
    leaf, d = layer()
    for a in (-40, -10, 20):
        r = math.radians(a - 90)
        poly(d, [(30, 25 + bob), (30 + math.cos(r - 0.25) * 9, 25 + bob + math.sin(r - 0.25) * 9),
                 (30 + math.cos(r + 0.25) * 9, 25 + bob + math.sin(r + 0.25) * 9)], (90, 220, 170, 255))
    beret, d = layer()
    ell(d, 30, 27.5 + bob, 10.5, 5, (140, 70, 170, 255))
    ell(d, 30 + 3, 28 + bob, 1.4, 1.0, (100, 46, 130, 255))
    det, d = layer()
    ell(d, 25, cy - 2, 3, 4, (210, 250, 255, 255))
    ell(d, 38, cy + 6, 5, 3, shade(c, 0.88))
    if face != "up":
        face_oval(d, 32 if face == "down" else 33, cy + 1, face == "side")
    return stack([arms, body, leaf, beret, (det,)])


def slime(face, ph, bob):
    """슬라임 — 물방울 젤리. 걸음 대신 찌그러졌다 늘어남 (스쿼시)."""
    c = (130, 236, 184, 255)   # 민트 — 보라는 플레이어 장판 계열 색과 겹친다
    sq = 1 + 0.14 * ph
    w, h = 12 * sq, 10 / sq
    base = 58
    body, d = layer()
    d.chord([(32 - w) * S, (base - h * 2) * S, (32 + w) * S, (base + h * 0.6) * S], 180, 360, fill=c)
    rrect(d, 32 - w, base - h * 0.9, 32 + w, base, 3, c)
    ell(d, 32 + 2, base - h * 2 - 1, 3, 3.6, c)
    det, d = layer()
    ell(d, 32 - w * 0.5, base - h * 1.3, 2.6, 3.6, (225, 255, 240, 255))
    ell(d, 32 - w * 0.5 + 1, base - h * 1.3 - 4.5, 1.1, 1.1, (225, 255, 240, 255))
    rrect(d, 32 - w + 2, base - 2.4, 32 + w - 2, base, 1.2, shade(c))
    if face != "up":
        face_dot(d, 32 if face == "down" else 33, base - h * 0.95, face == "side")
    return stack([body, (det,)])


def ghost(face, ph, bob):
    """고스트 — 둥근 머리 + 물결 꼬리. 떠다니며 꼬리가 펄럭인다."""
    # 라일락 — 흰색이면 사모예드(흰 솜 머리)와 겹쳐 플레이어가 고스트 무리에 묻힌다 (QA5 최대 밀도 캡처)
    c = (206, 188, 255, 255)
    cy = 30 + bob
    side = face == "side"
    body, d = layer()
    ell(d, 32, cy, 11, 11, c)
    top = cy
    bottom = 50 + bob
    pts = [(21, top), (43, top)]
    n = 4
    for i in range(n + 1):
        x = 43 - 22 * i / n
        y = bottom + (3 if i % 2 == 0 else -1) + ph * (1 if i % 2 else -1)
        pts.append((x - (4 if side else 0) * i / n, y))
    poly(d, pts, c)
    if side:
        poly(d, [(21, cy), (21, bottom - 4), (14, bottom + 2 + ph * 2), (20, bottom - 8)], c)
    arm, d = layer()
    if face != "up":
        ell(d, 21.5, cy + 8, 3, 2.2, c)
        ell(d, 42.5 if not side else 38, cy + 8, 3, 2.2, c)
    det, d = layer()
    ell(d, 38, cy + 12, 4, 6, (176, 156, 236, 255))
    ell(d, 26, cy - 5, 2.6, 1.8, WHITE)
    if face != "up":
        face_dot(d, 32 if face == "down" else 34, cy + 1, side, mood="o")
    return stack([body, arm, (det,)])


ENEMIES = {
    "grunt": (cloud, 12.0),
    "brute": (cowboy, 16.0),
    "spitter": (bubble, 12.0),
    "swarm": (slime, 11.0),
    "runner": (ghost, 10.0),
}

WALK = [(-1.0, 1), (-0.35, -1.5), (1.0, 1), (0.35, -1.5)]


def ufo(phase):
    """납치선 — 같은 그림체: 진보라 외곽선 · 파스텔 · 투명 돔 속 작은 구름 조종사."""
    w = 96
    bob = (0, -2, 0, 2)[phase % 4]
    dome, d = layer(w)
    ell(d, 48, 40 + bob, 20, 17, (190, 240, 255, 255))
    pilot, d = layer(w)
    for dx, r in ((-5, 6), (5, 6), (0, 7.5)):
        ell(d, 48 + dx, 42 + bob, r, r * 0.9, (255, 240, 150, 255))
    hull, d = layer(w)
    ell(d, 48, 54 + bob, 40, 12, (255, 160, 210, 255))
    ell(d, 48, 50 + bob, 30, 6, (255, 200, 230, 255))
    img = stack([pilot], w)
    domec = fin(dome, w)
    a = domec.split()[3].point(lambda v: int(v * 0.55))
    domec.putalpha(a)
    base = Image.new("RGBA", img.size, (0, 0, 0, 0))
    base.alpha_composite(domec)
    base.alpha_composite(img)
    base.alpha_composite(fin(hull, w))
    det, d = layer(w)
    face_grumpy(d, 48, 42 + bob, False)
    for i, x in enumerate((-26, -13, 0, 13, 26)):
        on = i != phase % 5
        ell(d, 48 + x, 56 + bob, 3.6, 3.6, (255, 236, 110, 255) if on else (190, 120, 170, 255))
    base.alpha_composite(shrink(det, w))
    g = ground(18, 5.4, 90, 120, w)
    g.alpha_composite(base)
    return g


def main():
    preview_only = "--preview" in sys.argv
    T = W * OUT
    sheet = Image.new("RGBA", (T * 5, T * (len(ENEMIES) * 3 + 1)), (70, 50, 100, 255))
    row = 0
    for key, (fn, gr) in ENEMIES.items():
        for face, suffix in (("down", ""), ("up", "up"), ("side", "side")):
            frames = [(f"alien_{key}_idle{suffix}", 0.0, 0)] + [(f"alien_{key}_walk{suffix}{i}", ph, b) for i, (ph, b) in enumerate(WALK)]
            for col, (name, ph, b) in enumerate(frames):
                img = ground(gr)
                img.alpha_composite(fn(face, ph, b))
                sheet.alpha_composite(img, (col * T, row * T))
                if not preview_only:
                    img.save(os.path.join(DST, name + ".png"))
            row += 1
    for i in range(4):
        u = ufo(i)
        sheet.alpha_composite(u.resize((T, T), Image.LANCZOS), (i * T, row * T))
        if not preview_only:
            u.save(os.path.join(DST, "alien_ufo_walk%d.png" % i))
            if i == 0:
                u.save(os.path.join(DST, "boss_ufo.png"))
    sheet.save(PREVIEW)
    print("적 5종 × 15장 + 보스 4장%s · 라인업 %s" % (" (미리보기만)" if preview_only else "", PREVIEW))


main()
