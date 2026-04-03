#!/usr/bin/env python3
"""Add tree background images to SkillTree column GOs."""

MELEE_GUID   = '29277c6ebc4485d4caa3afa6836b15ee'  # tree_bg_melee.png
RANGED_GUID  = '9977df407e633284885cae840555b1b5'  # tree_bg_ranged.png
MAGIC_GUID   = 'f7a029054f2648c47a36e73849bcc637'  # tree_bg_magic.png
IMG_SCRIPT   = 'fe87c0e1cc204ed48ad3b37840f39efc'   # UI.Image GUID

SCENE = 'Assets/Scenes/GameScene.unity'

with open(SCENE, 'r', encoding='utf-8') as f:
    scene = f.read()

def add_image_to_go(scene, go_id, rt_id, new_cr_id, new_img_id, sprite_guid):
    """Add CanvasRenderer + Image component to an existing GO."""
    # 1. Find the component list and add new components
    old_go_start = f'--- !u!1 &{go_id}\nGameObject:'
    # Find the m_Component section - add our new components
    # The component list ends at m_Layer:
    old_comp_line = f'  - component: {{fileID: {rt_id}}}'
    new_comp_line = (f'  - component: {{fileID: {rt_id}}}\n'
                     f'  - component: {{fileID: {new_cr_id}}}\n'
                     f'  - component: {{fileID: {new_img_id}}}')

    n = scene.count(old_comp_line)
    if n == 0:
        print(f'  [MISS] Could not find RT component {rt_id}')
        return scene, False
    elif n > 1:
        print(f'  [WARN] Multiple matches for RT {rt_id}')

    scene = scene.replace(old_comp_line, new_comp_line, 1)

    # 2. After the existing last component of this GO, insert CanvasRenderer + Image
    # Find the block that starts with "--- !u!114 &<last_known_comp>" and insert after it
    # Actually, insert before the NEXT GameObject or component of another GO
    # We'll insert right after the VerticalLayoutGroup component block
    # Find "m_GameObject: {fileID: <go_id>}" in the last MonoBehaviour before the next --- block

    # Find insertion point: after last component belonging to this GO
    # Strategy: find "  m_ReverseArrangement: 0\n" which ends the VLG component for columns
    # For MeleeColumn: &121467126, ends after m_ReverseArrangement

    # Instead, find the pattern "m_ReverseArrangement: 0\n--- !u!1" and insert between them
    insert_marker = '  m_ReverseArrangement: 0\n--- !u!1 &121940102'
    if insert_marker in scene and go_id == '121467123':
        canvas_renderer = (
            f'--- !u!222 &{new_cr_id}\n'
            f'CanvasRenderer:\n'
            f'  m_ObjectHideFlags: 0\n'
            f'  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n'
            f'  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {go_id}}}\n'
            f'  m_CullTransparentMesh: 1\n'
        )
        image_comp = (
            f'--- !u!114 &{new_img_id}\n'
            f'MonoBehaviour:\n'
            f'  m_ObjectHideFlags: 0\n'
            f'  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n'
            f'  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {go_id}}}\n'
            f'  m_Enabled: 1\n'
            f'  m_EditorHideFlags: 0\n'
            f'  m_Script: {{fileID: 11500000, guid: {IMG_SCRIPT}, type: 3}}\n'
            f'  m_Name: \n'
            f'  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
            f'  m_Material: {{fileID: 0}}\n'
            f'  m_Color: {{r: 1, g: 1, b: 1, a: 0.15}}\n'
            f'  m_RaycastTarget: 0\n'
            f'  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n'
            f'  m_Maskable: 1\n'
            f'  m_OnCullStateChanged:\n'
            f'    m_PersistentCalls:\n'
            f'      m_Calls: []\n'
            f'  m_Sprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}\n'
            f'  m_Type: 0\n'
            f'  m_PreserveAspect: 0\n'
            f'  m_FillCenter: 1\n'
            f'  m_FillMethod: 4\n'
            f'  m_FillAmount: 1\n'
            f'  m_FillClockwise: 1\n'
            f'  m_FillOrigin: 0\n'
            f'  m_UseSpriteMesh: 0\n'
            f'  m_PixelsPerUnitMultiplier: 1\n'
        )
        new_block = canvas_renderer + image_comp + '--- !u!1 &121940102'
        scene = scene.replace(insert_marker, f'  m_ReverseArrangement: 0\n' + new_block)
        return scene, True

    return scene, False

# For simplicity, let me use a different approach:
# Find the exact end of each column's last component and insert there

def find_go_component_block_end(scene, go_id):
    """Find the position right after all components of a GO."""
    import re
    # Find all MonoBehaviour blocks for this GO
    pattern = rf'(--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: \{{fileID: {go_id}\}}.*?)(?=\n--- !u!)'
    matches = list(re.finditer(pattern, scene, re.DOTALL))
    if not matches:
        return -1
    last = matches[-1]
    return last.end()

import re

# Column definitions: (go_id, rt_id, cr_id, img_id, sprite_guid, name)
columns = [
    ('121467123', '121467124', '121467127', '121467128', MELEE_GUID,  'MeleeColumn'),
    ('776698739', '776698740', '776698741', '776698742', RANGED_GUID, 'RangedColumn'),
    ('867405566', '867405567', '867405568', '867405569', MAGIC_GUID,  'MagicColumn'),
]

for go_id, rt_id, cr_id, img_id, sprite_guid, col_name in columns:
    # Add components to GO component list
    # Find the first component reference (always the RT)
    # MeleeColumn: first comp is {fileID: 121467124}
    old_first_comp = f'  - component: {{fileID: {rt_id}}}\n'
    new_first_comp = (f'  - component: {{fileID: {rt_id}}}\n'
                      f'  - component: {{fileID: {cr_id}}}\n'
                      f'  - component: {{fileID: {img_id}}}\n')

    if old_first_comp not in scene:
        print(f'  [MISS] {col_name}: RT component ref not found')
        continue
    scene = scene.replace(old_first_comp, new_first_comp, 1)

    # Find end of last component for this GO and insert CanvasRenderer + Image
    # Find the last MonoBehaviour for this GO
    last_mb = None
    for m in re.finditer(r'--- !u!114 &\d+\nMonoBehaviour:', scene):
        block_start = m.start()
        # Check if this block references our GO
        snippet = scene[block_start:block_start+300]
        if f'm_GameObject: {{fileID: {go_id}}}' in snippet:
            last_mb = m

    if last_mb is None:
        print(f'  [MISS] {col_name}: no MonoBehaviour found')
        continue

    # Find end of this block (next --- !u!)
    rest = scene[last_mb.start():]
    next_section = re.search(r'\n--- !u!', rest[10:])
    if not next_section:
        print(f'  [MISS] {col_name}: could not find next section')
        continue

    insert_pos = last_mb.start() + 10 + next_section.start() + 1  # after the newline before ---

    new_components = (
        f'--- !u!222 &{cr_id}\n'
        f'CanvasRenderer:\n'
        f'  m_ObjectHideFlags: 0\n'
        f'  m_CorrespondingSourceObject: {{fileID: 0}}\n'
        f'  m_PrefabInstance: {{fileID: 0}}\n'
        f'  m_PrefabAsset: {{fileID: 0}}\n'
        f'  m_GameObject: {{fileID: {go_id}}}\n'
        f'  m_CullTransparentMesh: 1\n'
        f'--- !u!114 &{img_id}\n'
        f'MonoBehaviour:\n'
        f'  m_ObjectHideFlags: 0\n'
        f'  m_CorrespondingSourceObject: {{fileID: 0}}\n'
        f'  m_PrefabInstance: {{fileID: 0}}\n'
        f'  m_PrefabAsset: {{fileID: 0}}\n'
        f'  m_GameObject: {{fileID: {go_id}}}\n'
        f'  m_Enabled: 1\n'
        f'  m_EditorHideFlags: 0\n'
        f'  m_Script: {{fileID: 11500000, guid: {IMG_SCRIPT}, type: 3}}\n'
        f'  m_Name: \n'
        f'  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
        f'  m_Material: {{fileID: 0}}\n'
        f'  m_Color: {{r: 1, g: 1, b: 1, a: 0.2}}\n'
        f'  m_RaycastTarget: 0\n'
        f'  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n'
        f'  m_Maskable: 1\n'
        f'  m_OnCullStateChanged:\n'
        f'    m_PersistentCalls:\n'
        f'      m_Calls: []\n'
        f'  m_Sprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}\n'
        f'  m_Type: 0\n'
        f'  m_PreserveAspect: 1\n'
        f'  m_FillCenter: 1\n'
        f'  m_FillMethod: 4\n'
        f'  m_FillAmount: 1\n'
        f'  m_FillClockwise: 1\n'
        f'  m_FillOrigin: 0\n'
        f'  m_UseSpriteMesh: 0\n'
        f'  m_PixelsPerUnitMultiplier: 1\n'
    )

    scene = scene[:insert_pos] + new_components + scene[insert_pos:]
    print(f'[OK] {col_name}: tree background added (cr={cr_id}, img={img_id})')

with open(SCENE, 'w', encoding='utf-8') as f:
    f.write(scene)
print('Done.')
