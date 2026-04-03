#!/usr/bin/env python3
"""Apply sprite assignments to UI prefabs."""
import os

BTN_GUID  = '3f8822007efb4508a45c366d40ce81f0'  # button_normal.png
SLOT_GUID = 'e6682f51a8b442c3a1173d7b5a09f1fc'  # slot_bg.png

def patch(path, replacements):
    """Apply list of (old, new) string replacements to a file."""
    with open(path, 'r', encoding='utf-8') as f:
        c = f.read()
    changed = 0
    for old, new in replacements:
        if old in c:
            c = c.replace(old, new)
            changed += 1
        else:
            print(f'  [MISS] pattern not found in {os.path.basename(path)}: {old[:50]}...')
    if changed > 0:
        with open(path, 'w', encoding='utf-8') as f:
            f.write(c)
    return changed

# ── QuestEntry.prefab ─────────────────────────────────────────────────────────
n = patch('Assets/Prefabs/UI/QuestEntry.prefab', [
    ('  m_Color: {r: 0.2, g: 0.2, b: 0.25, a: 0.8}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {SLOT_GUID}, type: 3}}\n'
     '  m_Type: 0\n'),
])
print(f'[{"OK" if n else "FAIL"}] QuestEntry: {n} change(s)')

# ── RecipeItem.prefab ─────────────────────────────────────────────────────────
n = patch('Assets/Prefabs/UI/RecipeItem.prefab', [
    # Icon background
    ('  m_Color: {r: 0.2, g: 0.2, b: 0.25, a: 0.8}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {SLOT_GUID}, type: 3}}\n'
     '  m_Type: 0\n'),
    # Craft button
    ('  m_Color: {r: 0.3, g: 0.3, b: 0.4, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {BTN_GUID}, type: 3}}\n'
     '  m_Type: 1\n'),
])
print(f'[{"OK" if n else "FAIL"}] RecipeItem: {n} change(s)')

# ── ShopItem.prefab ───────────────────────────────────────────────────────────
n = patch('Assets/Prefabs/UI/ShopItem.prefab', [
    # Buy button
    ('  m_Color: {r: 0.3, g: 0.3, b: 0.4, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n'
     '  m_PreserveAspect: 0\n'
     '  m_FillCenter: 1\n'
     '  m_FillMethod: 4\n'
     '  m_FillAmount: 1\n'
     '  m_FillClockwise: 1\n'
     '  m_FillOrigin: 0\n'
     '  m_UseSpriteMesh: 0\n'
     '  m_PixelsPerUnitMultiplier: 1\n'
     '--- !u!114 &4439877880911135170',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {BTN_GUID}, type: 3}}\n'
     '  m_Type: 1\n'
     '  m_PreserveAspect: 0\n'
     '  m_FillCenter: 1\n'
     '  m_FillMethod: 4\n'
     '  m_FillAmount: 1\n'
     '  m_FillClockwise: 1\n'
     '  m_FillOrigin: 0\n'
     '  m_UseSpriteMesh: 0\n'
     '  m_PixelsPerUnitMultiplier: 1\n'
     '--- !u!114 &4439877880911135170'),
    # Item icon slot background
    ('  m_Color: {r: 0.2, g: 0.2, b: 0.25, a: 0.8}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {SLOT_GUID}, type: 3}}\n'
     '  m_Type: 0\n'),
])
print(f'[{"OK" if n else "FAIL"}] ShopItem: {n} change(s)')

# ── SkillRow.prefab ───────────────────────────────────────────────────────────
# Skill icon background
n = patch('Assets/Prefabs/UI/SkillRow.prefab', [
    ('  m_Color: {r: 0.2, g: 0.2, b: 0.3, a: 0.8}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n'
     '  m_PreserveAspect: 0\n'
     '  m_FillCenter: 1\n'
     '  m_FillMethod: 4\n'
     '  m_FillAmount: 1\n'
     '  m_FillClockwise: 1\n'
     '  m_FillOrigin: 0\n'
     '  m_UseSpriteMesh: 0\n'
     '  m_PixelsPerUnitMultiplier: 1\n'
     '--- !u!114 &4013979409469326565',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {SLOT_GUID}, type: 3}}\n'
     '  m_Type: 0\n'
     '  m_PreserveAspect: 0\n'
     '  m_FillCenter: 1\n'
     '  m_FillMethod: 4\n'
     '  m_FillAmount: 1\n'
     '  m_FillClockwise: 1\n'
     '  m_FillOrigin: 0\n'
     '  m_UseSpriteMesh: 0\n'
     '  m_PixelsPerUnitMultiplier: 1\n'
     '--- !u!114 &4013979409469326565'),
    # Skill row buttons (level/equip)
    ('  m_Color: {r: 0.3, g: 0.3, b: 0.4, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     '  m_Sprite: {fileID: 0}\n'
     '  m_Type: 0\n',
     '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n'
     '  m_RaycastTarget: 1\n'
     '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
     '  m_Maskable: 1\n'
     '  m_OnCullStateChanged:\n'
     '    m_PersistentCalls:\n'
     '      m_Calls: []\n'
     f'  m_Sprite: {{fileID: 21300000, guid: {BTN_GUID}, type: 3}}\n'
     '  m_Type: 1\n'),
])
print(f'[{"OK" if n else "FAIL"}] SkillRow: {n} change(s)')

print('Done.')
