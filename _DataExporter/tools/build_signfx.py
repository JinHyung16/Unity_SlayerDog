# -*- coding: utf-8 -*-
"""
「간판」 이펙트 생성기 — 「개많이잡아」

레퍼런스(_Ref/)의 방향: **외곽만 따서 실루엣으로 눈에 띄게 승부한다.**
발바닥이면 발바닥, 뼈면 뼈. 스킬 이름이 그림 한 장으로 읽혀야 한다.

이전 build_skillfx.py 와 무엇이 다른가 (같은 실루엣인데 안 읽혔던 이유):
  1. 크기   — 64px 안에서 작게 그렸다. 레퍼는 캔버스를 **꽉 채운다**
  2. 속     — 흰색으로 꽉 채웠다. 레퍼는 **비어 있다** — 선만 남아 아래가 비친다
  3. 외곽   — 얇은 회색이었다. 레퍼는 **아주 두꺼운 어두운 선**
  4. 글로우 — 없었다. 레퍼는 바깥으로 번져서 배경에서 뜬다

[중요] 레퍼 절반은 **검은 배경 전제의 네온**이다. 우리 바닥은 밝은 초원이라
얇은 네온선을 그대로 올리면 묻힌다. 그래서 세 겹으로 간다 —
    바깥 글로우(계열색) → 어두운 외곽선 → 밝은 안쪽선
어두운 외곽이 밝은 배경에서 대비를 만들고, 밝은 안쪽선이 어두운 배경에서 산다.
어느 바닥 위에서도 읽히는 유일한 조합이다.

색은 흰색 계열로만 그린다 — 런타임에 스킬 속성색으로 틴트된다(SkillVisual.ApplyTint).
글로우만 회색 농도로 넣어 틴트를 타게 한다.
"""
from PIL import Image, ImageDraw, ImageFilter
import os
import sys
import math

sys.stdout.reconfigure(encoding="utf-8")

DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\FX"

S = 4                 # 슈퍼샘플 배율. 곡선을 매끄럽게 뽑고 마지막에 줄인다
W = H = 64
C = 32.0

INK = (26, 20, 18, 255)        # 어두운 외곽. 밝은 바닥에서 형태를 지킨다
LINE = (255, 255, 255, 255)    # 밝은 안쪽선
FILL = (255, 255, 255, 70)     # 속. 비워 두되 아주 옅게 깔아 덩어리감만 준다

INK_PX = 3                     # 외곽 두께(최종 픽셀). 레퍼의 「두꺼운 검정」
GLOW_PX = 2                    # 글로우 번짐 반경.
# [사고] 처음에 5로 뒀더니 64px 캔버스를 거의 다 덮어 **밝은 사각 얼룩**이 됐다.
# 초원 배경에서 이펙트가 아니라 흰 상자로 보인다. 글로우는 실루엣을 「살짝 띄우는」 것이지
# 배경을 밝히는 것이 아니다 — 형태가 아직 보이는 범위 안에서만 번진다.
GLOW_A = 0.30


def layer():
    im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def ell(d, cx, cy, rx, ry, color, width=0):
    box = [(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S]
    if width > 0:
        d.ellipse(box, outline=color, width=max(1, int(width * S)))
    else:
        d.ellipse(box, fill=color)


def rrect(d, x0, y0, x1, y1, r, color, width=0):
    # 위아래 대칭 도형(이빨 위/아래 줄)을 부호로 뒤집어 그리면 y0 > y1 이 된다.
    # 호출부마다 정렬하게 두면 잊는다. 여기서 한 번 정리한다.
    x0, x1 = min(x0, x1), max(x0, x1)
    y0, y1 = min(y0, y1), max(y0, y1)
    box = [x0 * S, y0 * S, x1 * S, y1 * S]
    if width > 0:
        d.rounded_rectangle(box, radius=r * S, outline=color, width=max(1, int(width * S)))
    else:
        d.rounded_rectangle(box, radius=r * S, fill=color)


def poly(d, pts, color, width=0):
    p = [(x * S, y * S) for x, y in pts]
    if width > 0:
        d.line(p + [p[0]], fill=color, width=max(1, int(width * S)), joint="curve")
    else:
        d.polygon(p, fill=color)


def line(d, x0, y0, x1, y1, w, color):
    d.line([x0 * S, y0 * S, x1 * S, y1 * S], fill=color, width=max(1, int(w * S)))



def hole(im, fn):
    """
    실루엣에 구멍을 뚫는다.

    [사고] 속을 비우니(hollow) 내부 디테일이 통째로 사라져 공 · 원반 · 눈이
    그냥 원 · 타원 · 육각형이 됐다. 레퍼의 발바닥은 윤곽만으로도 발바닥이지만,
    테니스공은 솔기가 없으면 동그라미일 뿐이다.

    해결은 선을 덧그리는 게 아니라 **구멍**이다 — 침식(MinFilter)은 바깥에서 깎으므로
    구멍이 있으면 그 둘레에서도 깎여 안쪽에 선이 하나 더 생긴다. 공짜로 얻는 디테일이다.
    """
    cut = Image.new("RGBA", im.size, (0, 0, 0, 0))
    fn(ImageDraw.Draw(cut))
    im.paste((0, 0, 0, 0), (0, 0), cut.split()[3])
    return im


def render(shape, glow=True, ink_px=INK_PX, hollow=True):
    """
    실루엣 한 장을 「간판」으로 굽는다.

    shape 는 **채워진** 실루엣이다(알파만 본다). 여기서 테두리를 입힌다.
    직접 선을 그리지 않고 실루엣을 침식/팽창시키는 이유는 두께가 어디서나 같아지기 때문 —
    선으로 그리면 좁은 곳에서 뭉치고 넓은 곳에서 얇아진다.

    hollow 는 레퍼런스의 두 갈래에 대응한다.
      True  — 네온형(발바닥 · 뼈 · 해골 · 불꽃). 속을 파내 선만 남긴다.
              **큰 단일 실루엣에만 쓴다.**
      False — 만화형(왈왈 · 으르렁). 속을 밝게 채우고 두꺼운 검정 외곽만 두른다.

    [사고] 전부 hollow 로 갔다가 왈왈 조각(폭 4~7px)이 통째로 검은 삼각형이 됐다.
    잉크가 양쪽에서 2px 씩 먹으면 남는 속이 0 이다 — 속을 비우려면 폭이 잉크의 4배는 돼야 한다.
    레퍼를 다시 보면 작은 조각들은 애초에 **속이 차 있다**. 모양이 처리를 정한다.
    """
    small = shape.resize((W, H), Image.BOX)
    a = small.split()[3].point(lambda v: 255 if v >= 100 else 0)

    # 안쪽으로 깎아 들어간 알파. 원본과의 차이가 곧 「선」이 된다.
    inner = a.filter(ImageFilter.MinFilter(ink_px * 2 + 1))
    core = inner.filter(ImageFilter.MinFilter(3))          # 밝은 선 안쪽 = 속

    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    if glow:
        # 글로우는 실루엣을 부풀려 흐린 것. 계열색 틴트를 타도록 회색으로 둔다.
        grown = a.filter(ImageFilter.MaxFilter(GLOW_PX * 2 + 1)).filter(
            ImageFilter.GaussianBlur(GLOW_PX))
        g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        g.paste((255, 255, 255, 255), (0, 0), grown.point(lambda v: int(v * GLOW_A)))
        out.alpha_composite(g)

    if hollow:
        # 네온형: 실루엣 전체를 잉크로 깔고 안쪽으로 깎은 만큼만 밝게 남긴다 = 테두리 선
        ink = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ink.paste(INK, (0, 0), a)
        out.alpha_composite(ink)

        bright = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        bright.paste(LINE, (0, 0), inner)
        out.alpha_composite(bright)
    else:
        # 만화형: 선을 **바깥으로 두른다.**
        #
        # [측정] 여기서도 안쪽을 깎았더니 폭 7px 조각의 잉크 비중이 58~66% 였다.
        # 틴트 색이 아무리 밝아도 화면에서는 검은 덩어리로 보인다.
        # 캐릭터 생성기가 쓰는 방식(실루엣을 팽창시켜 바깥에 선을 두름)이 옳다 —
        # 밝은 면이 통째로 남는다.
        outer = a.filter(ImageFilter.MaxFilter(ink_px * 2 + 1))
        ink = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ink.paste(INK, (0, 0), outer)
        out.alpha_composite(ink)

        bright = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        bright.paste(LINE, (0, 0), a)
        out.alpha_composite(bright)

    if hollow:
        inside = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        inside.paste(FILL, (0, 0), core)
        # 속은 덮어써야 한다. 위에 얹으면 밝은 선 위에 반투명이 겹쳐 탁해진다.
        out.paste(inside, (0, 0), core)

    return out


def punch(t):
    """튀어나왔다 잦아드는 크기 곡선. 레퍼의 임팩트 조각이 이렇게 움직인다."""
    if t < 0.35:
        return 0.55 + (t / 0.35) * 0.55        # 0.55 -> 1.10
    return 1.10 - ((t - 0.35) / 0.65) * 0.18   # 1.10 -> 0.92


def fade(t):
    """
    끝으로 갈수록 옅어진다. **0 까지는 안 내린다.**

    [사고] 마지막 프레임을 완전히 투명하게 만들었더니, 프리팹이 1회 재생 · 마지막 프레임 고정이라
    연출이 끝난 뒤 「보이지 않는 그림」에서 얼어붙었다. 장판처럼 오래 사는 스킬은
    나머지 시간 내내 아무것도 없었다.
    반복 재생으로 바꿨으므로 어느 프레임에서 멈춰도 보여야 한다.
    """
    if t < 0.5:
        return 1.0
    return 1.0 - (t - 0.5) / 0.5 * 0.45


def finish(img, t, scale_curve=True):
    """크기 펀치 + 끝에서 페이드."""
    if scale_curve:
        k = punch(t)
        n = max(8, int(W * k))
        img = img.resize((n, n), Image.BICUBIC)
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        canvas.alpha_composite(img, ((W - n) // 2, (H - n) // 2))
        img = canvas

    f = fade(t)
    if f < 1.0:
        alpha = img.split()[3].point(lambda v: int(v * f))
        img.putalpha(alpha)
    return img


# ── 실루엣들 ────────────────────────────────────────────────────────────────
# 좌표는 64 기준. 캔버스를 꽉 채운다 — 여백은 글로우 몫만 남긴다(반지름 26~28).

def sil_paw():
    """발바닥. 레퍼 그대로 — 패드 하나 + 발가락 넷."""
    im, d = layer()
    # 아래 큰 패드
    poly(d, [(C - 16, C + 6), (C - 12, C - 4), (C, C - 9), (C + 12, C - 4),
             (C + 16, C + 6), (C + 12, C + 17), (C, C + 20), (C - 12, C + 17)], LINE)
    # 발가락. 바깥 둘이 낮고 벌어진다
    for dx, dy, rx, ry in ((-19, -9, 6.0, 7.5), (-7.5, -17, 6.5, 8.5),
                           (7.5, -17, 6.5, 8.5), (19, -9, 6.0, 7.5)):
        ell(d, C + dx, C + dy, rx, ry, LINE)
    return im


def sil_bone():
    """개뼈. 레퍼 그대로 — 가운데 막대 + 양끝에 혹 둘씩."""
    im, d = layer()
    rrect(d, C - 17, C - 6.5, C + 17, C + 6.5, 3, LINE)
    for sx in (-1, 1):
        ell(d, C + sx * 20, C - 8.5, 8.5, 8.5, LINE)
        ell(d, C + sx * 20, C + 8.5, 8.5, 8.5, LINE)
    return im


def sil_bark(t):
    """
    왈왈 — 레퍼의 노란 임팩트 조각들. **만화형**(속 채움)이다.

    [사고 1] 예전엔 「))」 물결에 느낌표였다 — 소리 기호이지 이펙트가 아니다.
    [사고 2] 조각을 얇게 뒀더니 외곽선이 양쪽에서 먹어 검은 덩어리가 됐다.
    [사고 3] 전부 중심 근처에서 시작시켜 서로 겹쳤다. 레퍼는 가운데가 비어 있다.
    [사고 4] 떼어 놓았더니 이번엔 작은 조각이 속 없는 검은 삼각형이 됐다 — hollow 를 껐다.

    속이 차므로 조각 폭에 하한이 없다. 크기를 크게 섞어 「튀었다」를 만든다.
    전방 부채꼴이라 +X(오른쪽)를 크게 편다. SkillArtOrient 가 조준 방향으로 눕힌다.
    """
    im, d = layer()
    grow = 0.82 + t * 0.26

    # (각도, 안쪽거리, 길이, 안쪽폭) — 좌우 비대칭이라야 「터짐」으로 읽힌다
    # [측정] 얇은 조각 여덟이면 잉크 비중이 40%를 넘어 화면에서 검게 보인다.
    # 조각은 둘레로 잉크가 붙으므로 **얇을수록 검다** — 64px 안에서는 적고 굵은 쪽이 이긴다.
    # 안쪽 반지름을 14 로 밀어야 굵은 셋이 서로 안 닿는다(호 17px > 반폭 합 15px).
    shards = [
        (-70, 14, 12, 13.0),
        (0, 14, 15, 17.0),
        (70, 14, 12, 13.0),
    ]
    for ang, r0, ln, w in shards:
        a = math.radians(ang)
        cs, sn = math.cos(a), math.sin(a)
        r0g = r0 * grow
        r1 = (r0 + ln) * grow
        nx, ny = -sn, cs
        poly(d, [(C + cs * r0g + nx * w * 0.5, C + sn * r0g + ny * w * 0.5),
                 (C + cs * r0g - nx * w * 0.5, C + sn * r0g - ny * w * 0.5),
                 (C + cs * r1 - nx * w * 0.12, C + sn * r1 - ny * w * 0.12),
                 (C + cs * r1 + nx * w * 0.12, C + sn * r1 + ny * w * 0.12)], LINE)
    return im



# ── 물건 실루엣 (Billboard) ─────────────────────────────────────────────────
# 정면에서 본 그림이라 세워야 읽힌다. 두꺼운 쪽은 속을 비우고(네온형),
# 얇은 조각은 속을 채운다(만화형) — 폭이 잉크의 4배가 안 되면 속이 안 남는다.

def sil_teeth(t):
    """물기 — 위아래 송곳니가 맞물린다. 무는 순간이 보여야 「물기」로 읽힌다."""
    im, d = layer()
    gap = 13 * (1 - t)
    for sign in (-1, 1):
        base = C + sign * (gap + 7)
        # 잇몸 띠
        rrect(d, C - 26, base - sign * 5, C + 26, base + sign * 5, 4, LINE)
        for x in (-19, -8, 8, 19):
            h = 15 if abs(x) > 12 else 10
            poly(d, [(C + x - 6, base), (C + x + 6, base), (C + x, base + sign * h)], LINE)
    return im


def sil_frisbee(t):
    """원반 — 납작한 타원 + 가운데가 뚫린 테두리. 구멍이 없으면 그냥 타원이다."""
    im, d = layer()
    ry = 16 + 5 * math.sin(t * math.pi)
    ell(d, C, C, 28, ry, LINE)
    return hole(im, lambda dd: ell(dd, C, C, 14, max(3.0, ry * 0.45), LINE))
def sil_ball(t):
    """테니스공 — 원 + S자 솔기 둘. 솔기가 없으면 동그라미일 뿐이다."""
    im, d = layer()
    ell(d, C, C, 26, 26, LINE)

    def seams(dd):
        for sx in (-1, 1):
            dd.arc([(C + sx * 30 - 22) * S, (C - 22) * S, (C + sx * 30 + 22) * S, (C + 22) * S],
                   90 if sx > 0 else 270, 270 if sx > 0 else 90,
                   fill=(255, 255, 255, 255), width=int(6.5 * S))
    return hole(im, seams)
def sil_slipper(t):
    """
    슬리퍼 — 넓은 바닥창 + 발 들어가는 자리.

    [사고] 끈을 만들려고 위아래를 크게 파냈더니 **구멍이 형태를 다 먹고** 얇은 띠 셋만 남았다.
    구멍은 디테일을 「추가」하는 도구지 형태를 「대체」하는 도구가 아니다.
    바닥창 넓이의 1/3 을 넘지 않게 판다.
    """
    im, d = layer()
    poly(d, [(C - 15, C - 25), (C + 14, C - 23), (C + 18, C - 5), (C + 16, C + 13),
             (C + 7, C + 26), (C - 10, C + 26), (C - 18, C + 12), (C - 19, C - 5)], LINE)
    # 앞쪽에 발이 들어가는 구멍 하나. 이것만으로 「신는 것」이 된다
    # 구멍이 외곽선에 닿으면 위가 뚫려 보인다. 안쪽으로 충분히 들여 놓는다
    return hole(im, lambda dd: ell(dd, C - 1, C - 8, 8.5, 6.0, LINE))
def sil_bread(t):
    """식빵 — 아래는 각지고 위는 봉긋. 세로로 길면 아치문이 된다. 가로로 넓힌다."""
    im, d = layer()
    rrect(d, C - 27, C - 2, C + 27, C + 19, 4, LINE)
    d.pieslice([(C - 27) * S, (C - 24) * S, (C + 27) * S, (C + 16) * S], 180, 360, fill=LINE)
    # 속살을 파내면 테두리가 곧 「빵 껍질」이 된다
    return hole(im, lambda dd: dd.rounded_rectangle(
        [(C - 17) * S, (C - 10) * S, (C + 17) * S, (C + 10) * S], radius=6 * S, fill=LINE))
def sil_kibble(t):
    """사료 — 알갱이 여러 알. 하나면 돌멩이로 보인다. 여럿이라야 사료다."""
    im, d = layer()
    spread = 0.75 + t * 0.35
    for ang, r, rad in ((-90, 16, 9.5), (-20, 19, 8.0), (55, 17, 9.0),
                        (140, 18, 7.5), (200, 14, 6.5)):
        a = math.radians(ang)
        ell(d, C + math.cos(a) * r * spread, C + math.sin(a) * r * spread, rad, rad * 0.85, LINE)
    return im


def sil_gaze(t):
    """
    제3의 눈 — 아몬드 눈매 + 동공.

    [사고] 흰자를 좌우로 크게 파냈더니 아령 모양이 됐다. 구멍 둘이 아몬드를 갈라 버렸다.
    가운데에 동공 구멍 **하나만** 뚫으면, 침식이 아몬드 테두리와 동공 테두리를 같이 만들어
    그것만으로 눈이 된다.
    """
    im, d = layer()
    poly(d, [(C - 29, C), (C - 13, C - 16), (C + 13, C - 16), (C + 29, C),
             (C + 13, C + 16), (C - 13, C + 16)], LINE)
    r = 7.5 + 1.5 * math.sin(t * math.pi)     # 동공이 조였다 풀린다
    return hole(im, lambda dd: ell(dd, C, C, r, r, LINE))
def sil_zoom(t):
    """우다다 — 뒤로 흐르는 질주선. 얇으면 뭉개진다. 굵게 간다."""
    im, d = layer()
    for y, ln, w in ((-19, 30, 9.0), (-6, 40, 11.0), (7, 35, 10.0), (19, 24, 7.5)):
        x1 = C + 27 - t * 5
        poly(d, [(x1 - ln, C + y - w * 0.5), (x1 - ln, C + y + w * 0.5),
                 (x1, C + y + w * 0.18), (x1, C + y - w * 0.18)], LINE)
    return im
def sil_taunt(t):
    """
    개무시 — 금지 표식(원 + 사선).

    [사고] 처음엔 굵은 사선 두 줄이었다. 「무시」로 읽히지 않고 그냥 빗금이었다.
    무시 · 무효는 만국 공통의 그림이 이미 있다. 새로 만들 이유가 없다.
    """
    im, d = layer()
    ell(d, C, C, 27, 27, LINE)
    hole(im, lambda dd: ell(dd, C, C, 18, 18, LINE))
    bar, db = layer()
    a = math.radians(-45)
    cs, sn = math.cos(a), math.sin(a)
    nx, ny = -sn, cs
    w = 9.0
    poly(db, [(C - cs * 27 + nx * w * 0.5, C - sn * 27 + ny * w * 0.5),
              (C - cs * 27 - nx * w * 0.5, C - sn * 27 - ny * w * 0.5),
              (C + cs * 27 - nx * w * 0.5, C + sn * 27 - ny * w * 0.5),
              (C + cs * 27 + nx * w * 0.5, C + sn * 27 + ny * w * 0.5)], LINE)
    im.alpha_composite(bar)
    return im
def sil_growl(t):
    """으르렁 — 레퍼의 X자 조각들. 자기중심이라 사방으로 흩는다."""
    im, d = layer()
    grow = 0.72 + t * 0.38
    for ang in range(0, 360, 60):
        a = math.radians(ang + 30)
        r0, ln, w = 13, 14, 11.0
        cs, sn = math.cos(a), math.sin(a)
        nx, ny = -sn, cs
        r0g, r1 = r0 * grow, (r0 + ln) * grow
        poly(d, [(C + cs * r0g + nx * w * 0.5, C + sn * r0g + ny * w * 0.5),
                 (C + cs * r0g - nx * w * 0.5, C + sn * r0g - ny * w * 0.5),
                 (C + cs * r1 - nx * w * 0.12, C + sn * r1 - ny * w * 0.12),
                 (C + cs * r1 + nx * w * 0.12, C + sn * r1 + ny * w * 0.12)], LINE)
    return im


def sil_ring(t, thick=7.0, start=8.0):
    """퍼지는 고리. 하품 · 무리 울음이 같은 문법을 쓴다 — 범위가 정보다."""
    im, d = layer()
    r = start + t * (27 - start)
    ell(d, C, C, r, r, LINE, width=thick)
    return im


def sil_sonar(t):
    """귀 쫑긋 — 앞으로 퍼지는 호 셋. 부채꼴 방향이 읽혀야 한다."""
    im, d = layer()
    for i, k in enumerate((0.45, 0.72, 1.0)):
        r = (10 + t * 17) * k + i * 4
        if r < 6:
            continue
        d.arc([(C - r) * S, (C - r) * S, (C + r) * S, (C + r) * S],
              -55, 55, fill=LINE, width=int(6.0 * S))
    return im


def sil_scent(t):
    """냄새 추적 — 흘러가는 물결 셋. 추적이라 앞으로 흐른다."""
    im, d = layer()
    for row, yy in enumerate((-13, 0, 13)):
        pts = []
        for k in range(17):
            x = -26 + k * 3.3
            y = yy + math.sin(k * 0.8 + t * 5 + row) * 4.5
            pts.append((C + x, C + y))
        d.line([(x * S, y * S) for x, y in pts], fill=LINE, width=int(5.5 * S), joint="curve")
    return im


def sil_eyebeam(t):
    """개눈빛 레이저 — 전방 직선 + 발사점의 터짐. 막대만 있으면 빔인지 봉인지 모른다."""
    im, d = layer()
    w = 9 + 5 * math.sin(t * math.pi)
    poly(d, [(C - 20, C - w * 0.35), (C - 20, C + w * 0.35),
             (C + 30, C + w * 0.65), (C + 30, C - w * 0.65)], LINE)
    # 발사점에서 튀는 조각. 「여기서 나갔다」가 보여야 방향이 확실해진다
    for ang in (-140, -105, 105, 140):
        a = math.radians(ang)
        cs, sn = math.cos(a), math.sin(a)
        nx, ny = -sn, cs
        r0, r1, ww = 11, 24, 6.0
        poly(d, [(C - 18 + cs * r0 + nx * ww * 0.5, C + sn * r0 + ny * ww * 0.5),
                 (C - 18 + cs * r0 - nx * ww * 0.5, C + sn * r0 - ny * ww * 0.5),
                 (C - 18 + cs * r1, C + sn * r1)], LINE)
    return im
def sil_static(t):
    """정전기 털 — 꺾인 번개. 갈고리처럼 흩어지면 무엇인지 모른다. 지그재그를 굵게 세운다."""
    im, d = layer()
    grow = 0.80 + t * 0.28
    for ang in (-125, -60, 5, 70, 145):
        a = math.radians(ang)
        cs, sn = math.cos(a), math.sin(a)
        nx, ny = -sn, cs

        # 중심에서 바깥으로 뻗으며 좌우로 두 번 꺾인다
        spine = [(11, 0), (18, 6.0), (25, -4.5), (30, 1.5)]
        pts_l, pts_r = [], []
        for i, (rr, off) in enumerate(spine):
            r = rr * grow
            ww = 12.5 - i * 2.2
            bx = C + cs * r + nx * off
            by = C + sn * r + ny * off
            pts_l.append((bx + nx * ww * 0.5, by + ny * ww * 0.5))
            pts_r.append((bx - nx * ww * 0.5, by - ny * ww * 0.5))
        poly(d, pts_l + list(reversed(pts_r)), LINE)
    return im

# ── 팩 아트를 대체하는 것들 ────────────────────────────────────────────────
# [사고] 절차로 그린 것이 어설퍼서 이 자리들을 이펙트 팩 이미지로 바꿔 뒀었다.
# 그런데 새 문법(두꺼운 외곽 + 속 비움)으로 오니 팩 그림만 톤이 달라 겉돌았다.
# 한 화면에서 두 문법이 섞이면 둘 다 싸구려로 보인다. 같은 문법으로 다시 그린다.

def sil_dogclaw(t):
    """발톱 세례 — 나란한 갈퀴 자국 셋. 긁힌 방향이 정보다."""
    im, d = layer()
    for i, (off, ln, w) in enumerate(((-15, 46, 9.0), (0, 52, 11.0), (15, 44, 8.5))):
        x0 = C - ln * 0.5 - 2
        # 살짝 휜 자국. 곧으면 막대로 보인다
        pts_top, pts_bot = [], []
        for k in range(5):
            u = k / 4.0
            x = x0 + ln * u
            y = C + off + math.sin(u * math.pi) * 5.0
            ww = w * (1.0 - abs(u - 0.45) * 1.3)
            ww = max(2.0, ww)
            pts_top.append((x, y - ww * 0.5))
            pts_bot.append((x, y + ww * 0.5))
        poly(d, pts_top + list(reversed(pts_bot)), LINE)
    return im


def sil_tailsw(t):
    """꼬리 — 휘두른 초승달. 베어 나간 호가 곧 사거리다."""
    im, d = layer()
    r = 25
    pts_o, pts_i = [], []
    for k in range(13):
        a = math.radians(-72 + 144 * k / 12.0)
        w = 9.5 * (1.0 - abs(k / 12.0 - 0.5) * 1.4)
        w = max(2.0, w)
        pts_o.append((C + math.cos(a) * (r + w * 0.5), C + math.sin(a) * (r + w * 0.5)))
        pts_i.append((C + math.cos(a) * (r - w * 0.5), C + math.sin(a) * (r - w * 0.5)))
    poly(d, pts_o + list(reversed(pts_i)), LINE)
    return im


def sil_shield(t):
    """털뭉치 보호막 — 몸을 감싸는 고리. 속을 비워 안이 보여야 캐릭터를 안 가린다."""
    im, d = layer()
    r = 22 + 4 * math.sin(t * math.pi)
    ell(d, C, C, r, r, LINE)
    return hole(im, lambda dd: ell(dd, C, C, r - 9, r - 9, LINE))


def sil_awaken(t):
    """개쌘놈 모드 — 위로 솟는 기운 세 갈래. 각성은 아래에서 위로 간다."""
    im, d = layer()
    rise = t * 6
    for off, h, w in ((-16, 30, 10.0), (0, 40, 13.0), (16, 27, 9.0)):
        base = C + 24 - rise
        top = base - h
        # 아래는 넓고 위로 갈수록 좁아지며 살짝 휜다
        pts_l, pts_r = [], []
        for k in range(5):
            u = k / 4.0
            y = base + (top - base) * u
            x = C + off + math.sin(u * 2.4) * 4.0
            ww = w * (1.0 - u * 0.8)
            ww = max(2.0, ww)
            pts_l.append((x - ww * 0.5, y))
            pts_r.append((x + ww * 0.5, y))
        poly(d, pts_l + list(reversed(pts_r)), LINE)
    return im


def sil_burst(t):
    """
    폭발 — 뾰족함이 크게 들쭉날쭉한 별.

    [사고] 가운데를 구멍으로 뚫었더니 **톱니바퀴**로 보였다. 고리 + 규칙적인 이빨은
    기계의 그림이다. 폭발은 가운데가 꽉 차 있고 뾰족함의 길이가 제각각이라야 한다.
    구멍을 빼고 장단 차이를 키운다.
    """
    im, d = layer()
    grow = 0.68 + t * 0.48
    pts = []
    # 긴 뾰족함과 짧은 골이 번갈아, 길이는 제각각
    spikes = [29, 13, 24, 11, 30, 15, 22, 12, 28, 10, 26, 14, 21, 12]
    for i, r in enumerate(spikes):
        a = math.radians(i * (360.0 / len(spikes)) - 17)
        rr = r * grow
        pts.append((C + math.cos(a) * rr, C + math.sin(a) * rr))
    poly(d, pts, LINE)
    return im


SETS = [
    # (폴더, 프레임수, 그리기, 설명)
    #
    # [측정] 조각 계열에 ink_px=2 를 쓰니 화면에서 **검은 덩어리**로 보였다.
    # 조각 폭이 7.5px 인데 잉크가 양쪽에서 2px 씩 먹으면 남는 밝은 부분이 3.5px —
    # 절반 이상이 검정이다. 틴트 색이 아무리 밝아도 소용이 없다.
    # 잉크는 모양 폭의 4분의 1을 넘으면 안 된다. 캐릭터도 1px 로 쓰고 있고 충분히 읽힌다.
    #
    # hollow 는 모양이 정한다 — 얇은 곳이 12px 미만이면 속이 안 남아 검은 덩어리가 된다.
    # 물건 실루엣은 대개 hollow=True(네온형), 조각 · 선 계열은 False(만화형).
    ("fx_paw", 5, lambda t: finish(render(sil_paw()), t), "발바닥"),
    ("fx_bone", 5, lambda t: finish(render(sil_bone()), t), "개뼈"),
    ("fx_bark", 5, lambda t: finish(render(sil_bark(t), ink_px=1, hollow=False), t, False), "왈왈 조각"),

    ("fx_teeth", 5, lambda t: finish(render(sil_teeth(t), ink_px=1, hollow=False), t, False), "이빨"),
    ("fx_frisbee", 5, lambda t: finish(render(sil_frisbee(t)), t, False), "원반"),
    ("fx_ball", 5, lambda t: finish(render(sil_ball(t)), t, False), "테니스공"),
    ("fx_slipper", 5, lambda t: finish(render(sil_slipper(t)), t), "슬리퍼"),
    ("fx_bread", 5, lambda t: finish(render(sil_bread(t)), t), "식빵"),
    ("fx_kibble", 5, lambda t: finish(render(sil_kibble(t), ink_px=1, hollow=False), t, False), "사료 알갱이"),
    ("fx_gaze", 5, lambda t: finish(render(sil_gaze(t)), t), "제3의 눈"),
    ("fx_zoom", 5, lambda t: finish(render(sil_zoom(t), ink_px=1, hollow=False), t, False), "질주선"),
    ("fx_taunt", 5, lambda t: finish(render(sil_taunt(t)), t), "개무시"),

    ("fx_growl", 5, lambda t: finish(render(sil_growl(t), ink_px=1, hollow=False), t, False), "으르렁"),
    ("fx_yawn", 5, lambda t: finish(render(sil_ring(t, 7.5, 6), ink_px=1, hollow=False), t, False), "하품 고리"),
    ("fx_howl", 5, lambda t: finish(render(sil_ring(t, 6.5, 10), ink_px=1, hollow=False), t, False), "무리 울음"),
    ("fx_sonar", 5, lambda t: finish(render(sil_sonar(t), ink_px=1, hollow=False), t, False), "귀 쫑긋"),
    ("fx_scent", 5, lambda t: finish(render(sil_scent(t), ink_px=1, hollow=False), t, False), "냄새 추적"),
    ("fx_eyebeam", 5, lambda t: finish(render(sil_eyebeam(t), ink_px=1, hollow=False), t, False), "눈빛 레이저"),
    ("fx_static", 5, lambda t: finish(render(sil_static(t), ink_px=1, hollow=False), t, False), "정전기"),

    ("fx_dogclaw", 5, lambda t: finish(render(sil_dogclaw(t), ink_px=1, hollow=False), t, False), "발톱 자국"),
    ("fx_tailsw", 5, lambda t: finish(render(sil_tailsw(t), ink_px=1, hollow=False), t, False), "꼬리 초승달"),
    ("fx_shield", 5, lambda t: finish(render(sil_shield(t)), t, False), "보호막 고리"),
    ("fx_awaken", 5, lambda t: finish(render(sil_awaken(t), ink_px=1, hollow=False), t, False), "각성 기운"),
    ("fx_burst", 5, lambda t: finish(render(sil_burst(t)), t, False), "폭발"),
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
            t = (i + 1) / float(n)
            fn(t).save(os.path.join(outdir, "%02d.png" % i))
        print("%-12s %d프레임  %s" % (name, n, desc))
        total += n
    print("총 %d프레임 / %d종" % (total, len(SETS)))


main()
