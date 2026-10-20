# -*- coding: utf-8 -*-
# UI 스킨 생성기 v2 - SlayerDog 도트 (TASKS A-8)
#
# 핵심 규칙
# - 도트: 1/4 해상도에서 계단식으로 그리고 최근접 ×4. 캐릭터 · 맵과 같은 도트 밀도
# - 좌상 · 우하 모서리를 깎은 사선 컷 패널. 명암은 흰색 계조(위 · 왼쪽 밝게, 아래 · 오른쪽 어둡게) — 색을 곱해도 입체가 남는다
# - 전부 흰색으로 그린다. 색은 UIBuild 팔레트가 Image.color 로 곱한다 (한 장으로 모든 패널)
# - 크기와 9슬라이스 경계는 기존 파일과 같다 — .meta 의 spriteBorder 를 손으로 맞춰 둔 파일이라
#   build_meta.py 대상이 아니고, 크기가 같으면 경계를 다시 잡을 필요가 없다. 깎는 폭은 경계 안에 둔다
# - 이름도 기존 그대로 낸다 (UISkin 상수)

from PIL import Image, ImageDraw
import os, sys, re, hashlib
sys.stdout.reconfigure(encoding="utf-8")

DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\Resources\UI"
WHITE = (255, 255, 255, 255)

# 이름: (크기, 9슬라이스 경계, 깎는 폭, 테두리 두께 또는 0=채움, 모양)
SKIN = {
    "ui_rectangle_small_01":       (128, 15, 12, 0, "cut"),
    "ui_rectangle_frame_small_01": (128, 15, 12, 4, "cut"),
    "ui_rectangle_big_02":         (256, 30, 26, 0, "cut"),
    "ui_rectangle_frame_big_02":   (256, 31, 26, 6, "cut"),
    "ui_slider_bg_small_01":       (128, 7, 6, 0, "cut"),
    "ui_circle_bg_small_01":       (128, 61, 0, 0, "circle"),
    "ui_circle_frame_small_01":    (128, 62, 0, 7, "circle"),
}


UP = 4
LIGHT = (255, 255, 255, 255)
BODY = (226, 226, 230, 255)
SHADE = (178, 178, 188, 255)
PREVIEW = r"C:\jinhyeong_project\Unity_SkillSystem_Module\_DataExporter\preview\uiskin"


def cut_poly(n, c, inset=0):
    """좌상 · 우하를 c 만큼 깎은 육각형 (도트 좌표)."""
    i = inset
    k = max(0, c - i)
    return [(i + k, i), (n - 1 - i, i), (n - 1 - i, n - 1 - i - k), (n - 1 - i - k, n - 1 - i), (i, n - 1 - i), (i, i + k)]


def shade(mask_im):
    """도트 마스크 → 흰 계조. 위 · 왼쪽 1도트 빛, 아래 · 오른쪽 1도트 그늘."""
    w, h = mask_im.size
    m = mask_im.load()
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    o = out.load()

    def on(x, y):
        return 0 <= x < w and 0 <= y < h and m[x, y] > 127

    for y in range(h):
        for x in range(w):
            if not on(x, y):
                continue
            if not on(x + 1, y) or not on(x, y + 1):
                o[x, y] = SHADE
            elif not on(x - 1, y) or not on(x, y - 1):
                o[x, y] = LIGHT
            else:
                o[x, y] = BODY
    return out


def draw(size, cut, stroke, shape):
    n = size // UP
    c = max(1, cut // UP)
    s = max(1, stroke // UP) if stroke else 0
    mask = Image.new("L", (n, n), 0)
    d = ImageDraw.Draw(mask)
    if shape == "circle":
        d.ellipse([0, 0, n - 1, n - 1], fill=255)
        if s:
            d.ellipse([s, s, n - 1 - s, n - 1 - s], fill=0)
    else:
        d.polygon(cut_poly(n, c), fill=255)
        if s:
            d.polygon(cut_poly(n, c, s), fill=0)
    return shade(mask).resize((size, size), Image.NEAREST)


def draw_heart(size=128):
    """HP 바 머리의 하트 — 도트. 흰 계조, 색은 런타임이 곱한다."""
    n = size // UP
    mask = Image.new("L", (n, n), 0)
    d = ImageDraw.Draw(mask)
    r = n * 0.26
    for cx in (n * 0.30, n * 0.70):
        d.ellipse([cx - r, n * 0.36 - r, cx + r, n * 0.36 + r], fill=255)
    d.polygon([(n * 0.06, n * 0.44), (n * 0.94, n * 0.44), (n * 0.5, n * 0.92)], fill=255)
    return shade(mask).resize((size, size), Image.NEAREST)


def ensure_meta(name, template="ui_circle_bg_small_01"):
    """새 스킨 파일의 .meta 는 기존 스킨 meta 를 본떠 경계 0 · 경로 md5 GUID 로 한 번만 쓴다 (이후 손으로 고친 값을 덮지 않는다)."""
    path = os.path.join(DST, name + ".png.meta")
    if os.path.exists(path):
        return
    src = open(os.path.join(DST, template + ".png.meta"), encoding="utf-8").read()
    guid = hashlib.md5(("Assets/Resources/UI/" + name + ".png").encode("utf-8")).hexdigest()
    src = re.sub(r"guid: [0-9a-f]{32}", "guid: " + guid, src, count=1)
    src = src.replace(template, name)
    src = re.sub(r"spriteBorder: \{[^}]*\}", "spriteBorder: {x: 0, y: 0, z: 0, w: 0}", src)
    open(path, "w", encoding="utf-8", newline="\n").write(src)


def main():
    if "--preview" in sys.argv:
        os.makedirs(PREVIEW, exist_ok=True)
        draw_heart().save(os.path.join(PREVIEW, "ui_heart.png"))
        for name, (size, border, cut, stroke, shape) in SKIN.items():
            draw(size, cut, stroke, shape).save(os.path.join(PREVIEW, name + ".png"))
        print("UI 스킨 미리보기 %s" % PREVIEW)
        return
    draw_heart().save(os.path.join(DST, "ui_heart.png"))
    ensure_meta("ui_heart")
    for name, (size, border, cut, stroke, shape) in SKIN.items():
        path = os.path.join(DST, name + ".png")
        if shape == "cut":
            assert cut < border, "%s: 깎는 폭 %d 이 9슬라이스 경계 %d 를 넘는다" % (name, cut, border)
        old = Image.open(path).size if os.path.exists(path) else None
        assert old in (None, (size, size)), "%s: 크기가 바뀐다 %s → %d — .meta 경계를 다시 잡아야 한다" % (name, old, size)
        draw(size, cut, stroke, shape).save(path)
    print("UI 스킨 %d장" % len(SKIN))


main()
