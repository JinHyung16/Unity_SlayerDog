// pixelfx 이펙트 → SlayerDog 이펙트 소스 프레임 (_DataExporter/pixelfx/<폴더>/NN.png + kind.txt)
//
// pixelfx (https://github.com/patch-gg/pixelfx, MIT) 는 코드로 그리는 도트 이펙트 모음이다.
// 이 도구는 그중 color 버퍼만 떠 온다. light 버퍼(부드러운 번짐)는 버린다 —
// 번짐은 build_vfx.py 가 심지에서 도트 링으로 다시 만든다 (우리 그림 규칙: 가우시안 번짐 금지)
// 이후 build_vfx.py 가 이 프레임을 ×4 키워 core/ · glow/ 로 굽는다
//
// 캔버스 = 64 × K 도트. K 는 _art_scale.json (build_vfx.py 가 판정 반경으로 정한 배율) 을 따른다 —
// 판정이 큰 이펙트는 화면에서 그만큼 커지므로 캔버스를 키워 도트 크기를 캐릭터와 맞춘다
//
// 종류
//   tint    여러 스킬이 색만 바꿔 쓰는 그림 (보스 위험 색 포함). pixelfx 팔레트를 밝기만 남긴 회색조로 뽑고,
//           Art2DBuilder 가 데이터 색을 입힌다
//   object  한 가지 색으로만 쓰는 그림. 팔레트를 그대로(또는 표의 palette 로) 뽑고 제 색을 지킨다
//
// 사용: node pixelfx_import.mjs <pixelfx 체크아웃 경로> [폴더 ...]
//   pixelfx 는 저장소에 넣지 않는다. 필요할 때 git clone --depth 1 https://github.com/patch-gg/pixelfx.git
// 외부 npm 의존성 없음 (PNG 는 zlib 로 직접 쓴다)

import fs from "node:fs";
import path from "node:path";
import zlib from "node:zlib";
import { fileURLToPath, pathToFileURL } from "node:url";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(HERE, "..", "pixelfx");
const SCALE = path.resolve(HERE, "..", "_art_scale.json");
const UP = 4;
const FRAMES = 6;
// 캔버스 밖으로 나간 도트를 세려고 사방에 덧대는 폭
const PAD = 96;
// 시드 자동 탐색 범위 — 캔버스 밖으로 가장 적게 튀는 시드를 고른다
const SEEDS = 32;
// tint 회색조 밝기 감마 (1 = 그대로, 작을수록 밝다)
const GRAY_GAMMA = 0.55;

// 시바 벚꽃 분홍 (gen_slayer.js HEROES basic_iaido color #FF8FC8 근처의 어두움→밝음 단계)
const SAKURA = ["#5a1f3d", "#9c3566", "#d65a8e", "#f28db5", "#f9c3d8", "#fde6ef", "#ffffff"];
const SAKURA_SPARKS = ["#ffffff", "#fde6ef", "#f9c3d8", "#f28db5"];

// folder  Assets/_Art/FX 폴더 (= _fx_table.tsv 첫 열)
// fx      pixelfx 이펙트 id
// opts    pixelfx 옵션 (palette 를 주면 그 색, tint 는 기본 팔레트의 회색조)
// origin  pixelfx 원점을 캔버스의 어디에 둘지 (0~1 비율). 기본 가운데. 앞(+x)으로 뻗는 연출은 왼쪽에 둔다
// span    뽑을 시간 구간(초). 기본 0 ~ 길이. pixelfx 연출은 길어서(2~5초) 즉발 0.45초에 맞게 핵심만 자른다
// seed    true 면 자동 탐색, 숫자면 고정
const TABLE = [
    {
        folder: "fx_iaido", fx: "melee/sword-slash", kind: "object",
        opts: { radius: 28, angle: 0.85, palette: SAKURA, sparks: SAKURA_SPARKS },
        origin: [4 / 64, 39 / 64], times: [0.02, 0.06, 0.11, 0.18, 0.28, 0.42], seed: 12,
    },
    // 공용 타격 불꽃 (HitEffect · 무리 울음 · 일섬 적중)
    { folder: "fx_hit", fx: "impacts/hit-spark", kind: "tint", opts: {}, origin: [0.3, 0.55], span: [0, 0.4], seed: 15 },
    // 정전기 털 — 대상에 떨어지는 전격
    { folder: "fx_thunder", fx: "lightning/zap-hit", kind: "tint", opts: {}, origin: [0.5, 0.6], span: [0.08, 0.7], seed: 26 },
    // 뼈 지뢰 폭발 — 불 · 연기 제 색
    { folder: "fx_blast", fx: "explosions/small-explosion", kind: "object", opts: { size: 0.45 }, origin: [0.5, 0.75], span: [0.12, 1.3], seed: 6 },
    // 제3의 눈 · 성채 보스 기탄 — 앞(+x)으로 나는 플라스마
    { folder: "fx_qibolt", fx: "projectiles/plasma-shot", kind: "tint", opts: {}, origin: [0.62, 0.5], seed: 6 },
    // 눈빛 광선 — 앞(+x)으로 나는 광탄
    { folder: "fx_lightray", fx: "projectiles/laser-bolt", kind: "tint", opts: { length: 30 }, origin: [0.7, 0.5], seed: 30 },
];

const args = process.argv.slice(2);
const root = args[0];
const only = args.slice(1);
if (!root || !fs.existsSync(path.join(root, "src", "core", "index.js"))) {
    console.error("usage: node pixelfx_import.mjs <pixelfx 체크아웃 경로> [폴더 ...]");
    process.exit(1);
}
const core = await import(pathToFileURL(path.join(root, "src", "core", "index.js")));
const scale = fs.existsSync(SCALE) ? JSON.parse(fs.readFileSync(SCALE, "utf8")) : {};

for (const e of TABLE) {
    if (only.length > 0 && only.includes(e.folder) === false)
        continue;
    const mod = await import(pathToFileURL(path.join(root, "src", "fx", `${e.fx}.js`)));
    const k = (scale[`Assets/_Art/FX/${e.folder}/core`] ?? UP) / UP;
    const W = Math.round(64 * k);
    const opts = { ...e.opts };
    if (e.kind === "tint")
        Object.assign(opts, grayRamps(mod.meta));

    let seed = typeof e.seed === "number" ? e.seed : (mod.meta.options?.seed ?? 1);
    if (e.seed === true) {
        let best = Infinity;
        for (let s = 1; s <= SEEDS; s++) {
            const out = render(mod, e, { ...opts, seed: s }, W).outside;
            if (out < best) {
                best = out;
                seed = s;
            }
        }
    }
    const r = render(mod, e, { ...opts, seed }, W);

    const dir = path.join(OUT, e.folder);
    fs.mkdirSync(dir, { recursive: true });
    for (const f of fs.readdirSync(dir)) fs.rmSync(path.join(dir, f));
    r.frames.forEach((rgba, i) => fs.writeFileSync(path.join(dir, `${String(i).padStart(2, "0")}.png`), png(rgba, W, W)));
    fs.writeFileSync(path.join(dir, "kind.txt"), e.kind);
    console.log(`[OK] ${e.folder} ← ${e.fx} (${e.kind}, 캔버스 ${W}, 시드 ${seed}, 밖으로 나간 도트 ${r.outside})`);
}

// 덧댄 큰 프레임에 그려 캔버스 밖 도트를 세고, 가운데 W×W 만 잘라 돌려준다
function render(mod, e, opts, W) {
    const big = W + PAD * 2;
    const [ox, oy] = e.origin ?? [0.5, 0.5];
    const fx = core.createFx(mod, { ...opts, x: PAD + ox * W, y: PAD + oy * W });
    const frame = core.createFrame(big, big);
    const times = e.times ?? spanTimes(mod.meta, e.span);
    const frames = [];
    let outside = 0;
    const step = 1 / 120;
    let t = 0;
    for (const at of times) {
        // 시뮬은 120Hz 고정 걸음으로 시각까지 민다 (pixelfx 는 시드 고정이라 매번 같은 그림)
        while (t < at - 1e-9) { fx.update(step); t += step; }
        core.clearFrame(frame);
        fx.draw(frame, "back");
        fx.draw(frame, "front");
        const rgba = Buffer.alloc(W * W * 4);
        for (let y = 0; y < big; y++) {
            for (let x = 0; x < big; x++) {
                const c = frame.color[y * big + x];
                if ((c >>> 24) === 0)
                    continue;
                const cx = x - PAD, cy = y - PAD;
                if (cx < 0 || cy < 0 || cx >= W || cy >= W) {
                    outside++;
                    continue;
                }
                const o = (cy * W + cx) * 4;
                rgba[o] = c & 255;
                rgba[o + 1] = (c >>> 8) & 255;
                rgba[o + 2] = (c >>> 16) & 255;
                rgba[o + 3] = c >>> 24;
            }
        }
        frames.push(rgba);
    }
    return { frames, outside };
}

// 한 바퀴(루프) 또는 구간(한 번) 을 FRAMES 장으로 고르게. 루프는 끝 = 처음이라 마지막 칸을 겹치지 않는다
function spanTimes(meta, span) {
    const d = meta.duration ?? 1;
    const [a, b] = span ?? [0, d];
    const n = FRAMES;
    return Array.from({ length: n }, (_, i) => a + (b - a) * (meta.loop && span === undefined ? i / n : i / (n - 1)));
}

// 기본 팔레트(색 단계 옵션 전부)를 밝기만 남긴 회색조로. 명암 구조는 그대로, 색은 데이터가 입힌다
// 색을 곱하면 어두워지므로 밝기를 감마로 들어 올린다 — 밝은 단계가 흰 심지처럼 달아오르게
function grayRamps(meta) {
    const out = {};
    for (const [key, v] of Object.entries(meta.options ?? {})) {
        if (Array.isArray(v) && v.length > 0 && v.every((c) => typeof c === "string" && c.startsWith("#")))
            out[key] = v.map(gray);
    }
    return out;
}

function gray(hex) {
    let h = hex.slice(1);
    if (h.length <= 4)
        h = h.split("").map((c) => c + c).join("");
    const r = parseInt(h.slice(0, 2), 16), g = parseInt(h.slice(2, 4), 16), b = parseInt(h.slice(4, 6), 16);
    const lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    const l = Math.round(255 * Math.pow(lum, GRAY_GAMMA)).toString(16).padStart(2, "0");
    return `#${l}${l}${l}`;
}

function png(rgba, w, h) {
    const raw = Buffer.alloc((w * 4 + 1) * h);
    for (let y = 0; y < h; y++) rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
    const ihdr = Buffer.alloc(13);
    ihdr.writeUInt32BE(w, 0);
    ihdr.writeUInt32BE(h, 4);
    ihdr[8] = 8;
    ihdr[9] = 6;
    return Buffer.concat([
        Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
        chunk("IHDR", ihdr),
        chunk("IDAT", zlib.deflateSync(raw)),
        chunk("IEND", Buffer.alloc(0)),
    ]);
}

function chunk(type, data) {
    const len = Buffer.alloc(4);
    len.writeUInt32BE(data.length);
    const td = Buffer.concat([Buffer.from(type, "ascii"), data]);
    const crc = Buffer.alloc(4);
    crc.writeUInt32BE(crc32(td));
    return Buffer.concat([len, td, crc]);
}

function crc32(buf) {
    let c = ~0;
    for (let i = 0; i < buf.length; i++) {
        c ^= buf[i];
        for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
    }
    return ~c >>> 0;
}
