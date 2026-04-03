#!/usr/bin/env python3
"""Fix RangedColumn and MagicColumn tree background component ID collisions."""

SCENE = 'Assets/Scenes/GameScene.unity'

with open(SCENE, 'r', encoding='utf-8') as f:
    scene = f.read()

RANGED_GUID = '9977df407e633284885cae840555b1b5'
MAGIC_GUID  = 'f7a029054f2648c47a36e73849bcc637'
IMG_SCRIPT  = 'fe87c0e1cc204ed48ad3b37840f39efc'

# ─────────────────────────────────────────────────────────────────────────────
# 1. RangedColumn: insert missing Image block after CR block (776698753)
# ─────────────────────────────────────────────────────────────────────────────
RANGED_CR_END = (
    '--- !u!222 &776698753\n'
    'CanvasRenderer:\n'
    '  m_ObjectHideFlags: 0\n'
    '  m_CorrespondingSourceObject: {fileID: 0}\n'
    '  m_PrefabInstance: {fileID: 0}\n'
    '  m_PrefabAsset: {fileID: 0}\n'
    '  m_GameObject: {fileID: 776698739}\n'
    '  m_CullTransparentMesh: 1\n'
    '--- !u!1 &783065423'
)

RANGED_IMAGE = (
    '--- !u!114 &776698754\n'
    'MonoBehaviour:\n'
    '  m_ObjectHideFlags: 0\n'
    '  m_CorrespondingSourceObject: {fileID: 0}\n'
    '  m_PrefabInstance: {fileID: 0}\n'
    '  m_PrefabAsset: {fileID: 0}\n'
    '  m_GameObject: {fileID: 776698739}\n'
    '  m_Enabled: 1\n'
    '  m_EditorHideFlags: 0\n'
    f'  m_Script: {{fileID: 11500000, guid: {IMG_SCRIPT}, type: 3}}\n'
    '  m_Name: \n'
    '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
    '  m_Material: {fileID: 0}\n'
    '  m_Color: {r: 1, g: 1, b: 1, a: 0.2}\n'
    '  m_RaycastTarget: 0\n'
    '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
    '  m_Maskable: 1\n'
    '  m_OnCullStateChanged:\n'
    '    m_PersistentCalls:\n'
    '      m_Calls: []\n'
    f'  m_Sprite: {{fileID: 21300000, guid: {RANGED_GUID}, type: 3}}\n'
    '  m_Type: 0\n'
    '  m_PreserveAspect: 1\n'
    '  m_FillCenter: 1\n'
    '  m_FillMethod: 4\n'
    '  m_FillAmount: 1\n'
    '  m_FillClockwise: 1\n'
    '  m_FillOrigin: 0\n'
    '  m_UseSpriteMesh: 0\n'
    '  m_PixelsPerUnitMultiplier: 1\n'
)

RANGED_CR_END_NEW = (
    '--- !u!222 &776698753\n'
    'CanvasRenderer:\n'
    '  m_ObjectHideFlags: 0\n'
    '  m_CorrespondingSourceObject: {fileID: 0}\n'
    '  m_PrefabInstance: {fileID: 0}\n'
    '  m_PrefabAsset: {fileID: 0}\n'
    '  m_GameObject: {fileID: 776698739}\n'
    '  m_CullTransparentMesh: 1\n'
    + RANGED_IMAGE +
    '--- !u!1 &783065423'
)

if RANGED_CR_END in scene:
    scene = scene.replace(RANGED_CR_END, RANGED_CR_END_NEW, 1)
    print('[OK] RangedColumn: Image block 776698754 inserted')
else:
    print('[MISS] RangedColumn: CR block pattern not found')

# ─────────────────────────────────────────────────────────────────────────────
# 2. MagicColumn: fix component list and rename duplicate IDs
#    Old: 867405568 (CSF), 867405569 (VLG), 867405569 (dupe), 867405568 (dupe)
#    New: 867405568 (CSF), 867405569 (VLG), 867405573 (CR), 867405574 (Image)
# ─────────────────────────────────────────────────────────────────────────────

# Fix component list
OLD_COMP_LIST = (
    '  - component: {fileID: 867405568}\n'
    '  - component: {fileID: 867405569}\n'
    '  - component: {fileID: 867405569}\n'
    '  - component: {fileID: 867405568}\n'
)
NEW_COMP_LIST = (
    '  - component: {fileID: 867405568}\n'
    '  - component: {fileID: 867405569}\n'
    '  - component: {fileID: 867405573}\n'
    '  - component: {fileID: 867405574}\n'
)

if OLD_COMP_LIST in scene:
    scene = scene.replace(OLD_COMP_LIST, NEW_COMP_LIST, 1)
    print('[OK] MagicColumn: component list fixed')
else:
    print('[MISS] MagicColumn: component list pattern not found')

# Rename the duplicate CR block: --- !u!222 &867405568 → --- !u!222 &867405573
OLD_CR = '--- !u!222 &867405568\nCanvasRenderer:'
NEW_CR = '--- !u!222 &867405573\nCanvasRenderer:'
if OLD_CR in scene:
    scene = scene.replace(OLD_CR, NEW_CR, 1)
    print('[OK] MagicColumn: CR block ID 867405568 → 867405573')
else:
    print('[MISS] MagicColumn: CR block not found')

# Rename the duplicate Image block: second --- !u!114 &867405569 (Image) → 867405574
# The first 867405569 is VerticalLayoutGroup. The second one is the Image.
# Use the Image's m_EditorClassIdentifier to distinguish.
OLD_IMG_BLOCK = (
    '--- !u!114 &867405569\n'
    'MonoBehaviour:\n'
    '  m_ObjectHideFlags: 0\n'
    '  m_CorrespondingSourceObject: {fileID: 0}\n'
    '  m_PrefabInstance: {fileID: 0}\n'
    '  m_PrefabAsset: {fileID: 0}\n'
    '  m_GameObject: {fileID: 867405566}\n'
    '  m_Enabled: 1\n'
    '  m_EditorHideFlags: 0\n'
    f'  m_Script: {{fileID: 11500000, guid: {IMG_SCRIPT}, type: 3}}\n'
)
NEW_IMG_BLOCK = (
    '--- !u!114 &867405574\n'
    'MonoBehaviour:\n'
    '  m_ObjectHideFlags: 0\n'
    '  m_CorrespondingSourceObject: {fileID: 0}\n'
    '  m_PrefabInstance: {fileID: 0}\n'
    '  m_PrefabAsset: {fileID: 0}\n'
    '  m_GameObject: {fileID: 867405566}\n'
    '  m_Enabled: 1\n'
    '  m_EditorHideFlags: 0\n'
    f'  m_Script: {{fileID: 11500000, guid: {IMG_SCRIPT}, type: 3}}\n'
)
if OLD_IMG_BLOCK in scene:
    scene = scene.replace(OLD_IMG_BLOCK, NEW_IMG_BLOCK, 1)
    print('[OK] MagicColumn: Image block ID 867405569 → 867405574')
else:
    print('[MISS] MagicColumn: Image block pattern not found')

# Also fix m_GameObject reference in the duplicate CR: was already using 867405566 ✓
# Fix m_GameObject reference in CR block (now 867405573)
# already points to 867405566 ✓

with open(SCENE, 'w', encoding='utf-8') as f:
    f.write(scene)
print('Done.')
