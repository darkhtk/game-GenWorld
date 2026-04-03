#!/usr/bin/env python3
"""Apply aesthetic improvements to GameScene.unity panels."""

PANEL_BG_GUID   = '5fd17c11c12c4529bfda2d1307dc35e7'  # panel_bg.png
DIALOG_BG_GUID  = '64d83ad0d9b940dba0bdc4ec6535b4a2'  # dialog_panel.png

with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    scene = f.read()

# ── InventoryPanel Image (&2070316352) ────────────────────────────────────────
old = ('--- !u!114 &2070316352\n'
       'MonoBehaviour:\n'
       '  m_ObjectHideFlags: 0\n'
       '  m_CorrespondingSourceObject: {fileID: 0}\n'
       '  m_PrefabInstance: {fileID: 0}\n'
       '  m_PrefabAsset: {fileID: 0}\n'
       '  m_GameObject: {fileID: 2070316350}\n'
       '  m_Enabled: 1\n'
       '  m_EditorHideFlags: 0\n'
       '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
       '  m_Name: \n'
       '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
       '  m_Material: {fileID: 0}\n'
       '  m_Color: {r: 0.12, g: 0.12, b: 0.18, a: 0.95}\n'
       '  m_RaycastTarget: 1\n'
       '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
       '  m_Maskable: 1\n'
       '  m_OnCullStateChanged:\n'
       '    m_PersistentCalls:\n'
       '      m_Calls: []\n'
       '  m_Sprite: {fileID: 0}\n'
       '  m_Type: 0')
new = ('--- !u!114 &2070316352\n'
       'MonoBehaviour:\n'
       '  m_ObjectHideFlags: 0\n'
       '  m_CorrespondingSourceObject: {fileID: 0}\n'
       '  m_PrefabInstance: {fileID: 0}\n'
       '  m_PrefabAsset: {fileID: 0}\n'
       '  m_GameObject: {fileID: 2070316350}\n'
       '  m_Enabled: 1\n'
       '  m_EditorHideFlags: 0\n'
       '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
       '  m_Name: \n'
       '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
       '  m_Material: {fileID: 0}\n'
       '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
       '  m_RaycastTarget: 1\n'
       '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
       '  m_Maskable: 1\n'
       '  m_OnCullStateChanged:\n'
       '    m_PersistentCalls:\n'
       '      m_Calls: []\n'
       f'  m_Sprite: {{fileID: 21300000, guid: {PANEL_BG_GUID}, type: 3}}\n'
       '  m_Type: 1')
assert old in scene, 'InventoryPanel Image not found'
scene = scene.replace(old, new)
print('[OK] InventoryPanel: panel_bg sprite assigned (9-slice)')

# ── DialoguePanel Image (&478752852) ─────────────────────────────────────────
old2 = ('  m_GameObject: {fileID: 478752850}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
        '  m_Name: \n'
        '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
        '  m_Material: {fileID: 0}\n'
        '  m_Color: {r: 0.12, g: 0.12, b: 0.18, a: 0.95}\n'
        '  m_RaycastTarget: 1\n'
        '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
        '  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n'
        '    m_PersistentCalls:\n'
        '      m_Calls: []\n'
        '  m_Sprite: {fileID: 0}\n'
        '  m_Type: 0')
new2 = ('  m_GameObject: {fileID: 478752850}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
        '  m_Name: \n'
        '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
        '  m_Material: {fileID: 0}\n'
        '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
        '  m_RaycastTarget: 1\n'
        '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
        '  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n'
        '    m_PersistentCalls:\n'
        '      m_Calls: []\n'
        f'  m_Sprite: {{fileID: 21300000, guid: {DIALOG_BG_GUID}, type: 3}}\n'
        '  m_Type: 1')
assert old2 in scene, 'DialoguePanel Image not found'
scene = scene.replace(old2, new2)
print('[OK] DialoguePanel: dialog_panel sprite assigned (9-slice)')

# ── SkillTreePanel Image (&1253140598) ────────────────────────────────────────
old3 = ('  m_GameObject: {fileID: 1253140596}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
        '  m_Name: \n'
        '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
        '  m_Material: {fileID: 0}\n'
        '  m_Color: {r: 0.12, g: 0.12, b: 0.18, a: 0.95}\n'
        '  m_RaycastTarget: 1\n'
        '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
        '  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n'
        '    m_PersistentCalls:\n'
        '      m_Calls: []\n'
        '  m_Sprite: {fileID: 0}\n'
        '  m_Type: 0')
new3 = ('  m_GameObject: {fileID: 1253140596}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}\n'
        '  m_Name: \n'
        '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
        '  m_Material: {fileID: 0}\n'
        '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
        '  m_RaycastTarget: 1\n'
        '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
        '  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n'
        '    m_PersistentCalls:\n'
        '      m_Calls: []\n'
        f'  m_Sprite: {{fileID: 21300000, guid: {PANEL_BG_GUID}, type: 3}}\n'
        '  m_Type: 1')
assert old3 in scene, 'SkillTreePanel Image not found'
scene = scene.replace(old3, new3)
print('[OK] SkillTreePanel: panel_bg sprite assigned (9-slice)')

with open('Assets/Scenes/GameScene.unity', 'w', encoding='utf-8') as f:
    f.write(scene)
print('Scene saved.')
