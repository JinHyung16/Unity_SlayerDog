# -*- coding: utf-8 -*-
# 스킬 이펙트 생성기 - 「개많이잡아」 아트 리부트 (_Ref/ArtReboot/기획서.md · TASKS E-2)
#
# 이펙트 한 종 = 두 층. 프리팹은 Art2DBuilder 가 Glow · Core 두 자식으로 굽는다
#   glow/NN.png  흰색 번짐 + 스파클. 가산 머티리얼(Jinhyeong/SpriteAdditive)로 그리고 데이터 색(계열 색)으로 틴트한다
#   core/NN.png  기운형: 흰 심지(틴트 25% 만 받는다 → 하얗게 달아오른 네온)
#                물건형(뼈 · 공 · 원반 · 슬리퍼 · 사료 · 식빵 · 이빨): 제 색 + 진보라 외곽선(틴트 0)
# - 검은 외곽선은 물건형만 (E-2). 기운형은 외곽선 없이 번짐이 테두리를 만든다
# - 캔버스 64 좌표 = 2 유닛 = 판정 지름. 반경 있는 연출은 외곽이 반지름 30 근처에 닿게 그린다 (크기가 곧 사거리)
# - 앞 방향은 +x (오른쪽). Ground 방향 연출(부채꼴 · 빔 · 발톱)은 오른쪽을 향해 그린다
# - 프레임 6 (물건형 투사체는 회전 4). 수명은 데이터가 정하고 SkillVisual 이 fps 를 맞춘다
# - 이름은 기존 FX 키 폴더 그대로 (fx_bark …). 쓰지 않는 옛 팩 폴더는 이 생성기가 지운다 (PURGE)

from PIL import Image, ImageDraw, ImageFilter, ImageChops
import os, sys, math, json, random, shutil
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\FX")
PREVIEW = os.path.join(ROOT, r"_Ref\ArtReboot\lineup_vfx.png")
SS = 2
OUT = 4
S = SS * OUT          # 논리 1 = 8px (작업) → 4px (출력)
W = 64
N = 6
INK = (46, 20, 70, 255)
WHITE = (255, 255, 255, 255)

# 옛 팩(64px 도트 · Tiny Swords) — 데이터 · 코드 참조 0 (2026-10-03 grep). 폴더째 지운다
PURGE = ["fx_hit_small", "fx_explosion", "fx_burst_big", "fx_slash", "fx_slash_cross", "fx_arc_wave", "fx_ring",
         "fx_pillar", "fx_bolt", "fx_vortex", "fx_drop", "fx_ground", "fx_buff", "fx_heal", "fx_trail", "fx_claw3",
         "fx_ring_spike", "fx_crescent", "fx_swirl", "fx_ring_dense", "fx_sparkle", "fx_ts_dust", "fx_ts_explosion",
         "fx_ts_fire", "fx_ts_splash"]


# ── 그리기 도구 (논리 좌표) ───────────────────────────────────────────────

def canvas():
    im = Image.new("RGBA", (W * S, W * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def P(x, y):
    return (x * S, y * S)


def circ(d, x, y, r, c=WHITE):
    if r > 0:
        d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c)


def ring(d, x, y, r, w, c=WHITE):
    if r <= 0 or w <= 0:
        return
    d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], outline=c, width=max(1, int(w * S)))


def poly(d, pts, c=WHITE):
    d.polygon([P(x, y) for x, y in pts], fill=c)


def seg(d, x0, y0, x1, y1, w, c=WHITE):
    d.line([P(x0, y0), P(x1, y1)], fill=c, width=max(1, int(w * S)))
    circ(d, x0, y0, w / 2, c)
    circ(d, x1, y1, w / 2, c)


def taper(d, pts, w0, w1, c=WHITE):
    """폴리라인을 굵기 w0 → w1 로 가늘어지게. 붓터치 · 베기 자국."""
    n = len(pts)
    for i in range(n - 1):
        a = w0 + (w1 - w0) * (i / max(1, n - 1))
        seg(d, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], a, c)


def arc_pts(cx, cy, r, a0, a1, n=24):
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * r,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * r) for i in range(n + 1)]


def arc_band(d, cx, cy, r, w, a0, a1, c=WHITE, tip=True):
    """두께 w 의 호. tip 이면 양 끝이 뾰족하다 (음파 · 베기)."""
    n = 32
    outer, inner = [], []
    for i in range(n + 1):
        u = i / n
        k = math.sin(math.pi * u) if tip else 1.0
        a = math.radians(a0 + (a1 - a0) * u)
        ww = w * (0.25 + 0.75 * k)
        outer.append((cx + math.cos(a) * (r + ww / 2), cy + math.sin(a) * (r + ww / 2)))
        inner.append((cx + math.cos(a) * (r - ww / 2), cy + math.sin(a) * (r - ww / 2)))
    poly(d, outer + inner[::-1], c)


def star4(d, x, y, r, c=WHITE, k=0.28, rot=0.0):
    pts = []
    for i in range(8):
        a = math.radians(rot + 45 * i - 90)
        rr = r if i % 2 == 0 else r * k
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    poly(d, pts, c)


def starburst(d, x, y, r0, r1, n, c=WHITE, rot=0.0, jag=None):
    pts = []
    for i in range(n * 2):
        a = math.radians(rot + 180.0 / n * i)
        rr = r1 if i % 2 == 0 else r0
        if jag:
            rr *= jag[i % len(jag)]
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    poly(d, pts, c)


def cut(src, fn):
    """fn 으로 그린 모양을 src 에서 파낸다."""
    m, d = canvas()
    fn(d)
    a = ImageChops.subtract(src.split()[3], m.split()[3])
    out = src.copy()
    out.putalpha(a)
    return out


def ease_out(t):
    return 1 - (1 - t) ** 3


def pop(t):
    """0→1 로 튀어나왔다가 살짝 줄어든다 (스쿼시 앤 스트레치 대용)."""
    # 0 에서 시작하면 첫 프레임이 비어 루프 재생 때 깜빡인다 — 0.45 에서 튀어나온다
    if t < 0.35:
        return 0.45 + ease_out(t / 0.35) * 0.67
    return 1.12 - 0.12 * min(1.0, (t - 0.35) / 0.3)


def fade(t, start=0.55):
    # 끝을 0 이 아니라 0.25 로 둔다. SkillVisual 은 루프로 재생하므로 빈 프레임이 깜빡임이 된다
    return 1.0 if t < start else max(0.25, 1 - 0.75 * (t - start) / (1 - start))


# ── 층 합성 ──────────────────────────────────────────────────────────────

def down(im):
    return im.resize((W * OUT, W * OUT), Image.LANCZOS)


def alpha_mul(im, k):
    if k >= 0.999:
        return im
    a = im.split()[3].point(lambda v: int(v * k))
    out = im.copy()
    out.putalpha(a)
    return out


def glow_of(core, grow=2.5, blur=3.2, strength=1.0, sparkles=(), seed=0):
    """심지 실루엣을 키우고 번지게. 흰색 — 색은 런타임 틴트 + 가산."""
    a = core.split()[3]
    k = int(grow * S) * 2 + 1
    a = a.filter(ImageFilter.MaxFilter(k if k % 2 else k + 1))
    a = a.filter(ImageFilter.GaussianBlur(blur * S))
    a = a.point(lambda v: min(255, int(v * 1.25 * strength)))
    g = Image.new("RGBA", core.size, (255, 255, 255, 0))
    g.putalpha(a)
    if sparkles:
        sp, d = canvas()
        for (x, y, r) in sparkles:
            star4(d, x, y, r)
        g.alpha_composite(sp)
    return g


def inked(lay, px=1.4):
    """물건형: 실루엣 팽창 외곽선 (주인공과 같은 규칙)."""
    a = lay.split()[3]
    k = int(px * S) * 2 + 1
    grown = a.filter(ImageFilter.MaxFilter(k))
    out = Image.new("RGBA", lay.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", lay.size, INK), (0, 0), grown)
    out.alpha_composite(lay)
    return out


def sparkle_ring(t, r, n=5, seed=1, size=2.6):
    rnd = random.Random(seed)
    out = []
    for i in range(n):
        a = rnd.uniform(0, 2 * math.pi)
        rr = r * rnd.uniform(0.75, 1.15)
        s = size * rnd.uniform(0.6, 1.2) * (1 - abs(t - 0.5))
        out.append((32 + math.cos(a) * rr, 32 + math.sin(a) * rr, s))
    return out


# ── 기운형 ───────────────────────────────────────────────────────────────

def fx_bark(t):
    im, d = canvas()
    for k in range(3):
        u = (t - k * 0.12) / 0.7
        if u <= 0:
            continue
        r = 8 + k * 8 + ease_out(min(1, u)) * 6
        arc_band(d, 4, 32, r, 3.6 - k * 0.6, -38, 38)
    for a in (-24, 0, 24):
        x = 4 + math.cos(math.radians(a)) * (30 + t * 2)
        y = 32 + math.sin(math.radians(a)) * (30 + t * 2)
        star4(d, x, y, 2.2 * pop(t), rot=a)
    return im, sparkle_ring(t, 26, 4, 3)


def fx_sonar(t):
    im, d = canvas()
    for k in range(4):
        r = 6 + k * 6.5 + t * 9
        if r > 31:
            continue
        arc_band(d, 2, 32, r, 2.6, -50, 50)
    return im, ()


def fx_eyebeam(t):
    im, d = canvas()
    h = 5.5 * math.sin(math.pi * min(1.0, 0.15 + t * 0.85))
    poly(d, [(6, 32 - h / 2), (62, 32 - h * 0.35), (62, 32 + h * 0.35), (6, 32 + h / 2)])
    circ(d, 7, 32, h * 0.9 + 1.5)
    star4(d, 7, 32, 7 * pop(t), k=0.22)
    return im, [(20 + 30 * t, 26, 1.8), (36 + 20 * t, 38, 1.5)]


def fx_gaze(t):
    im, d = canvas()
    s = pop(t)
    w, h = 22 * s, 11 * s
    top = arc_pts(32, 32 + h * 1.6, h * 2.6, -125, -55)
    bot = arc_pts(32, 32 - h * 1.6, h * 2.6, 55, 125)
    taper(d, top, 3.2, 3.2)
    taper(d, bot, 3.2, 3.2)
    circ(d, 32, 32, 6.5 * s)
    for i in range(8):
        a = math.radians(i * 45 + t * 30)
        seg(d, 32 + math.cos(a) * 18 * s, 32 + math.sin(a) * 18 * s, 32 + math.cos(a) * 26 * s, 32 + math.sin(a) * 26 * s, 1.6)
    im = cut(im, lambda dd: circ(dd, 32, 32, 2.6 * s))
    return im, ()


def fx_growl(t):
    im, d = canvas()
    r = 10 + ease_out(t) * 19
    starburst(d, 32, 32, r * 0.80, r, 14, rot=t * 20, jag=(1.0, 1.0, 0.92, 1.0, 1.05, 1.0))
    im = cut(im, lambda dd: circ(dd, 32, 32, r * 0.70))
    return im, sparkle_ring(t, r, 5, 7)


def fx_howl(t):
    im, d = canvas()
    r = 8 + ease_out(t) * 22
    ring(d, 32, 32, r, 3.4 * (1 - t) + 1.2)
    ring(d, 32, 32, r * 0.72, 1.2)
    for i in range(12):
        a = math.radians(i * 30 + t * 15)
        seg(d, 32 + math.cos(a) * (r + 2), 32 + math.sin(a) * (r + 2), 32 + math.cos(a) * (r + 5), 32 + math.sin(a) * (r + 5), 1.6)
    return im, sparkle_ring(t, r, 6, 11)


def fx_yawn(t):
    im, d = canvas()
    r = 9 + ease_out(t) * 20
    pts = [(32 + math.cos(a) * (r + 1.6 * math.sin(a * 6 + t * 6)), 32 + math.sin(a) * (r + 1.6 * math.sin(a * 6 + t * 6)))
           for a in [2 * math.pi * i / 72 for i in range(73)]]
    taper(d, pts, 2.6, 2.6)
    rnd = random.Random(5)
    for i in range(6):
        a = rnd.uniform(0, 2 * math.pi)
        rr = r * rnd.uniform(0.3, 0.8)
        ring(d, 32 + math.cos(a) * rr, 32 + math.sin(a) * rr - t * 4, rnd.uniform(1.6, 3.2), 1.0)
    return im, ()


def fx_static(t):
    im, d = canvas()
    rnd = random.Random(int(t * 10) + 3)
    for i in range(6):
        a0 = math.radians(i * 60 + t * 40)
        pts = []
        for j in range(6):
            rr = 6 + j * 4.6
            jit = rnd.uniform(-9, 9)
            pts.append((32 + math.cos(a0 + math.radians(jit)) * rr, 32 + math.sin(a0 + math.radians(jit)) * rr))
        taper(d, pts, 2.6, 0.8)
    circ(d, 32, 32, 4)
    return im, sparkle_ring(t, 24, 6, int(t * 10))


def fx_shield(t):
    im, d = canvas()
    s = pop(t)
    hexa = [(32 + math.cos(math.radians(60 * i + 30)) * 27 * s, 32 + math.sin(math.radians(60 * i + 30)) * 27 * s) for i in range(7)]
    taper(d, hexa, 3.0, 3.0)
    arc_band(d, 32, 32, 21 * s, 2.4, 200, 250)
    return im, sparkle_ring(t, 27, 4, 13)


def fx_awaken(t):
    im, d = canvas()
    for i, x in enumerate((14, 22, 32, 42, 50)):
        h = (18 + (6 if i == 2 else 0) - abs(i - 2) * 3) * pop(t)
        yb = 56
        sway = math.sin(t * 6 + i) * 2
        poly(d, [(x - 4, yb), (x + sway, yb - h), (x + 4, yb)])
    return im, [(18, 30 - t * 10, 2.4), (46, 26 - t * 8, 2.0), (32, 18 - t * 6, 2.8)]


def fx_zoom(t):
    im, d = canvas()
    rnd = random.Random(9)
    for i in range(7):
        y = 12 + i * 6.5
        x0 = rnd.uniform(4, 22) - t * 14
        ln = rnd.uniform(18, 34)
        poly(d, [(x0, y), (x0 + ln, y - 1.4), (x0 + ln + 3, y), (x0 + ln, y + 1.4)])
    return im, ()


def fx_taunt(t):
    im, d = canvas()
    s = pop(t) * 1.0
    # 분노 마크 — 네 꼭지가 바깥으로 굽은 십자
    for k in range(4):
        a = math.radians(45 + 90 * k)
        cx, cy = 32 + math.cos(a) * 6 * s, 32 + math.sin(a) * 6 * s
        pts = arc_pts(cx, cy, 6 * s, math.degrees(a) + 100, math.degrees(a) + 260, 14)
        taper(d, pts, 3.4, 3.4)
    ring(d, 32, 32, 14 + ease_out(t) * 14, 2.2 * (1 - t) + 0.6)
    return im, sparkle_ring(t, 24, 4, 17)


def fx_scent(t):
    im, d = canvas()
    for k, x in enumerate((20, 32, 44)):
        pts = [(x + math.sin(y * 0.35 + t * 7 + k) * 3, y) for y in [52 - i * 1.5 - t * 6 for i in range(24)]]
        taper(d, pts, 3.0, 0.8)
    return im, ()


def fx_tailswing(t):
    im, d = canvas()
    a0 = -70 + t * 30
    arc_band(d, 32, 32, 22, 9 * fade(t, 0.5) + 2, a0, a0 + 140)
    im = cut(im, lambda dd: circ(dd, 30, 32, 17))
    return im, [(32 + math.cos(math.radians(a0 + 140)) * 24, 32 + math.sin(math.radians(a0 + 140)) * 24, 3)]


def fx_dogclaw(t):
    im, d = canvas()
    for k in range(3):
        u = min(1.0, max(0.0, (t - k * 0.1) / 0.5))
        if u <= 0:
            continue
        y0 = 16 + k * 10
        x0, x1 = 8, 8 + 50 * ease_out(u)
        taper(d, [(x0 + (x1 - x0) * i / 10, y0 + 14 * i / 10 + math.sin(i / 10 * math.pi) * -3) for i in range(11)], 1.0, 4.4)
    return im, ()


def fx_paw(t):
    im, d = canvas()
    s = pop(t) * 0.9
    poly(d, [(32 - 9 * s, 38 * s + 32 * (1 - s) + 2), (32 + 9 * s, 38 * s + 32 * (1 - s) + 2), (32 + 6 * s, 32 + 12 * s), (32 - 6 * s, 32 + 12 * s)])
    circ(d, 32, 32 + 5 * s, 9 * s)
    for dx, dy in ((-11, -6), (-4, -12), (4, -12), (11, -6)):
        circ(d, 32 + dx * s, 32 + dy * s, 3.8 * s)
    for i in range(8):
        a = math.radians(i * 45 + 22)
        r0, r1 = 20 * s, (24 + 4 * t) * s
        seg(d, 32 + math.cos(a) * r0, 32 + math.sin(a) * r0, 32 + math.cos(a) * r1, 32 + math.sin(a) * r1, 1.8)
    return im, ()


def fx_burst(t):
    im, d = canvas()
    r = 8 + ease_out(t) * 22
    starburst(d, 32, 32, r * 0.55, r, 8, rot=t * 25)
    im = cut(im, lambda dd: circ(dd, 32, 32, r * 0.45 * min(1.0, t * 2)))
    return im, sparkle_ring(t, r * 1.05, 6, 19, 3.2)


def fx_dogdust(t):
    """흙먼지 구름 — 고리로 퍼지는 뭉게 덩어리. 속을 파지 않는다 (파면 글자처럼 읽혔다)."""
    im, d = canvas()
    rnd = random.Random(23)
    spread = 0.55 + 0.6 * ease_out(t)
    for i in range(9):
        a = math.radians(i * 40 + rnd.uniform(-10, 10))
        rr = (14 + rnd.uniform(-2, 3)) * spread
        circ(d, 32 + math.cos(a) * rr, 32 + math.sin(a) * rr * 0.8, rnd.uniform(4.5, 6.5) * (1.05 - 0.35 * t))
    for i in range(4):
        a = math.radians(i * 90 + 45)
        circ(d, 32 + math.cos(a) * 7 * spread, 32 + math.sin(a) * 6 * spread, 4.0 * (1 - 0.4 * t))
    return im, ()


def fx_hit(t):
    im, d = canvas()
    s = pop(t)
    star4(d, 32, 32, 22 * s, k=0.18)
    star4(d, 32, 32, 13 * s, k=0.22, rot=45)
    ring(d, 32, 32, 8 + t * 16, 2.0 * (1 - t) + 0.4)
    return im, ()


def fx_shards(t):
    """적중 버스트 — 스킬 색 파편이 사방으로 튄다. 모든 스킬이 적을 맞힐 때 나온다 (SkillHit)."""
    im, d = canvas()
    s = pop(t)
    star4(d, 32, 32, 12 * s * (1 - 0.6 * t), k=0.2)
    rnd = random.Random(41)
    for i in range(9):
        a = math.radians(i * 40 + rnd.uniform(-12, 12))
        r0 = 6 + ease_out(t) * 16
        ln = 6 * (1 - t) + 2
        x0, y0 = 32 + math.cos(a) * r0, 32 + math.sin(a) * r0
        x1, y1 = 32 + math.cos(a) * (r0 + ln), 32 + math.sin(a) * (r0 + ln)
        taper(d, [(x0, y0), (x1, y1)], 2.6 * (1 - 0.5 * t), 0.6)
    return im, sparkle_ring(t, 14 + 12 * t, 4, 43, 2.2)


# ── 음표 (음악 전투 10-04 — .claude/design.md) ─────────────────────────

def note(d, x, y, s, flags=1, hollow=False, c=WHITE, stem=True):
    """음표 하나. 머리는 기운 타원, 기둥은 오른쪽 위, flags 0=4분 1=8분 2=16분."""
    head = []
    for i in range(24):
        a = 2 * math.pi * i / 24
        hx, hy = math.cos(a) * 4.4 * s, math.sin(a) * 3.1 * s
        r = math.radians(-22)
        head.append((x + hx * math.cos(r) - hy * math.sin(r), y + hx * math.sin(r) + hy * math.cos(r)))
    poly(d, head, c)
    if hollow:
        inner = []
        for i in range(24):
            a = 2 * math.pi * i / 24
            hx, hy = math.cos(a) * 2.4 * s, math.sin(a) * 1.5 * s
            r = math.radians(-50)
            inner.append((x + hx * math.cos(r) - hy * math.sin(r), y + hx * math.sin(r) + hy * math.cos(r)))
        poly(d, inner, (0, 0, 0, 0))
    if stem:
        sx = x + 3.6 * s
        poly(d, [(sx - 0.9 * s, y - 1 * s), (sx + 0.7 * s, y - 1 * s), (sx + 0.7 * s, y - 15 * s), (sx - 0.9 * s, y - 15 * s)], c)
        for k in range(flags):
            fy = y - 15 * s + k * 4 * s
            poly(d, [(sx, fy), (sx + 6 * s, fy + 4 * s), (sx + 5 * s, fy + 7 * s), (sx, fy + 3.2 * s)], c)


def fx_notebass(t):
    """베이스 드롭 — 큰 8분음표 + 저음 파문 두 겹 (단타)."""
    im, d = canvas()
    s = pop(t) * 1.35
    note(d, 30, 40, s, flags=1)
    for k in range(2):
        r = 12 + k * 6 + t * 6
        arc_band(d, 30, 40, r, 2.0, 120, 240)
    return im, [(46, 18, 2.4), (18, 22, 1.8)]


def fx_notebeat(t):
    """드럼 롤 — 16분음표 (연발 한 발)."""
    im, d = canvas()
    s = pop(t) * 1.35
    note(d, 30, 40, s, flags=2)
    star4(d, 22, 28, 3 * s, k=0.25)
    return im, ()


def fx_staffbeam(t):
    """현의 선율 — 오선지 레이저. 다섯 줄이 앞으로 뻗고 음표가 줄을 타고 흐른다 (+x 방향)."""
    im, d = canvas()
    h = math.sin(math.pi * min(1.0, 0.15 + t * 0.85))
    for k in range(5):
        y = 32 + (k - 2) * 2.6 * h
        seg(d, 6, y, 62, y, 0.9 if k != 2 else 1.4)
    for i, x in enumerate((14, 30, 46)):
        xx = 6 + ((x - 6 + t * 30) % 56)
        note(d, xx, 32 + ((i % 3) - 1) * 2.6 * h, 0.55 * h + 0.2, flags=i % 2)
    star4(d, 7, 32, 6 * pop(t), k=0.22)
    return im, [(20 + 30 * t, 25, 1.6), (40 + 15 * t, 39, 1.4)]


def fx_notescale(t):
    """도레미 스케일 — 4분음표 (부채꼴 연발 한 발)."""
    im, d = canvas()
    s = pop(t) * 1.4
    note(d, 30, 40, s, flags=0)
    return im, [(42, 22, 2.0)]


def fx_notewhole(t):
    """자장가 온음표 — 속 빈 온음표 + 달 · 별 (유도 · 터지면 졸음)."""
    im, d = canvas()
    s = pop(t) * 1.6
    note(d, 32, 34, s, hollow=True, stem=False)
    m = 0.6 + 0.4 * math.sin(t * math.pi)
    circ(d, 46, 18, 4.5 * m)
    im = cut(im, lambda dd: circ(dd, 48, 16.5, 3.8 * m))
    return im, [(18, 20, 2.0), (22, 48, 1.6), (46, 46, 1.8)]


def fx_chord(t):
    """다장조 화음 — 도 · 미 · 솔 세 음이 겹쳐 떨어지며 착지 고리."""
    im, d = canvas()
    fall = (1 - ease_out(min(1.0, t * 1.6))) * 10
    for i, (x, y) in enumerate(((24, 44), (32, 38), (40, 32))):
        note(d, x, y - fall, 0.8, flags=0)
    if t > 0.35:
        ring(d, 32, 44, 10 + (t - 0.35) * 30, 2.2 * (1 - t) + 0.6)
    return im, sparkle_ring(t, 22, 5, 51, 2.4)


# ── 물건형 (제 색 + 외곽선) ──────────────────────────────────────────────

def rot_img(im, deg):
    return im.rotate(-deg, resample=Image.BICUBIC, center=(32 * S, 32 * S))


def obj_bone(t):
    """베이스 드럼 매설 — 옆으로 누운 작은 베이스 드럼 (예전 뼈다귀)."""
    im, d = canvas()
    circ(d, 32, 32, 13, (255, 90, 170, 255))
    circ(d, 32, 32, 10.5, (255, 248, 240, 255))
    circ(d, 32, 32, 4, (255, 210, 230, 255))
    for a in range(0, 360, 45):
        r = math.radians(a)
        circ(d, 32 + math.cos(r) * 11.8, 32 + math.sin(r) * 11.8, 1.2, (230, 230, 245, 255))
    return rot_img(inked(im), t * 20)


def obj_ball(t):
    """탬버린 부메랑 — 나무 테 + 쇠방울 (예전 테니스공). 돈다."""
    im, d = canvas()
    circ(d, 32, 32, 12, (230, 160, 90, 255))
    circ(d, 32, 32, 8.6, (255, 245, 225, 255))
    jg, d2 = canvas()
    for k in range(5):
        a = math.radians(k * 72 + 20)
        circ(d2, 32 + math.cos(a) * 10.4, 32 + math.sin(a) * 10.4, 2.6, (225, 230, 245, 255))
        circ(d2, 32 + math.cos(a) * 10.4, 32 + math.sin(a) * 10.4, 1.0, (160, 160, 190, 255))
    im.alpha_composite(jg)
    return rot_img(inked(im), t * 90)


def obj_frisbee(t):
    """LP 판 날리기 — 검은 비닐 · 분홍 라벨 · 홈 반사 (예전 원반)."""
    im, d = canvas()
    d.ellipse([15 * S, 23 * S, 49 * S, 41 * S], fill=(40, 30, 60, 255))
    for k in (14, 11):
        d.ellipse([(32 - k) * S, (32 - k * 0.53) * S, (32 + k) * S, (32 + k * 0.53) * S], outline=(80, 66, 110, 255), width=int(0.8 * S))
    d.ellipse([26 * S, 29 * S, 38 * S, 35 * S], fill=(255, 80, 170, 255))
    circ(d, 32, 32, 1.0, (40, 30, 60, 255))
    hl, d2 = canvas()
    taper(d2, arc_pts(32, 32, 13, 200 + t * 360, 235 + t * 360, 8), 1.3, 1.3, (200, 190, 240, 255))
    m, d3 = canvas()
    d3.ellipse([15 * S, 23 * S, 49 * S, 41 * S], fill=WHITE)
    hl.putalpha(ImageChops.multiply(hl.split()[3], m.split()[3]))
    im.alpha_composite(hl)
    return inked(im)


def obj_slipper(t):
    """캐스터네츠 딱! — 빨간 조개 두 짝 + 끈 (예전 슬리퍼)."""
    im, d = canvas()
    gap = 2.5 * abs(math.sin(t * math.pi * 2))
    d.chord([18 * S, (20 - gap) * S, 46 * S, (40 - gap) * S], 180, 360, fill=(230, 50, 70, 255))
    d.chord([18 * S, (24 + gap) * S, 46 * S, (44 + gap) * S], 0, 180, fill=(200, 36, 60, 255))
    st, d2 = canvas()
    seg(d2, 32, 30, 32, 34, 1.4, (255, 220, 120, 255))
    circ(d2, 26, 27 - gap, 1.6, (255, 150, 160, 255))
    im.alpha_composite(st)
    return rot_img(inked(im), t * 90)


def obj_kibble(t):
    """음표 소나기 · 심벌 크래시 — 색색 음표가 흩어진다 (예전 사료)."""
    im, d = canvas()
    cols = [(255, 80, 170, 255), (60, 220, 240, 255), (255, 230, 80, 255), (170, 110, 255, 255), (150, 255, 90, 255)]
    spread = 4 + t * 14
    for i in range(5):
        a = i * 72 + t * 40
        x = 32 + math.cos(math.radians(a)) * spread
        y = 36 + math.sin(math.radians(a)) * spread
        note(d, x, y, 0.8, flags=i % 3, c=cols[i])
    return inked(im, 1.2)


def obj_bread(t):
    """건반 슬램 — 피아노 건반 한 옥타브가 쾅 (예전 식빵)."""
    im, d = canvas()
    sq = 1 + 0.12 * math.sin(t * math.pi * 2)
    w, h = 16 / sq, 10 * sq
    d.rounded_rectangle([(32 - w) * S, (32 - h) * S, (32 + w) * S, (32 + h) * S], radius=2 * S, fill=(255, 255, 255, 255))
    keys, d2 = canvas()
    n = 7
    for k in range(1, n):
        x = 32 - w + 2 * w * k / n
        seg(d2, x, 32 - h, x, 32 + h, 0.6, (60, 40, 90, 255))
    for k in (1, 2, 4, 5, 6):
        x = 32 - w + 2 * w * k / n
        d2.rectangle([(x - 1.4) * S, (32 - h) * S, (x + 1.4) * S, (32 + h * 0.15) * S], fill=(40, 30, 60, 255))
    im.alpha_composite(keys)
    return inked(im)


def obj_teeth(t):
    """이빨이 닫힌다. 위 · 아래 턱."""
    im, d = canvas()
    gap = 10 * (1 - ease_out(min(1.0, t / 0.5))) + 1
    gum = (255, 110, 160, 255)
    d.rounded_rectangle([12 * S, (32 - gap - 9) * S, 52 * S, (32 - gap) * S], radius=4 * S, fill=gum)
    d.rounded_rectangle([12 * S, (32 + gap) * S, 52 * S, (32 + gap + 9) * S], radius=4 * S, fill=gum)
    tt, d2 = canvas()
    for x in range(15, 50, 6):
        poly(d2, [(x, 32 - gap - 1), (x + 5, 32 - gap - 1), (x + 2.5, 32 - gap + 5)], WHITE)
        poly(d2, [(x + 3, 32 + gap + 1), (x + 8, 32 + gap + 1), (x + 5.5, 32 + gap - 5)], WHITE)
    im.alpha_composite(tt)
    return inked(im)


ENERGY = {
    "fx_bark": fx_bark, "fx_sonar": fx_sonar, "fx_eyebeam": fx_eyebeam, "fx_gaze": fx_gaze, "fx_growl": fx_growl,
    "fx_howl": fx_howl, "fx_yawn": fx_yawn, "fx_static": fx_static, "fx_shield": fx_shield, "fx_awaken": fx_awaken,
    "fx_zoom": fx_zoom, "fx_taunt": fx_taunt, "fx_scent": fx_scent, "fx_tailsw": fx_tailswing, "fx_dogclaw": fx_dogclaw,
    "fx_paw": fx_paw, "fx_burst": fx_burst, "fx_dogdust": fx_dogdust, "fx_hit": fx_hit, "fx_shards": fx_shards,
    "fx_notebass": fx_notebass, "fx_notebeat": fx_notebeat, "fx_staffbeam": fx_staffbeam,
    "fx_notescale": fx_notescale, "fx_notewhole": fx_notewhole, "fx_chord": fx_chord,
}
OBJECT = {
    "fx_bone": obj_bone, "fx_ball": obj_ball, "fx_frisbee": obj_frisbee, "fx_slipper": obj_slipper,
    "fx_kibble": obj_kibble, "fx_bread": obj_bread, "fx_teeth": obj_teeth,
}
# 회전 · 반복형 투사체는 4 프레임 루프, 나머지 물건형(사료 터짐 · 식빵 · 이빨)은 6
LOOP4 = {"fx_bone", "fx_ball", "fx_frisbee", "fx_slipper"}


def frames_for(key):
    n = 4 if key in LOOP4 else N
    return [i / (n - 1) if key not in LOOP4 else i / n for i in range(n)]


def build_one(key):
    out_core, out_glow = [], []
    for t in frames_for(key):
        if key in ENERGY:
            core, spk = ENERGY[key](t)
            g = glow_of(core, sparkles=spk)
            k = fade(t)
            core, g = alpha_mul(core, k), alpha_mul(g, k)
        else:
            core = OBJECT[key](t)
            g = glow_of(core, grow=1.5, blur=2.6, strength=0.7)
            if key not in LOOP4:
                k = fade(t, 0.6)
                core, g = alpha_mul(core, k), alpha_mul(g, k)
        out_core.append(down(core))
        out_glow.append(down(g))
    return out_core, out_glow


def write_scale(keys):
    path = os.path.join(ROOT, "_DataExporter", "_art_scale.json")
    table = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
    for k in PURGE:
        table.pop("Assets/_Art/FX/" + k, None)
    for k in keys:
        table.pop("Assets/_Art/FX/" + k, None)
        table["Assets/_Art/FX/%s/core" % k] = OUT
        table["Assets/_Art/FX/%s/glow" % k] = OUT
    json.dump(table, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=1, sort_keys=True)


def main():
    keys = list(ENERGY) + list(OBJECT)
    for k in PURGE:
        p = os.path.join(DST, k)
        if os.path.isdir(p):
            shutil.rmtree(p)
        if os.path.exists(p + ".meta"):
            os.remove(p + ".meta")
    T = W * OUT // 2
    sheet = Image.new("RGBA", (T * 6, T * len(keys)), (40, 26, 60, 255))
    tints = {}
    for r, k in enumerate(keys):
        d = os.path.join(DST, k)
        # 옛 단층 프레임(00.png …)을 지우고 두 층 폴더로 다시 낸다
        if os.path.isdir(d):
            for f in os.listdir(d):
                p = os.path.join(d, f)
                if os.path.isfile(p):
                    os.remove(p)
        core, glow = build_one(k)
        for sub, frames in (("core", core), ("glow", glow)):
            os.makedirs(os.path.join(d, sub), exist_ok=True)
            for f in os.listdir(os.path.join(d, sub)):
                os.remove(os.path.join(d, sub, f))
            for i, im in enumerate(frames):
                im.save(os.path.join(d, sub, "%02d.png" % i))
        # 미리보기: 가산 근사 = 계열 색 번짐 + 심지
        tint = (60, 230, 255) if k in ENERGY else (160, 255, 60)
        for i in range(min(6, len(core))):
            g = glow[i].copy()
            col = Image.new("RGBA", g.size, tint + (255,))
            col.putalpha(g.split()[3])
            cell = Image.new("RGBA", g.size, (0, 0, 0, 0))
            cell.alpha_composite(col)
            cell.alpha_composite(core[i])
            sheet.alpha_composite(cell.resize((T, T), Image.LANCZOS), (i * T, r * T))
    write_scale(keys)
    sheet.save(PREVIEW)
    print("이펙트 %d종 (기운 %d · 물건 %d) · 옛 팩 %d 폴더 삭제 · 미리보기 %s" % (len(keys), len(ENERGY), len(OBJECT), len(PURGE), PREVIEW))


main()
