#!/usr/bin/env python3
"""Patch GameScene.unity: scrollable 10-col inventory grid + body-map equipment layout."""

with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    scene = f.read()

original_len = len(scene)

# ── 1. GridLayoutGroup: ConstraintCount 5->10, CellSize 64->54 ────────────────
old = '  m_CellSize: {x: 64, y: 64}\n  m_Spacing: {x: 4, y: 4}\n  m_Constraint: 1\n  m_ConstraintCount: 5'
new = '  m_CellSize: {x: 54, y: 54}\n  m_Spacing: {x: 4, y: 4}\n  m_Constraint: 1\n  m_ConstraintCount: 10'
assert old in scene, 'FAIL: GridLayoutGroup pattern not found'
scene = scene.replace(old, new)
print('[OK] GridLayoutGroup: 5->10 cols, 64->54 cell')

# ── 2. Grid RT: reparent + anchors for ScrollRect content ─────────────────────
old_grid_rt = (
    '  m_Children: []\n'
    '  m_Father: {fileID: 2070316351}\n'
    '  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
    '  m_AnchorMin: {x: 0.5, y: 0.5}\n'
    '  m_AnchorMax: {x: 0.5, y: 0.5}\n'
    '  m_AnchoredPosition: {x: -160, y: 60}\n'
    '  m_SizeDelta: {x: 680, y: 560}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &2067993275'
)
new_grid_rt = (
    '  m_Children: []\n'
    '  m_Father: {fileID: 2900002001}\n'
    '  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
    '  m_AnchorMin: {x: 0, y: 1}\n'
    '  m_AnchorMax: {x: 1, y: 1}\n'
    '  m_AnchoredPosition: {x: 0, y: 0}\n'
    '  m_SizeDelta: {x: 0, y: 460}\n'
    '  m_Pivot: {x: 0.5, y: 1}\n'
    '--- !u!114 &2067993275'
)
assert old_grid_rt in scene, 'FAIL: Grid RT pattern not found'
scene = scene.replace(old_grid_rt, new_grid_rt)
print('[OK] Grid RT: reparented to Viewport, anchors updated')

# ── 3. Grid GO: add ContentSizeFitter component reference ─────────────────────
old_grid_go = (
    '  m_Component:\n'
    '  - component: {fileID: 2067993274}\n'
    '  - component: {fileID: 2067993275}\n'
    '  m_Layer: 0\n'
    '  m_Name: Grid'
)
new_grid_go = (
    '  m_Component:\n'
    '  - component: {fileID: 2067993274}\n'
    '  - component: {fileID: 2067993275}\n'
    '  - component: {fileID: 2900003001}\n'
    '  m_Layer: 0\n'
    '  m_Name: Grid'
)
assert old_grid_go in scene, 'FAIL: Grid GO pattern not found'
scene = scene.replace(old_grid_go, new_grid_go)
print('[OK] Grid GO: ContentSizeFitter component added')

# ── 4. InventoryPanel RT: replace Grid RT child with ScrollView RT ─────────────
old_inv_children = (
    '  m_Children:\n'
    '  - {fileID: 693807383}\n'
    '  - {fileID: 209430242}\n'
    '  - {fileID: 2067993274}\n'
    '  - {fileID: 937304480}\n'
)
new_inv_children = (
    '  m_Children:\n'
    '  - {fileID: 693807383}\n'
    '  - {fileID: 209430242}\n'
    '  - {fileID: 2900001001}\n'
    '  - {fileID: 937304480}\n'
)
assert old_inv_children in scene, 'FAIL: InventoryPanel children pattern not found'
scene = scene.replace(old_inv_children, new_inv_children)
print('[OK] InventoryPanel children: Grid->ScrollView')

# ── 5. Equipment slots: body-map positions ────────────────────────────────────
# WeaponSlot RT (937304480): pos(380,200) size(180x80) -> pos(210,220) size(135x90)
old_weapon = (
    '  m_AnchoredPosition: {x: 380, y: 200}\n'
    '  m_SizeDelta: {x: 180, y: 80}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &937304481'
)
new_weapon = (
    '  m_AnchoredPosition: {x: 210, y: 220}\n'
    '  m_SizeDelta: {x: 135, y: 90}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &937304481'
)
assert old_weapon in scene, 'FAIL: WeaponSlot RT not found'
scene = scene.replace(old_weapon, new_weapon)
print('[OK] WeaponSlot: pos(210,220) size(135x90)')

# HelmetSlot RT (853866930): pos(380,110) size(180x80) -> pos(310,315) size(290x70)
old_helmet = (
    '  m_AnchoredPosition: {x: 380, y: 110}\n'
    '  m_SizeDelta: {x: 180, y: 80}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &853866931'
)
new_helmet = (
    '  m_AnchoredPosition: {x: 310, y: 315}\n'
    '  m_SizeDelta: {x: 290, y: 70}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &853866931'
)
assert old_helmet in scene, 'FAIL: HelmetSlot RT not found'
scene = scene.replace(old_helmet, new_helmet)
print('[OK] HelmetSlot: pos(310,315) size(290x70)')

# ArmorSlot RT (303150477): pos(380,20) size(180x80) -> pos(405,220) size(135x90)
old_armor = (
    '  m_AnchoredPosition: {x: 380, y: 20}\n'
    '  m_SizeDelta: {x: 180, y: 80}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &303150478'
)
new_armor = (
    '  m_AnchoredPosition: {x: 405, y: 220}\n'
    '  m_SizeDelta: {x: 135, y: 90}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &303150478'
)
assert old_armor in scene, 'FAIL: ArmorSlot RT not found'
scene = scene.replace(old_armor, new_armor)
print('[OK] ArmorSlot: pos(405,220) size(135x90)')

# BootsSlot RT (1685891928): pos(380,-70) size(180x80) -> pos(310,120) size(290x70)
old_boots = (
    '  m_AnchoredPosition: {x: 380, y: -70}\n'
    '  m_SizeDelta: {x: 180, y: 80}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &1685891929'
)
new_boots = (
    '  m_AnchoredPosition: {x: 310, y: 120}\n'
    '  m_SizeDelta: {x: 290, y: 70}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &1685891929'
)
assert old_boots in scene, 'FAIL: BootsSlot RT not found'
scene = scene.replace(old_boots, new_boots)
print('[OK] BootsSlot: pos(310,120) size(290x70)')

# AccessorySlot RT (1232950548): pos(380,-160) size(180x80) -> pos(310,25) size(290x60)
old_acc = (
    '  m_AnchoredPosition: {x: 380, y: -160}\n'
    '  m_SizeDelta: {x: 180, y: 80}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &1232950549'
)
new_acc = (
    '  m_AnchoredPosition: {x: 310, y: 25}\n'
    '  m_SizeDelta: {x: 290, y: 60}\n'
    '  m_Pivot: {x: 0.5, y: 0.5}\n'
    '--- !u!114 &1232950549'
)
assert old_acc in scene, 'FAIL: AccessorySlot RT not found'
scene = scene.replace(old_acc, new_acc)
print('[OK] AccessorySlot: pos(310,25) size(290x60)')

# ── 6. Insert new YAML blocks before InventoryPanel block ─────────────────────
# ScrollView GO/RT/ScrollRect/CanvasRenderer
# Viewport GO/RT/RectMask2D/CanvasRenderer
# ContentSizeFitter MonoBehaviour for Grid

new_blocks = '''\
--- !u!1 &2900001000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2900001001}
  - component: {fileID: 2900001002}
  - component: {fileID: 2900001003}
  m_Layer: 0
  m_Name: ScrollView
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &2900001001
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900001000}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 2900002001}
  m_Father: {fileID: 2070316351}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: -155, y: 144}
  m_SizeDelta: {x: 590, y: 480}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &2900001002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900001000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 1aa08ab6e0800fa44ae55d278d1423e3, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ScrollRect
  m_Content: {fileID: 2067993274}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 10
  m_Viewport: {fileID: 2900002001}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: 0}
  m_HorizontalScrollbarVisibility: 0
  m_VerticalScrollbarVisibility: 0
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
--- !u!222 &2900001003
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900001000}
  m_CullTransparentMesh: 1
--- !u!1 &2900002000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2900002001}
  - component: {fileID: 2900002002}
  - component: {fileID: 2900002003}
  m_Layer: 0
  m_Name: Viewport
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &2900002001
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900002000}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 2067993274}
  m_Father: {fileID: 2900001001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 1}
--- !u!114 &2900002002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900002000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3312d7739989d2b4e91e6319e9a96d76, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.RectMask2D
  m_Padding: {x: 0, y: 0, z: 0, w: 0}
  m_Softness: {x: 0, y: 0}
--- !u!222 &2900002003
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2900002000}
  m_CullTransparentMesh: 1
--- !u!114 &2900003001
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2067993273}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter
  m_HorizontalFit: 0
  m_VerticalFit: 2
'''

marker = '--- !u!1 &2070316350\nGameObject:'
assert marker in scene, 'FAIL: InventoryPanel GO marker not found'
scene = scene.replace(marker, new_blocks + marker)
print('[OK] Inserted ScrollView + Viewport + ContentSizeFitter blocks')

# ── verify ────────────────────────────────────────────────────────────────────
assert len(scene) > original_len, 'ERROR: scene shrank?'
print(f'Scene size: {original_len} -> {len(scene)} (+{len(scene)-original_len})')

with open('Assets/Scenes/GameScene.unity', 'w', encoding='utf-8') as f:
    f.write(scene)
print('DONE: GameScene.unity patched')
