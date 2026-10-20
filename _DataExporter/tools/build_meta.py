# -*- coding: utf-8 -*-
# PNG -> Unity .meta 생성기 (에디터 없이 import 설정을 확정한다)
#
# - GUID 는 경로 md5 로 고정한다. 재실행해도 같은 GUID -> 프리팹 참조가 안 끊긴다
# - 전부 Bilinear + 무압축. 도트 그림은 생성기가 최근접 ×4 로 키워 내므로(pixkit.big) 확대본 + 밉맵으로 선명함을 지킨다
#   Point 는 쓰지 않는다 — 정수 배율을 보장할 수 없는 카메라에서 텍셀이 1px/2px 로 들쭉날쭉 찍혀 뭉개진다
# - PPU 는 논리 캔버스 64px = 2유닛. 생성기가 고해상도로 낸 폴더는 _art_scale.json 배율만큼 PPU 를 곱한다
#   (크기가 그대로여야 프리팹 · 판정 반경이 안 어긋난다). 고해상도는 화면에서 축소되므로 밉맵을 켠다
#   밉맵 편향 -0.75 · 덮개 보존: 한 단계 날카로운 밉을 골라 외곽선 1도트가 흐려지거나 얇아지지 않게
# - 캐릭터/적은 피벗을 발밑으로 내려 지면 정렬이 자동으로 맞게 한다

import os, sys, hashlib, json
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
# 생성기가 쓰는 출력 배율 표. {"Assets/_Art/FX/fx_paw": 4, ...}. 없으면 전부 1
SCALE_FILE = os.path.join(ROOT, "_DataExporter", "_art_scale.json")
# [주의] 이 스크립트는 .meta 를 «통째로 덮어쓴다» — spriteBorder 도 0 으로 밀린다.
#        그래서 9슬라이스를 쓰는 Assets\Resources\UI 는 «대상이 아니다».
#        아이콘 폴더(Icons)만 콕 집어 넣는다. 부모 폴더를 넣으면 UI 스킨의 경계가 전부 날아간다.
TARGETS = [r"Assets\_Art\FX", r"Assets\_Art\Sprites", r"Assets\_Art\Stage",
           r"Assets\Resources\Lobby", r"Assets\Resources\UI\Icons"]
PPU = 32

# 발밑 피벗을 쓸 접두어 (64px 캔버스에서 발끝 y=61 -> 아래에서 3px -> 3/64)
FOOT_PIVOT_PREFIX = ("dog_", "mob_")


def guid_for(rel):
    return hashlib.md5(("jinhyeong::" + rel.replace("\\", "/")).encode("utf-8")).hexdigest()


def load_scales():
    if not os.path.exists(SCALE_FILE):
        return {}
    with open(SCALE_FILE, encoding="utf-8") as fp:
        return json.load(fp)


def platform_block(name):
    return (
        "  - serializedVersion: 4\n"
        "    buildTarget: %s\n"
        "    maxTextureSize: 2048\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 0\n"
        "    compressionQuality: 50\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "    ignorePlatformSupport: 0\n"
        "    androidETC2FallbackOverride: 0\n"
        "    forceMaximumCompressionQuality_BC6H_BC7: 0\n" % name
    )


def make_meta(rel, is_sprite, wrap, alignment, pivot, filter_mode=1, mips=0, ppu=PPU):
    g = guid_for(rel)
    bias = -0.75 if mips else 0
    plats = "".join(platform_block(n) for n in ("DefaultTexturePlatform", "Standalone", "Android"))
    return """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: {mips}
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: {mips}
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: {bias}
    wrapU: {wrap}
    wrapV: {wrap}
    wrapW: {wrap}
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {spritemode}
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: {alignment}
  spritePivot: {{x: {px}, y: {py}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: {ttype}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
{plats}  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""".format(guid=g, bias=bias, wrap=wrap, spritemode=1 if is_sprite else 0,
           alignment=alignment, px=pivot[0], py=pivot[1], ppu=ppu,
           filter=filter_mode, mips=mips,
           ttype=8 if is_sprite else 0, plats=plats)


def main():
    written = 0
    hires = 0
    index = {}
    scales = load_scales()
    for t in TARGETS:
        base = os.path.join(ROOT, t)
        for dirpath, _, files in os.walk(base):
            for f in sorted(files):
                if not f.lower().endswith(".png"):
                    continue
                full = os.path.join(dirpath, f)
                rel = os.path.relpath(full, ROOT)
                name = os.path.splitext(f)[0]

                rel_dir = os.path.dirname(rel).replace("\\", "/")
                scale = int(scales.get(rel_dir, 1))
                opts = dict(
                    mips=1 if scale > 1 else 0,
                    ppu=PPU * scale,
                )

                if name.startswith(FOOT_PIVOT_PREFIX) or name.startswith("deco_"):
                    # 캐릭터 · 적 · 보스 · 소품: 캔버스가 역할 · 소품 배율마다 달라 바닥선(아래에서 3 도트 × 출력 배율)으로 피벗을 잡는다
                    from PIL import Image as _Img
                    hgt = _Img.open(full).height
                    meta = make_meta(rel, True, 1, 9, (0.5, round(3.0 * scale / hgt, 4)), **opts)
                else:
                    meta = make_meta(rel, True, 1, 0, (0.5, 0.5), **opts)

                with open(full + ".meta", "w", encoding="utf-8", newline="\n") as fp:
                    fp.write(meta)
                index[rel.replace("\\", "/")] = guid_for(rel)
                written += 1
                if scale > 1:
                    hires += 1

    # 프리팹/씬 작성 때 쓸 GUID 색인
    out = os.path.join(ROOT, "_DataExporter", "_art_guids.json")
    with open(out, "w", encoding="utf-8") as fp:
        json.dump(index, fp, ensure_ascii=False, indent=1, sort_keys=True)
    print("meta %d개 작성 (고해상도 %d), GUID 색인 -> %s" % (written, hires, out))


main()
