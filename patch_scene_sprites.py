#!/usr/bin/env python3
"""Assign sprites to all remaining unassigned Image components in GameScene.unity."""

BTN_GUID      = '3f8822007efb4508a45c366d40ce81f0'  # button_normal.png  (border 6,6,6,6)
PANEL_GUID    = '5fd17c11c12c4529bfda2d1307dc35e7'  # panel_bg.png       (border 12,12,12,12)
SLOT_GUID     = 'e6682f51a8b442c3a1173d7b5a09f1fc'  # slot_bg.png        (no border)
EQUIP_GUID    = '3d7d0f36d746f4f611f3711671baa317'  # equip_slot_bg.png  (border 8,8,8,8)
SKILL_GUID    = '106df11c6171449a8e7dbb66af5022b4'  # skillbar_slot_bg.png (border 3,3,3,3)
BARBG_GUID    = 'f6badba808274ac6a08f211a41e8ed05'  # bar_bg.png         (no border)

SCENE = 'Assets/Scenes/GameScene.unity'

with open(SCENE, 'r', encoding='utf-8') as f:
    scene = f.read()

BLOCK_SUFFIX = (
    '  m_RaycastTarget: 1\n'
    '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
    '  m_Maskable: 1\n'
    '  m_OnCullStateChanged:\n'
    '    m_PersistentCalls:\n'
    '      m_Calls: []\n'
    '  m_Sprite: {fileID: 0}\n'
    '  m_Type: 0\n'
)

changes = 0

def replace_all(scene, old_color, new_color, new_sprite_guid, new_type):
    global changes
    old = f'  m_Color: {{{old_color}}}\n' + BLOCK_SUFFIX
    new_sprite = f'{{fileID: 21300000, guid: {new_sprite_guid}, type: 3}}'
    new = (f'  m_Color: {{{new_color}}}\n'
           '  m_RaycastTarget: 1\n'
           '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
           '  m_Maskable: 1\n'
           '  m_OnCullStateChanged:\n'
           '    m_PersistentCalls:\n'
           '      m_Calls: []\n'
           f'  m_Sprite: {new_sprite}\n'
           f'  m_Type: {new_type}\n')
    count = scene.count(old)
    result = scene.replace(old, new)
    changes += count
    return result, count

# ── Buttons (0.3, 0.3, 0.4, 1) → button_normal, Sliced ──────────────────────
scene, n = replace_all(scene,
    'r: 0.3, g: 0.3, b: 0.4, a: 1',
    'r: 1, g: 1, b: 1, a: 1',
    BTN_GUID, 1)
print(f'[{"OK" if n else "FAIL"}] Buttons (0.3,0.3,0.4): {n} change(s)')

# ── Panel backgrounds (0.12, 0.12, 0.18, 0.95) → panel_bg, Sliced ────────────
scene, n = replace_all(scene,
    'r: 0.12, g: 0.12, b: 0.18, a: 0.95',
    'r: 1, g: 1, b: 1, a: 1',
    PANEL_GUID, 1)
print(f'[{"OK" if n else "FAIL"}] Panels (0.12,0.12,0.18): {n} change(s)')

# ── Equipment slots (0.25, 0.25, 0.3, 1) → equip_slot_bg, Sliced ─────────────
scene, n = replace_all(scene,
    'r: 0.25, g: 0.25, b: 0.3, a: 1',
    'r: 1, g: 1, b: 1, a: 1',
    EQUIP_GUID, 1)
print(f'[{"OK" if n else "FAIL"}] EquipSlots (0.25,0.25,0.3): {n} change(s)')

# ── Skill nodes (0.15, 0.15, 0.2, 1) → skillbar_slot_bg, Sliced ─────────────
scene, n = replace_all(scene,
    'r: 0.15, g: 0.15, b: 0.2, a: 1',
    'r: 1, g: 1, b: 1, a: 1',
    SKILL_GUID, 1)
print(f'[{"OK" if n else "FAIL"}] SkillNodes (0.15,0.15,0.2): {n} change(s)')

# ── HUD bar tracks (0.15, 0.15, 0.15, 1) → bar_bg, Simple ───────────────────
scene, n = replace_all(scene,
    'r: 0.15, g: 0.15, b: 0.15, a: 1',
    'r: 1, g: 1, b: 1, a: 1',
    BARBG_GUID, 0)
print(f'[{"OK" if n else "FAIL"}] BarTracks (0.15,0.15,0.15): {n} change(s)')

with open(SCENE, 'w', encoding='utf-8') as f:
    f.write(scene)
print(f'\nTotal changes: {changes}')
print('Scene saved.')
