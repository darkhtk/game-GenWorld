"""
Deep audit: check all sprite metas referenced in GameScene.unity + SkillRow.prefab.
Also verify InventoryPanel + SkillTreePanel layout via direct RT lookup.
"""
import re, os, struct

# ── PNG size reader ───────────────────────────────────────────────────────────
def read_png_size(path):
    try:
        with open(path, 'rb') as f:
            if f.read(8) != b'\x89PNG\r\n\x1a\n':
                return None, None
            f.read(4)
            if f.read(4) != b'IHDR':
                return None, None
            return struct.unpack('>I', f.read(4))[0], struct.unpack('>I', f.read(4))[0]
    except:
        return None, None

# ── build guid -> meta_path index ────────────────────────────────────────────
def build_guid_map(search_root='Assets'):
    gmap = {}
    for root, dirs, files in os.walk(search_root):
        for fn in files:
            if not fn.endswith('.meta'):
                continue
            p = os.path.join(root, fn)
            try:
                with open(p) as f:
                    f.readline()
                    line2 = f.readline()
                m = re.match(r'guid: ([0-9a-f]+)', line2.strip())
                if m:
                    gmap[m.group(1)] = p
            except:
                pass
    return gmap

# ── sprite meta checker ───────────────────────────────────────────────────────
def check_sprite_meta(meta_path, guid_map):
    issues = []
    try:
        with open(meta_path) as f:
            c = f.read()
    except:
        return ['cannot read meta']

    if 'textureType: 8' not in c:
        return []  # not a sprite

    if 'filterMode: 0' not in c:
        issues.append('filterMode!=0 (need Point)')

    if 'spritePixelsToUnits: 32' not in c:
        issues.append('PPU!=32')

    # Check default platform compression = 0 (None)
    m = re.search(r'buildTarget: DefaultTexturePlatform.*?textureCompression: (\d+)', c, re.DOTALL)
    if m and m.group(1) != '0':
        issues.append('DefaultPlatform compression=' + m.group(1) + ' (need 0=None)')

    # For single sprites: rect should match PNG dimensions
    if 'spriteMode: 2' not in c:  # skip spritesheets
        rm = re.search(r'sprites:[\s\S]*?rect:\s+serializedVersion: \d+\s+x: ([\d\.]+)\s+y: ([\d\.]+)\s+width: ([\d\.]+)\s+height: ([\d\.]+)', c)
        if rm:
            rx, ry = float(rm.group(1)), float(rm.group(2))
            rw, rh = float(rm.group(3)), float(rm.group(4))
            png_path = meta_path[:-5]
            if os.path.exists(png_path):
                pw, ph = read_png_size(png_path)
                if pw and ph:
                    if abs(rx) > 0.5 or abs(ry) > 0.5:
                        issues.append('rect origin ({:.0f},{:.0f}) != (0,0)'.format(rx, ry))
                    if abs(rw - pw) > 0.5 or abs(rh - ph) > 0.5:
                        issues.append('rect {:.0f}x{:.0f} != PNG {}x{}'.format(rw, rh, pw, ph))

    return issues

# ── RT layout checker (same as audit.py) ─────────────────────────────────────
def parse_rts(text):
    rts = {}
    lines = text.split('\n')
    i = 0
    while i < len(lines):
        m = re.match(r'^--- !u!224 &(\d+)', lines[i])
        if m:
            fid = m.group(1)
            data = {'pos': None, 'size': None, 'father': '0', 'children': [], 'go_fid': None, 'in_children': False}
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                l = lines[i]
                gm = re.match(r'\s+m_GameObject: \{fileID: (\d+)\}', l)
                if gm:
                    data['go_fid'] = gm.group(1)
                fm = re.match(r'\s+m_Father: \{fileID: (\d+)\}', l)
                if fm:
                    data['father'] = fm.group(1)
                    data['in_children'] = False
                if 'm_Children:' in l:
                    data['in_children'] = True
                elif data['in_children']:
                    cm = re.match(r'\s+- \{fileID: (\d+)\}', l)
                    if cm:
                        data['children'].append(cm.group(1))
                    elif re.match(r'\s+m_\w+:', l):
                        data['in_children'] = False
                pm = re.match(r'\s+m_AnchoredPosition: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}', l)
                if pm:
                    data['pos'] = (float(pm.group(1)), float(pm.group(2)))
                sm = re.match(r'\s+m_SizeDelta: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}', l)
                if sm:
                    data['size'] = (float(sm.group(1)), float(sm.group(2)))
                i += 1
            if data['pos'] and data['size']:
                rts[fid] = data
        else:
            i += 1
    return rts

def parse_go_names(text):
    gos = {}
    lines = text.split('\n')
    i = 0
    while i < len(lines):
        m = re.match(r'^--- !u!1 &(\d+)', lines[i])
        if m:
            go_fid = m.group(1)
            name = None
            active = True
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                l = lines[i]
                nm = re.match(r'\s+m_Name: (\S+)', l)
                if nm:
                    name = nm.group(1)
                am = re.match(r'\s+m_IsActive: (\d)', l)
                if am:
                    active = am.group(1) == '1'
                i += 1
            if name:
                gos[go_fid] = {'name': name, 'active': active}
        else:
            i += 1
    return gos

# ── main ──────────────────────────────────────────────────────────────────────
print('Loading scene...')
with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    scene = f.read()

print('Building GUID map...')
guid_map = build_guid_map('Assets')
print('  {} .meta files indexed'.format(len(guid_map)))

rts = parse_rts(scene)
gos = parse_go_names(scene)
go_to_rt = {d['go_fid']: fid for fid, d in rts.items() if d['go_fid']}

def find_rt_by_name(name):
    for go_fid, go in gos.items():
        if go['name'] == name:
            return go_to_rt.get(go_fid)
    return None

def rt_name(fid):
    d = rts.get(fid, {})
    gf = d.get('go_fid')
    return gos[gf]['name'] if gf and gf in gos else 'RT#' + fid

def is_active(fid):
    d = rts.get(fid, {})
    gf = d.get('go_fid')
    return gos[gf]['active'] if gf and gf in gos else True

OVERLAY_NAMES = {'DragIcon', 'Tooltip', 'DragSlot', 'DragGhost', 'DragItem'}

def get_all_descendants(panel_rt_fid):
    visited = set()
    q = [panel_rt_fid]
    while q:
        cur = q.pop()
        if cur in visited:
            continue
        visited.add(cur)
        d = rts.get(cur)
        if d:
            q.extend(d['children'])
    return visited

def bounds(fid):
    d = rts.get(fid)
    if not d:
        return None
    px, py = d['pos']
    sw, sh = d['size']
    return px - sw/2, py - sh/2, px + sw/2, py + sh/2

def check_panel_layout(panel_name, expected_w, expected_h):
    issues = []
    fid = find_rt_by_name(panel_name)
    if not fid:
        print('[SKIP] ' + panel_name)
        return []
    d = rts[fid]
    w, h = d['size']
    ok = abs(w - expected_w) < 1 and abs(h - expected_h) < 1
    print('[' + ('PASS' if ok else 'FAIL') + '] ' + panel_name + ' ' + str(int(w)) + 'x' + str(int(h)))
    if not ok:
        issues.append(panel_name + ' size ' + str(int(w)) + 'x' + str(int(h)))

    children = d.get('children', [])
    pb = bounds(fid)
    child_info = []
    for c in children:
        if not is_active(c) or rt_name(c) in OVERLAY_NAMES:
            continue
        cb = bounds(c)
        if not cb:
            continue
        cl, cbottom, cr, ct = cb
        if cl < pb[0]-2 or cbottom < pb[1]-2 or cr > pb[2]+2 or ct > pb[3]+2:
            cd = rts[c]
            print('  [FAIL-OOB] {} pos({:.0f},{:.0f}) size({:.0f}x{:.0f}) -> [{:.0f},{:.0f},{:.0f},{:.0f}]'.format(
                rt_name(c), cd['pos'][0], cd['pos'][1], cd['size'][0], cd['size'][1], cl, cbottom, cr, ct))
            issues.append('OOB ' + rt_name(c))
        child_info.append((c, cb, rt_name(c)))

    for i in range(len(child_info)):
        for j in range(i+1, len(child_info)):
            fa, ba, na = child_info[i]
            fb, bb, nb = child_info[j]
            ol = min(ba[2], bb[2]) - max(ba[0], bb[0])
            ob = min(ba[3], bb[3]) - max(ba[1], bb[1])
            if ol > 2 and ob > 2:
                print('  [FAIL-OVL] {} x {}: {}x{}px'.format(na, nb, int(ol), int(ob)))
                issues.append('OVL {}/{}'.format(na, nb))

    if not issues:
        print('  [PASS] {} direct children OK'.format(len(children)))
    return issues

# ── 1. Layout checks ──────────────────────────────────────────────────────────
print()
print('=' * 55)
print('LAYOUT: InventoryPanel + SkillTreePanel + DialoguePanel')
print('=' * 55)
all_issues = []
all_issues += check_panel_layout('InventoryPanel', 1000, 1000)
all_issues += check_panel_layout('SkillTreePanel', 1100, 720)
for col in ['MeleeColumn', 'RangedColumn', 'MagicColumn']:
    fid = find_rt_by_name(col)
    if fid:
        d = rts[fid]
        print('  {} pos({:.0f},{:.0f}) size({:.0f}x{:.0f})'.format(
            col, d['pos'][0], d['pos'][1], d['size'][0], d['size'][1]))
all_issues += check_panel_layout('DialoguePanel', 1000, 1000)

# ── 2. Collect all sprite GUIDs from scene + prefabs ─────────────────────────
print()
print('=' * 55)
print('SPRITE GUID SCAN (scene + prefabs)')
print('=' * 55)

sprite_ref_pat = re.compile(r'm_Sprite: \{fileID: \d+, guid: ([0-9a-f]+), type: \d+\}')
bg_sprite_pat  = re.compile(r'm_OverrideSprite: \{fileID: \d+, guid: ([0-9a-f]+), type: \d+\}')

all_guids = set()

# From scene
for m in sprite_ref_pat.finditer(scene):
    all_guids.add(m.group(1))
for m in bg_sprite_pat.finditer(scene):
    all_guids.add(m.group(1))

# From SkillRow prefab
with open('Assets/Prefabs/UI/SkillRow.prefab') as f:
    prefab = f.read()
for m in sprite_ref_pat.finditer(prefab):
    all_guids.add(m.group(1))

# From any other UI prefabs
for root, dirs, files in os.walk('Assets/Prefabs/UI'):
    for fn in files:
        if fn.endswith('.prefab'):
            with open(os.path.join(root, fn)) as f:
                txt = f.read()
            for m in sprite_ref_pat.finditer(txt):
                all_guids.add(m.group(1))

# Remove null guid
all_guids.discard('0000000000000000000000000000000')

print('Total unique sprite GUIDs referenced: {}'.format(len(all_guids)))

# ── 3. Check each referenced sprite meta ─────────────────────────────────────
print()
print('=' * 55)
print('REFERENCED SPRITE META CHECKS')
print('=' * 55)

meta_issues = []
no_meta = []
not_sprite = []

for guid in sorted(all_guids):
    meta_path = guid_map.get(guid)
    if not meta_path:
        no_meta.append(guid)
        continue
    issues = check_sprite_meta(meta_path, guid_map)
    if issues is None:
        not_sprite.append(os.path.basename(meta_path))
        continue
    if issues:
        rel = meta_path.replace('\\', '/')
        print('[FAIL] ' + os.path.basename(meta_path))
        for iss in issues:
            print('       ' + iss)
        meta_issues.append(os.path.basename(meta_path) + ': ' + '; '.join(issues))
    # else: PASS, silent

if not meta_issues:
    print('[PASS] All {} referenced sprite metas OK'.format(len(all_guids) - len(no_meta)))
if no_meta:
    print('[WARN] {} GUIDs with no .meta: {}'.format(len(no_meta), no_meta[:5]))

# ── 4. ALL UI sprite metas (even unreferenced) ────────────────────────────────
print()
print('=' * 55)
print('ALL ART/SPRITES METAS (completeness check)')
print('=' * 55)
extra_fails = []
for root, dirs, files in os.walk('Assets/Art/Sprites'):
    for fn in sorted(files):
        if not fn.endswith('.meta'):
            continue
        p = os.path.join(root, fn)
        issues = check_sprite_meta(p, guid_map)
        if issues:
            rel = p.replace('\\', '/')
            already = any(fn[:-5] in x for x in meta_issues)
            if not already:
                extra_fails.append(fn + ': ' + '; '.join(issues))
                print('[FAIL] ' + fn + ': ' + '; '.join(issues))

if not extra_fails:
    n = sum(1 for r,d,fs in os.walk('Assets/Art/Sprites') for f in fs if f.endswith('.meta'))
    print('[PASS] All {} art sprite metas OK'.format(n))

# ── 5. SkillRow prefab layout ─────────────────────────────────────────────────
print()
print('=' * 55)
print('SKILLROW PREFAB LAYOUT')
print('=' * 55)
prefab_rts = parse_rts(prefab)
root_rt = prefab_rts.get('6255708628353042259')
if root_rt:
    w, h = root_rt['size']
    print('[{}] Root {}x{}'.format('PASS' if abs(w-340)<1 and abs(h-60)<1 else 'FAIL', int(w), int(h)))
icon_rt = prefab_rts.get('2222222222222222222')
print('[{}] Icon RT {}'.format('PASS' if icon_rt else 'FAIL', 'found' if icon_rt else 'NOT FOUND'))

# ── final ─────────────────────────────────────────────────────────────────────
print()
print('=' * 55)
combined = all_issues + meta_issues + extra_fails
if combined:
    print('ISSUES REMAIN: {}'.format(len(combined)))
    for i in combined:
        print('  - ' + i)
else:
    print('ALL PASS')
