# -*- coding: utf-8 -*-
# 주인공 스프라이트 생성기 v2 - 「개많이잡아」 (.claude/requests.md 2 · 10번, _Ref/ArtReboot/기획서.md)
#
# v1 이 조잡했던 이유 (10-04 사용자): 얼굴이 점눈 · 단색 머리 덩어리 · 기둥 몸통 · 반바지 — 레퍼(뮤즈대시 키 아트)의
#   큰 눈 · 가닥 머리 · 가는 허리 · 플레어 치마 · 니삭스 같은 «여성스러운 실루엣»이 없었다.
# v2 규칙
# - 눈: 크게(2.9×3.9), 위 속눈썹 굵은 호 + 끝 튐, 홍채 2톤 + 큰 하이라이트 + 작은 점, 아래 속눈썹 한 획
# - 머리: 뒷머리 덩어리 위에 끝이 뾰족한 앞머리 가닥 · 볼 옆 옆머리 · 천사링 하이라이트 · 가닥 사이 그림자
# - 몸: 어깨 > 허리(잘록) < 치마 플레어. 다리는 가늘게, 니삭스 줄무늬 · 굽 있는 신발. 팔은 가늘고 손이 있다
# - 의상 · 소품이 캐릭터를 가른다 (악기 · 리본 · 초커 · 헤드셋 · 수면모자)
# - 외곽선은 실루엣 팽창, 진보라 (46,20,70) · 두께 1.3 (v1 1.6 — 굵으면 디테일이 뭉갠다)
# - 캔버스 64 좌표 · 발끝 y 61 · OUT 4. 로비 초상화는 같은 그림을 OUT 10 으로 따로 굽는다 (확대해도 선명)
# - 원형은 뮤즈대시 기본 캐릭터의 «역할»만. 이름 · 얼굴 · 고유 의상은 가져오지 않는다
#
# 사용: python build_heroes.py [--preview] [--dst <폴더>]

from PIL import Image, ImageDraw, ImageFilter
import os, sys, math
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, "Assets", "_Art", "Sprites")
LOBBY = os.path.join(ROOT, "Assets", "Resources", "Lobby")
PREVIEW = os.path.join(ROOT, "_Ref", "ArtReboot", "lineup_heroes.png")
if "--dst" in sys.argv:
    _d = sys.argv[sys.argv.index("--dst") + 1]
    DST, LOBBY, PREVIEW = _d, os.path.join(_d, "lobby"), os.path.join(_d, "lineup.png")

SS = 4
OUT = 4
S = SS * OUT
W = H = 64
PORTRAIT_OUT = 10

INK = (46, 20, 70, 255)
INK_PX = 1.3
SKIN = (255, 230, 218, 255)
SKIN_SH = (246, 196, 196, 255)
BLUSH = (255, 128, 170, 255)
WHITE = (255, 255, 255, 255)
MOUTH = (150, 40, 80, 255)
TONGUE = (255, 120, 150, 255)


def set_scale(out):
    global OUT, S
    OUT = out
    S = SS * OUT


def layer():
    im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def P(x, y):
    return (x * S, y * S)


def ell(d, x, y, rx, ry, c):
    d.ellipse([(x - rx) * S, (y - ry) * S, (x + rx) * S, (y + ry) * S], fill=c)


def poly(d, pts, c):
    d.polygon([P(x, y) for x, y in pts], fill=c)


def rrect(d, x0, y0, x1, y1, r, c):
    d.rounded_rectangle([min(x0, x1) * S, min(y0, y1) * S, max(x0, x1) * S, max(y0, y1) * S], radius=r * S, fill=c)


def seg(d, x0, y0, x1, y1, w, c):
    d.line([P(x0, y0), P(x1, y1)], fill=c, width=max(1, int(w * S)))
    ell(d, x0, y0, w / 2, w / 2, c)
    ell(d, x1, y1, w / 2, w / 2, c)


def limb(d, x0, y0, x1, y1, w0, w1, c):
    """굵기가 w0 → w1 로 가늘어지는 팔다리."""
    ang = math.atan2(y1 - y0, x1 - x0) + math.pi / 2
    dx, dy = math.cos(ang), math.sin(ang)
    poly(d, [(x0 + dx * w0, y0 + dy * w0), (x1 + dx * w1, y1 + dy * w1), (x1 - dx * w1, y1 - dy * w1), (x0 - dx * w0, y0 - dy * w0)], c)
    ell(d, x0, y0, w0, w0, c)
    ell(d, x1, y1, w1, w1, c)


def arc(d, x, y, rx, ry, a0, a1, w, c):
    d.arc([(x - rx) * S, (y - ry) * S, (x + rx) * S, (y + ry) * S], a0, a1, fill=c, width=max(1, int(w * S)))


def lock(d, x0, y0, x1, y1, w, c, bend=0.0):
    """끝이 뾰족한 머리 가닥 — 뿌리 (x0,y0) 폭 w, 끝 (x1,y1). bend 로 휜다."""
    mx, my = (x0 + x1) / 2 + bend, (y0 + y1) / 2
    poly(d, [(x0 - w, y0), (mx - w * 0.6, my), (x1, y1), (mx + w * 0.6, my), (x0 + w, y0)], c)


def shrink(im):
    return im.resize((W * OUT, H * OUT), Image.LANCZOS)


def fin(lay):
    small = shrink(lay)
    k = int(round(INK_PX * OUT)) * 2 + 1
    grown = small.split()[3].filter(ImageFilter.MaxFilter(k))
    out = Image.new("RGBA", small.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", small.size, INK), (0, 0), grown)
    out.alpha_composite(small)
    return out


def stack(layers):
    out = Image.new("RGBA", (W * OUT, H * OUT), (0, 0, 0, 0))
    for lay in layers:
        if isinstance(lay, tuple):
            out.alpha_composite(shrink(lay[0]))
        else:
            out.alpha_composite(fin(lay))
    return out


def mix(a, b, k):
    return tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,)


def shadow(c):
    import colorsys
    h, s, v = colorsys.rgb_to_hsv(*(x / 255.0 for x in c[:3]))
    dh = ((290 / 360.0 - h + 0.5) % 1.0) - 0.5
    r, g, b = colorsys.hsv_to_rgb((h + dh * 0.12) % 1.0, min(1.0, s * 1.06 + 0.06), v * 0.78)
    return (int(r * 255), int(g * 255), int(b * 255), 255)


def light(c, k=0.5):
    return mix(c, WHITE, k)


# ── 주인공 표 (순서 = RunSelection.Heroes) ────────────────────────────
HEROES = {
    # 1 시바 — 베이시스트 (린 원형: 성급하고 상냥한 록커). 주황 트윈테일 · 뾰족 귀 · 마젠타 크롭 재킷 · 검정 주름치마 · 분홍/흰 줄무늬 니삭스
    "shiba": dict(hair=(255, 140, 56), ear="point", tail="curl", style="twin", inner=(255, 236, 214),
                  eye=(255, 70, 160), top=(255, 60, 160), trim=(70, 230, 240), skirt=(52, 40, 96),
                  sock=(255, 255, 255), stripe=(255, 90, 170), shoe=(70, 230, 240),
                  acc="star", prop="bass", mouth="grin"),
    # 2 코기 — 드러머 (부로 원형: 활기 1000%). 크림 단발 · 큰 귀 · 시안 후드 · 분홍 치마 · 노랑/흰 줄무늬 · 드럼 스틱
    "corgi": dict(hair=(255, 210, 120), ear="big", tail="stub", style="bob", inner=(255, 160, 190),
                  eye=(40, 160, 255), top=(70, 220, 240), trim=(255, 236, 90), skirt=(255, 100, 180),
                  sock=(255, 255, 255), stripe=(255, 220, 70), shoe=(255, 80, 120),
                  acc="bow", prop="drum", mouth="open"),
    # 3 그레이하운드 — 바이올리니스트 (마리쟈 원형: 지적이고 쿨함). 은보라 긴 생머리 · 접힌 귀 · 진남 원피스 + 형광 리본 · 검은 타이츠
    "greyhound": dict(hair=(200, 194, 238), ear="fold", tail="thin", style="long", inner=(150, 140, 200),
                      eye=(140, 255, 110), top=(56, 50, 120), trim=(150, 255, 90), skirt=(40, 34, 90),
                      sock=(50, 40, 80), stripe=None, shoe=(255, 60, 160),
                      acc="ribbon", prop="violin", mouth="calm"),
    # 4 포메라니안 — 아이돌 보컬 (부로 아이돌 의상 원형). 복숭아 솜털 퍼프 · 작은 귀 · 레몬 프릴 상의 · 분홍 튀튀 · 흰 양말 · 헤드셋
    "pomeranian": dict(hair=(255, 184, 150), ear="small", tail="puff", style="puff", inner=(255, 140, 170),
                       eye=(255, 130, 40), top=(255, 236, 100), trim=(255, 255, 255), skirt=(255, 120, 196),
                       sock=(255, 255, 255), stripe=None, shoe=(255, 255, 255),
                       acc="heart", prop="mic", mouth="open", tutu=True),
    # 5 사모예드 — 자장가 (린 몽유병 의상 원형). 흰 솜 긴머리 · 둥근 귀 · 별무늬 잠옷 원피스 · 수면모자 · 오르골
    "samoyed": dict(hair=(248, 246, 255), ear="fluff", tail="puff", style="cap", inner=(200, 190, 240),
                    eye=(130, 120, 255), top=(160, 176, 255), trim=(255, 240, 150), skirt=(130, 140, 240),
                    sock=(220, 210, 255), stripe=(255, 255, 255), shoe=(200, 180, 255),
                    acc=None, prop="musicbox", mouth="o", sleepy=True),
}
for _c in HEROES.values():
    for _k, _v in list(_c.items()):
        if isinstance(_v, tuple) and len(_v) == 3:
            _c[_k] = _v + (255,)

HY = 20.0          # 머리 중심
HR = 11.0          # 얼굴 반지름
NECK = 30.5
WAIST = 38.5
HEM = 46.0
FOOT = 59.5


# ── 머리 · 귀 ────────────────────────────────────────────────────────

def ears(d, cx, hy, cfg, side=False):
    hair, kind = cfg["hair"], cfg["ear"]
    for s in ((1,) if side else (-1, 1)):
        bx = cx + (s * 7.5 if not side else -3)
        if kind == "point":
            poly(d, [(bx - 4.2, hy - 7), (bx + s * 3.0, hy - 20), (bx + 4.6, hy - 8)], hair)
        elif kind == "big":
            if side:
                poly(d, [(bx - 7, hy - 6), (bx - 5, hy - 26), (bx + 6, hy - 7)], hair)
            else:
                poly(d, [(bx - 5.6, hy - 6), (bx + s * 5, hy - 24), (bx + 6, hy - 7)], hair)
        elif kind == "small":
            poly(d, [(bx - 3, hy - 13), (bx + s * 2.5, hy - 21), (bx + 3.5, hy - 13)], hair)
        elif kind == "fluff":
            ell(d, bx + s * 1.5, hy - 13, 4.4, 5.6, hair)
        else:  # fold
            ex = cx + s * (HR - 1) if not side else cx - 5
            poly(d, [(ex - s * 3, hy - 10), (ex + s * 5.5, hy - 8), (ex + s * 7, hy + 2), (ex + s * 3.2, hy + 3)], hair)


def ears_inner(d, cx, hy, cfg, side=False):
    kind, inner = cfg["ear"], cfg["inner"]
    for s in ((1,) if side else (-1, 1)):
        bx = cx + (s * 7.5 if not side else -3)
        if kind == "point":
            poly(d, [(bx - 2, hy - 9), (bx + s * 2.6, hy - 17), (bx + 2.4, hy - 9.5)], inner)
        elif kind == "big":
            if side:
                poly(d, [(bx - 4.6, hy - 8), (bx - 4, hy - 22), (bx + 3, hy - 8.5)], inner)
            else:
                poly(d, [(bx - 3, hy - 8), (bx + s * 4.2, hy - 20.5), (bx + 3.4, hy - 8.5)], inner)
        elif kind == "small":
            poly(d, [(bx - 1.4, hy - 14), (bx + s * 2, hy - 19), (bx + 1.8, hy - 14)], inner)
        elif kind == "fluff":
            ell(d, bx + s * 1.5, hy - 13, 2.0, 3.0, inner)


def hair_back(d, cx, hy, cfg, view):
    """뒷머리 실루엣. view: front · back · side."""
    c = shadow(cfg["hair"]) if view != "back" else cfg["hair"]
    st = cfg["style"]
    ell(d, cx, hy - 0.5, HR + 2.2, HR + 1.8, c)
    if st == "twin":
        for s in ((-1,) if view == "side" else (-1, 1)):
            ox = cx + s * 14 if view != "side" else cx - 13
            # 묶음 + 길게 흘러내리는 트윈테일 (끝이 두 갈래)
            ell(d, ox, hy - 4, 3.6, 3.2, c)
            poly(d, [(ox - 4, hy - 2), (ox + 4.5, hy - 2), (ox + 6, hy + 14), (ox + 2, hy + 26), (ox - 1, hy + 18), (ox - 4, hy + 25), (ox - 6, hy + 10)], c)
    elif st == "bob":
        poly(d, [(cx - HR - 3, hy - 3), (cx + HR + 3, hy - 3), (cx + HR + 3.5, hy + 9), (cx + HR - 1, hy + 12), (cx - HR + 1, hy + 12), (cx - HR - 3.5, hy + 9)], c)
    elif st == "long":
        poly(d, [(cx - HR - 2.5, hy - 2), (cx + HR + 2.5, hy - 2), (cx + HR + 4, hy + 24), (cx + 4, hy + 27), (cx - 4, hy + 27), (cx - HR - 4, hy + 24)], c)
    elif st == "puff":
        for a in range(0, 360, 30):
            r = math.radians(a)
            ell(d, cx + math.cos(r) * (HR + 3), hy + math.sin(r) * (HR + 2), 5.3, 5.3, c)
        for s in ((-1,) if view == "side" else (-1, 1)):
            ell(d, cx + s * 17 if view != "side" else cx - 17, hy + 3, 6.2, 6.2, c)
    elif st == "cap":
        poly(d, [(cx - HR - 3.5, hy - 2), (cx + HR + 3.5, hy - 2), (cx + HR + 4.5, hy + 22), (cx - HR - 4.5, hy + 22)], c)
        for x in (-15, -9, -3, 3, 9, 15):
            ell(d, cx + x, hy + 22.5, 4.2, 3.8, c)


def bangs(d, cx, hy, cfg, side=False):
    """앞머리 — 정수리 덮개 + 끝이 뾰족한 가닥 여럿 + 볼 옆 옆머리."""
    c = cfg["hair"]
    if side:
        ell(d, cx - 1, hy - 4.5, HR + 1, 8, c)
        poly(d, [(cx - HR - 1.5, hy - 5), (cx - 2, hy - 5), (cx - 3, hy + 9), (cx - HR, hy + 6)], c)
        for x0, x1, y1 in ((2, 4, 1.5), (6, 9, 0.5)):
            lock(d, cx + x0, hy - 6, cx + x1, hy + y1, 2.6, c)
        return
    ell(d, cx, hy - 5, HR + 1, 8.2, c)
    # 가닥: 가운데가 짧고 가장자리가 길다
    for x0, x1, y1, w in ((-8, -10, 3.5, 2.8), (-4.5, -5.5, 1.5, 2.8), (-1, -2, 0.0, 2.4), (2.5, 3.5, 1.0, 2.6), (6, 7.5, 2.5, 2.8), (9, 11, 4.5, 2.4)):
        lock(d, cx + x0, hy - 5.5, cx + x1, hy + y1, w, c)
    # 볼 옆 옆머리
    for s in (-1, 1):
        lock(d, cx + s * (HR - 0.5), hy - 4, cx + s * (HR + 0.5), hy + 10 if cfg["style"] not in ("bob", "puff") else hy + 7, 2.2, c, bend=s * 0.8)


def hair_light(d, cx, hy, cfg, side=False):
    """천사링 하이라이트 + 가닥 사이 그림자 (선 없이 얹는다)."""
    hl = light(cfg["hair"], 0.6)
    sh = shadow(cfg["hair"])
    if side:
        arc(d, cx - 2, hy - 2, 9.5, 8, 200, 290, 1.3, hl)
        return
    arc(d, cx, hy - 6, 8.5, 5.5, 215, 325, 1.0, hl)
    for x in (-6, -2.5, 1.5, 5):
        seg(d, cx + x, hy - 6.5, cx + x - 0.3, hy - 3.8, 0.5, sh)


def face_skin(d, cx, hy, side=False):
    """얼굴 — 볼은 둥글고 턱은 살짝 뾰족 (v1 은 동그라미 하나였다)."""
    if side:
        ell(d, cx + 1.5, hy + 1, HR - 1.5, HR - 1.5, SKIN)
        poly(d, [(cx + 3, hy + 7), (cx + 9.5, hy + 4), (cx + 6, hy + 10)], SKIN)
        return
    ell(d, cx, hy + 0.5, HR - 0.5, HR - 1.2, SKIN)
    poly(d, [(cx - 7, hy + 5), (cx + 7, hy + 5), (cx + 2, hy + 10.5), (cx - 2, hy + 10.5)], SKIN)
    # 앞머리가 드리운 이마 그림자 — 앞머리보다 먼저 그려 가닥 사이로만 보인다
    poly(d, [(cx - 9, hy - 3.5), (cx + 9, hy - 3.5), (cx + 7.5, hy + 1.6), (cx - 7.5, hy + 1.6)], SKIN_SH)


def face(d, cx, hy, cfg, side=False):
    """눈 · 입 · 볼터치 (선 없이 얹는 층)."""
    sleepy = cfg.get("sleepy", False)
    eye = cfg["eye"]
    eye_d = shadow(eye)
    eye_l = light(eye, 0.45)
    xs = (cx + 5.6,) if side else (cx - 4.6, cx + 4.6)
    ey = hy + 2.4
    for x in xs:
        out = 1 if (side or x > cx) else -1
        if sleepy:
            arc(d, x, ey - 0.5, 2.8, 2.2, 0, 180, 1.2, INK)
            seg(d, x + out * 2.6, ey - 0.4, x + out * 3.8, ey - 1.6, 0.8, INK)
            continue
        ell(d, x, ey, 2.9, 3.9, WHITE)
        ell(d, x, ey + 0.4, 2.5, 3.4, eye)
        poly(d, [(x - 2.5, ey - 1.6), (x + 2.5, ey - 1.6), (x + 2.2, ey - 3), (x - 2.2, ey - 3)], eye_d)
        ell(d, x, ey + 2.2, 1.8, 1.0, eye_l)
        ell(d, x, ey + 0.4, 1.0, 1.5, eye_d)
        ell(d, x - 0.9 * out, ey - 1.2, 1.1, 1.4, WHITE)
        ell(d, x + 1.0 * out, ey + 1.8, 0.45, 0.45, WHITE)
        arc(d, x, ey + 0.4, 3.3, 4.3, 200, 340, 1.1, INK)
        poly(d, [(x + out * 2.6, ey - 2.6), (x + out * 4.6, ey - 3.4), (x + out * 3.1, ey - 1.6)], INK)
        seg(d, x - 1.0, ey + 4.1, x + 1.0, ey + 4.1, 0.4, mix(INK, SKIN, 0.4))
    for x in ((cx + 2.8,) if side else (cx - 7.2, cx + 7.2)):
        ell(d, x, hy + 6.3, 2.2, 1.0, BLUSH)
        for k in (-1, 0, 1):
            seg(d, x + k * 1.1 - 0.4, hy + 6.8, x + k * 1.1 + 0.4, hy + 5.8, 0.25, mix(BLUSH, WHITE, 0.6))
    mx, my = (cx + 7.8, hy + 7.4) if side else (cx, hy + 7.4)
    m = cfg.get("mouth", "calm")
    if m == "grin":
        d.chord([(mx - 2.2) * S, (my - 1.2) * S, (mx + 2.2) * S, (my + 2.2) * S], 0, 180, fill=MOUTH)
        poly(d, [(mx + 0.6, my + 0.4), (mx + 1.4, my + 0.4), (mx + 1.0, my + 1.3)], WHITE)
    elif m == "open":
        d.chord([(mx - 1.9) * S, (my - 1.0) * S, (mx + 1.9) * S, (my + 2.8) * S], 0, 180, fill=MOUTH)
        ell(d, mx, my + 1.6, 1.1, 0.6, TONGUE)
    elif m == "o":
        ell(d, mx, my + 0.6, 0.8, 1.0, MOUTH)
    else:
        arc(d, mx, my - 0.3, 1.5, 1.2, 20, 160, 0.6, MOUTH)


def accessory(d, cx, hy, cfg, view):
    a = cfg.get("acc")
    if a is None or view == "back":
        return
    x = cx + 8 if view != "side" else cx + 1
    if a == "star":
        pts = []
        for i in range(10):
            r = math.radians(-90 + i * 36)
            rr = 2.6 if i % 2 == 0 else 1.1
            pts.append((x + math.cos(r) * rr, hy - 6 + math.sin(r) * rr))
        poly(d, pts, (255, 236, 90, 255))
    elif a == "bow":
        poly(d, [(x, hy - 7), (x - 3.4, hy - 9.4), (x - 3.4, hy - 4.6)], (255, 80, 120, 255))
        poly(d, [(x, hy - 7), (x + 3.4, hy - 9.4), (x + 3.4, hy - 4.6)], (255, 80, 120, 255))
        ell(d, x, hy - 7, 1.0, 1.0, (255, 160, 190, 255))
    elif a == "heart":
        ell(d, x - 1.1, hy - 7, 1.3, 1.3, (255, 70, 150, 255))
        ell(d, x + 1.1, hy - 7, 1.3, 1.3, (255, 70, 150, 255))
        poly(d, [(x - 2.3, hy - 6.6), (x + 2.3, hy - 6.6), (x, hy - 4)], (255, 70, 150, 255))
    elif a == "ribbon":
        rx = cx - 9 if view != "side" else cx - 8
        poly(d, [(rx, hy - 8), (rx - 3.6, hy - 10.5), (rx - 3.2, hy - 5.6)], cfg["trim"])
        poly(d, [(rx, hy - 8), (rx + 3.6, hy - 10.5), (rx + 3.2, hy - 5.6)], cfg["trim"])


# ── 몸 ──────────────────────────────────────────────────────────────

def torso(d, cx, bob, cfg, view):
    """상의 — 어깨 > 잘록한 허리. 치마는 허리에서 퍼진다."""
    top = cfg["top"] if view != "back" else shadow(cfg["top"])
    w_sh, w_wa = (5.4, 3.4) if view != "side" else (3.6, 2.8)
    poly(d, [(cx - w_sh, NECK + 1 + bob), (cx + w_sh, NECK + 1 + bob), (cx + w_wa + 0.3, WAIST + bob), (cx - w_wa - 0.3, WAIST + bob)], top)
    ell(d, cx - w_sh + 0.8, NECK + 2 + bob, 1.8, 1.8, top)
    ell(d, cx + w_sh - 0.8, NECK + 2 + bob, 1.8, 1.8, top)
    sk = cfg["skirt"]
    hem_w = 9.0 if view != "side" else 7.0
    if cfg.get("tutu"):
        hem_w += 2.0
    poly(d, [(cx - w_wa - 0.5, WAIST - 0.8 + bob), (cx + w_wa + 0.5, WAIST - 0.8 + bob), (cx + hem_w, HEM + bob), (cx - hem_w, HEM + bob)], sk)


def torso_detail(d, cx, bob, cfg, view):
    """옷 디테일 (선 없이): 칼라 · 지퍼 · 주름 · 프릴 · 초커 · 그림자 단."""
    if view == "back":
        for k in (-4, 0, 4):
            seg(d, cx + k * 0.6, WAIST + 1 + bob, cx + k * 1.4, HEM - 0.5 + bob, 0.4, shadow(cfg["skirt"]))
        return
    trim = cfg["trim"]
    rrect(d, cx - 2.6, NECK - 0.6 + bob, cx + 2.6, NECK + 0.7 + bob, 0.5, INK)
    ell(d, cx, NECK + 1.4 + bob, 0.8, 0.8, trim)
    if view != "side":
        poly(d, [(cx + 1.6, NECK + 1.5 + bob), (cx + 5.4, NECK + 1.5 + bob), (cx + 3.7, WAIST + bob), (cx + 0.8, WAIST + bob)], shadow(cfg["top"]))
        poly(d, [(cx - 3.6, NECK + 1 + bob), (cx, NECK + 4 + bob), (cx + 3.6, NECK + 1 + bob), (cx + 2, NECK + 1 + bob), (cx, NECK + 2.6 + bob), (cx - 2, NECK + 1 + bob)], trim)
        seg(d, cx, NECK + 4 + bob, cx, WAIST - 0.5 + bob, 0.45, trim)
    sk_sh = shadow(cfg["skirt"])
    for k in ((-5, -2, 1, 4) if view != "side" else (-3, 0, 3)):
        seg(d, cx + k * 0.55, WAIST + 0.5 + bob, cx + k * 1.35, HEM - 0.6 + bob, 0.45, sk_sh)
    if cfg.get("tutu"):
        for k in range(-5, 6):
            ell(d, cx + k * 2.0, HEM + 0.3 + bob, 1.4, 0.9, WHITE)
    else:
        hem_w = 9.0 if view != "side" else 7.0
        seg(d, cx - hem_w + 0.6, HEM - 0.6 + bob, cx + hem_w - 0.6, HEM - 0.6 + bob, 0.5, trim)
    rrect(d, cx - 3.6, WAIST - 1.0 + bob, cx + 3.6, WAIST + 0.2 + bob, 0.4, trim)
    if cfg.get("prop") == "musicbox":
        for x, y in ((-2.5, 34), (2.5, 36.5), (-1, 43), (4, 44)):
            ell(d, cx + x, y + bob, 0.7, 0.7, (255, 240, 150, 255))


def leg(d, x_hip, x_foot, y_top, y_foot, cfg, side_dir=0):
    """가는 다리 + 니삭스 + 굽 신발. side_dir: 옆모습이면 발끝 방향(+1)."""
    limb(d, x_hip, y_top, x_foot, y_foot - 1.2, 1.9, 1.3, SKIN)
    ky = y_top + (y_foot - y_top) * 0.35
    kx = x_hip + (x_foot - x_hip) * 0.35
    limb(d, kx, ky, x_foot, y_foot - 1.2, 1.75, 1.35, cfg["sock"])
    fx = x_foot + (1.4 * side_dir)
    rrect(d, fx - 2.3, y_foot - 1.8, fx + 2.3 + side_dir * 1.0, y_foot + 0.6, 1.0, cfg["shoe"])
    rrect(d, fx - 1.6, y_foot + 0.2, fx + 1.6 + side_dir * 0.6, y_foot + 1.4, 0.3, mix(cfg["shoe"], INK, 0.5))


def leg_stripes(d, x_hip, x_foot, y_top, y_foot, cfg):
    if cfg.get("stripe") is None:
        return
    for k in range(4):
        u = 0.45 + k * 0.13
        x = x_hip + (x_foot - x_hip) * u
        y = y_top + (y_foot - y_top) * u
        seg(d, x - 1.6, y, x + 1.6, y, 0.7, cfg["stripe"])


def arm(d, sx, sy, hx, hy, cfg, view):
    top = cfg["top"] if view != "back" else shadow(cfg["top"])
    limb(d, sx, sy, (sx + hx) / 2, (sy + hy) / 2, 1.6, 1.3, top)
    limb(d, (sx + hx) / 2, (sy + hy) / 2, hx, hy, 1.2, 1.0, SKIN)
    ell(d, hx, hy + 0.6, 1.5, 1.5, SKIN)


def tail(d, cx, bob, cfg, facing):
    c = cfg["hair"]
    hip = WAIST + 3 + bob
    k = cfg["tail"]
    x = cx + (9 if facing > 0 else (0 if facing == 0 else -10))
    if k == "curl":
        ell(d, x, hip - 3, 5, 5, c)
        ell(d, x, hip - 3, 2, 2, (0, 0, 0, 0))
    elif k == "stub":
        ell(d, x, hip + 1, 3.8, 3.2, c)
    elif k == "puff":
        for dx, dy, r in ((0, -4, 4.4), (-2.4, -1, 3.6), (2.4, -1, 3.6)):
            ell(d, x + dx, hip + dy, r, r, c)
    else:
        dx = 1 if facing >= 0 else -1
        limb(d, x, hip, x + dx * 5, hip + 8, 1.3, 0.6, c)


# ── 소품 ────────────────────────────────────────────────────────────
WOOD = (196, 112, 64, 255)
WOOD_D = (150, 80, 50, 255)
METAL = (230, 230, 245, 255)
PINK = (255, 60, 160, 255)


def prop_back(d, cfg, cx, hy, bob, view):
    if cfg.get("prop") == "bass" and view == "front":
        poly(d, [(cx - 12, hy - 9), (cx - 9, hy - 11), (cx + 8, WAIST - 2 + bob), (cx + 5, WAIST + 1 + bob)], WOOD_D)
        ell(d, cx + 9, WAIST + 3 + bob, 6, 7, PINK)
        rrect(d, cx - 14, hy - 15, cx - 9, hy - 9, 1.4, (60, 40, 90, 255))


def prop_front(d, cfg, cx, hy, bob, phase, view, hands):
    p = cfg.get("prop")
    (lhx, lhy), (rhx, rhy) = hands
    if p == "bass":
        if view == "back":
            poly(d, [(cx - 2.5, hy - 14), (cx + 1, hy - 14), (cx + 1.5, WAIST - 2 + bob), (cx - 2.5, WAIST - 2 + bob)], WOOD_D)
            ell(d, cx - 0.5, WAIST + 2 + bob, 6.5, 7.5, PINK)
            ell(d, cx - 0.5, WAIST + bob, 2, 2, (60, 40, 90, 255))
            rrect(d, cx - 4, hy - 20, cx + 2, hy - 13, 1.5, (60, 40, 90, 255))
        elif view == "side":
            poly(d, [(cx - 9, hy - 9), (cx - 6, hy - 11), (cx - 2, WAIST - 2 + bob), (cx - 5, WAIST + bob)], WOOD_D)
            ell(d, cx - 6, WAIST + 3 + bob, 4.6, 6.4, PINK)
    elif p == "drum":
        if view != "back":
            dx = -7 if view == "front" else 4
            ell(d, cx + dx, WAIST + 6 + bob, 4.8, 2.8, WHITE)
            rrect(d, cx + dx - 4.8, WAIST + 6 + bob, cx + dx + 4.8, WAIST + 9.5 + bob, 1.4, (255, 90, 170, 255))
        for hx, hy2, s in (((lhx, lhy, -1), (rhx, rhy, 1)) if view == "front" else ((rhx, rhy, 1),)):
            seg(d, hx, hy2, hx + s * 3.5, hy2 - 8, 1.1, WOOD)
            ell(d, hx + s * 3.5, hy2 - 8, 1.1, 1.1, (255, 245, 200, 255))
    elif p == "violin":
        if view == "front":
            ell(d, cx - 8.5, NECK + 5 + bob, 3.4, 4.4, WOOD)
            ell(d, cx - 8.5, NECK + 8 + bob, 4.0, 3.2, WOOD)
            rrect(d, cx - 9.1, NECK - 4 + bob, cx - 7.9, NECK + 2 + bob, 0.4, (40, 30, 30, 255))
            seg(d, rhx, rhy, cx - 3, NECK + 1 + bob, 0.6, METAL)
        elif view == "side":
            ell(d, cx + 5, NECK + 4 + bob, 3.2, 4.2, WOOD)
            seg(d, rhx, rhy, cx + 11, NECK + 2 + bob, 0.6, METAL)
        else:
            ell(d, cx - 8, NECK + 5 + bob, 3.2, 4.2, WOOD_D)
    elif p == "musicbox" and view != "back":
        bx = (lhx + rhx) / 2 if view == "front" else rhx + 2
        by = max(lhy, rhy) - 1
        rrect(d, bx - 4.4, by - 1, bx + 4.4, by + 4, 1.2, (255, 200, 120, 255))
        rrect(d, bx - 4.4, by - 3, bx + 4.4, by - 0.4, 1.0, (255, 150, 200, 255))
        ell(d, bx + 5.4, by + 1.5, 1.1, 1.1, (255, 230, 120, 255))


def prop_head(d, cfg, cx, hy, view):
    p = cfg.get("prop")
    if p == "mic":
        arc(d, cx, hy - 2, 13, 12, 200, 340, 1.4, (60, 40, 90, 255))
        if view != "back":
            ex = cx + (11.5 if view == "front" else 1)
            ell(d, ex, hy + 1, 2.4, 3.0, (255, 110, 190, 255))
            mx = cx + (4 if view == "front" else 9)
            seg(d, ex, hy + 3, mx, hy + 8.5, 0.8, (60, 40, 90, 255))
            ell(d, mx, hy + 8.5, 1.3, 1.3, (60, 40, 90, 255))
    elif p == "musicbox":
        cap = (120, 140, 255, 255)
        tipx = cx + (20 if view != "side" else -18)
        poly(d, [(cx - 11, hy - 8), (cx + 11, hy - 8), (cx + 8, hy - 22), (tipx + 2, hy + 6), (tipx - 3, hy + 6), (cx + 2, hy - 18)], cap)
        rrect(d, cx - 12, hy - 9.5, cx + 12, hy - 6, 1.6, WHITE)
        ell(d, tipx, hy + 8, 3.8, 3.8, (255, 236, 120, 255))


# ── 방향별 ──────────────────────────────────────────────────────────

def draw_front(cfg, phase, bob):
    cx = 32
    hy = HY + bob
    sw = 1.4 * phase
    lift = 2.4 * phase

    back, d = layer()
    prop_back(d, cfg, cx, hy, bob, "front")
    hair_back(d, cx, hy, cfg, "front")
    tail(d, cx, bob, cfg, 1)
    leg(d, cx - 2.6, cx - 3.0 - 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, lift), cfg)

    body, d = layer()
    hands = ((cx - 8.5 - sw, NECK + 10 + bob), (cx + 8.5 + sw, NECK + 10 + bob))
    arm(d, cx - 5, NECK + 2 + bob, hands[0][0], hands[0][1], cfg, "front")
    arm(d, cx + 5, NECK + 2 + bob, hands[1][0], hands[1][1], cfg, "front")
    leg(d, cx + 2.6, cx + 3.0 + 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, -lift), cfg)
    torso(d, cx, bob, cfg, "front")

    deco, d = layer()
    leg_stripes(d, cx - 2.6, cx - 3.0 - 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, lift), cfg)
    leg_stripes(d, cx + 2.6, cx + 3.0 + 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, -lift), cfg)
    torso_detail(d, cx, bob, cfg, "front")

    props, d = layer()
    prop_front(d, cfg, cx, hy, bob, phase, "front", hands)

    head, d = layer()
    ears(d, cx, hy, cfg)
    face_skin(d, cx, hy)
    bangs(d, cx, hy, cfg)

    hat, d = layer()
    prop_head(d, cfg, cx, hy, "front")
    accessory(d, cx, hy, cfg, "front")

    top, d = layer()
    ears_inner(d, cx, hy, cfg)
    hair_light(d, cx, hy, cfg)
    face(d, cx, hy, cfg)

    return stack([back, body, (deco,), props, head, hat, (top,)])


def draw_back(cfg, phase, bob):
    cx = 32
    hy = HY + bob
    sw = 1.4 * phase
    lift = 2.4 * phase

    back, d = layer()
    leg(d, cx - 2.6, cx - 3.0 + 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, lift), cfg)

    body, d = layer()
    hands = ((cx - 8.5 + sw, NECK + 10 + bob), (cx + 8.5 - sw, NECK + 10 + bob))
    arm(d, cx - 5, NECK + 2 + bob, hands[0][0], hands[0][1], cfg, "back")
    arm(d, cx + 5, NECK + 2 + bob, hands[1][0], hands[1][1], cfg, "back")
    leg(d, cx + 2.6, cx + 3.0 - 0.4 * phase, HEM - 3 + bob, FOOT - max(0.0, -lift), cfg)
    torso(d, cx, bob, cfg, "back")

    deco, d = layer()
    leg_stripes(d, cx - 2.6, cx - 3.0, HEM - 3 + bob, FOOT - max(0.0, lift), cfg)
    leg_stripes(d, cx + 2.6, cx + 3.0, HEM - 3 + bob, FOOT - max(0.0, -lift), cfg)
    torso_detail(d, cx, bob, cfg, "back")

    tl, d = layer()
    tail(d, cx, bob, cfg, 0)

    head, d = layer()
    ears(d, cx, hy, cfg)
    hair_back(d, cx, hy, cfg, "back")

    props, d = layer()
    prop_front(d, cfg, cx, hy, bob, phase, "back", hands)
    hat, d = layer()
    prop_head(d, cfg, cx, hy, "back")

    hl, d = layer()
    sh = shadow(cfg["hair"])
    for x in (-6, -2, 2, 6):
        seg(d, cx + x * 0.6, hy - 9, cx + x * 1.3, hy + 9, 0.5, sh)
    arc(d, cx, hy - 2, 10, 8.5, 205, 335, 1.3, light(cfg["hair"], 0.6))
    if cfg["style"] == "twin":
        for s in (-1, 1):
            ell(d, cx + s * 14, hy - 4, 2.2, 1.6, cfg["top"])

    return stack([back, body, (deco,), tl, head, (hl,), props, hat])


def draw_side(cfg, phase, bob):
    cx = 31
    hy = HY + bob
    stride = 4.2 * phase

    back, d = layer()
    hair_back(d, cx - 1, hy, cfg, "side")
    tail(d, cx, bob, cfg, -1)
    leg(d, cx - 0.5, cx - stride, HEM - 3 + bob, FOOT, cfg, side_dir=1)
    arm(d, cx, NECK + 2 + bob, cx + 3 * phase, NECK + 10 + bob, cfg, "back")

    body, d = layer()
    leg(d, cx + 0.5, cx + stride, HEM - 3 + bob, FOOT, cfg, side_dir=1)
    torso(d, cx, bob, cfg, "side")

    deco, d = layer()
    leg_stripes(d, cx - 0.5, cx - stride, HEM - 3 + bob, FOOT, cfg)
    leg_stripes(d, cx + 0.5, cx + stride, HEM - 3 + bob, FOOT, cfg)
    torso_detail(d, cx, bob, cfg, "side")

    armf, d = layer()
    hand = (cx - 3 * phase + 1, NECK + 10 + bob)
    arm(d, cx, NECK + 2 + bob, hand[0], hand[1], cfg, "side")

    props, d = layer()
    prop_front(d, cfg, cx, hy, bob, phase, "side", (hand, hand))

    head, d = layer()
    ears(d, cx, hy, cfg, side=True)
    face_skin(d, cx, hy, side=True)
    bangs(d, cx, hy, cfg, side=True)

    hat, d = layer()
    prop_head(d, cfg, cx, hy, "side")
    accessory(d, cx, hy, cfg, "side")

    top, d = layer()
    ears_inner(d, cx, hy, cfg, side=True)
    hair_light(d, cx, hy, cfg, side=True)
    face(d, cx, hy, cfg, side=True)

    return stack([back, body, (deco,), armf, props, head, hat, (top,)])


def ground():
    im, d = layer()
    d.ellipse([(32 - 11) * S, (60.2 - 3.4) * S, (32 + 11) * S, (60.2 + 3.4) * S], fill=(30, 12, 50, 150))
    return shrink(im)


WALK = [(-1.0, 0.8), (-0.35, -1.0), (1.0, 0.8), (0.35, -1.0)]
DRAW = {"down": draw_front, "up": draw_back, "side": draw_side}
PREVIEW_ONLY = "--preview" in sys.argv


def save(img, name):
    base = ground()
    base.alpha_composite(img)
    if not PREVIEW_ONLY:
        base.save(os.path.join(DST, name + ".png"))
    return base


def portrait(cfg):
    """로비 초상화 — 같은 그림을 OUT 10 으로 크게 굽는다. 사용자 일러스트가 오면 교체 (TASKS W-2)."""
    set_scale(PORTRAIT_OUT)
    img = draw_front(cfg, 0, 0)
    set_scale(4)
    return img


def main():
    os.makedirs(DST, exist_ok=True)
    os.makedirs(LOBBY, exist_ok=True)
    T = W * OUT
    sheet = Image.new("RGBA", (T * 5, T * 3 * len(HEROES)), (60, 30, 90, 255))
    n = 0
    for r, (key, cfg) in enumerate(HEROES.items()):
        for f, (face_dir, suffix) in enumerate((("down", ""), ("up", "up"), ("side", "side"))):
            img = save(DRAW[face_dir](cfg, 0, 0), "dog_%s_idle%s" % (key, suffix))
            sheet.alpha_composite(img, (0, (r * 3 + f) * T))
            n += 1
            for i, (ph, bob) in enumerate(WALK):
                img = save(DRAW[face_dir](cfg, ph, bob), "dog_%s_walk%s%d" % (key, suffix, i))
                sheet.alpha_composite(img, ((i + 1) * T, (r * 3 + f) * T))
                n += 1
        pimg = portrait(cfg)
        if not PREVIEW_ONLY:
            pimg.save(os.path.join(LOBBY, "portrait_%s.png" % key))
    sheet.save(PREVIEW)
    print("주인공 스프라이트 %d장 · 초상화 %d장 (OUT %d) · 라인업 %s" % (n, len(HEROES), PORTRAIT_OUT, PREVIEW))


main()
