# -*- coding: utf-8 -*-
# 먹는 아이템 생성기 - 「개많이잡아」 (.claude/requests.md 5 · 6번)
#
# - 보물상자 → 스피커 (pickup_box 이름 그대로 — PartDropper 키 · 프리팹 변화 없음)
# - 경험치 사료 (pickup_kibble) — 같은 그림체로 다시
# - 아이템마다 <이름>_glow.png: 물체 실루엣을 그대로 키워 번지게 한 흰 테두리. 가산 머티리얼로 몸 뒤에 깔려
#   «모양 그대로 테두리가 빛난다». 색은 런타임이 등급으로 틴트한다 (PartPickup)
# 그림 규칙은 주인공 · 적과 같다: 진보라 외곽선 · 평면 + 그림자 1단 · 64 좌표 × OUT 4

from PIL import Image, ImageDraw, ImageFilter
import os, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
DST = os.path.join(ROOT, r"Assets\_Art\Sprites")
PREVIEW = os.path.join(ROOT, r"_Ref\ArtReboot\lineup_pickups.png")
SS = 4
OUT = 4
S = SS * OUT
W = 64
INK = (64, 30, 104, 255)
GLOW_GROW = 3.0    # 테두리 두께 (64 좌표)
GLOW_BLUR = 1.4


def layer():
    im = Image.new("RGBA", (W * S, W * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def ell(d, x, y, rx, ry, c):
    d.ellipse([(x - rx) * S, (y - ry) * S, (x + rx) * S, (y + ry) * S], fill=c)


def rrect(d, x0, y0, x1, y1, r, c):
    d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=r * S, fill=c)


def shrink(im):
    return im.resize((W * OUT, W * OUT), Image.BOX)


def fin(lay, px=1.8):
    small = shrink(lay)
    k = int(round(px * OUT)) * 2 + 1
    grown = small.split()[3].filter(ImageFilter.MaxFilter(k))
    out = Image.new("RGBA", small.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", small.size, INK), (0, 0), grown)
    out.alpha_composite(small)
    return out


def speaker():
    """스피커 — 진보라 통 · 우퍼 · 트위터 · 위에 음표 하나."""
    body, d = layer()
    rrect(d, 18, 16, 46, 58, 4, (90, 60, 150, 255))
    rrect(d, 40, 18, 46, 56, 2, (70, 44, 120, 255))
    cones, d = layer()
    ell(d, 31, 42, 9.5, 9.5, (255, 90, 180, 255))
    ell(d, 31, 42, 5.5, 5.5, (60, 30, 90, 255))
    ell(d, 31, 42, 2.4, 2.4, (255, 200, 230, 255))
    ell(d, 31, 24, 4.4, 4.4, (60, 220, 240, 255))
    ell(d, 31, 24, 1.8, 1.8, (220, 255, 255, 255))
    note, d = layer()
    ell(d, 47, 13, 3.4, 2.4, (255, 236, 90, 255))
    rrect(d, 49.4, 4, 50.8, 13, 0.6, (255, 236, 90, 255))
    d.polygon([(50 * S, 4 * S), (56 * S, 8 * S), (55 * S, 11 * S), (50 * S, 8 * S)], fill=(255, 236, 90, 255))
    img = fin(body)
    det = shrink(cones)
    img.alpha_composite(det)
    img.alpha_composite(fin(note, 1.4))
    return img


KIBBLE_SCALE = 0.55   # 10-04 사용자: 개뼈가 너무 크다 — 캔버스는 그대로 두고 그림만 줄인다 (판정 반경과 무관)


def kibble():
    """경험치 사료 — 뼈다귀 모양 알갱이. 작아도 읽히게 외곽선은 굵게 유지한다."""
    k, d = layer()
    c = (226, 150, 82, 255)
    z = KIBBLE_SCALE
    X = lambda v: 32 + (v - 32) * z
    rrect(d, X(22), X(28), X(42), X(36), 3 * z, c)
    for x in (22, 42):
        for y in (27.5, 36.5):
            ell(d, X(x), X(y), 4.6 * z, 4.6 * z, c)
    hl, d = layer()
    ell(d, X(27), X(29.5), 3 * z, 1.4 * z, (255, 210, 150, 255))
    img = fin(k, 1.6)
    img.alpha_composite(shrink(hl))
    return img


def glow_of(img, grow=GLOW_GROW):
    a = img.split()[3]
    k = int(grow * OUT) * 2 + 1
    a = a.filter(ImageFilter.MaxFilter(k)).filter(ImageFilter.GaussianBlur(GLOW_BLUR * OUT))
    a = a.point(lambda v: min(255, int(v * 1.6)))
    g = Image.new("RGBA", img.size, (255, 255, 255, 0))
    g.putalpha(a)
    return g


def main():
    items = {"pickup_box": speaker(), "pickup_kibble": kibble()}
    T = W * OUT
    sheet = Image.new("RGBA", (T * 2, T), (50, 30, 80, 255))
    for i, (name, img) in enumerate(items.items()):
        g = glow_of(img, GLOW_GROW if name != "pickup_kibble" else 2.0)
        img.save(os.path.join(DST, name + ".png"))
        g.save(os.path.join(DST, name + "_glow.png"))
        tint = Image.new("RGBA", g.size, (255, 230, 90, 255) if i == 0 else (255, 120, 200, 255))
        tint.putalpha(g.split()[3])
        sheet.alpha_composite(tint, (i * T, 0))
        sheet.alpha_composite(img, (i * T, 0))
    sheet.save(PREVIEW)
    print("먹는 아이템 %d종 + 테두리 빛 · 미리보기 %s" % (len(items), PREVIEW))


main()
