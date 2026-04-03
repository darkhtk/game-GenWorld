#!/usr/bin/env python3
"""Generate UI sprites for the new inventory layout."""
from PIL import Image, ImageDraw
import os, struct

OUT = 'Assets/Art/Sprites/UI'

def save_meta(png_path, tex_type=8, filter_mode=0, ppu=32, border=(0,0,0,0), sprite_mode=1):
    """Write a Unity .meta file for a sprite."""
    import uuid, hashlib
    # Generate deterministic GUID from filename
    h = hashlib.md5(png_path.encode()).hexdigest()
    guid = h[:32]
    bx, by, bz, bw = border
    meta = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1001 &4343727234547468602
AssetImporter:
  serializedVersion: 2
  userData:
  assetBundleName:
  assetBundleVariant:
--- !u!0 &0
TextureImporter:
  serializedVersion: 12
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaCoverageThresholds: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter_mode}
    aniso: 1
    mipBias: 0
    wrapMode: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {sprite_mode}
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: {bx}, y: {by}, z: {bz}, w: {bw}}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: {tex_type}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    with open(png_path + '.meta', 'w') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\n')
        # Write TextureImporter data
        data = f"""TextureImporter:
  serializedVersion: 12
  internalIDToNameTable: []
  externalObjects: {{}}
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaCoverageThresholds: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter_mode}
    aniso: 1
    mipBias: 0
    wrapMode: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {sprite_mode}
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: {bx}, y: {by}, z: {bz}, w: {bw}}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: {tex_type}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
        f.write(data)
    print(f'  meta: {png_path}.meta')

def make_stat_plus_btn():
    """32x32 [+] button sprite."""
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Background: dark blue rounded rect
    d.rounded_rectangle([0, 0, 31, 31], radius=4, fill=(30, 50, 100, 220))
    # Border
    d.rounded_rectangle([0, 0, 31, 31], radius=4, outline=(80, 130, 255, 255), width=1)
    # Plus sign
    d.rectangle([14, 8, 17, 23], fill=(180, 220, 255, 255))
    d.rectangle([8, 14, 23, 17], fill=(180, 220, 255, 255))
    p = f'{OUT}/stat_plus_btn.png'
    img.save(p)
    save_meta(p, border=(0,0,0,0))
    print(f'  {p}')

def make_stat_minus_btn():
    """32x32 [-] button sprite."""
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, 31, 31], radius=4, fill=(80, 30, 30, 200))
    d.rounded_rectangle([0, 0, 31, 31], radius=4, outline=(200, 80, 80, 255), width=1)
    d.rectangle([8, 14, 23, 17], fill=(255, 160, 160, 255))
    p = f'{OUT}/stat_minus_btn.png'
    img.save(p)
    save_meta(p, border=(0,0,0,0))
    print(f'  {p}')

def make_scrollbar_handle():
    """16x64 scrollbar handle, 9-slice."""
    img = Image.new('RGBA', (16, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([1, 1, 14, 62], radius=6, fill=(60, 100, 160, 200))
    d.rounded_rectangle([1, 1, 14, 62], radius=6, outline=(100, 150, 220, 255), width=1)
    p = f'{OUT}/scrollbar_handle.png'
    img.save(p)
    save_meta(p, border=(0, 8, 0, 8))
    print(f'  {p}')

def make_equip_slot_bg():
    """64x64 equipment slot background, 9-slice 8px border."""
    img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, 63, 63], radius=6, fill=(20, 30, 50, 200))
    d.rounded_rectangle([0, 0, 63, 63], radius=6, outline=(60, 100, 160, 255), width=2)
    # Subtle inner highlight
    d.rounded_rectangle([2, 2, 61, 30], radius=4, fill=(40, 60, 90, 80))
    p = f'{OUT}/equip_slot_bg.png'
    img.save(p)
    save_meta(p, border=(8,8,8,8))
    print(f'  {p}')

def make_equip_slot_helmet():
    """64x64 helmet slot icon outline."""
    img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, 63, 63], radius=6, fill=(20, 30, 50, 200))
    d.rounded_rectangle([0, 0, 63, 63], radius=6, outline=(100, 160, 255, 255), width=2)
    # Helmet silhouette (simple arc)
    d.arc([12, 8, 51, 40], start=180, end=0, fill=(120, 180, 255, 200), width=3)
    d.rectangle([14, 38, 49, 48], fill=(80, 130, 200, 180))
    p = f'{OUT}/equip_slot_helmet.png'
    img.save(p)
    save_meta(p, border=(8,8,8,8))
    print(f'  {p}')

os.makedirs(OUT, exist_ok=True)
print(f'Generating inventory UI sprites in {OUT}/')
make_stat_plus_btn()
make_stat_minus_btn()
make_scrollbar_handle()
make_equip_slot_bg()
make_equip_slot_helmet()
print('Done.')
