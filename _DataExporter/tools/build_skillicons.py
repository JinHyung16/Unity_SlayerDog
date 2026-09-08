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
    from PIL import Image
except ImportError:
    print("[ERROR] Pillow 가 필요하다: pip install pillow")
    raise SystemExit(1)

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
GAMEDATA = os.path.join(ROOT, "Assets", "Resources", "GameData")
FXDIR = os.path.join(ROOT, "Assets", "_Art", "FX")
OUTDIR = os.path.join(ROOT, "Assets", "Resources", "UI", "Icons")

# 아트키(SkillVisual 이 쓰는 이름) -> FX 폴더. Art2DBuilder.FxAddress 의 역방향이다.
# [주의] 두 곳에 같은 표가 있으면 어긋난다. 여기서는 «폴더 이름을 규칙으로 만들고»
#        규칙에서 벗어나는 것만 예외로 적는다 — 예외가 셋뿐이라 표 전체를 옮기지 않는다.
EXCEPTIONS = {
    "Fx_TailSwing": "fx_tailsw",
    "Fx_HitSmall": "fx_hit_small",
    "Fx_BurstBig": "fx_burst_big",
    "Fx_SlashCross": "fx_slash_cross",
    "Fx_ArcWave": "fx_arc_wave",
    "Fx_RingSpike": "fx_ring_spike",
    "Fx_RingDense": "fx_ring_dense",
    "Fx_DogClaw": "fx_dogclaw",
    "Fx_DogDust": "fx_dogdust",
    "Fx_EyeBeam": "fx_eyebeam",
}

ALPHA_ON = 40


def folder_for(key):
    if key in EXCEPTIONS:
        return EXCEPTIONS[key]
    return key.lower()


def load_json(name):
    with open(os.path.join(GAMEDATA, name), encoding="utf-8") as f:
        return json.load(f)


def coverage(im):
    a = im.split()[3]
    on = sum(1 for p in a.getdata() if p > ALPHA_ON)
    return on * 100.0 / (im.size[0] * im.size[1])


# 이 이상 잉크가 차면 «그림»이 아니라 덩어리다. 폭발 연출의 절정 프레임이 여기 걸린다.
BLOB_INK = 45.0


def best_frame(folder):
    """
    잉크가 «많되 덩어리는 아닌» 프레임을 고른다.

    첫 프레임은 대개 이제 막 시작한 그림이라 비어 있고, 절정 프레임은 화면을 덮는 덩어리다.
    아이콘으로 읽히는 것은 그 사이다.
    """
    files = sorted(glob.glob(os.path.join(FXDIR, folder, "*.png")))
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
    w, h = im.size
    src = im.split()[3].load()
    out = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    dst = out.load()

    for x in range(w):
        for y in range(h):
            a = 0
            for dx in range(-radius, radius + 1):
                for dy in range(-radius, radius + 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        v = src[nx, ny]
                        if v > a:
                            a = v
            if a > ALPHA_ON:
                # 아이콘은 색을 코드가 정한다. 흰색으로 통일해 두면 어느 계열색에도 얹힌다.
                dst[x, y] = (255, 255, 255, 255)
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
    cut = cut.resize((nw, nh), Image.NEAREST)

    out = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    out.paste(cut, ((size - nw) // 2, (size - nh) // 2))
    return out


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

        icon = trim_and_pad(thicken(frame))
        icon.save(os.path.join(OUTDIR, f"{s['id']}.png"))
        made += 1

    print(f"[OK] 스킬 아이콘 {made}개 -> {OUTDIR}")
    for mid, why in missing:
        print(f"[WARN] {mid}: {why}")
    if missing:
        return 1
    print("이어서: python _DataExporter/tools/build_meta.py")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
