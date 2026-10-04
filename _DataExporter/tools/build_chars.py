# -*- coding: utf-8 -*-
# 2D 캐릭터 스프라이트 생성기 - 「개많이잡아」
#
# 핵심 규칙
# - 외곽선은 그리지 않는다. 색만 칠하고 실루엣을 팽창시켜 만든다 -> 두께가 항상 일정하다
# - 부위를 레이어로 나눠 각각 외곽선을 씌우고 겹친다 -> 팔/다리 분리선이 생겨 걷는게 읽힌다
# - 단순 도형(원/둥근사각)만 쓴다. 디테일을 넣지 않는게 이 톤에 맞다
# - 캔버스 64x64 좌표, 발끝 y=61. 걷기 4장 + 대기 1장
# - 원본은 64*OUT 해상도의 깨끗한 그림으로 낸다. 도트 룩은 게임에서 캐릭터 셰이더(Jinhyeong/PixelSprite)가
#   밉 한 단계를 한 칸으로 찍어 만든다 — 원본이 도트면 확대 배율마다 뭉개지고, UI(로비 초상화)에서도 계단이 보인다
# - 그래서 알파를 이진화하지 않는다. 테두리는 슈퍼샘플이 만든 안티에일리어싱을 그대로 둔다

from PIL import Image, ImageDraw, ImageFilter
import os, sys, json
import numpy as np
sys.stdout.reconfigure(encoding="utf-8")

DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\Sprites"
# 로비 초상화는 Resources 로 따로 낸다. UI(Image)는 Sprite 참조가 필요한데
# 어드레서블에 스프라이트를 따로 등록하는 것보다 이쪽이 훨씬 짧다.
LOBBY = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\Resources\Lobby"
SS = 4             # 슈퍼샘플
OUT = 4            # 출력 배율. 셰이더의 _PixelSize(도트 한 칸 = 텍셀 OUT 개)와 짝이다
S = SS * OUT
W = H = 64         # 좌표계
OUTLINE = (36, 26, 22, 255)

# [측정] 2px 로 두었더니 시바 스프라이트의 **픽셀 43%가 외곽선**이었다(649 / 전체).
# fin() 이 레이어마다 선을 씌우고 그걸 쌓기 때문에 겹치는 자리마다 선이 누적된다.
# 선이 그림을 잡아먹으면 형태가 뭉갠다 — 레퍼런스 도트는 선이 훨씬 얇고,
# 밝은 쪽은 아예 몸색의 어두운 톤으로 처리한다.
OUTLINE_PX = 1
EYE = (30, 22, 20, 255)
BLUSH = (255, 150, 150, 255)
NOSE = (58, 40, 36, 255)


def layer(w=W, h=H):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def ell(d, cx, cy, rx, ry, color):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=color)


def rrect(d, x0, y0, x1, y1, r, color):
    d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=color)


def tri(d, pts, color):
    d.polygon([(x * S, y * S) for x, y in pts], fill=color)


def shrink(src, w=W, h=H):
    """슈퍼샘플을 출력 해상도로 줄인다. 알파는 이진화하지 않는다."""
    return src.resize((w * OUT, h * OUT), Image.BOX)


def fin(src, w=W, h=H):
    # 다운샘플 -> 실루엣 팽창으로 균일 외곽선 합성. 두께는 64 기준 OUTLINE_PX
    small = shrink(src, w, h)
    size = (w * OUT, h * OUT)
    grown = small.split()[3].filter(ImageFilter.MaxFilter(OUTLINE_PX * OUT * 2 + 1))
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", size, OUTLINE), (0, 0), grown)
    out.alpha_composite(small)
    return out


def stack(layers, w=W, h=H):
    out = Image.new("RGBA", (w * OUT, h * OUT), (0, 0, 0, 0))
    for lay in layers:
        out.alpha_composite(fin(lay, w, h))
    return out


def shade(c, k):
    return (int(c[0] * k), int(c[1] * k), int(c[2] * k), 255)


# 빛은 위에서 온다. 몸의 위쪽을 밝히고 아래쪽을 낮춘다.
# 픽셀아트는 그라디언트가 아니라 **단(段)** 으로 나눠야 도트로 보인다.
LIT_BANDS = ((0, 26, 1.16), (26, 44, 1.0), (44, 64, 0.82))


def lit(img):
    """
    평면 채색에 명암 단을 넣는다.

    [측정] 기존 스프라이트는 실질 색이 2단계(몸통색 + 뒷면 0.76배)뿐이었다.
    레퍼런스 도트는 한 부위에 3~4단이 들어가 있어 입체가 산다.
    외곽선은 건드리지 않는다 — 선까지 밝히면 형태가 흐려진다.
    """
    arr = np.asarray(img).astype(np.float32)
    rgb = arr[..., :3]
    visible = arr[..., 3] > 0
    ink = (np.abs(rgb - np.array(OUTLINE[:3], np.float32)) < 10).all(-1)
    for y0, y1, k in LIT_BANDS:
        if k == 1.0:
            continue
        band = np.zeros(visible.shape, bool)
        band[y0 * OUT:y1 * OUT] = True
        m = band & visible & ~ink
        rgb[m] = np.minimum(255, np.floor(rgb[m] * k))
    arr[..., :3] = rgb
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def ground(rx=12.0, ry=3.6, y=60.0, a=110, w=W, h=H):
    """
    발밑 접지 그림자.

    [측정] 기존 스프라이트에는 이것이 **0픽셀**이었다. 탑다운에서 바닥에 붙은 그림자가 없으면
    캐릭터가 떠 보인다 — 레퍼런스 시트는 예외 없이 전원이 발밑에 어두운 타원을 깔고 있다.
    가장 싸고 효과가 큰 한 가지다.

    외곽선을 씌우지 않는다. 선이 들어가면 그림자가 아니라 접시로 보인다.
    """
    im, d = layer(w, h)
    cx = w / 2.0
    d.ellipse([(cx - rx) * S, (y - ry) * S, (cx + rx) * S, (y + ry) * S], fill=(22, 18, 30, a))
    return shrink(im, w, h)


DOGS = {
    "shiba": dict(body=(232, 150, 60, 255), belly=(255, 240, 220, 255),
                  ear="point", tail="curl", head_r=14),
    "corgi": dict(body=(219, 141, 66, 255), belly=(255, 255, 255, 255),
                  ear="biground", tail="stub", head_r=15),
    "greyhound": dict(body=(172, 172, 188, 255), belly=(238, 238, 246, 255),
                      ear="fold", tail="thin", head_r=13),
}


def face_paint(img, cx, hy, hr, belly, side=False):
    """
    얼굴은 외곽선 팽창 **뒤에** 찍는다 — 먼저 찍으면 선에 먹혀 표정이 사라진다.
    side 면 눈 하나 · 주둥이가 옆으로 나간다.
    """
    lay, d = layer()
    if side:
        ell(d, cx + hr - 3, hy + 2, 4.0, 3.2, belly)
        ell(d, cx + hr + 1, hy + 2, 2.6, 2.2, NOSE)
        ell(d, cx + 1, hy - 2, 2.8, 3.2, EYE)
        ell(d, cx - 4, hy + 4, 2.4, 1.7, BLUSH)
    else:
        ell(d, cx, hy + 3, 3.0, 2.4, NOSE)
        ell(d, cx - 6, hy - 2, 2.8, 3.2, EYE)
        ell(d, cx + 6, hy - 2, 2.8, 3.2, EYE)
        ell(d, cx - 11, hy + 4, 2.6, 1.8, BLUSH)
        ell(d, cx + 11, hy + 4, 2.6, 1.8, BLUSH)

    img.alpha_composite(shrink(lay))

    # 눈에 점 하나. 이것만으로 살아 있는 눈이 된다 (64 좌표 1칸 크기)
    p = img.load()
    d = ImageDraw.Draw(img)
    yy = int(round(hy)) - 3
    spots = [(int(cx) + 2, yy)] if side else [(int(cx) - 7, yy), (int(cx) + 5, yy)]
    for ex, ey in spots:
        if 0 <= ey < H and 0 <= ex < W and p[int((ex + 0.5) * OUT), int((ey + 0.5) * OUT)][3] > 0:
            d.ellipse([ex * OUT, ey * OUT, (ex + 1) * OUT - 1, (ey + 1) * OUT - 1], fill=(255, 255, 255, 255))
    return img


def ears(d, cx, hy, hr, kind, col):
    """귀. 머리보다 먼저 그려 같은 레이어에 두면 귀와 머리 사이에 선이 안 생긴다."""
    if kind == "point":
        tri(d, [(cx - hr + 4, hy - 4), (cx - hr + 1, hy - 15), (cx - 3, hy - 7)], col)
        tri(d, [(cx + hr - 4, hy - 4), (cx + hr - 1, hy - 15), (cx + 3, hy - 7)], col)
    elif kind == "biground":
        ell(d, cx - hr + 1, hy - 8, 6.0, 8.0, col)
        ell(d, cx + hr - 1, hy - 8, 6.0, 8.0, col)
    else:
        ell(d, cx - hr + 1, hy - 3, 5.0, 6.5, col)
        ell(d, cx + hr - 1, hy - 3, 5.0, 6.5, col)


def leg(d, x, y0, y1, w, col, paw=True):
    """다리 한 짝. 끝에 발을 붙여야 「딛고 있다」가 읽힌다."""
    rrect(d, x - w, y0, x + w, y1, w * 0.8, col)
    if paw:
        ell(d, x, y1, w + 1.2, w * 0.8, col)


# ── 몸 비율 ────────────────────────────────────────────────────────────────
# [측정] 예전 실루엣은 머리 폭 35 · 몸통 30 · 다리 20 이었다. 목이 없어 머리와 몸이 한 덩어리로
# 붙고, 허리 구간(y34~44)이 계속 폭 30 이라 가슴과 엉덩이 구분이 없었다.
# 게다가 다리가 각진 기둥이고 **발이 없어** 「서 있다」가 안 읽혔다.
#
# 살아 있는 몸은 위에서 아래로 넓이가 오르내린다 — 머리(넓다) · 목(좁다) · 가슴 · 허리(좁다) ·
# 엉덩이(넓다) · 다리(좁다) · 발(넓다). 그 굴곡이 실루엣을 몸으로 만든다.
CHEST_Y = 35.0
HIP_Y = 45.0
LEG_TOP = 45.0
LEG_BOT = 56.0
LEG_X = 6.5        # 중심에서 다리까지. 사이 홈이 5px 는 돼야 두 다리로 보인다
LEG_W = 3.8
FOOT_Y = 57.0


def paws(d, x, y, col, forward=1):
    """발. 앞으로 살짝 나와야 「딛고 있다」가 읽힌다. 없으면 다리가 잘린 기둥으로 보인다."""
    ell(d, x + 1.2 * forward, y, 5.0, 2.8, col)


def draw_dog(cfg, phase, bob, face='down'):
    """
    정면. 두 발로 서서 걷는 슈퍼독.

    [사고] 팔을 몸통 옆에 둥근사각으로 붙였더니 팔이 아니라 **각진 돌기**로 보였다.
    64px 안에서 팔은 3~4px 밖에 못 쓰는데, 그 크기에서는 어떤 모양을 그려도 혹으로 읽힌다.
    빼는 편이 낫다 — 없어도 아무도 팔을 찾지 않는다.

    대신 몸통과 **가까운 쪽 다리를 같은 레이어**에 둔다. 레이어마다 외곽선이 씌워지므로
    나눠 두면 몸과 다리 사이에 선이 생겨 「붙여 놓은 것」으로 보인다.
    """
    if face == 'up':
        return draw_dog_up(cfg, phase, bob)
    if face == 'side':
        return draw_dog_side(cfg, phase, bob)

    c = cfg["body"]
    belly = cfg["belly"]
    back_c = shade(c, 0.76)
    hr = cfg["head_r"]

    chest_y = CHEST_Y + bob
    hip_y = HIP_Y + bob
    lift = 3.0 * phase

    # 먼 쪽 다리 + 꼬리 (어둡다). 발까지 그려야 기둥이 아니라 다리가 된다
    back, d = layer()
    lx = 32 - LEG_X - 1.0 * phase
    rrect(d, lx - LEG_W, LEG_TOP + bob, lx + LEG_W, LEG_BOT - max(0.0, lift), LEG_W, back_c)
    paws(d, lx, FOOT_Y - max(0.0, lift), back_c, -1)

    t = cfg["tail"]
    if t == "curl":
        ell(d, 32 + 13, hip_y - 4, 5.5, 5.5, back_c)
    elif t == "stub":
        ell(d, 32 + 11, hip_y, 4.0, 4.0, back_c)
    else:
        for dx, dy, r in ((10, -4, 3.0), (14, -1, 2.5), (18, 3, 2.0)):
            ell(d, 32 + dx, hip_y + dy, r, r, back_c)

    # 몸통 + 가까운 쪽 다리 = 한 덩어리. 가슴은 좁고 엉덩이는 넓다
    body, d = layer()
    ell(d, 32, chest_y, 8.5, 7.5, c)
    ell(d, 32, hip_y, 10.5, 7.0, c)
    rx = 32 + LEG_X + 1.0 * phase
    rrect(d, rx - LEG_W, LEG_TOP + bob, rx + LEG_W, LEG_BOT - max(0.0, -lift), LEG_W, c)
    paws(d, rx, FOOT_Y - max(0.0, -lift), c, 1)
    ell(d, 32, chest_y + 4, 6.0, 6.5, belly)

    # 머리. 목이 보이도록 몸통 위로 충분히 올린다
    hy = chest_y - hr - 1
    head, d = layer()
    ears(d, 32, hy, hr, cfg["ear"], c)
    ell(d, 32, hy, hr, hr - 1, c)
    ell(d, 32, hy + 6, 9.5, 6.5, belly)

    img = stack([back, body, head])
    return face_paint(img, 32, hy, hr, belly)


def draw_dog_up(cfg, phase, bob):
    """뒤통수. 얼굴이 없고 꼬리가 가운데로 온다 — 이 둘만으로 「등을 보이고 있다」가 읽힌다."""
    c = cfg["body"]
    back_c = shade(c, 0.76)
    hr = cfg["head_r"]

    chest_y = CHEST_Y + bob
    hip_y = HIP_Y + bob
    lift = 3.0 * phase

    back, d = layer()
    lx = 32 - LEG_X - 1.0 * phase
    rrect(d, lx - LEG_W, LEG_TOP + bob, lx + LEG_W, LEG_BOT - max(0.0, lift), LEG_W, back_c)
    paws(d, lx, FOOT_Y - max(0.0, lift), back_c, -1)

    body, d = layer()
    ell(d, 32, chest_y, 8.5, 7.5, c)
    ell(d, 32, hip_y, 10.5, 7.0, c)
    rx = 32 + LEG_X + 1.0 * phase
    rrect(d, rx - LEG_W, LEG_TOP + bob, rx + LEG_W, LEG_BOT - max(0.0, -lift), LEG_W, c)
    paws(d, rx, FOOT_Y - max(0.0, -lift), c, 1)

    # 꼬리가 등 한가운데. 뒷모습에선 이게 정체성이다
    tail, d = layer()
    t = cfg["tail"]
    if t == "curl":
        ell(d, 32, hip_y - 5, 5.5, 6.0, back_c)
    elif t == "stub":
        ell(d, 32, hip_y - 3, 4.5, 4.0, back_c)
    else:
        rrect(d, 32 - 2.5, hip_y - 6, 32 + 2.5, hip_y + 6, 2, back_c)
        ell(d, 32, hip_y + 6, 2.5, 2.5, back_c)

    hy = chest_y - hr - 1
    head, d = layer()
    ears(d, 32, hy, hr, cfg["ear"], c)
    ell(d, 32, hy, hr, hr - 1, back_c)

    return stack([back, body, tail, head])


def draw_dog_side(cfg, phase, bob):
    """옆모습. 주둥이가 오른쪽으로 튀어나오고 눈·귀가 하나씩만 보인다.
    좌우 반전으로 왼쪽 방향까지 함께 쓴다."""
    c = cfg["body"]
    belly = cfg["belly"]
    back_c = shade(c, 0.76)
    hr = cfg["head_r"]

    chest_y = CHEST_Y + bob
    hip_y = HIP_Y + bob
    stride = 5.0 * phase

    # 옆모습은 앞뒤로 벌어진다. 먼 쪽 다리는 뒤로
    back, d = layer()
    lx = 32 - 3 - stride
    rrect(d, lx - LEG_W, LEG_TOP + bob, lx + LEG_W, LEG_BOT, LEG_W, back_c)
    paws(d, lx, FOOT_Y, back_c, -1)

    tail, d = layer()
    t = cfg["tail"]
    if t == "curl":
        ell(d, 32 - 12, hip_y - 5, 5.5, 5.5, back_c)
    elif t == "stub":
        ell(d, 32 - 11, hip_y - 2, 4.0, 4.0, back_c)
    else:
        for dx, dy, r in ((-10, -3, 3.0), (-14, 0, 2.5), (-18, 4, 2.0)):
            ell(d, 32 + dx, hip_y + dy, r, r, back_c)

    body, d = layer()
    ell(d, 32 + 1, chest_y, 7.5, 7.5, c)
    ell(d, 32 - 1, hip_y, 9.0, 7.0, c)
    rx = 32 + 3 + stride
    rrect(d, rx - LEG_W, LEG_TOP + bob, rx + LEG_W, LEG_BOT, LEG_W, c)
    paws(d, rx, FOOT_Y, c, 1)
    ell(d, 32 + 4, chest_y + 4, 4.5, 5.5, belly)

    hx = 32 + 3
    hy = chest_y - hr - 1
    head, d = layer()
    if cfg["ear"] == "point":
        tri(d, [(hx - hr + 4, hy - 5), (hx - hr + 2, hy - 16), (hx - 2, hy - 8)], c)
    elif cfg["ear"] == "biground":
        ell(d, hx - hr + 2, hy - 9, 6.0, 8.0, c)
    else:
        ell(d, hx - hr + 2, hy - 3, 5.0, 6.5, c)
    ell(d, hx, hy, hr, hr - 1, c)
    ell(d, hx + hr - 3, hy + 3, 5.5, 4.5, c)

    img = stack([back, tail, body, head])
    return face_paint(img, hx, hy, hr, belly, side=True)


ALIENS = {
    "grunt":   dict(c=(107, 212, 107, 255), rx=12.0, ry=12.5, eyes=1, ant=True,  legs=True),
    "brute":   dict(c=(154, 107, 212, 255), rx=17.0, ry=15.5, eyes=3, ant=False, legs=True),
    "runner":  dict(c=(64, 190, 172, 255),  rx=10.0, ry=14.5, eyes=2, ant=True,  legs=True),
    "spitter": dict(c=(198, 180, 68, 255),  rx=13.0, ry=11.0, eyes=2, ant=False, legs=True),
    "swarm":   dict(c=(214, 98, 155, 255),  rx=9.0,  ry=9.5,  eyes=1, ant=True,  legs=False),
}


def draw_alien(cfg, phase, face='down', bob=0):
    """외계인. 몸이 좌우 대칭이라 **눈 위치가 곧 방향**이다.
      down — 눈이 정면에
      up   — 눈이 없다(뒤통수)
      side — 눈이 한쪽으로 몰린다
    플레이어만 방향을 보고 적은 안 보면, 물량이 많아 그 어색함이 훨씬 크게 눈에 띈다."""
    c = cfg["c"]
    back_c = shade(c, 0.76)
    rx, ry = cfg["rx"], cfg["ry"]
    # [사고] 적에게는 상하 이동이 **아예 없었다.** 화면을 채우는 것이 적이라
    # 물량이 많을수록 그 어색함이 크게 보인다. 다리가 없는 적은 떠 있는 것이므로
    # 오히려 더 흔들려야 「둥둥 떠 온다」로 읽힌다.
    cy = 61 - ry - (5 if cfg["legs"] else 2) + bob
    sq = 1 + 0.08 * phase
    bx, by = rx / sq, ry * sq

    back, d = layer()
    if cfg["legs"]:
        lx = 32 - rx * 0.5 - 3 * phase
        rrect(d, lx - 2.5, cy + by - 3, lx + 2.5, 61, 2, back_c)

    body, d = layer()
    if cfg["ant"]:
        d.line([(32 * S, int((cy - by) * S)), (32 * S, int((cy - by - 7) * S))], fill=c, width=2 * S)
        ell(d, 32, cy - by - 8, 2.5, 2.5, c)
    ell(d, 32, cy, bx, by, c)

    front, d = layer()
    if cfg["legs"]:
        rxp = 32 + rx * 0.5 + 3 * phase
        rrect(d, rxp - 2.5, cy + by - 3, rxp + 2.5, 61, 2, c)

    img = stack([back, body, front])

    # 뒤통수는 눈이 없다. 그것만으로 「등을 보이고 있다」가 읽힌다.
    if face == 'up':
        return img

    face_lay, d = layer()
    n = cfg["eyes"]
    xs = [0] if n == 1 else ([-5, 5] if n == 2 else [-7, 0, 7])
    if face == 'side':
        # 옆을 보면 눈이 한쪽으로 몰리고, 뒤쪽 눈은 몸에 가려 하나만 남는다.
        xs = [rx * 0.45] if n == 1 else [rx * 0.3, rx * 0.62][:max(1, n - 1)]
    for x in xs:
        er = 4.2 if n == 1 else 3.2
        ell(d, 32 + x, cy - by * 0.15, er, er, (255, 255, 255, 255))
        ell(d, 32 + x, cy - by * 0.15, er * 0.52, er * 0.58, EYE)
    img.alpha_composite(shrink(face_lay))
    return img


def draw_kibble():
    L, d = layer(32, 32)
    b = (245, 235, 210, 255)
    rrect(d, 8, 14, 24, 18, 2, b)
    for cx in (9, 23):
        ell(d, cx, 14.5, 3.5, 3.5, b)
        ell(d, cx, 17.5, 3.5, 3.5, b)
    return stack([L], 32, 32)


def draw_box():
    back, d = layer()
    rrect(d, 16, 26, 48, 52, 3, (198, 146, 92, 255))
    lid, d = layer()
    tape = (232, 206, 160, 255)
    d.rectangle([28 * S, 26 * S, 36 * S, 52 * S], fill=tape)
    d.rectangle([16 * S, 33 * S, 48 * S, 38 * S], fill=tape)
    img = fin(back)
    img.alpha_composite(shrink(lid))
    return img


def draw_bowl():
    L, d = layer()
    ell(d, 32, 40, 16, 6, (150, 106, 66, 255))
    rrect(d, 16, 40, 48, 52, 5, (226, 84, 74, 255))
    ell(d, 32, 52, 17, 4, (226, 84, 74, 255))
    return stack([L])


def draw_ufo(phase=0):
    """납치선. phase 0~3 으로 위아래로 떠 있고 불빛이 돌아간다.

    다른 적들과 달리 걷지 않으므로 걷기 대신 **호버**를 넣는다.
    가만히 있는 그림이면 보스가 아니라 배경 오브젝트로 읽힌다.
    """
    bob = (0, -2, 0, 2)[phase % 4]

    dome, d = layer(96, 96)
    ell(d, 48, 40 + bob, 20, 16, (140, 220, 235, 255))
    hull, d = layer(96, 96)
    ell(d, 48, 52 + bob, 40, 13, (150, 158, 178, 255))
    img = stack([dome, hull], 96, 96)
    lights, d = layer(96, 96)
    for i, x in enumerate((-26, -13, 0, 13, 26)):
        # 한 칸씩 꺼져 도는 불빛. 전부 켜져 있으면 움직임이 안 읽힌다
        on = (i != phase % 5)
        c = (255, 226, 120, 255) if on else (150, 130, 70, 255)
        ell(d, 48 + x, 55 + bob, 4, 4, c)
    img.alpha_composite(shrink(lights, 96, 96))
    return img


# (다리 위상, 몸통 상하)
#
# [사고] 예전 값은 [(-1,0), (0,-1), (1,0), (0,-1)] 이었다. 두 가지가 잘못됐다 —
#   1) 상하 이동이 **1픽셀**이라 사실상 안 보였다. 픽셀 걷기에서 몸통이 위아래로 뛰는 것은
#      장식이 아니라 걷는다는 **주된 단서**다. 그게 없으면 「미끄러지며 다리만 떠는」 것으로 보인다.
#   2) 1번과 3번 프레임이 **완전히 같았다.** 4장을 그렸는데 실제 포즈는 셋뿐이었다.
#
# 접지(0·2)에서 몸이 내려가고 통과(1·3)에서 떠오른다. 통과 두 장은 기우는 쪽을 달리해 구분한다.
WALK = [(-1.0, 1), (-0.35, -2), (1.0, 1), (0.35, -2)]


# 그림자 크기는 종류마다 다르다. 뜬 것(UFO)은 넓고 옅게, 걷는 것은 좁고 진하게.
# [사고] 처음엔 rx 11 · 알파 110 으로 뒀는데 화면에서 거의 안 보였다.
# 레퍼런스 시트의 그림자는 발보다 **넓고 진하다** — 캐릭터를 바닥에 못 박는 역할이라
# 은은하면 아무 일도 안 한 것과 같다.
# UFO 는 96 캔버스다. 그림자는 캔버스 바닥(발밑 피벗 3/64 와 같은 비율)에 둬서 떠 있는 틈이 보이게 한다.
GROUND_BY_PREFIX = (
    ("boss_ufo", (18.0, 5.4, 90.0, 120)),
    ("alien_ufo", (18.0, 5.4, 90.0, 120)),
    ("alien_", (11.0, 3.8, 60.5, 155)),
    ("dog_", (13.0, 4.3, 60.5, 160)),
)


def ground_for(name):
    for prefix, args in GROUND_BY_PREFIX:
        if name.startswith(prefix):
            return args
    return None


def save(img, name):
    """
    캐릭터에는 명암 단과 접지 그림자를 붙여 낸다.

    호출부마다 붙이게 두면 잊는다 — 실제로 방향별 그리기 함수가 다섯 군데라
    한 곳만 빠져도 그 방향만 떠 보인다. 이름으로 한 번에 정한다.
    """
    os.makedirs(DST, exist_ok=True)
    g = ground_for(name)
    if g is not None:
        img = lit(img)
        # 바탕을 그림 크기에 맞춘다 — 64 바탕에 96 그림을 얹으면 오른쪽 · 아래가 잘린다
        units = img.size[0] // OUT
        base = ground(*g, w=units, h=units)
        base.alpha_composite(img)
        img = base
    img.save(os.path.join(DST, name + ".png"))


def save_lobby(img, name):
    os.makedirs(LOBBY, exist_ok=True)
    img.save(os.path.join(LOBBY, name + ".png"))


def main():
    n = 0
    # 이 생성기는 더 굽지 않는다 (10-04). 주인공 = build_heroes.py · 적/보스 = build_enemies.py · 먹는 아이템 = build_pickups.py
    # 돌리면 새 그림을 옛 그림으로 덮으므로 배율 표만 쓴다
    write_scale()
    print("스프라이트 %d장" % n)


def write_scale():
    """build_meta.py 가 PPU · 밉맵을 정하는 표. 이 생성기가 쓰는 폴더만 덮는다."""
    path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "_art_scale.json")
    table = {}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as fp:
            table = json.load(fp)
    table["Assets/_Art/Sprites"] = OUT
    table["Assets/Resources/Lobby"] = OUT
    with open(path, "w", encoding="utf-8") as fp:
        json.dump(table, fp, ensure_ascii=False, indent=1, sort_keys=True)


main()
