# -*- coding: utf-8 -*-
# 스킬 아이콘 생성기 — 성좌 트리의 «별 안에» 넣을 그림.
#
# FX 스프라이트를 그대로 쓰지 않는 이유:
#   FX 는 «외곽선만» 있는 실루엣이다(연출용으로는 그게 맞다). 그런데 성좌에서는
#   한 변이 20~30px 로 그려지므로 2px 짜리 선은 1px 미만이 되어 뭉개진다.
#
# [사고] 처음엔 속을 «채웠다». 이빨·뼈다귀는 잘 나왔지만 보호막·각성·시선처럼
#   원래 고리 모양인 것들이 전부 «흰 원반»이 되어 서로 구분이 안 됐다.
#   채우기는 모양을 바꾸는 짓이다. 필요한 것은 «굵히기» — 고리는 고리로 남고 얇은 선만 살아난다.
#
# 이름을 스킬 id 로 붙이는 이유:
#   런타임에 「스킬 → 아트키」 표를 또 들고 있을 필요가 없다.
#   Resources.Load("UI/Icons/2001") 한 줄로 끝난다.
#
# 사용: python _DataExporter/tools/build_skillicons.py  후 build_meta.py

import os, sys, json, glob, hashlib

sys.stdout.reconfigure(encoding="utf-8")

try:
    from PIL import Image, ImageFilter
except ImportError:
    print("[ERROR] Pillow 가 필요하다: pip install pillow")
    raise SystemExit(1)

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
GAMEDATA = os.path.join(ROOT, "Assets", "Resources", "GameData")
FXDIR = os.path.join(ROOT, "Assets", "_Art", "FX")
OUTDIR = os.path.join(ROOT, "Assets", "Resources", "UI", "Icons")

# 아트키(SkillVisual 이 쓰는 이름) -> FX 폴더 · 종류. build_vfx.py 가 쓰는 표(_fx_table.tsv) 한 곳에서 읽는다
FX_TABLE = os.path.join(ROOT, "_DataExporter", "_fx_table.tsv")


def load_fx_table():
    folders, objects = {}, set()
    with open(FX_TABLE, encoding="utf-8") as fp:
        for line in fp:
            line = line.rstrip("\n")
            if not line or line.startswith("#"):
                continue
            folder, addr, kind = line.split("\t")[:3]
            folders[addr] = folder
            if kind == "object":
                objects.add(folder)
    return folders, objects


FOLDERS, OBJECT_FOLDERS = load_fx_table()

ALPHA_ON = 40


def folder_for(key):
    return FOLDERS.get(key, key.lower())


def load_json(name):
    with open(os.path.join(GAMEDATA, name), encoding="utf-8") as f:
        return json.load(f)


def coverage(im):
    a = im.split()[3]
    on = sum(1 for p in a.tobytes() if p > ALPHA_ON)
    return on * 100.0 / (im.size[0] * im.size[1])


# 이 이상 잉크가 차면 «그림»이 아니라 덩어리다. 폭발 연출의 절정 프레임이 여기 걸린다.
BLOB_INK = 45.0


def best_frame(folder):
    """
    잉크가 «많되 덩어리는 아닌» 프레임을 고른다.

    첫 프레임은 대개 이제 막 시작한 그림이라 비어 있고, 절정 프레임은 화면을 덮는 덩어리다.
    아이콘으로 읽히는 것은 그 사이다.
    """
    # build_vfx.py 는 core/ · glow/ 두 층으로 낸다. 아이콘은 모양만 필요하니 심지(core)에서 고른다
    files = sorted(glob.glob(os.path.join(FXDIR, folder, "core", "*.png")))
    files = [f for f in files if os.path.basename(f)[0].isdigit()]
    if not files:
        return None

    scored = []
    for f in files:
        im = Image.open(f).convert("RGBA")
        scored.append((coverage(im), im))

    slim = [(c, im) for c, im in scored if c <= BLOB_INK]
    if slim:
        return max(slim, key=lambda t: t[0])[1]
    # 전부 덩어리면 그중 제일 덜한 것 — 아무것도 안 주는 것보다 낫다
    return min(scored, key=lambda t: t[0])[1]


def thicken(im, radius=1):
    """
    선을 굵힌다(알파 팽창). 모양은 그대로 두고 «보이게»만 만든다.

    채우기와 다른 점: 고리는 고리로 남는다. 24px 로 줄었을 때 2px 선은 사라지지만
    4px 선은 남는다 — 그 차이가 「무슨 그림인지 알겠다」와 「점 하나」를 가른다.
    """
    # radius 는 64px 기준이다. 생성기가 고해상도로 낸 프레임(256px)은 그 배율만큼 넓힌다.
    w, h = im.size
    r = radius * max(1, w // 64)
    grown = im.split()[3].filter(ImageFilter.MaxFilter(r * 2 + 1))
    mask = grown.point(lambda v: 255 if v > ALPHA_ON else 0)

    # 아이콘은 색을 코드가 정한다. 흰색으로 통일해 두면 어느 계열색에도 얹힌다.
    out = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    out.paste((255, 255, 255, 255), (0, 0), mask)
    return out


# 물건형(build_vfx.py OBJECT)은 제 색으로 칠한 그림이라 흰 실루엣으로 바꾸면 공 · 원반 · 사료가 전부 덩어리가 된다.
# 외곽선(진보라)과 색 경계를 «틈»으로 남겨 솔기 · 끈 · 낱알이 보이게 한다. 팽창은 하지 않는다 (틈이 닫힌다).


def object_glyph(im):
    rgb = im.convert("RGB")
    lum = rgb.convert("L")
    a = im.split()[3]
    solid = a.point(lambda v: 255 if v > ALPHA_ON else 0)
    not_ink = lum.point(lambda v: 255 if v > 80 else 0)
    edges = rgb.filter(ImageFilter.FIND_EDGES).convert("L").point(lambda v: 255 if v > 40 else 0)
    edges = edges.filter(ImageFilter.MaxFilter(3))
    from PIL import ImageChops
    body = ImageChops.multiply(solid, not_ink)
    body = ImageChops.subtract(body, edges)
    body = body.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    out = Image.new("RGBA", im.size, (255, 255, 255, 0))
    out.paste((255, 255, 255, 255), (0, 0), body)
    return out


def trim_and_pad(im, size=64, margin=4):
    """딱 맞게 자른 뒤 여백을 «일정하게» 준다. 그래야 아이콘마다 크기가 들쭉날쭉하지 않다."""
    bb = im.split()[3].getbbox()
    if bb is None:
        return im
    cut = im.crop(bb)
    inner = size - margin * 2
    cw, ch = cut.size
    scale = min(inner / cw, inner / ch)
    nw, nh = max(1, int(round(cw * scale))), max(1, int(round(ch * scale)))
    # 도트 아이콘: 면적 평균으로 줄인 뒤 알파를 반에서 끊는다 — 반투명 경계(흐린 가장자리)를 남기지 않는다
    cut = cut.resize((nw, nh), Image.BOX)
    cut.putalpha(cut.split()[3].point(lambda v: 255 if v >= 110 else 0))

    out = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    out.paste(cut, ((size - nw) // 2, (size - nh) // 2))
    return out


# 레벨업 능력 카드 아이콘 (SurvivorHud.FillCard: Icons/stat_<이름>). 흰 실루엣 — 색은 카드 강조색으로 틴트
def stat_icons():
    from PIL import ImageDraw
    shapes = {}

    def canvas():
        im = Image.new("RGBA", (64, 64), (255, 255, 255, 0))
        return im, ImageDraw.Draw(im)

    # 날 벼리기 (공격력): 비스듬한 칼
    im, d = canvas()
    d.polygon([(44, 8), (56, 8), (56, 20), (26, 50), (14, 38)], fill=(255, 255, 255, 255))
    d.rectangle([10, 40, 14, 56], fill=(255, 255, 255, 255))
    d.polygon([(6, 36), (12, 30), (34, 52), (28, 58)], fill=(255, 255, 255, 255))
    d.rectangle([4, 52, 12, 60], fill=(255, 255, 255, 255))
    shapes["damage"] = im
    # 강철 갑옷 (최대 체력): 방패
    im, d = canvas()
    d.polygon([(32, 6), (56, 14), (54, 36), (32, 58), (10, 36), (8, 14)], fill=(255, 255, 255, 255))
    d.polygon([(32, 16), (44, 20), (43, 34), (32, 46), (21, 34), (20, 20)], fill=(255, 255, 255, 0))
    d.rectangle([30, 18, 34, 44], fill=(255, 255, 255, 255))
    d.rectangle([22, 26, 42, 30], fill=(255, 255, 255, 255))
    shapes["maxhp"] = im
    # 바람 발걸음 (이동속도): 날개 달린 신발
    im, d = canvas()
    d.polygon([(20, 18), (34, 18), (34, 38), (54, 42), (56, 52), (14, 52), (14, 40), (20, 36)], fill=(255, 255, 255, 255))
    for i, y in enumerate((14, 22, 30)):
        d.polygon([(34, y + 4), (52 - i * 4, y - 2), (48 - i * 4, y + 6)], fill=(255, 255, 255, 255))
    d.rectangle([6, 56, 58, 59], fill=(255, 255, 255, 255))
    shapes["movespeed"] = im
    # 전장의 함성 (처치 시 체력): 발바닥 + 십자
    im, d = canvas()
    d.ellipse([14, 30, 42, 56], fill=(255, 255, 255, 255))
    for x, y in ((10, 20), (20, 12), (32, 12), (42, 20)):
        d.ellipse([x - 5, y - 6, x + 5, y + 6], fill=(255, 255, 255, 255))
    d.rectangle([48, 30, 52, 50], fill=(255, 255, 255, 255))
    d.rectangle([40, 38, 60, 42], fill=(255, 255, 255, 255))
    shapes["healonkill"] = im

    for name, im in shapes.items():
        trim_and_pad(im).save(os.path.join(OUTDIR, "stat_%s.png" % name))
    return len(shapes)


def main():
    skills = load_json("Skill.json")
    visuals = {v["path"]: v for v in load_json("SkillVisual.json")}

    os.makedirs(OUTDIR, exist_ok=True)
    made = 0
    missing = []

    for s in skills:
        vis = visuals.get(s.get("visualPath", ""))
        if vis is None:
            missing.append((s["id"], "비주얼 없음"))
            continue

        vals = vis.get("values", {})
        key = vals.get("Projectile") or vals.get("Impact") or vals.get("Cast")
        if not key:
            missing.append((s["id"], "아트키 없음"))
            continue

        folder = folder_for(key)
        frame = best_frame(folder)
        if frame is None:
            missing.append((s["id"], f"{key} -> {folder} 폴더 없음"))
            continue

        glyph = object_glyph(frame) if folder in OBJECT_FOLDERS else thicken(frame)
        # 화살 · 칼날처럼 가는 물건은 외곽선을 빼면 몸통이 사라진다 — 남는 면적이 작으면 실루엣을 굵힌다
        if folder in OBJECT_FOLDERS and coverage(glyph) < 2.0:
            glyph = thicken(frame)
        icon = trim_and_pad(glyph)
        icon.save(os.path.join(OUTDIR, f"{s['id']}.png"))
        made += 1

    made += stat_icons()
    print(f"[OK] 스킬 아이콘 {made}개 -> {OUTDIR}")
    for mid, why in missing:
        print(f"[WARN] {mid}: {why}")
    if missing:
        return 1
    print("이어서: python _DataExporter/tools/build_meta.py")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
