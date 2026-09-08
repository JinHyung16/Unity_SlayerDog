# -*- coding: utf-8 -*-
"""Tiny Swords 타일셋 → 반복 가능한 바닥 텍스처 + 장식. 타일에 들어갈 것만 가져온다."""
from PIL import Image, ImageEnhance
import os, sys, random
sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\wlsgu\Downloads\Tiny Swords (Free Pack)\Tiny Swords (Free Pack)\Terrain"
DST = r"C:\jinhyeong_project\Unity_SkillSystem_Module\Assets\_Art\Tiles"
os.makedirs(DST, exist_ok=True)
random.seed(7)

# 타일셋 576x384 = 9열 x 6행, 64px. (열1,행1) 이 잔디 내부 타일.
sheet = Image.open(os.path.join(SRC, "Tileset", "Tilemap_color1.png")).convert("RGBA")
inner = sheet.crop((64, 64, 128, 128))

# 4x4 로 엮어 256x256 반복 텍스처를 만든다. 타일마다 미세한 밝기 차 + 뒤집기로 패턴 티를 줄인다.
GROUND = 4
g = Image.new("RGBA", (64 * GROUND, 64 * GROUND))
for ty in range(GROUND):
    for tx in range(GROUND):
        t = inner.copy()
        if random.random() < 0.5:
            t = t.transpose(Image.FLIP_LEFT_RIGHT)
        if random.random() < 0.5:
            t = t.transpose(Image.FLIP_TOP_BOTTOM)
        t = ImageEnhance.Brightness(t).enhance(random.uniform(0.94, 1.06))
        g.paste(t, (tx * 64, ty * 64))
g.save(os.path.join(DST, "ground_grass.png"))
print("ground_grass.png", g.size)

# 장식 — 바위 4종 (64x64 그대로)
for i in range(1, 5):
    p = os.path.join(SRC, "Decorations", "Rocks", "Rock%d.png" % i)
    Image.open(p).convert("RGBA").save(os.path.join(DST, "decor_rock%d.png" % i))
print("decor_rock1~4.png 64x64")

# 장식 — 덤불 (1024x128 = 8열 x 128px, 첫 프레임만)
b = Image.open(os.path.join(SRC, "Decorations", "Bushes", "Bushe1.png")).convert("RGBA")
b.crop((0, 0, 128, 128)).save(os.path.join(DST, "decor_bush.png"))
print("decor_bush.png 128x128")
