# -*- coding: utf-8 -*-
# PNG -> Unity .meta 생성기 (에디터 없이 import 설정을 확정한다)
#
# - GUID 는 경로 md5 로 고정한다. 재실행해도 같은 GUID -> 프리팹 참조가 안 끊긴다
# - 픽셀아트라 Point 필터 + 무압축 + 밉맵 없음
# - PPU 32 (64px = 2유닛). 기존 휴머노이드 스케일(캐릭터 ~1.8유닛)에 맞춘 값
# - 캐릭터/적은 피벗을 발밑으로 내려 지면 정렬이 자동으로 맞게 한다

import os, sys, hashlib
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
# [주의] 이 스크립트는 .meta 를 «통째로 덮어쓴다» — spriteBorder 도 0 으로 밀린다.
#        그래서 9슬라이스를 쓰는 Assets\Resources\UI 는 «대상이 아니다».
#        아이콘 폴더(Icons)만 콕 집어 넣는다. 부모 폴더를 넣으면 UI 스킨의 경계가 전부 날아간다.
TARGETS = [r"Assets\_Art\FX", r"Assets\_Art\Sprites", r"Assets\_Art\Tiles",
           r"Assets\Resources\Lobby", r"Assets\Resources\UI\Icons"]
PPU = 32

# 발밑 피벗을 쓸 접두어 (64px 캔버스에서 발끝 y=61 -> 아래에서 3px -> 3/64)
FOOT_PIVOT_PREFIX = ("dog_", "alien_")
FOOT_PIVOT_Y = 3.0 / 64.0


def guid_for(rel):
    return hashlib.md5(("jinhyeong::" + rel.replace("\\", "/")).encode("utf-8")).hexdigest()


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


def make_meta(rel, is_sprite, wrap, alignment, pivot):
    g = guid_for(rel)
    plats = "".join(platform_block(n) for n in ("DefaultTexturePlatform", "Standalone", "Android"))
    return """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
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
    filterMode: 0
    aniso: 1
    mipBias: 0
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
""".format(guid=g, wrap=wrap, spritemode=1 if is_sprite else 0,
           alignment=alignment, px=pivot[0], py=pivot[1], ppu=PPU,
           ttype=8 if is_sprite else 0, plats=plats)


def main():
    written = 0
    index = {}
    for t in TARGETS:
        base = os.path.join(ROOT, t)
        for dirpath, _, files in os.walk(base):
            for f in sorted(files):
                if not f.lower().endswith(".png"):
                    continue
                full = os.path.join(dirpath, f)
                rel = os.path.relpath(full, ROOT)
                name = os.path.splitext(f)[0]

                if name == "ground_grass":
                    # 바닥은 머티리얼로 타일링한다 -> 스프라이트가 아니라 Default + Repeat
                    meta = make_meta(rel, False, 0, 0, (0.5, 0.5))
                elif name.startswith(FOOT_PIVOT_PREFIX):
                    meta = make_meta(rel, True, 1, 9, (0.5, round(FOOT_PIVOT_Y, 4)))
                else:
                    meta = make_meta(rel, True, 1, 0, (0.5, 0.5))

                with open(full + ".meta", "w", encoding="utf-8", newline="\n") as fp:
                    fp.write(meta)
                index[rel.replace("\\", "/")] = guid_for(rel)
                written += 1

    # 프리팹/씬 작성 때 쓸 GUID 색인
    out = os.path.join(ROOT, "_DataExporter", "_art_guids.json")
    import json
    with open(out, "w", encoding="utf-8") as fp:
        json.dump(index, fp, ensure_ascii=False, indent=1, sort_keys=True)
    print("meta %d개 작성, GUID 색인 -> %s" % (written, out))


main()
