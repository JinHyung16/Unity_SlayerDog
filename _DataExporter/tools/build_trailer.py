# -*- coding: utf-8 -*-
# 스팀 트레일러 · 스크린샷 생성기
#
# 재료: 하네스 촬영 클립 Temp/clips/<이름>/f00000.png … (RunLoopCheck 9 · 10번째 칸, 1/30 초 고정 간격 = 30fps)
#   촬영 예: mortal:820:<주인공>:-1:<맵>:<시드>:<성장>:0.0333:<시작 초>:<끝 초>
# 결과: Docs/steam/trailer/slayerdog_trailer.mp4 (1920×1080 · 30fps · 칩튠 배경음악)
#       Docs/steam/screenshots/*.png (1920×1080)
# - 자막은 도스고딕 도트 글씨 (build_promo 와 같은 규칙), 한국어 위 · 영어 아래
# - 오프닝 = 메인 캡슐, 엔딩 = 헤더 캡슐 + 부제
#
# 사용: python build_trailer.py   (ffmpeg 필요, build_promo.py 먼저)

from PIL import Image, ImageDraw, ImageFont
import os, sys, shutil, subprocess
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
CLIPS = os.path.join(ROOT, "Temp", "clips")
WORK = os.path.join(ROOT, "Temp", "trailer_frames")
STEAM = os.path.join(ROOT, "Docs", "steam")
OUT_DIR = os.path.join(STEAM, "trailer")
SHOT_DIR = os.path.join(STEAM, "screenshots")
FONT = os.path.join(ROOT, "Assets", "_Art", "Fonts", "DOSGothic", "DOSGothic.ttf")
BGM = os.path.join(ROOT, "Assets", "Resources", "Bgm", "bgm_skull_deck.wav")
W, H, FPS = 1280, 720, 30
INK = (24, 16, 30, 255)

# (클립, 시작 프레임, 끝 프레임, 한국어 자막, 영어 자막)
SEQ = [
    ("a_bamboo", 0, 240, "이동만 하세요. 공격은 자동입니다", "Just move. Attacks fire on their own"),
    ("b_library", 0, 240, "열 마리 개 영웅, 저마다 다른 고유 공격", "Ten heroic dogs, each with a signature attack"),
    ("d_skull", 0, 255, "무기 39종, 시너지, 통달로 몰려오는 적을 쓸어버린다", "39 weapons, synergies and mastery vs. endless hordes"),
    ("c_boss", 0, 200, "열 개의 세계, 열 마리 보스", "Ten worlds, ten bosses"),
]


def text_img(text, scale, color=(255, 246, 230)):
    # 도스고딕 16px 비트맵에는 가운뎃점이 없다 — 자막에서는 쓰지 않는다
    text = text.replace("·", ",")
    font = ImageFont.truetype(FONT, 16)
    tmp = Image.new("L", (1200, 24), 0)
    d = ImageDraw.Draw(tmp)
    d.fontmode = "1"
    d.text((2, 2), text, font=font, fill=255)
    mask = tmp.crop(tmp.getbbox())
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
                o[x + 1, y + 1] = color + (255,)
    return out.resize((out.width * scale, out.height * scale), Image.NEAREST)


def caption(ko, en):
    """화면 아래 1/4 지점 자막 띠 (어두운 반투명 + 금선)."""
    a = text_img(ko, 2, (255, 214, 90))
    b = text_img(en, 2)
    bw = max(a.width, b.width) + 40
    bh = a.height + b.height + 22
    band = Image.new("RGBA", (bw, bh), (20, 14, 28, 200))
    d = ImageDraw.Draw(band)
    d.rectangle([0, 0, bw - 1, 1], fill=(226, 128, 40, 255))
    d.rectangle([0, bh - 2, bw - 1, bh - 1], fill=(226, 128, 40, 255))
    band.alpha_composite(a, ((bw - a.width) // 2, 8))
    band.alpha_composite(b, ((bw - b.width) // 2, 12 + a.height))
    return band


def fade(img, k):
    if k >= 1:
        return img
    black = Image.new("RGB", img.size, (0, 0, 0))
    return Image.blend(black, img, max(0.0, k))


def card(path, seconds, n0, sub=None):
    base = Image.open(path).convert("RGB")
    base = base.resize((W, int(base.height * W / base.width)), Image.NEAREST) if base.width != W else base
    canvas = Image.new("RGB", (W, H), (12, 10, 18))
    canvas.paste(base, (0, (H - base.height) // 2))
    if sub:
        c = text_img(sub, 2, (255, 214, 90))
        rgba = canvas.convert("RGBA")
        rgba.alpha_composite(c, ((W - c.width) // 2, H - c.height - 30))
        canvas = rgba.convert("RGB")
    n = int(seconds * FPS)
    for i in range(n):
        k = min(1.0, i / 12, (n - i) / 12)
        fade(canvas, k).save(os.path.join(WORK, "t%05d.png" % (n0 + i)))
    return n0 + n


def main():
    if shutil.which("ffmpeg") is None:
        raise SystemExit("ffmpeg 가 없다")
    if os.path.isdir(WORK):
        shutil.rmtree(WORK)
    os.makedirs(WORK)
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(SHOT_DIR, exist_ok=True)

    n = card(os.path.join(STEAM, "main_capsule_1232x706.png"), 3.0, 0)
    for clip, f0, f1, ko, en in SEQ:
        cap = caption(ko, en)
        total = f1 - f0
        for i in range(total):
            p = os.path.join(CLIPS, clip, "f%05d.png" % (f0 + i))
            if os.path.exists(p) is False:
                break
            fr = Image.open(p).convert("RGBA")
            if fr.size != (W, H):
                fr = fr.resize((W, H), Image.LANCZOS)
            # 자막은 클립 앞 2/3 동안 (들어오고 나갈 때 6프레임 페이드)
            if i < total * 2 // 3:
                c = cap.copy()
                ka = min(1.0, i / 6, (total * 2 // 3 - i) / 6)
                if ka < 1:
                    c.putalpha(c.split()[3].point(lambda v: int(v * ka)))
                fr.alpha_composite(c, ((W - c.width) // 2, int(H * 0.72)))
            k = min(1.0, i / 8, (total - i) / 8)
            fade(fr.convert("RGB"), k).save(os.path.join(WORK, "t%05d.png" % n))
            n += 1
    n = card(os.path.join(STEAM, "header_capsule_920x430.png"), 4.0, n, "Steam 에서 만나요  /  Coming to Steam")
    seconds = n / FPS

    out = os.path.join(OUT_DIR, "slayerdog_trailer.mp4")
    cmd = ["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(WORK, "t%05d.png"),
           "-stream_loop", "-1", "-i", BGM,
           "-filter_complex", "[0:v]scale=1920:1080:flags=neighbor,format=yuv420p[v];[1:a]atrim=0:%.2f,afade=t=in:st=0:d=0.5,afade=t=out:st=%.2f:d=1.5,volume=0.8[a]" % (seconds, seconds - 1.5),
           "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-c:a", "aac", "-b:a", "192k",
           "-shortest", out]
    subprocess.run(cmd, check=True)
    print("트레일러 %s · %.1f초 · %d프레임" % (out, seconds, n))

    # 스크린샷: 클립마다 고른 프레임을 1920×1080 으로
    picks = [("a_bamboo", 150), ("b_library", 130), ("d_skull", 150), ("c_boss", 60), ("d_skull", 40), ("b_library", 230)]
    for i, (clip, f) in enumerate(picks, start=1):
        p = os.path.join(CLIPS, clip, "f%05d.png" % f)
        if os.path.exists(p):
            Image.open(p).convert("RGB").resize((1920, 1080), Image.LANCZOS).save(os.path.join(SHOT_DIR, "screenshot_%02d.png" % i))
    print("스크린샷 %d장 · %s" % (len(os.listdir(SHOT_DIR)), SHOT_DIR))


main()
