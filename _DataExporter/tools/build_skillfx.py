# -*- coding: utf-8 -*-
"""
스킬 정체성 이펙트 생성기 - 「개많이잡아」

받아온 이펙트 팩은 범용 폭발/베기뿐이라 「물기」와 「으르렁」이 같은 그림으로 나갔다.
이름이 있는 스킬은 이름이 보여야 한다 - 물면 이빨이, 으르렁대면 드러낸 이빨이 나와야 한다.

캐릭터 생성기와 같은 규칙:
- 외곽선을 그리지 않는다. 색만 칠하고 실루엣을 팽창시킨다 -> 두께가 항상 일정하다
- 흰색 계열로만 그린다. 색은 런타임에 스킬 속성색으로 틴트된다
- 64x64, 프레임은 폴더에 00.png 부터
"""
from PIL import Image, ImageDraw, ImageFilter
import os, sys, math
sys.stdout.reconfigure(encoding="utf-8")

DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\FX"
S = 4
W = H = 64
OUTLINE = (32, 24, 20, 255)
OUTLINE_PX = 2
WHITE = (255, 255, 255, 255)
SOFT = (226, 226, 232, 255)
DEEP = (188, 188, 198, 255)
C = 32


def layer():
    im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def ell(d, cx, cy, rx, ry, color):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=color)


def ring(d, cx, cy, rx, ry, w, color):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S],
              outline=color, width=int(w * S))


def rrect(d, x0, y0, x1, y1, r, color):
    d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=color)


def tri(d, pts, color):
    d.polygon([(x * S, y * S) for x, y in pts], fill=color)


def arc(d, cx, cy, r, w, a0, a1, color):
    d.arc([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S],
          a0, a1, fill=color, width=int(w * S))


def line(d, x0, y0, x1, y1, w, color):
    d.line([x0 * S, y0 * S, x1 * S, y1 * S], fill=color, width=int(w * S))


def fin(src):
    """실루엣을 팽창시켜 균일한 외곽선을 만든다."""
    small = src.resize((W, H), Image.BOX)
    a = small.split()[3].point(lambda v: 255 if v >= 110 else 0)
    grow = a.filter(ImageFilter.MaxFilter(OUTLINE_PX * 2 + 1))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(OUTLINE, (0, 0), grow)
    body = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body.paste(small, (0, 0), a)
    out.alpha_composite(body)
    return out


def stack(layers):
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for lay in layers:
        out.alpha_composite(fin(lay))
    return out


def fx_teeth(t):
    """물기 - 위아래 이빨이 맞물린다. 무는 순간이 보여야 「물기」로 읽힌다."""
    gap = 15 * (1 - t)
    up, d = layer()
    rrect(d, C - 20, C - gap - 14, C + 20, C - gap - 4, 4, SOFT)
    for x in (-14, -5, 5, 14):
        h = 11 if abs(x) > 9 else 7
        tri(d, [(C + x - 4, C - gap - 6), (C + x + 4, C - gap - 6), (C + x, C - gap - 6 + h)], WHITE)

    lo, d2 = layer()
    rrect(d2, C - 20, C + gap + 4, C + 20, C + gap + 14, 4, SOFT)
    for x in (-14, -5, 5, 14):
        h = 11 if abs(x) > 9 else 7
        tri(d2, [(C + x - 4, C + gap + 6), (C + x + 4, C + gap + 6), (C + x, C + gap + 6 - h)], WHITE)

    img = stack([up, lo])

    if t > 0.6:
        sp, d3 = layer()
        for a in (25, 155, 205, 335):
            r = math.radians(a)
            line(d3, C + math.cos(r) * 21, C + math.sin(r) * 13,
                 C + math.cos(r) * 29, C + math.sin(r) * 18, 2.5, WHITE)
        img.alpha_composite(fin(sp))
    return img


def fx_growl(t):
    """
    으르렁 - 드러낸 이빨 + 퍼지는 고리.

    자기중심 광역이라 바닥에 눕으면 **원**으로 보여야 범위가 읽힌다.
    예전엔 위쪽만 감싼 호였는데, 눕히면 반쪽짜리로 보여 어디까지 닿는지 알 수 없었다.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in range(3):
        p = t - k * 0.22
        if p <= 0 or p > 1:
            continue
        lay, d = layer()
        r = 9 + p * 21
        ring(d, C, C, r, r, 2.6, WHITE)
        img.alpha_composite(fin(lay))

    mouth, d = layer()
    rrect(d, C - 12, C - 5, C + 12, C + 8, 5, DEEP)
    for x in (-8, -2.5, 3, 8.5):
        tri(d, [(C + x - 2.6, C - 4), (C + x + 2.6, C - 4), (C + x, C + 1.5)], WHITE)
    for x in (-6, 0, 6):
        tri(d, [(C + x - 2.6, C + 7), (C + x + 2.6, C + 7), (C + x, C + 1.5)], WHITE)
    img.alpha_composite(fin(mouth))
    return img


def fx_bark(t):
    """왈왈 - 앞으로 터져나가는 소리 부채꼴."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in range(3):
        p = t - k * 0.25
        if p <= 0 or p > 1:
            continue
        lay, d = layer()
        arc(d, C - 15, C, 7 + p * 24, 3.0, -55, 55, WHITE)
        img.alpha_composite(fin(lay))
    if t > 0.35:
        ex, d = layer()
        rrect(d, C + 13, C - 12, C + 18, C + 2, 2, WHITE)
        ell(d, C + 15.5, C + 7, 3, 3, WHITE)
        img.alpha_composite(fin(ex))
    return img


def fx_yawn(t):
    """하품 - 느리게 커지는 졸음 고리 + Z. 안을 채우면 바닥이 안 보인다."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    lay, d = layer()
    r = 8 + t * 17
    ring(d, C, C, r, r, 2.4, SOFT)
    img.alpha_composite(fin(lay))

    zz, d = layer()
    for i, (dx, dy, s) in enumerate(((11, -13, 5.5), (19, -21, 4.2), (25, -27, 3.0))):
        if t < i * 0.22:
            continue
        x0, y0 = C + dx, C + dy
        line(d, x0 - s, y0 - s, x0 + s, y0 - s, 1.8, WHITE)
        line(d, x0 + s, y0 - s, x0 - s, y0 + s, 1.8, WHITE)
        line(d, x0 - s, y0 + s, x0 + s, y0 + s, 1.8, WHITE)
    img.alpha_composite(fin(zz))
    return img


def fx_claw(t):
    """발톱 세례 - 발톱 자국 3줄이 순서대로 그어진다."""
    lay, d = layer()
    for i, off in enumerate((-13, 0, 13)):
        p = min(1.0, max(0.0, t * 1.4 - i * 0.12))
        if p <= 0:
            continue
        x0 = C - 22 + off * 0.5
        y0 = C - 22
        line(d, x0, y0, x0 + 38 * p, y0 + 42 * p, 3.4 - i * 0.5, WHITE)
    return stack([lay])


def fx_bone(t):
    """뼈다귀 - 돌면서 날아간다."""
    lay, d = layer()
    r = math.radians(t * 120)
    dx, dy = math.cos(r) * 13, math.sin(r) * 13
    line(d, C - dx, C - dy, C + dx, C + dy, 6, WHITE)
    for sx, sy in ((-dx, -dy), (dx, dy)):
        nx, ny = -sy * 0.42, sx * 0.42
        ell(d, C + sx + nx, C + sy + ny, 4.6, 4.6, WHITE)
        ell(d, C + sx - nx, C + sy - ny, 4.6, 4.6, WHITE)
    return stack([lay])


def fx_tail(t):
    """꼬리 후리기 - 휘두르는 호."""
    lay, d = layer()
    a0 = -120 + t * 150
    arc(d, C, C + 10, 24, 4.5, a0, a0 + 95, WHITE)
    arc(d, C, C + 10, 17, 2.4, a0 + 12, a0 + 80, SOFT)
    return stack([lay])


def fx_scent(t):
    """냄새 추적 - 흘러 올라가는 물결 3겹."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in range(3):
        lay, d = layer()
        y = C + 13 - k * 12 - t * 7
        prev = None
        for i in range(0, 41):
            x = C - 20 + i
            yy = y + math.sin((i / 40.0) * math.pi * 2 + t * 6 + k) * 4
            if prev is not None:
                line(d, prev[0], prev[1], x, yy, 2.6, WHITE)
            prev = (x, yy)
        img.alpha_composite(fin(lay))
    return img


def fx_ring_wave(t, span=24.0):
    """
    퍼지는 링 - 귀 쫑긋 / 무리 울음.

    [사고] 링을 4겹으로 겹쳤더니 외곽선(양쪽 2px)끼리 붙어 검은 덩어리가 됐다.
    링 사이는 최소 8px 이 필요하다. 세기는 겹 수가 아니라 **퍼지는 폭**으로 낸다.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in range(3):
        p = t - k * 0.24
        if p <= 0 or p > 1:
            continue
        lay, d = layer()
        r = 5 + p * span
        ring(d, C, C, r, r * 0.55, 2.6, WHITE)
        img.alpha_composite(fin(lay))
    return img


def fx_kibble(t):
    """사료 알갱이가 흩뿌려진다."""
    lay, d = layer()
    seeds = ((-14, -8, 0), (5, -15, 40), (14, 2, 80), (-6, 10, 120), (2, 0, 200), (-18, 6, 260))
    for i, (bx, by, a) in enumerate(seeds):
        r = math.radians(a)
        x = C + bx + math.cos(r) * t * 13
        y = C + by + math.sin(r) * t * 13
        s = 5.0 - i * 0.35
        tri(d, [(x - s, y + s * 0.8), (x + s, y + s * 0.8), (x, y - s)], WHITE)
    return stack([lay])


def fx_frisbee(t):
    """원반 - 회전하며 납작해졌다 펴진다."""
    lay, d = layer()
    sq = abs(math.cos(math.radians(t * 180)))
    ell(d, C, C, 20, 4 + 9 * sq, WHITE)
    ell(d, C, C, 11, 2 + 5 * sq, DEEP)
    return stack([lay])


def fx_ball(t):
    """테니스공 - 곡선 두 줄이 돈다."""
    lay, d = layer()
    ell(d, C, C, 13, 13, WHITE)
    off = math.cos(math.radians(t * 200)) * 2
    arc(d, C - 9 + off, C, 12, 2.4, -60, 60, DEEP)
    arc(d, C + 9 + off, C, 12, 2.4, 120, 240, DEEP)
    return stack([lay])


def fx_slipper(t):
    """슬리퍼 - 앞이 넓은 밑창 + 가로지르는 끈. 이 둘이 있어야 알약이 아니라 신발로 읽힌다."""
    sole, db = layer()
    # 밑창: 앞(오른쪽)이 넓고 뒤꿈치가 좁다
    ell(db, C + 9, C, 11, 9, WHITE)
    ell(db, C - 11, C, 8, 6.5, WHITE)
    rrect(db, C - 11, C - 7.5, C + 9, C + 7.5, 5, WHITE)
    sole = sole.rotate(t * 90 - 25, center=(C * S, C * S))

    strap, ds = layer()
    rrect(ds, C + 1, C - 9.5, C + 7, C + 9.5, 3, DEEP)
    strap = strap.rotate(t * 90 - 25, center=(C * S, C * S))
    return stack([sole, strap])


def fx_bread(t):
    """
    식빵 썰기 - 식빵 한 덩이가 사선으로 갈려 어긋난다.

    [사고] 두 조각을 위아래로 붙여 놓으니 외곽선이 맞닿아 햄버거처럼 보였다.
    갈라짐은 **간격**으로 보여야 한다 - 조각을 서로 반대로 밀어낸다.
    """
    off = 2 + t * 8

    top, d = layer()
    rrect(d, C - 17 - off, C - 15, C + 17 - off, C - 3, 4, WHITE)
    ell(d, C - off, C - 15, 15, 8.5, WHITE)

    # 아래 조각도 식빵 몸통 모양을 유지해야 「잘린 한 덩이」로 읽힌다.
    bot, d2 = layer()
    rrect(d2, C - 16 + off, C + 1, C + 16 + off, C + 16, 5, SOFT)
    rrect(d2, C - 13 + off, C + 4, C + 13 + off, C + 13, 3, WHITE)

    return stack([top, bot])


def fx_paw(t):
    """발바닥 자국 - 개다운 범용 타격."""
    lay, d = layer()
    k = 0.7 + t * 0.45
    ell(d, C, C + 6 * k, 12 * k, 10 * k, WHITE)
    for bx, by, r in ((-12, -7, 5), (-4.5, -12, 5.5), (4.5, -12, 5.5), (12, -7, 5)):
        ell(d, C + bx * k, C + by * k, r * k, r * k, WHITE)
    return stack([lay])


def fx_dust(t):
    """
    흙먼지 - 뒹굴기/질주가 남기는 자국.

    [사고] 원래는 구름 덩어리를 꽉 채워 그렸다. 탑다운에서 바닥에 눕히면
    **바닥과 적을 통째로 가리는 갈색 덩어리**가 됐다.
    광역기는 「어디까지 닿는가」가 정보다 — 테두리로 범위를 보이고 안은 비워 둔다.
    """
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    ring_lay, d = layer()
    r = 16 + t * 10
    ring(d, C, C, r, r, 2.6, WHITE)
    img.alpha_composite(fin(ring_lay))

    # 테두리 위에 흙 알갱이 몇 개. 「먼지」라는 성격은 이걸로 낸다.
    puff, d2 = layer()
    for i in range(7):
        a = math.radians(i * 51 + t * 40)
        px = C + math.cos(a) * (r - 2)
        py = C + math.sin(a) * (r - 2)
        ell(d2, px, py, 3.2 - i * 0.2, 2.6 - i * 0.15, SOFT)
    img.alpha_composite(fin(puff))
    return img



def fx_shield(t):
    """털뭉치 보호막 - 털이 뭉쳐 막이 된다. 둥근 막 + 삐죽한 털."""
    lay, d = layer()
    r = 14 + t * 8
    ring(d, C, C, r, r, 3.0, WHITE)
    # 삐죽한 털. 매끈한 원이면 「막」이지 「털뭉치」가 아니다.
    for i in range(12):
        a = math.radians(i * 30 + t * 15)
        x0, y0 = C + math.cos(a) * r, C + math.sin(a) * r
        x1, y1 = C + math.cos(a) * (r + 5), C + math.sin(a) * (r + 5)
        line(d, x0, y0, x1, y1, 2.2, SOFT)
    return stack([lay])


def fx_awaken(t):
    """개쌘놈 모드 - 위로 솟는 기 + 별. 공격속도가 붙는 각성."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    up, d = layer()
    for i, dy in enumerate((10, 0, -10)):
        p = t - i * 0.18
        if p <= 0:
            continue
        w = 13 - i * 2
        y = C + dy - p * 10
        line(d, C - w, y + 6, C, y - 3, 3.0, WHITE)
        line(d, C, y - 3, C + w, y + 6, 3.0, WHITE)
    img.alpha_composite(fin(up))

    st, d2 = layer()
    r = 4 + t * 4
    for i in range(4):
        a = math.radians(i * 45)
        line(d2, C - math.cos(a) * r, C - math.sin(a) * r,
             C + math.cos(a) * r, C + math.sin(a) * r, 2.4, WHITE)
    img.alpha_composite(fin(st))
    return img


def fx_zoom(t):
    """우다다 모드 - 뒤로 흘러가는 속도선. 이동속도가 붙는 질주."""
    lay, d = layer()
    rows = ((-14, 22, 3.2), (-5, 28, 2.6), (5, 24, 3.0), (14, 18, 2.2))
    for i, (dy, ln, w) in enumerate(rows):
        off = t * 10 + i * 3
        x1 = C + 20 - off
        line(d, x1 - ln, C + dy, x1, C + dy, w, WHITE if i % 2 == 0 else SOFT)
    return stack([lay])


def fx_taunt(t):
    """개무시 - 안쪽으로 빨려드는 화살표. 「끌어당긴다」가 보여야 한다."""
    lay, d = layer()
    r = 26 - t * 12
    for i in range(6):
        a = math.radians(i * 60 + t * 20)
        cx, cy = C + math.cos(a) * r, C + math.sin(a) * r
        # 중심을 향하는 삼각형
        nx, ny = -math.cos(a), -math.sin(a)
        px, py = -ny, nx
        tri(d, [(cx + nx * 6, cy + ny * 6),
                (cx + px * 4.5, cy + py * 4.5),
                (cx - px * 4.5, cy - py * 4.5)], WHITE)
    ring(d, C, C, 7, 7, 2.4, SOFT)
    return stack([lay])


def fx_eyebeam(t):
    """개눈빛 레이저 - 두 줄기 눈빛이 앞으로 뻗는다."""
    lay, d = layer()
    reach = 14 + t * 34
    for dy in (-5, 5):
        rrect(d, C - 24, C + dy - 2.6, C - 24 + reach, C + dy + 2.6, 2, WHITE)
    # 발사점의 눈
    ell(d, C - 25, C - 5, 3.4, 3.4, SOFT)
    ell(d, C - 25, C + 5, 3.4, 3.4, SOFT)
    return stack([lay])


def fx_gaze(t):
    """제3의 눈 - 세로 눈동자 + 관통선. 단일 고위력이라 한 줄이 꿰뚫는다."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    pierce, d = layer()
    ln = 16 + t * 28
    rrect(d, C - ln, C - 2.2, C + ln, C + 2.2, 2, WHITE)
    img.alpha_composite(fin(pierce))

    eye, d2 = layer()
    ell(d2, C, C, 13, 9, WHITE)
    ell(d2, C, C, 4.2, 7.5, DEEP)   # 세로 동공 - 이게 있어야 「눈」이다
    img.alpha_composite(fin(eye))
    return img


def fx_static(t):
    """정전기 털 - 사방으로 튀는 꺾인 섬광."""
    lay, d = layer()
    for i in range(6):
        a = math.radians(i * 60 + t * 25)
        r0 = 6
        r1 = 14 + t * 8
        r2 = 20 + t * 10
        mid = a + math.radians(16)
        line(d, C + math.cos(a) * r0, C + math.sin(a) * r0,
             C + math.cos(mid) * r1, C + math.sin(mid) * r1, 2.6, WHITE)
        line(d, C + math.cos(mid) * r1, C + math.sin(mid) * r1,
             C + math.cos(a) * r2, C + math.sin(a) * r2, 2.2, WHITE)
    ell(d, C, C, 4.5, 4.5, SOFT)
    return stack([lay])


SETS = [
    ("fx_teeth",   4, fx_teeth,   "물기 - 맞물리는 이빨"),
    ("fx_growl",   5, fx_growl,   "으르렁 - 드러낸 이빨 + 파동"),
    ("fx_bark",    5, fx_bark,    "왈왈 - 앞으로 터지는 소리"),
    ("fx_yawn",    5, fx_yawn,    "하품 - 졸음 고리"),
    ("fx_dogclaw", 4, fx_claw,    "발톱 자국 3줄"),
    ("fx_bone",    4, fx_bone,    "뼈다귀"),
    ("fx_tailsw",  4, fx_tail,    "꼬리 후리기"),
    ("fx_scent",   5, fx_scent,   "냄새 물결"),
    ("fx_sonar",   5, lambda t: fx_ring_wave(t, 20.0), "귀 쫑긋 - 퍼지는 링"),
    ("fx_howl",    6, lambda t: fx_ring_wave(t, 27.0), "무리 울음 - 더 멀리 퍼지는 링"),
    ("fx_kibble",  4, fx_kibble,  "사료 알갱이"),
    ("fx_frisbee", 4, fx_frisbee, "원반"),
    ("fx_ball",    4, fx_ball,    "테니스공"),
    ("fx_slipper", 4, fx_slipper, "슬리퍼"),
    ("fx_bread",   4, fx_bread,   "식빵 썰기"),
    ("fx_paw",     4, fx_paw,     "발바닥 자국"),
    ("fx_dogdust", 4, fx_dust,    "흙먼지"),

    # [사고] 「보호」 하나를 4개 스킬이 돌려썼는데, 실제로는 보호막·공격속도 각성·질주·도발로
    # 전부 다른 것이었다. 「눈빛」도 지속 빔과 관통 시선으로 갈린다. 이름이 다르면 그림도 달라야 한다.
    ("fx_shield",  4, fx_shield,  "털뭉치 보호막"),
    ("fx_awaken",  5, fx_awaken,  "개쌘놈 모드 - 솟는 기"),
    ("fx_zoom",    4, fx_zoom,    "우다다 모드 - 속도선"),
    ("fx_taunt",   5, fx_taunt,   "개무시 - 끌어당김"),
    ("fx_eyebeam", 5, fx_eyebeam, "개눈빛 레이저"),
    ("fx_gaze",    4, fx_gaze,    "제3의 눈 - 관통 시선"),
    ("fx_static",  4, fx_static,  "정전기 털"),
]


def main():
    total = 0
    for name, n, fn, desc in SETS:
        outdir = os.path.join(DST, name)
        os.makedirs(outdir, exist_ok=True)
        for old in os.listdir(outdir):
            if old.endswith(".png") or old.endswith(".meta"):
                os.remove(os.path.join(outdir, old))
        for i in range(n):
            # t=0 을 쓰면 「퍼지는」 계열의 첫 프레임이 통째로 비어 한 프레임을 버린다.
            t = (i + 1) / float(n)
            fn(t).save(os.path.join(outdir, "%02d.png" % i))
        print("%-14s %d프레임  %s" % (name, n, desc))
        total += n
    print("총 %d프레임 / %d종" % (total, len(SETS)))


main()
