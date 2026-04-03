import re, os

# Parse RectTransforms by scanning line by line
def parse_rts(text):
    """Returns dict: fileID -> {pos, size, father, children, go_fid}"""
    rts = {}
    lines = text.split('\n')
    i = 0
    while i < len(lines):
        line = lines[i]
        # Start of RT block
        m = re.match(r'^--- !u!224 &(\d+)', line)
        if m:
            fid = m.group(1)
            data = {'pos': None, 'size': None, 'father': '0', 'children': [], 'go_fid': None}
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                l = lines[i]
                gm = re.match(r'\s+m_GameObject: \{fileID: (\d+)\}', l)
                if gm:
                    data['go_fid'] = gm.group(1)
                fm = re.match(r'\s+m_Father: \{fileID: (\d+)\}', l)
                if fm:
                    data['father'] = fm.group(1)
                cm = re.match(r'\s+- \{fileID: (\d+)\}', l)
                if cm and data.get('in_children'):
                    data['children'].append(cm.group(1))
                if 'm_Children:' in l:
                    data['in_children'] = True
                elif re.match(r'\s+m_\w+:', l) and 'm_Children' not in l:
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

with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    scene = f.read()

rts = parse_rts(scene)
gos = parse_go_names(scene)

# Build go_fid -> rt_fid map
go_to_rt = {}
for rt_fid, data in rts.items():
    if data['go_fid']:
        go_to_rt[data['go_fid']] = rt_fid

def find_rt_by_name(name):
    for go_fid, go in gos.items():
        if go['name'] == name:
            return go_to_rt.get(go_fid)
    return None

def rt_name(fid):
    data = rts.get(fid, {})
    go_fid = data.get('go_fid')
    if go_fid and go_fid in gos:
        return gos[go_fid]['name']
    return 'RT#' + fid

def is_active(fid):
    data = rts.get(fid, {})
    go_fid = data.get('go_fid')
    if go_fid and go_fid in gos:
        return gos[go_fid]['active']
    return True

def bounds(fid):
    d = rts.get(fid)
    if not d or not d['pos'] or not d['size']:
        return None
    px, py = d['pos']
    sw, sh = d['size']
    return px - sw/2, py - sh/2, px + sw/2, py + sh/2  # L, B, R, T

def check_panel(panel_name, expected_w, expected_h):
    issues = []
    fid = find_rt_by_name(panel_name)
    if not fid:
        print('[SKIP] ' + panel_name + ' not found')
        return []
    d = rts[fid]
    w, h = d['size']
    ok = abs(w - expected_w) < 1 and abs(h - expected_h) < 1
    print('[' + ('PASS' if ok else 'FAIL') + '] ' + panel_name + ' ' + str(int(w)) + 'x' + str(int(h)) + ' (expected ' + str(expected_w) + 'x' + str(expected_h) + ')')
    if not ok:
        issues.append(panel_name + ' size ' + str(int(w)) + 'x' + str(int(h)))

    children = d.get('children', [])
    pb = bounds(fid)
    if not pb:
        return issues

    child_info = []
    for c in children:
        if not is_active(c):
            continue
        cb = bounds(c)
        if not cb:
            continue
        cl, cbottom, cr, ct = cb
        name = rt_name(c)
        oob = cl < pb[0] - 2 or cbottom < pb[1] - 2 or cr > pb[2] + 2 or ct > pb[3] + 2
        if oob:
            cd = rts.get(c, {})
            pos = cd.get('pos', (0,0))
            sz = cd.get('size', (0,0))
            print('  [FAIL-OOB] ' + name + ': pos(' + str(int(pos[0])) + ',' + str(int(pos[1])) + ') size(' + str(int(sz[0])) + 'x' + str(int(sz[1])) + ') bounds L=' + str(int(cl)) + ' B=' + str(int(cbottom)) + ' R=' + str(int(cr)) + ' T=' + str(int(ct)))
            issues.append('OOB ' + name)
        child_info.append((c, cb, name))

    for i in range(len(child_info)):
        for j in range(i + 1, len(child_info)):
            fa, ba, na = child_info[i]
            fb, bb, nb = child_info[j]
            ol = min(ba[2], bb[2]) - max(ba[0], bb[0])
            ob = min(ba[3], bb[3]) - max(ba[1], bb[1])
            if ol > 2 and ob > 2:
                print('  [FAIL-OVL] ' + na + ' x ' + nb + ': ' + str(int(ol)) + 'x' + str(int(ob)) + 'px overlap')
                issues.append('OVL ' + na + '/' + nb)

    if not any(x.startswith('OOB') or x.startswith('OVL') for x in issues):
        print('  [PASS] ' + str(len(children)) + ' children OK')
    return issues

all_issues = []

print('=' * 50)
print('INVENTORY PANEL')
print('=' * 50)
all_issues += check_panel('InventoryPanel', 1000, 1000)

print()
print('=' * 50)
print('SKILL TREE PANEL')
print('=' * 50)
all_issues += check_panel('SkillTreePanel', 1100, 720)
print()
for col_name in ['MeleeColumn', 'RangedColumn', 'MagicColumn']:
    fid = find_rt_by_name(col_name)
    if fid:
        d = rts[fid]
        print('  ' + col_name + ': pos(' + str(int(d['pos'][0])) + ',' + str(int(d['pos'][1])) + ') size(' + str(int(d['size'][0])) + 'x' + str(int(d['size'][1])) + ')')

print()
print('=' * 50)
print('DIALOGUE PANEL')
print('=' * 50)
all_issues += check_panel('DialoguePanel', 1000, 1000)

print()
print('=' * 50)
print('SPRITE METAS')
print('=' * 50)

def chk_meta(path, ew=None, eh=None, border=None):
    errs = []
    if not os.path.exists(path):
        return ['not found']
    with open(path) as f:
        c = f.read()
    if 'filterMode: 0' not in c:
        errs.append('filterMode!=0(Point)')
    if 'spritePixelsToUnits: 32' not in c:
        errs.append('PPU!=32')
    if 'spriteMode: 2' not in c and ew and eh:
        rm = re.search(r'sprites:.*?rect:.*?width: (\d+).*?height: (\d+)', c, re.DOTALL)
        if rm:
            rw, rh = int(rm.group(1)), int(rm.group(2))
            if rw != ew or rh != eh:
                errs.append('rect=' + str(rw) + 'x' + str(rh) + '!=' + str(ew) + 'x' + str(eh))
    if border:
        bx, by, bz, bw = border
        bs = 'spriteBorder: {x: ' + str(bx) + ', y: ' + str(by) + ', z: ' + str(bz) + ', w: ' + str(bw) + '}'
        if bs not in c:
            errs.append('border!=' + str(border))
    return errs

specific_checks = [
    ('Assets/Art/Sprites/UI/panel_bg.png.meta', 64, 64, (12, 12, 12, 12)),
    ('Assets/Art/Sprites/UI/dialog_panel.png.meta', 64, 64, (12, 12, 12, 12)),
    ('Assets/Art/Sprites/UI/slot_bg.png.meta', 64, 64, (0, 0, 0, 0)),
    ('Assets/Art/Sprites/UI/slot_hover.png.meta', 64, 64, (0, 0, 0, 0)),
    ('Assets/Art/Sprites/UI/button_normal.png.meta', 64, 20, (4, 4, 4, 4)),
    ('Assets/Art/Sprites/UI/button_hover.png.meta', 64, 20, (4, 4, 4, 4)),
    ('Assets/Art/Sprites/UI/button_pressed.png.meta', 64, 20, (4, 4, 4, 4)),
    ('Assets/Art/Sprites/UI/separator.png.meta', 64, 4, (2, 0, 2, 0)),
    ('Assets/Art/Sprites/UI/SkillTree/conn_branch_left.png.meta', 8, 32, None),
    ('Assets/Art/Sprites/UI/SkillTree/conn_branch_right.png.meta', 8, 32, None),
]

meta_fails = []
for path, ew, eh, border in specific_checks:
    errs = chk_meta(path, ew, eh, border)
    if errs:
        meta_fails.append(os.path.basename(path) + ': ' + str(errs))

for root, dirs, files in os.walk('Assets/Art/Sprites'):
    for fn in sorted(files):
        if not fn.endswith('.meta'):
            continue
        p = os.path.join(root, fn)
        with open(p) as f:
            c = f.read()
        if 'textureType: 8' not in c:
            continue
        errs = []
        if 'filterMode: 0' not in c:
            errs.append('filterMode!=0')
        if 'spritePixelsToUnits: 32' not in c:
            errs.append('PPU!=32')
        if errs and not any(fn in x for x in meta_fails):
            meta_fails.append(fn + ': ' + str(errs))

n_metas = sum(1 for r, d, fs in os.walk('Assets/Art/Sprites') for fn in fs if fn.endswith('.meta'))
print('Total sprite metas: ' + str(n_metas))
if meta_fails:
    for x in meta_fails:
        print('  FAIL ' + x)
    print('[FAIL] ' + str(len(meta_fails)) + ' meta issue(s)')
    all_issues += meta_fails
else:
    print('[PASS] All sprite metas OK')

print()
print('=' * 50)
if all_issues:
    print('ISSUES REMAIN: ' + str(len(all_issues)))
    for i in all_issues:
        print('  - ' + str(i))
else:
    print('ALL PASS')
