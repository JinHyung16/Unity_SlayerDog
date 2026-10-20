# -*- coding: utf-8 -*-
# 도트 공용 엔진 — build_heroes.py · build_stage.py 가 같이 쓴다
#
# - 64×64 논리 캔버스를 픽셀 그대로 찍는다. 발끝(바닥선) y 61
# - 부위마다 단색으로 칠하고 명암은 자동 3단: 오른쪽 아래 2도트 그늘, 왼쪽 위 1도트 하이라이트
# - 외곽선 1도트, 색은 이웃한 칠의 동색 어두운 톤. 겹친 부위 경계에도 위 부위의 동색 선
# - 출력은 최근접 정수배 확대 (big). Bilinear + 밉맵으로 반입한다 (build_meta)

from PIL import Image, ImageDraw
import numpy as np
import colorsys

W = H = 64
FOOT = 61

BLACK = (34, 28, 36)
WHITE = (250, 250, 250)
PINK = (240, 140, 150)
BLUSH = (250, 160, 170)
GOLD = (236, 190, 70)
SILVER = (196, 204, 216)
IRON = (96, 100, 116)
WOOD = (150, 98, 56)
RED = (206, 52, 52)


# ---------------------------------------------------------------- 색

def _hsv(c):
    return colorsys.rgb_to_hsv(c[0] / 255.0, c[1] / 255.0, c[2] / 255.0)


def _rgb(h, s, v):
    r, g, b = colorsys.hsv_to_rgb(h, max(0.0, min(1.0, s)), max(0.0, min(1.0, v)))
    return (int(r * 255), int(g * 255), int(b * 255))


def ink(c):
    h, s, v = _hsv(c)
    r = _rgb(h, s * 0.8 + 0.35, v * 0.32)
    return tuple(int(r[i] * 0.7 + (40, 22, 34)[i] * 0.3) for i in range(3))


def shade(c):
    h, s, v = _hsv(c)
    return _rgb(h + 0.01, s * 1.08 + 0.06, v * 0.78)


def lite(c):
    h, s, v = _hsv(c)
    return _rgb(h - 0.01, s * 0.8, v * 1.12 + 0.05)


# ---------------------------------------------------------------- 그리기

class Pen:
    """좌표는 늘 64 논리 좌표. Fig 배율 k 가 1 이 아니면 바닥선(32, 61)을 기준으로 키워 같은 도트 밀도로 크게 그린다."""

    def __init__(self, im, k=1.0):
        self.d = ImageDraw.Draw(im)
        self.k = k
        self.cx = im.width / 2.0
        self.by = im.height - (H - FOOT)

    def t(self, x, y):
        if self.k == 1.0:
            return (x, y)
        return ((x - 32) * self.k + self.cx, (y - FOOT) * self.k + self.by)

    def e(self, x0, y0, x1, y1):
        a, b = self.t(x0, y0), self.t(x1, y1)
        self.d.ellipse([a[0], a[1], b[0], b[1]], fill=255)

    def r(self, x0, y0, x1, y1):
        a, b = self.t(min(x0, x1), min(y0, y1)), self.t(max(x0, x1), max(y0, y1))
        self.d.rectangle([a[0], a[1], b[0], b[1]], fill=255)

    def g(self, *pts):
        self.d.polygon([self.t(x, y) for x, y in pts], fill=255)

    def l(self, *pts, w=1):
        self.d.line([self.t(x, y) for x, y in pts], fill=255, width=max(1, int(round(w * self.k))))

    def d1(self, *pts):
        for x, y in pts:
            if self.k == 1.0:
                self.d.point((x, y), fill=255)
            else:
                a = self.t(x, y)
                s = max(1, int(round(self.k)))
                self.d.rectangle([a[0], a[1], a[0] + s - 1, a[1] + s - 1], fill=255)


class Part:
    def __init__(self, color, line, shade, light, size):
        self.im = Image.new("L", size, 0)
        self.color = color
        self.line = line
        self.shade = shade
        self.light = light


class Fig:
    """k = 그림 배율. 캔버스는 64×k 정사각, 바닥선은 아래에서 3 도트 (피벗 계산이 같다)."""

    def __init__(self, k=1.0):
        self.parts = []
        self.k = k
        n = int(round(W * k))
        self.size = (n, n)

    def p(self, color, line=True, shade=True, light=True):
        part = Part(color, line, shade, light, self.size)
        self.parts.append(part)
        return Pen(part.im, self.k)

    def flat(self, color):
        return self.p(color, line=False, shade=False, light=False)


def nb(m, dy, dx):
    """out[y, x] = m[y+dy, x+dx], 바깥은 False."""
    out = np.zeros_like(m)
    h, w = m.shape
    ys0, ys1 = max(0, -dy), h - max(0, dy)
    xs0, xs1 = max(0, -dx), w - max(0, dx)
    out[ys0:ys1, xs0:xs1] = m[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
    return out


def dilate(m):
    return m | nb(m, 1, 0) | nb(m, -1, 0) | nb(m, 0, 1) | nb(m, 0, -1)


def render(fig, shadow_cx=32, shadow_w=11):
    cw, ch = fig.size
    rgb = np.zeros((ch, cw, 3), dtype=np.int32)
    a = np.zeros((ch, cw), dtype=bool)
    for part in fig.parts:
        m = np.array(part.im) > 127
        if not m.any():
            continue
        if part.line:
            ring = dilate(m) & ~m & a
            rgb[ring] = ink(part.color)
        area = int(m.sum())
        lay = np.empty((ch, cw, 3), dtype=np.int32)
        lay[:] = part.color
        sh = np.zeros_like(m)
        # 큰 그림(거구 · 보스, k >= 1.3)의 넓은 면은 명암 띠를 배율만큼 넓히고 그 안쪽에 체커 디더 한 띠를 더 둔다
        # — 2도트 고정 띠로는 보스 몸이 단색 면(도형을 키운 것)처럼 보였다
        big = fig.k >= 1.3 and area >= 300
        sd = max(2, int(round(2 * fig.k))) if big else 2
        if part.shade:
            if area >= 24:
                sh = m & ~nb(m, sd, max(1, sd // 2))
            else:
                sh = m & ~nb(m, 1, 1)
            lay[sh] = shade(part.color)
            if big:
                yy, xx = np.indices(m.shape)
                band = m & ~nb(m, sd + 2, max(1, sd // 2) + 1) & ~sh & ((yy + xx) % 2 == 0)
                lay[band] = shade(part.color)
        if part.light and area >= 24:
            ld = max(1, int(round(fig.k))) if big else 1
            lt = m & ~nb(m, -ld, -ld) & ~sh
            lay[lt] = lite(part.color)
        rgb[m] = lay[m]
        a |= m

    # 실루엣 외곽선 — 이웃 칠의 동색 어두운 톤
    ring = dilate(a) & ~a
    out = np.zeros((ch, cw, 4), dtype=np.uint8)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        src = nb(a, dy, dx) & ring
        ys, xs = np.nonzero(src)
        for y, x in zip(ys, xs):
            if out[y, x, 3] == 0:
                c = ink(tuple(int(v) for v in rgb[y + dy, x + dx]))
                out[y, x] = (c[0], c[1], c[2], 255)
    out[a, :3] = rgb[a]
    out[a, 3] = 255

    # 바닥 그림자 (외곽선 밖)
    sm = Image.new("L", (cw, ch), 0)
    if shadow_w > 0:
        sx = (shadow_cx - 32) * fig.k + cw / 2.0
        sw = shadow_w * fig.k
        by = ch - (H - FOOT)
        ImageDraw.Draw(sm).ellipse([sx - sw, by - 2, sx + sw, by + 2], fill=255)
    smask = (np.array(sm) > 127) & (out[:, :, 3] == 0)
    out[smask] = (30, 18, 40, 110)
    return Image.fromarray(out, "RGBA")


# 적 공통 바깥선 — 몸(불투명 픽셀) 바깥 1도트를 어둡게. 바닥색과 몹 색이 비슷한 맵에서도 적 실루엣이 떨어진다
# 주인공에는 쓰지 않는다 (주인공은 발밑 링으로 따로 표시) — 테두리 유무가 피아 구분 축이 된다
RIM = (34, 22, 40, 255)
# 주인공 바깥선 — 적과 반대 신호(밝은 1도트). 비슷한 색 바닥 · 몹 사이에서도 내 캐릭터 실루엣이 떠 보이게
HERO_RIM = (255, 248, 228, 255)


def rim(img, color=RIM):
    a = np.array(img)
    body = a[..., 3] == 255
    grow = body.copy()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        grow |= nb(body, dy, dx)
    edge = grow & (body == False)
    a[edge] = color
    return Image.fromarray(a, "RGBA")


def big(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)
