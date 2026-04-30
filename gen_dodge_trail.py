#!/usr/bin/env python3
"""S-114: dodge trail sprite — 32x32 4-frame sheet (128x32)."""
from PIL import Image, ImageDraw
import hashlib, os

OUT = 'Assets/Art/Sprites/VFX/vfx_dodge_trail.png'
FRAMES = 4
W = 32
H = 32

def humanoid(draw, ox, alpha):
    """Stylized humanoid silhouette in cyan-blue. ox = frame x offset."""
    cyan = (136, 221, 255, alpha)            # #88ddff main
    deep = (74, 170, 230, alpha)             # darker rim
    glow = (200, 240, 255, max(alpha - 60, 0))  # soft outer glow

    # outer glow ring (slightly larger, faint)
    for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
        draw.ellipse([ox + 9 + dx, 4 + dy, ox + 22 + dx, 13 + dy], fill=glow)

    # head
    draw.ellipse([ox + 10, 5, ox + 21, 14], fill=cyan)
    draw.ellipse([ox + 11, 6, ox + 20, 13], fill=deep)
    draw.ellipse([ox + 12, 7, ox + 19, 12], fill=cyan)

    # torso
    draw.rectangle([ox + 11, 13, ox + 20, 22], fill=cyan)
    draw.rectangle([ox + 12, 14, ox + 19, 21], fill=deep)

    # arms (motion-streaked: short stubs)
    draw.rectangle([ox + 8, 14, ox + 11, 19], fill=cyan)
    draw.rectangle([ox + 20, 14, ox + 23, 19], fill=cyan)

    # legs
    draw.rectangle([ox + 12, 22, ox + 14, 28], fill=cyan)
    draw.rectangle([ox + 17, 22, ox + 19, 28], fill=cyan)
    draw.rectangle([ox + 13, 23, ox + 14, 27], fill=deep)
    draw.rectangle([ox + 17, 23, ox + 18, 27], fill=deep)

    # streak lines behind (motion)
    for y in (10, 14, 18, 22):
        draw.line([(ox + 4, y), (ox + 8, y)], fill=glow)


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet = Image.new('RGBA', (W * FRAMES, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)

    # Frame opacity: 0=most opaque, 3=ghost
    alphas = [220, 170, 110, 55]
    for i, a in enumerate(alphas):
        humanoid(draw, i * W, a)

    sheet.save(OUT)
    print(f'wrote {OUT} ({sheet.size})')

    # Generate .meta with sliced sub-sprites
    guid = hashlib.md5(OUT.encode()).hexdigest()[:32]
    sprite_entries = []
    name_table = []
    for i in range(FRAMES):
        sid = hashlib.md5(f'{OUT}_{i}'.encode()).hexdigest()[:32]
        internal = -(int(hashlib.md5(f'{OUT}_{i}_iid'.encode()).hexdigest()[:15], 16))
        sprite_entries.append(f"""    - serializedVersion: 2
      name: vfx_dodge_trail_{i}
      rect:
        serializedVersion: 2
        x: {i * W}
        y: 0
        width: {W}
        height: {H}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sid}
      internalID: {internal}
      vertices: []
      indices:
      edges: []
      weights: []""")
        name_table.append(f'      vfx_dodge_trail_{i}: {internal}')

    meta = f"""fileFormatVersion: 2
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
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
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
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{chr(10).join(sprite_entries)}
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {hashlib.md5((OUT + '_sheet').encode()).hexdigest()[:24]}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{chr(10).join(name_table)}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    with open(OUT + '.meta', 'w') as f:
        f.write(meta)
    print(f'wrote {OUT}.meta')


if __name__ == '__main__':
    main()
