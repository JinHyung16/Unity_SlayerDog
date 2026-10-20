# -*- coding: utf-8 -*-
# 스팀 스토어 · 라이브러리 이미지 생성기 — 게임과 같은 도트 그림(캐릭터 · 보스 · 맵 바닥 · 소품)과 도스고딕 로고를 합성한다
#
# - 모든 그림은 도트 격자 그대로 정수배 확대 (최근접). 도트 크기 n = 이미지마다 정한다
# - 로고: 도스고딕 16px 비트맵(안티앨리어스 없음) → 금색 3단 · 1도트 외곽선 · 그림자 → 정수배
# - 결과: Docs/steam/*.png (스팀 파트너 페이지에 올리는 규격). 라이브러리 로고만 투명 배경
#
# 사용: python build_promo.py   (build_heroes · build_bosses · build_stage 를 먼저 돌린 상태)

from PIL import Image, ImageDraw, ImageFont
import os, sys, random
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
SPR = os.path.join(ROOT, "Assets", "_Art", "Sprites")
STAGE = os.path.join(ROOT, "Assets", "_Art", "Stage")
FONT = os.path.join(ROOT, "Assets", "_Art", "Fonts", "DOSGothic", "DOSGothic.ttf")
OUT = os.path.join(ROOT, "Docs", "steam")
UP = 4  # 생성기 출력 배율 (64 도트 = 256px)

HEROES = ["shiba", "jindo", "akita", "chow", "pug", "shepherd", "poodle", "husky", "beagle", "dachshund"]
MAP = "snow_sakura"

INK = (24, 16, 30, 255)
GOLD = [(255, 236, 140), (255, 200, 70), (226, 128, 40)]


def dots(path):
    """생성기 출력(×4)을 도트 격자로 되돌린다."""
    im = Image.open(path).convert("RGBA")
    return im.resize((im.width // UP, im.height // UP), Image.NEAREST)


def up(im, n):
    return im.resize((im.width * n, im.height * n), Image.NEAREST)


def floor(w, h, rnd):
    """맵 바닥 타일 8장을 섞어 깐다 (도트 공간)."""
    tiles = [dots(os.path.join(STAGE, MAP, "tile_stage_%d.png" % i)) for i in range(1, 9)
             if os.path.exists(os.path.join(STAGE, MAP, "tile_stage_%d.png" % i))]
    im = Image.new("RGBA", (w, h))
    for y in range(0, h, 64):
        for x in range(0, w, 64):
            im.alpha_composite(tiles[rnd.randrange(len(tiles))], (x, y))
    return im


def props(rnd):
    d = os.path.join(STAGE, MAP)
    return [dots(os.path.join(d, f)) for f in sorted(os.listdir(d)) if f.startswith("deco_") and f.endswith(".png")]


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def vignette(im, strength=0.6):
    """도트 디더 비네트 — 타원 거리로 어둡기를 정하고 4×4 베이어 디더로 단계를 섞는다 (부드러운 그라데이션 대신 도트)."""
    w, h = im.size
    px = im.load()
    for y in range(h):
        for x in range(w):
            dx = (x - w / 2) / (w / 2)
            dy = (y - h / 2) / (h / 2)
            d = (dx * dx + dy * dy) ** 0.5
            t = (d - 0.6) / 0.6
            if t <= 0:
                continue
            t = min(1.0, t) * 4 + BAYER[y % 4][x % 4] / 16.0
            k = 1 - strength * int(t) / 4
            r, g, b, a = px[x, y]
            px[x, y] = (int(r * k), int(g * k), int(b * k), a)
    return im


def banner(w, h):
    """부제 띠 — 어두운 반투명 띠, 위아래 1도트 금선."""
    im = Image.new("RGBA", (w, h), (20, 14, 28, 190))
    d = ImageDraw.Draw(im)
    d.line([(0, 0), (w, 0)], fill=GOLD[2] + (255,))
    d.line([(0, h - 1), (w, h - 1)], fill=GOLD[2] + (255,))
    return im


def logo(scale_note=""):
    """'SlayerDog' 도트 로고 (도트 공간). 금색 3단 + 1도트 외곽선 + 아래 2도트 그림자."""
    font = ImageFont.truetype(FONT, 16)
    text = "SlayerDog"
    tmp = Image.new("L", (200, 24), 0)
    d = ImageDraw.Draw(tmp)
    d.fontmode = "1"
    d.text((2, 2), text, font=font, fill=255)
    bb = tmp.getbbox()
    mask = tmp.crop((bb[0], bb[1], bb[2], bb[3]))
    w, h = mask.size
    # 굵게: 오른쪽으로 1도트 겹쳐 획을 2도트로 (도트 로고의 무게)
    bold = Image.new("L", (w + 1, h), 0)
    bold.paste(mask, (0, 0), mask)
    bold.paste(mask, (1, 0), mask)
    w += 1
    out = Image.new("RGBA", (w + 4, h + 6), (0, 0, 0, 0))
    m = bold.load()
    o = out.load()
    # 그림자 (아래 2도트)
    for y in range(h):
        for x in range(w):
            if m[x, y]:
                for sy in (2, 3):
                    o[x + 2, y + 2 + sy] = (60, 20, 30, 255)
    # 외곽선
    for y in range(h):
        for x in range(w):
            if m[x, y]:
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        o[x + 2 + dx, y + 2 + dy] = INK
    # 금색 3단 (위 밝게)
    for y in range(h):
        band = GOLD[0] if y < h * 0.35 else (GOLD[1] if y < h * 0.7 else GOLD[2])
        for x in range(w):
            if m[x, y]:
                o[x + 2, y + 2] = band + (255,)
    return out


def subtitle(text):
    font = ImageFont.truetype(FONT, 16)
    tmp = Image.new("L", (400, 24), 0)
    d = ImageDraw.Draw(tmp)
    d.fontmode = "1"
    d.text((2, 2), text, font=font, fill=255)
    bb = tmp.getbbox()
    mask = tmp.crop(bb)
    w, h = mask.size
    out = Image.new("RGBA", (w + 2, h + 2), (0, 0, 0, 0))
    m = mask.load()
    o = out.load()
    for y in range(h):
        for x in range(w):
            if m[x, y]:
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        o[x + 1 + dx, y + 1 + dy] = INK
    for y in range(h):
        for x in range(w):
            if m[x, y]:
                o[x + 1, y + 1] = (255, 246, 230, 255)
    return out


def scene(W_, H_, n, layout, with_logo=True, seed=7):
    """도트 공간 (W_/n × H_/n) 에서 합성하고 n 배로 키운다."""
    w, h = W_ // n, H_ // n
    rnd = random.Random(seed)
    im = floor(w + 64, h + 64, rnd).crop((0, 0, w, h))

    # 소품 · 졸병을 흩뿌린다 (뒤 → 앞 순서로 y 정렬)
    layer = []
    for p in props(rnd):
        for _ in range(layout.get("props", 2)):
            layer.append((p, rnd.randrange(-20, w), rnd.randrange(-30, int(h * 0.55))))
    for _ in range(layout.get("mobs", 8)):
        role = rnd.choice(["grunt", "runner", "swarm", "spitter"])
        mob = dots(os.path.join(SPR, "mob_%s_%s_walk%d.png" % (MAP, role, rnd.randrange(4))))
        layer.append((mob, rnd.randrange(0, w - 40), rnd.randrange(int(h * 0.25), h - 40)))
    layer.sort(key=lambda t: t[2] + t[0].height)
    for spr, x, y in layer:
        im.alpha_composite(spr, (x, y))

    # 보스
    if layout.get("boss"):
        bx, by = layout["boss"]
        boss = dots(os.path.join(SPR, "mob_%s_boss_walk0.png" % MAP))
        im.alpha_composite(boss, (int(w * bx) - boss.width // 2, int(h * by) - boss.height))

    im = vignette(im)

    # 주인공들 (뒷줄 → 앞줄). 앞 주인공은 2배
    for key, hx, hy, k in layout["heroes"]:
        hero = dots(os.path.join(SPR, "dog_%s_idle.png" % key))
        if k > 1:
            hero = up(hero, k)
        im.alpha_composite(hero, (int(w * hx) - hero.width // 2, int(h * hy) - hero.height))

    if with_logo:
        lg = logo()
        lk = layout.get("logo_k", 2)
        lg = up(lg, lk)
        lx, ly = layout["logo"]
        im.alpha_composite(lg, (int(w * lx) - lg.width // 2, int(h * ly) - lg.height // 2))
        if layout.get("sub"):
            sb = subtitle(layout["sub"])
            sk = layout.get("sub_k", 1)
            sb = up(sb, sk)
            by0 = int(h * ly) + lg.height // 2 + 3
            bn = banner(sb.width + 16, sb.height + 6)
            im.alpha_composite(bn, (int(w * lx) - bn.width // 2, by0 - 3))
            im.alpha_composite(sb, (int(w * lx) - sb.width // 2, by0))

    out = up(im, n)
    return out.crop((0, 0, W_, H_)).convert("RGB")


SUB = "열 마리 개 영웅의 12분 서바이버"

LAYOUTS = {
    # 이름: (가로, 세로, 도트 크기, 배치)
    "header_capsule_920x430": (920, 430, 2, dict(
        logo=(0.30, 0.30), logo_k=3, sub=SUB, sub_k=1, boss=(0.80, 0.90), mobs=10,
        heroes=[("poodle", 0.58, 0.95, 1), ("shepherd", 0.70, 0.97, 1), ("husky", 0.88, 0.95, 1),
                ("shiba", 0.30, 0.99, 2)])),
    "small_capsule_462x174": (462, 174, 2, dict(
        logo=(0.38, 0.45), logo_k=2, boss=None, props=0, mobs=3,
        heroes=[("shiba", 0.86, 1.0, 1)])),
    "main_capsule_1232x706": (1232, 706, 2, dict(
        logo=(0.5, 0.15), logo_k=4, sub=SUB, sub_k=1, boss=(0.5, 0.74), mobs=16,
        heroes=[("jindo", 0.14, 0.86, 1), ("akita", 0.26, 0.90, 1), ("chow", 0.74, 0.90, 1), ("pug", 0.86, 0.86, 1),
                ("poodle", 0.36, 0.99, 1), ("husky", 0.64, 0.99, 1), ("shiba", 0.5, 1.02, 2)])),
    "vertical_capsule_748x896": (748, 896, 2, dict(
        logo=(0.5, 0.12), logo_k=3, sub=SUB, sub_k=1, boss=(0.5, 0.62), props=1, mobs=12,
        heroes=[("poodle", 0.2, 0.86, 1), ("shepherd", 0.8, 0.86, 1), ("shiba", 0.5, 0.99, 2)])),
    "library_capsule_600x900": (600, 900, 2, dict(
        logo=(0.5, 0.11), logo_k=3, boss=(0.5, 0.60), props=1, mobs=10,
        heroes=[("husky", 0.22, 0.86, 1), ("beagle", 0.78, 0.86, 1), ("shiba", 0.5, 0.99, 2)])),
    "library_hero_3840x1240": (3840, 1240, 4, dict(
        logo=(0.5, 0.5), boss=(0.5, 0.60), mobs=24,
        heroes=[(k, 0.08 + i * 0.093, 0.97, 1) for i, k in enumerate(HEROES)])),
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (w, h, n, lay) in LAYOUTS.items():
        img = scene(w, h, n, lay, with_logo=not name.startswith("library_hero"))
        img.save(os.path.join(OUT, name + ".png"))
        print(name, img.size)
    # 라이브러리 로고 (투명, 1280×720 안에 로고만)
    lg = up(logo(), 8)
    canvas = Image.new("RGBA", (1280, 720), (0, 0, 0, 0))
    canvas.alpha_composite(lg, ((1280 - lg.width) // 2, (720 - lg.height) // 2))
    canvas.save(os.path.join(OUT, "library_logo_1280x720.png"))
    print("library_logo_1280x720", canvas.size)


main()
