# -*- coding: utf-8 -*-
# 스킬 이펙트 생성기 v3 - SlayerDog 도트 (볼트 SlayerDog/SlayerDog 기획서.md · TASKS A-9)
#
# 이펙트 한 종 = 두 층. 프리팹은 Art2DBuilder 가 Glow · Core 두 자식으로 굽는다
#   core/NN.png  도트 심지 64 → 최근접 ×4 (256px). 캐릭터와 같은 도트 격자
#   glow/NN.png  심지 실루엣을 1도트씩 키운 흰 도트 링(알파 단계). 가산 머티리얼 + 데이터 색 틴트
# 종류 두 가지
#   기운(energy)  흰 심지 + 연회색 가장자리. 데이터 색을 25% 만 받아 하얗게 달아오른 빛으로 보인다
#   물건(object)  무기 · 투사체. pixkit 으로 제 색 · 외곽선 · 3단 명암 (틴트 0) — 주인공과 같은 그림 규칙
#   glow 는 도트 링(64 격자에서 1도트씩 팽창 · 알파 단계)이다 — 부드러운 번짐을 쓰지 않는다
# - 캔버스 64 = 2 유닛 = 판정 지름. 반경 있는 연출은 외곽이 반지름 30 근처에 닿게 (크기가 곧 사거리)
# - 앞 방향은 +x (오른쪽). Ground 연출(베기 · 화살 · 빔)은 오른쪽을 향해 그린다
# - 캐릭터 고유 공격 10종은 전용 이펙트 + 캐릭터 색 (gen_slayer.js HEROES 의 fx · color)
# - 표(폴더 · 주소 · 종류)는 _DataExporter/_fx_table.tsv 로 낸다. Art2DBuilder 가 그 표로 프리팹을 굽는다
#
# 사용: python build_vfx.py [--preview]

from PIL import Image, ImageDraw, ImageFilter
import os, sys, math, json, random, shutil
from pixkit import *
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\FX")
TABLE = os.path.join(ROOT, "_DataExporter", "_fx_table.tsv")
PREVIEW_DIR = os.path.join(ROOT, "_DataExporter", "preview")
UP = 4
N = 6

CORE = (255, 255, 255, 255)
EDGE = (206, 212, 232, 255)
FIRE = (255, 128, 40)
FLAME = (255, 210, 80)
SMOKE = (120, 116, 130)


# ---------------------------------------------------------------- 공용

def ease(t):
    return 1 - (1 - t) ** 3


def fade(t, start=0.55):
    if t <= start:
        return 1.0
    return max(0.0, 1 - (t - start) / (1 - start))


# 지금 굽는 이펙트의 캔버스 배율. 판정 반경이 큰 이펙트는 화면에서 그만큼 커지므로(SkillVisual.ApplyHitScale)
# 64 × K 캔버스에 같은 좌표로 그려 도트를 K 배 촘촘하게 둔다 — 화면 크기는 PPU 가 K 배라 그대로, 도트만 캐릭터와 같아진다
K = 1.0


class KDraw:
    """64 격자 좌표를 받아 64×K 캔버스에 그린다. 선 두께도 K 배 (화면 두께 유지, 가장자리만 촘촘)."""

    def __init__(self, im, k):
        self.d = ImageDraw.Draw(im)
        self.k = k

    def _pt(self, xy):
        k = self.k
        return (xy[0] * k + (k - 1) / 2, xy[1] * k + (k - 1) / 2)

    def _pts(self, xy):
        if len(xy) and isinstance(xy[0], (int, float)):
            xy = [(xy[i], xy[i + 1]) for i in range(0, len(xy), 2)]
        return [self._pt(p) for p in xy]

    def _box(self, b):
        k = self.k
        if len(b) == 2:
            b = (b[0][0], b[0][1], b[1][0], b[1][1])
        return [b[0] * k, b[1] * k, (b[2] + 1) * k - 1, (b[3] + 1) * k - 1]

    def _w(self, kw):
        if "width" in kw:
            kw["width"] = max(1, int(round(kw["width"] * self.k)))
        return kw

    def ellipse(self, b, **kw):
        if "outline" in kw and "width" not in kw:
            kw["width"] = 1
        self.d.ellipse(self._box(b), **self._w(kw))

    def rectangle(self, b, **kw):
        self.d.rectangle(self._box(b), **self._w(kw))

    def pieslice(self, b, start, end, **kw):
        self.d.pieslice(self._box(b), start, end, **self._w(kw))

    def line(self, xy, **kw):
        if "width" not in kw:
            kw["width"] = 1
        self.d.line(self._pts(xy), **self._w(kw))

    def polygon(self, xy, **kw):
        self.d.polygon(self._pts(xy), **kw)

    def point(self, xy, fill=None):
        for p in self._pts(xy):
            k = self.k
            self.d.rectangle([p[0] - (k - 1) / 2, p[1] - (k - 1) / 2, p[0] + (k - 1) / 2, p[1] + (k - 1) / 2], fill=fill)


def blank():
    n = int(round(W * K))
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    return im, KDraw(im, K)


def band(d, cx, cy, r, w_fn, a0, a1, c, steps=40):
    """반지름 r 바깥선, 안쪽으로 w_fn(u) 두께인 호. 각도는 도(0 = +x, 시계 = +y)."""
    outer, inner = [], []
    for i in range(steps + 1):
        u = i / steps
        a = math.radians(a0 + (a1 - a0) * u)
        w = w_fn(u)
        outer.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        inner.append((cx + math.cos(a) * (r - w), cy + math.sin(a) * (r - w)))
    d.polygon(outer + inner[::-1], fill=c)


def ring(d, cx, cy, r, w, c):
    if r <= 0 or w <= 0:
        return
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=c, width=max(1.0, w))


def star4(d, x, y, r, c):
    d.polygon([(x, y - r), (x + r * 0.3, y - r * 0.3), (x + r, y), (x + r * 0.3, y + r * 0.3),
               (x, y + r), (x - r * 0.3, y + r * 0.3), (x - r, y), (x - r * 0.3, y - r * 0.3)], fill=c)


def zig(d, pts, c, w=1):
    d.line(pts, fill=c, width=w)


def alpha_mul(im, k):
    if k >= 0.999:
        return im
    out = im.copy()
    out.putalpha(im.split()[3].point(lambda v: int(v * k)))
    return out


def glow_of(core64, grow=2, blur=3.0, strength=1.0):
    # 번짐도 도트로: 64 격자에서 심지를 1도트씩 키운 링을 알파 단계(안쪽 진하게)로 쌓고 최근접으로 키운다
    # grow = 링 수, blur 는 호환용(안 쓴다). 가우시안 번짐은 도트 캐릭터 옆에서 흐릿한 PC 이펙트로 읽힌다
    a0 = core64.split()[3].point(lambda v: 255 if v > 40 else 0)
    rings = max(1, grow + 1)
    steps = [0.62, 0.36, 0.18, 0.09]
    # 링 한 겹 = 64 격자 1 도트 = 캔버스 K 픽셀
    step = 2 * max(1, int(round(K))) + 1
    masks = [a0]
    for i in range(rings):
        masks.append(masks[-1].filter(ImageFilter.MaxFilter(step)))
    # 바깥 링부터 칠하고 안쪽이 덮는다
    out = Image.new("L", core64.size, 0)
    for i in range(rings, 0, -1):
        level = int(255 * steps[min(i - 1, len(steps) - 1)] * strength)
        out.paste(Image.new("L", core64.size, level), (0, 0), masks[i])
    # 심지 자리 자체에도 은은하게 (가산 틴트가 심지 색을 데운다)
    out.paste(Image.new("L", core64.size, int(255 * steps[0] * strength)), (0, 0), a0)
    size = (core64.width * UP, core64.height * UP)
    g = Image.new("RGBA", size, (255, 255, 255, 0))
    g.putalpha(out.resize(size, Image.NEAREST))
    return g


def obj(fn):
    f = Fig(K)
    fn(f)
    return render(f, shadow_w=0)


def spin(im, deg):
    return im.rotate(-deg, resample=Image.NEAREST, center=(im.width / 2, im.height / 2))


# ---------------------------------------------------------------- 캐릭터 고유 10

def e_iaido(t):
    """시바 발도 일섬 — 오른쪽으로 휘두르는 반달 + 벚꽃잎."""
    im, d = blank()
    sweep = ease(min(1.0, t * 1.8))
    a0, a1 = -75, -75 + 150 * sweep
    if a1 > a0 + 4:
        band(d, 22, 32, 26, lambda u: 1 + 5 * math.sin(math.pi * u), a0, a1, EDGE)
        band(d, 22, 32, 25, lambda u: 0.5 + 3 * math.sin(math.pi * u), a0, a1, CORE)
    rnd = random.Random(3)
    for i in range(7):
        a = math.radians(rnd.uniform(-70, 70))
        r = 22 + t * rnd.uniform(4, 12)
        x, y = 22 + math.cos(a) * r, 32 + math.sin(a) * r + t * 4
        if t > 0.15:
            d.polygon([(x, y), (x + 2, y - 1), (x + 3, y + 1), (x + 1, y + 2)], fill=CORE)
    return im


def o_arrow_short(f):
    """진돗개 편전 — 짧은 화살, 붉은 깃."""
    f.p(WOOD, light=False).l((16, 32), (46, 32), w=2)
    f.p(IRON).g((45, 29), (53, 32), (45, 35))
    f.p((210, 50, 40), light=False).g((15, 31), (20, 31), (16, 26), (12, 26))
    f.p((210, 50, 40), light=False).g((15, 33), (20, 33), (16, 38), (12, 38))


def o_shuriken(f):
    """아키타 수리검 — 네 날 강철 별."""
    f.p(SILVER).g((32, 21), (35, 29), (43, 32), (35, 35), (32, 43), (29, 35), (21, 32), (29, 29))
    f.p((70, 74, 90), light=False).e(29, 29, 35, 35)
    f.flat((40, 36, 48)).r(31, 31, 33, 33)


def e_palm(t):
    """차우차우 장풍 — 퍼지는 기의 고리 두 겹 + 가운데 손바닥."""
    im, d = blank()
    r = 6 + 24 * ease(t)
    ring(d, 32, 32, r, 3 * (1 - t) + 1, CORE)
    ring(d, 32, 32, max(1, r - 6), 2 * (1 - t) + 1, EDGE)
    if t < 0.5:
        d.ellipse([27, 31, 37, 41], fill=CORE)
        for x, y in ((27, 27), (30, 25), (34, 25), (37, 27)):
            d.ellipse([x - 1, y - 2, x + 1, y + 1], fill=CORE)
    return im


def o_talisman(f, sway):
    """퍼그 추혼부 — 붉은 글자 노란 부적."""
    k = int(round(sway))
    f.p((250, 222, 90)).g((26 + k, 20), (38 + k, 20), (38 - k, 44), (26 - k, 44))
    f.flat(RED).l((32 + k, 23), (30, 27), (34, 30), (31, 34), (33, 38))
    f.flat(RED).r(28, 40, 36, 41)
    f.p((220, 60, 50), light=False).l((32 - k, 44), (32 - 2 * k, 50))


def e_holy(t):
    """셰퍼드 심판 — 발밑 성광 원 + 십자 + 빛줄기."""
    im, d = blank()
    p = 0.85 + 0.15 * math.sin(t * math.pi * 2)
    ring(d, 32, 32, 29, 2, CORE)
    ring(d, 32, 32, 23, 1, EDGE)
    w = int(3 * p)
    d.rectangle([32 - w // 2 - 1, 16, 32 + w // 2, 48], fill=CORE)
    d.rectangle([20, 26, 44, 26 + w], fill=CORE)
    for i in range(8):
        a = math.radians(i * 45 + t * 30)
        d.line([(32 + math.cos(a) * 24, 32 + math.sin(a) * 24), (32 + math.cos(a) * 28, 32 + math.sin(a) * 28)], fill=EDGE)
    return im


def o_fireball(f, k):
    """푸들 화염구 — 겹 불꽃 공 + 위로 일렁이는 꼬리."""
    j = (k % 2) * 2 - 1
    f.p(FIRE, line=True).g((24, 34), (30, 18 + j), (33, 26), (37, 16 - j), (40, 34))
    f.p(FIRE).e(23, 25, 41, 43)
    f.p(FLAME, line=False).e(27, 29, 37, 39)
    f.flat((255, 250, 220)).e(30, 31, 34, 35)


def o_blast(f, t):
    """화염구 · 폭뢰 · 화염 단지 착탄 — 불꽃 폭발 뒤 연기."""
    r = 6 + 20 * ease(t)
    if t < 0.75:
        f.p(FIRE).e(32 - r, 32 - r, 32 + r, 32 + r)
        rr = r * 0.62
        f.p(FLAME, line=False).e(32 - rr, 32 - rr, 32 + rr, 32 + rr)
        if t < 0.4:
            rc = r * 0.3
            f.flat((255, 250, 230)).e(32 - rc, 32 - rc, 32 + rc, 32 + rc)
    else:
        for dx, dy in ((-10, -6), (8, -9), (0, 6), (-6, 10), (11, 7)):
            s = 6 * (1.2 - t)
            f.p(SMOKE).e(32 + dx - s, 32 + dy - s, 32 + dx + s, 32 + dy + s)


def e_axewhirl(t):
    """허스키 회전 도끼 — 몸을 도는 두 날 + 바람 고리."""
    im, d = blank()
    rot = t * 360
    ring(d, 32, 32, 26, 1, EDGE)
    for base in (0, 180):
        a = base + rot
        band(d, 32, 32, 27, lambda u: 1 + 4 * u, a - 70, a, EDGE)
        band(d, 32, 32, 27, lambda u: 0.5 + 2.5 * u, a - 50, a, CORE)
        x, y = 32 + math.cos(math.radians(a)) * 24, 32 + math.sin(math.radians(a)) * 24
        d.polygon([(x - 4, y - 4), (x + 4, y - 4), (x + 5, y + 4), (x - 5, y + 4)], fill=CORE)
    return im


def o_longarrow(f):
    """비글 관통 화살 — 긴 화살, 초록 깃."""
    f.p(WOOD, light=False).l((6, 32), (50, 32), w=2)
    f.p(IRON).g((49, 28), (58, 32), (49, 36))
    f.p((80, 170, 70), light=False).g((6, 31), (13, 31), (9, 25), (3, 25))
    f.p((80, 170, 70), light=False).g((6, 33), (13, 33), (9, 39), (3, 39))


def o_pellet(f):
    """닥스훈트 산탄 — 쇠구슬 셋 + 불티."""
    for x, y in ((30, 30), (35, 34), (29, 36)):
        f.p(IRON).e(x - 2, y - 2, x + 2, y + 2)
    f.flat(FLAME).d1((25, 32), (24, 34), (26, 29))


def e_muzzle(t):
    """핸드캐넌 · 화살 발사 — 앞쪽으로 튀는 섬광."""
    im, d = blank()
    r = 5 + 8 * ease(t)
    star4(d, 38, 32, r, CORE)
    d.ellipse([34, 29, 42, 35], fill=CORE)
    return im


# ---------------------------------------------------------------- 무기 (판타지)

def o_seal(f, k):
    """폭뢰 부적 — 붉은 테 부적, 깜빡이는 인."""
    f.p((240, 226, 200)).r(24, 18, 40, 46)
    f.p((200, 40, 40), line=False).r(25, 19, 39, 20)
    f.p((200, 40, 40), line=False).r(25, 44, 39, 45)
    glyph = (230, 40, 40) if k % 2 == 0 else (255, 120, 60)
    f.flat(glyph).e(28, 26, 36, 34)
    f.flat((240, 226, 200)).e(30, 28, 34, 32)
    f.flat(glyph).l((32, 35), (32, 41))


def o_boomerang(f):
    f.p(WOOD).g((20, 24), (26, 22), (34, 32), (26, 42), (20, 40), (27, 32))
    f.p((200, 60, 50), light=False).g((21, 25), (24, 24), (26, 27), (23, 28))


def o_chakram(f):
    f.p(GOLD).e(20, 20, 44, 44)
    for i in range(8):
        a = math.radians(i * 45)
        x, y = 32 + math.cos(a) * 12, 32 + math.sin(a) * 12
        f.p(GOLD).g((x - 2, y - 2), (32 + math.cos(a) * 16, 32 + math.sin(a) * 16), (x + 2, y + 2))
    f.p((60, 50, 40), line=False).e(26, 26, 38, 38)


def o_kunai(f):
    f.p(SILVER).g((30, 29), (50, 32), (30, 35))
    f.p((60, 50, 60), light=False).r(20, 31, 30, 33)
    f.p(IRON).e(14, 29, 20, 35)
    f.flat((0, 0, 0)).e(16, 31, 18, 33)


def o_firejar(f, k):
    f.p((170, 100, 60)).e(24, 28, 40, 46)
    f.p((170, 100, 60)).r(28, 22, 36, 30)
    f.p((240, 230, 200), light=False).r(27, 20, 37, 23)
    j = (k % 2) * 2 - 1
    f.p(FIRE).g((28, 21), (31, 12 + j), (33, 17), (36, 11 - j), (37, 21))


def o_arrowrain(f, t):
    """화살비 — 비스듬히 내리꽂히는 화살 다섯."""
    rnd = random.Random(7)
    for i in range(5):
        x0 = 10 + i * 10 + rnd.randrange(-2, 3)
        delay = i * 0.08
        u = max(0.0, min(1.0, (t - delay) * 2.2))
        y1 = 18 + u * 30
        x1 = x0 + u * 6
        f.p(WOOD, light=False).l((x1 - 6, y1 - 14), (x1, y1), w=1)
        f.p(IRON, light=False).g((x1 - 1, y1 - 1), (x1 + 2, y1 + 3), (x1 + 1, y1 - 2))


def o_greatsword(f, t):
    """대검 내려치기 — 위에서 꽂히는 대검 + 갈라지는 땅."""
    u = ease(min(1.0, t * 2.2))
    y = -14 + 30 * u
    f.p(SILVER).g((29, y), (35, y), (35, y + 26), (32, y + 31), (29, y + 26))
    f.p(GOLD).r(24, y - 2, 40, y + 1)
    f.p((110, 60, 40)).r(30, y - 10, 34, y - 2)
    if t > 0.4:
        cr = f.flat((60, 50, 50))
        cr.l((32, 47), (22, 52), (14, 50))
        cr.l((32, 47), (42, 53), (50, 51))
        cr.l((32, 47), (30, 56))


def o_fang(f, t):
    """흡혈 송곳니 — 닫히는 송곳니 + 핏방울."""
    gap = int(8 * (1 - ease(min(1.0, t * 2)))) + 1
    gum = (190, 40, 60)
    f.p(gum).r(16, 32 - gap - 7, 48, 32 - gap)
    f.p(gum).r(16, 32 + gap, 48, 32 + gap + 7)
    for x in range(18, 46, 6):
        f.flat(WHITE).g((x, 32 - gap), (x + 4, 32 - gap), (x + 2, 32 - gap + 4))
        f.flat(WHITE).g((x + 3, 32 + gap), (x + 7, 32 + gap), (x + 5, 32 + gap - 4))
    if t > 0.45:
        f.p(RED).e(40, 44, 43, 48)


def o_meteor(f, t):
    """유성 낙하 — 불꼬리 달린 바위가 떨어져 터진다."""
    if t < 0.6:
        u = t / 0.6
        x, y = 12 + 20 * u, 6 + 26 * u
        f.p(FIRE, line=False).g((x - 3, y - 2), (x - 16, y - 18), (x - 8, y - 18), (x + 3, y - 3))
        f.p((110, 90, 90)).e(x - 5, y - 5, x + 5, y + 5)
        f.flat((160, 130, 120)).r(x - 2, y - 3, x, y - 2)
    else:
        o_blast(f, (t - 0.6) / 0.4 * 0.8)


def o_knife(f):
    f.p(SILVER).g((28, 30), (46, 32), (28, 34))
    f.p((150, 40, 40), light=False).r(20, 31, 28, 33)


def o_spear(f, t):
    u = ease(min(1.0, t * 2))
    x = -10 + 18 * u
    f.p(WOOD, light=False).l((x, 32), (x + 40, 32), w=2)
    f.p(SILVER).g((x + 40, 28), (x + 52, 32), (x + 40, 36))
    f.p(RED, light=False).g((x + 38, 30), (x + 34, 27), (x + 34, 37), (x + 38, 34))


def o_chainwhip(f, t):
    u = ease(min(1.0, t * 1.8))
    n = int(4 + 8 * u)
    for i in range(n):
        x = 8 + i * 4
        y = 32 + math.sin(i * 0.9 + t * 6) * 3 * (1 - u * 0.5)
        f.p(IRON).e(x - 2, y - 2, x + 2, y + 2)
    if n >= 12:
        f.p(SILVER).g((54, 28), (60, 32), (54, 36), (52, 32))


def e_frost(t):
    """서리 숨결 — 앞쪽 부채꼴 냉기 + 눈송이."""
    im, d = blank()
    r = 8 + 22 * ease(t)
    d.pieslice([4 - r + 28, 32 - r, 4 + r + 28, 32 + r], -30, 30, fill=EDGE)
    d.pieslice([4 - r * 0.7 + 28, 32 - r * 0.7, 4 + r * 0.7 + 28, 32 + r * 0.7], -22, 22, fill=CORE)
    rnd = random.Random(2)
    for _ in range(5):
        a = math.radians(rnd.uniform(-25, 25))
        rr = rnd.uniform(0.5, 1.0) * r
        x, y = 32 + math.cos(a) * rr, 32 + math.sin(a) * rr
        d.line([(x - 2, y), (x + 2, y)], fill=CORE)
        d.line([(x, y - 2), (x, y + 2)], fill=CORE)
    return im


def e_roar(t):
    """사자후 — 세 겹 충격파."""
    im, d = blank()
    for k in range(3):
        u = max(0.0, min(1.0, t * 1.4 - k * 0.18))
        if u > 0:
            ring(d, 32, 32, 4 + 26 * ease(u), 3 * (1 - u) + 1, CORE if k == 0 else EDGE)
    return im


def e_rune(t):
    """공명진 · 맥동 룬 — 룬 글자가 박힌 고리가 퍼진다."""
    im, d = blank()
    r = 10 + 19 * ease(t)
    ring(d, 32, 32, r, 2, CORE)
    ring(d, 32, 32, max(1, r - 5), 1, EDGE)
    for i in range(6):
        a = math.radians(i * 60 + t * 40)
        x, y = 32 + math.cos(a) * (r - 2.5), 32 + math.sin(a) * (r - 2.5)
        d.line([(x - 1, y - 2), (x + 1, y + 2)], fill=CORE)
        d.line([(x + 1, y - 2), (x - 1, y)], fill=CORE)
    return im


def e_lightray(t):
    """빛의 창 — 앞쪽 긴 빛줄기 + 창날."""
    im, d = blank()
    flick = 1 if int(t * 10) % 2 else 0
    d.rectangle([2, 30 - flick, 52, 34 + flick], fill=EDGE)
    d.rectangle([2, 31, 52, 33], fill=CORE)
    d.polygon([(52, 27), (62, 32), (52, 37)], fill=CORE)
    return im


def e_qibolt(t):
    """심안 관통 — 길쭉한 기의 마름모 + 잔광."""
    im, d = blank()
    d.polygon([(14, 32), (36, 28), (52, 32), (36, 36)], fill=EDGE)
    d.polygon([(22, 32), (38, 30), (50, 32), (38, 34)], fill=CORE)
    for i, x in enumerate((8, 12)):
        d.point((x, 32), fill=EDGE)
    return im


def e_magicshot(t):
    """추적 마탄 — 마력 구슬 + 감기는 꼬리."""
    im, d = blank()
    d.ellipse([27, 27, 37, 37], fill=EDGE)
    d.ellipse([29, 29, 35, 35], fill=CORE)
    for i in range(10):
        a = i * 0.7 + t * 6
        r = 6 + i * 0.9
        d.point((32 - i * 1.6 + math.cos(a) * 2, 32 + math.sin(a) * r * 0.3), fill=CORE)
    return im


def e_lightning(t):
    """연쇄 번개 — 지그재그 번개."""
    im, d = blank()
    rnd = random.Random(int(t * 5))
    pts = [(2, 32)]
    for x in range(8, 64, 7):
        pts.append((x, 32 + rnd.randint(-6, 6)))
    zig(d, pts, EDGE, 3)
    zig(d, pts, CORE, 1)
    return im


def e_thunder(t):
    """뇌운 — 위에서 내리꽂는 번개 + 바닥 섬광 (Billboard)."""
    im, d = blank()
    rnd = random.Random(int(t * 6) + 4)
    pts = [(32, 0)]
    for y in range(6, 54, 6):
        pts.append((32 + rnd.randint(-5, 5), y))
    pts.append((32, 56))
    if t < 0.7:
        zig(d, pts, EDGE, 4)
        zig(d, pts, CORE, 2)
    r = 4 + 10 * ease(t)
    d.ellipse([32 - r, 56 - r * 0.4, 32 + r, 56 + r * 0.4], fill=CORE)
    return im


def e_bladeswirl(t):
    """회오리 베기 · 회전 검무 — 도는 칼날 세 개."""
    im, d = blank()
    for base in (0, 120, 240):
        a = base + t * 360
        band(d, 32, 32, 28, lambda u: 1 + 4 * u, a - 60, a, EDGE)
        band(d, 32, 32, 27, lambda u: 0.5 + 2.5 * u, a - 40, a, CORE)
    return im


def e_afterimage(t):
    """분신 질주 — 흩어지는 바람 자국."""
    im, d = blank()
    for i, (x, y) in enumerate(((20, 26), (30, 36), (40, 28))):
        r = 4 + 6 * ease(t) - i
        ring(d, x, y, max(1, r), 1, CORE if i == 1 else EDGE)
        d.line([(x - r - 6, y), (x - r - 1, y)], fill=EDGE)
    return im


def e_barrier(t):
    """수호 결계 — 육각 방패."""
    im, d = blank()
    r = 24 + 2 * math.sin(t * math.pi * 2)
    pts = [(32 + math.cos(math.radians(a)) * r, 32 + math.sin(math.radians(a)) * r) for a in range(30, 390, 60)]
    d.line(pts + [pts[0]], fill=CORE, width=2)
    inner = [(32 + (x - 32) * 0.6, 32 + (y - 32) * 0.6) for x, y in pts]
    d.line(inner + [inner[0]], fill=EDGE, width=1)
    return im


def e_rage(t):
    """광폭화 — 몸에서 솟는 불꽃 기운."""
    im, d = blank()
    rnd = random.Random(int(t * 6))
    for i in range(6):
        x = 18 + i * 6 + rnd.randint(-1, 1)
        h = 10 + rnd.randint(0, 12)
        d.polygon([(x - 3, 54), (x, 54 - h), (x + 3, 54)], fill=EDGE if i % 2 else CORE)
    return im


def e_steadfast(t):
    """부동심 — 땅에서 솟는 금빛 기둥 고리."""
    im, d = blank()
    d.ellipse([12, 38, 52, 54], outline=CORE, width=2)
    for x in (16, 24, 32, 40, 48):
        h = 10 + 18 * ease(t) * (1 - abs(x - 32) / 24)
        d.line([(x, 46), (x, 46 - h)], fill=EDGE if x != 32 else CORE, width=2 if x == 32 else 1)
    return im


def e_sleep(t):
    """잠의 주문 — 앞쪽으로 퍼지는 소용돌이 + z."""
    im, d = blank()
    for k in range(3):
        u = max(0.0, min(1.0, t * 1.3 - k * 0.2))
        x, y = 18 + k * 12, 34 - k * 6 - u * 6
        s = 3 + k
        d.line([(x - s, y - s), (x + s, y - s), (x - s, y + s), (x + s, y + s)], fill=CORE, width=1)
    ring(d, 32, 32, 6 + 18 * ease(t), 1, EDGE)
    return im


def e_gale(t):
    """질풍 — 몸을 감는 바람 선."""
    im, d = blank()
    for i, y in enumerate((22, 30, 38, 46)):
        x0 = 6 + i * 3 + t * 10
        d.line([(x0, y), (x0 + 16 + i * 2, y)], fill=CORE if i % 2 == 0 else EDGE)
    return im


def e_paw(t):
    """발 도장 — 할퀴기 · 물기 시전."""
    im, d = blank()
    s = 0.6 + 0.4 * ease(min(1.0, t * 2))
    d.ellipse([32 - 7 * s, 34 - 5 * s, 32 + 7 * s, 34 + 6 * s], fill=CORE)
    for dx, dy in ((-7, -6), (-2, -9), (3, -9), (8, -6)):
        x, y = 32 + dx * s, 34 + dy * s
        d.ellipse([x - 2 * s, y - 2 * s, x + 2 * s, y + 2 * s], fill=CORE)
    return im


def e_hit(t):
    im, d = blank()
    r = 4 + 9 * ease(t)
    star4(d, 32, 32, r, CORE)
    d.ellipse([29, 29, 35, 35], fill=CORE)
    return im


def e_shards(t):
    im, d = blank()
    r = 4 + 18 * ease(t)
    for i in range(8):
        a = math.radians(i * 45 + 20)
        x, y = 32 + math.cos(a) * r, 32 + math.sin(a) * r
        d.polygon([(x, y), (x + math.cos(a) * 4, y + math.sin(a) * 4), (x + 1, y + 1)], fill=CORE)
    return im


# ---------------------------------------------------------------- 표: 폴더 · 주소 · 종류 · 프레임

def E(fn):
    return ("energy", [fn(i / (N - 1)) for i in range(N)], False)


def O_static(fn, loop=True):
    im = obj(fn)
    return ("object", [im] * 4, loop)


def O_spin(fn, step=22.5):
    im = obj(fn)
    return ("object", [spin(im, i * step) for i in range(4)], True)


def O_frames(fn, n=N, loop=False):
    return ("object", [obj(lambda f, k=k: fn(f, k / (n - 1) if not loop else k)) for k in range(n)], loop)


FX = {
    # 캐릭터 고유 10 (+ 착탄 · 발사)
    "fx_iaido": ("Fx_Iaido", lambda: E(e_iaido)),
    "fx_arrow": ("Fx_Arrow", lambda: O_static(o_arrow_short)),
    "fx_shuriken": ("Fx_Shuriken", lambda: O_spin(o_shuriken)),
    "fx_palm": ("Fx_Palm", lambda: E(e_palm)),
    "fx_talisman": ("Fx_Talisman", lambda: ("object", [obj(lambda f, s=s: o_talisman(f, s)) for s in (-2, 0, 2, 0)], True)),
    "fx_holy": ("Fx_Holy", lambda: E(e_holy)),
    "fx_fireball": ("Fx_Fireball", lambda: O_frames(o_fireball, 4, loop=True)),
    "fx_blast": ("Fx_Blast", lambda: O_frames(o_blast)),
    "fx_axewhirl": ("Fx_AxeWhirl", lambda: E(e_axewhirl)),
    "fx_longarrow": ("Fx_LongArrow", lambda: O_static(o_longarrow)),
    "fx_pellet": ("Fx_Pellet", lambda: O_static(o_pellet)),
    "fx_muzzle": ("Fx_Muzzle", lambda: E(e_muzzle)),
    # 무기
    "fx_seal": ("Fx_Seal", lambda: O_frames(o_seal, 4, loop=True)),
    "fx_boomerang": ("Fx_Boomerang", lambda: O_spin(o_boomerang, 90)),
    "fx_chakram": ("Fx_Chakram", lambda: O_spin(o_chakram, 11.25)),
    "fx_kunai": ("Fx_Kunai", lambda: O_static(o_kunai)),
    "fx_firejar": ("Fx_FireJar", lambda: ("object", [spin(obj(lambda f, k=k: o_firejar(f, k)), k * 90) for k in range(4)], True)),
    "fx_arrowrain": ("Fx_ArrowRain", lambda: O_frames(o_arrowrain)),
    "fx_greatsword": ("Fx_Greatsword", lambda: O_frames(o_greatsword)),
    "fx_fang": ("Fx_Fang", lambda: O_frames(o_fang)),
    "fx_meteor": ("Fx_Meteor", lambda: O_frames(o_meteor)),
    "fx_knife": ("Fx_Knife", lambda: O_static(o_knife)),
    "fx_spear": ("Fx_Spear", lambda: O_frames(o_spear)),
    "fx_chainwhip": ("Fx_ChainWhip", lambda: O_frames(o_chainwhip)),
    "fx_frost": ("Fx_Frost", lambda: E(e_frost)),
    "fx_roar": ("Fx_Roar", lambda: E(e_roar)),
    "fx_rune": ("Fx_Rune", lambda: E(e_rune)),
    "fx_lightray": ("Fx_LightRay", lambda: E(e_lightray)),
    "fx_qibolt": ("Fx_QiBolt", lambda: E(e_qibolt)),
    "fx_magicshot": ("Fx_MagicShot", lambda: E(e_magicshot)),
    "fx_lightning": ("Fx_Lightning", lambda: E(e_lightning)),
    "fx_thunder": ("Fx_Thunder", lambda: E(e_thunder)),
    "fx_bladeswirl": ("Fx_BladeSwirl", lambda: E(e_bladeswirl)),
    "fx_afterimage": ("Fx_Afterimage", lambda: E(e_afterimage)),
    "fx_barrier": ("Fx_Barrier", lambda: E(e_barrier)),
    "fx_rage": ("Fx_Rage", lambda: E(e_rage)),
    "fx_steadfast": ("Fx_Steadfast", lambda: E(e_steadfast)),
    "fx_sleep": ("Fx_Sleep", lambda: E(e_sleep)),
    "fx_gale": ("Fx_Gale", lambda: E(e_gale)),
    # 공용 (코드 키: CommonConfig HitFxKey · HitBurstKey, 시전 발자국)
    "fx_paw": ("Fx_Paw", lambda: E(e_paw)),
    "fx_hit": ("Fx_Hit", lambda: E(e_hit)),
    "fx_shards": ("Fx_HitBurst", lambda: E(e_shards)),
}


# 공용 키 (HitEffect 가 작게 띄운다) 는 배율 1
COMMON = ("fx_paw", "fx_hit", "fx_shards")


def scale_table():
    """이펙트 주소별 캔버스 배율 K. SkillVisual.ApplyHitScale 과 같은 식(반경 → 배율, 1.15~3.5, 반경 없음 1.45)으로
    그 이펙트를 쓰는 스킬 중 가장 큰 화면 배율을 0.5 단위로 내림 — 크게 쓰는 쪽 도트를 캐릭터에 맞춘다."""
    path = os.path.join(ROOT, r"Assets\Resources\GameData\Skill.json")
    if os.path.exists(path) is False:
        return {}
    out = {}

    def walk(n, acc):
        acc.append(n)
        for c in n.get("children", []):
            walk(c, acc)

    for sk in json.load(open(path, encoding="utf-8-sig")):
        nodes = []
        walk(sk["tree"], nodes)
        hits = [n for n in nodes if n["type"].startswith("Hit")]
        r = hits[0]["params"].get("Radius", 0) if hits else 0
        sc = 1.45 if r <= 0 else max(1.15, min(3.5, r))
        for n in nodes:
            v = n["params"].get("Visual")
            if v:
                out[v] = max(out.get(v, 1.0), sc)
    return {v: max(1.0, math.floor(sc * 2) / 2) for v, sc in out.items()}


def build(kind, frames, loop):
    cores, glows = [], []
    for i, im in enumerate(frames):
        k = 1.0
        if kind == "energy":
            k = fade(i / max(1, len(frames) - 1))
        elif not loop:
            k = fade(i / max(1, len(frames) - 1), 0.7)
        g = glow_of(im, grow=2 if kind == "energy" else 1, blur=3.0 if kind == "energy" else 2.2,
                    strength=1.0 if kind == "energy" else 0.3)
        cores.append(alpha_mul(big(im, UP), k))
        glows.append(alpha_mul(g, k))
    return cores, glows


def write_scale(keys, ks):
    path = os.path.join(ROOT, "_DataExporter", "_art_scale.json")
    table = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
    for k in [k for k in table if k.startswith("Assets/_Art/FX/")]:
        table.pop(k)
    for k in keys:
        table["Assets/_Art/FX/%s/core" % k] = int(round(UP * ks.get(k, 1.0)))
        table["Assets/_Art/FX/%s/glow" % k] = int(round(UP * ks.get(k, 1.0)))
    json.dump(table, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=1, sort_keys=True)


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    global K
    table = scale_table()
    ks = {k: (1.0 if k in COMMON else table.get(addr, 1.0)) for k, (addr, _) in FX.items()}
    built, kinds = {}, {}
    for k, (_, mk) in FX.items():
        K = ks[k]
        made = mk()
        kinds[k] = made[0]
        built[k] = build(*made)
    K = 1.0
    print("캔버스 배율: " + " ".join("%s=%g" % (k[3:], v) for k, v in ks.items() if v > 1))

    if not preview_only:
        # 표에 없는 옛 폴더(음악 · 개 소품 이펙트)는 폴더째 지운다
        for name in os.listdir(DST):
            p = os.path.join(DST, name)
            if name.startswith("fx_") and name not in FX:
                if os.path.isdir(p):
                    shutil.rmtree(p)
                elif name.endswith(".meta") and name[:-5] not in FX:
                    os.remove(p)
        for k, (cores, glows) in built.items():
            d = os.path.join(DST, k)
            for sub, frames in (("core", cores), ("glow", glows)):
                sd = os.path.join(d, sub)
                os.makedirs(sd, exist_ok=True)
                for f in os.listdir(sd):
                    os.remove(os.path.join(sd, f))
                for i, im in enumerate(frames):
                    im.save(os.path.join(sd, "%02d.png" % i))
        write_scale(list(FX), ks)
        with open(TABLE, "w", encoding="utf-8", newline="\n") as fp:
            fp.write("# build_vfx.py 가 쓴다. 폴더\t주소\t종류(energy=흰 심지 틴트 / object=제 색)\n")
            for k, (addr, _) in FX.items():
                fp.write("%s\t%s\t%s\n" % (k, addr, kinds[k]))

    # 미리보기: 어두운 바닥 위에 번짐(청록 근사) + 심지, 프레임 6칸
    T = 96
    sheet = Image.new("RGBA", (T * 6, T * len(FX)), (34, 30, 44, 255))
    for r, (k, (cores, glows)) in enumerate(built.items()):
        for i in range(min(6, len(cores))):
            g = glows[i]
            col = Image.new("RGBA", g.size, (120, 200, 255, 255))
            col.putalpha(g.split()[3].point(lambda v: v // 2))
            cell = Image.new("RGBA", g.size, (0, 0, 0, 0))
            cell.alpha_composite(col)
            cell.alpha_composite(cores[i])
            sheet.alpha_composite(cell.resize((T, T), Image.NEAREST), (i * T, r * T))
    sheet.save(os.path.join(PREVIEW_DIR, "vfx_all.png"))
    print("이펙트 %d종 (기운 %d · 물건 %d)%s" % (len(FX), sum(1 for v in kinds.values() if v == "energy"),
                                        sum(1 for v in kinds.values() if v == "object"), " (미리보기만)" if preview_only else ""))


main()
