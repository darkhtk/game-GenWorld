"""
Final comprehensive audit:
1. InventoryPanel + SkillTreePanel + DialoguePanel layout (size, OOB, overlap)
2. SkillRow prefab layout + Icon child
3. All sprite .meta files under Assets/Art/Sprites/ :
   - PNG file exists
   - filterMode=0 (Point)
   - PPU=32
   - textureCompression=0 (None) on DefaultPlatform
   - spriteMode correct
   - rect matches PNG size (single sprites)
   - 9-slice border valid (border <= half of dimension)
4. Newly generated UI sprites: dimension + border checks
"""
import re, os, struct

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

def read_png_size(path):
    try:
        with open(path, 'rb') as f:
            sig = f.read(8)
            if sig != b'\x89PNG\r\n\x1a\n':
                return None, None
            f.read(4)
            chunk = f.read(4)
            if chunk != b'IHDR':
                return None, None
            w = struct.unpack('>I', f.read(4))[0]
            h = struct.unpack('>I', f.read(4))[0]
            return w, h
    except:
        return None, None

def parse_rts(text):
    rts = {}
    lines = text.split('\n')
    i = 0
    while i < len(lines):
        m = re.match(r'^--- !u!224 &(\d+)', lines[i])
        if m:
            fid = m.group(1)
            data = {'pos': None, 'size': None, 'father': '0', 'children': [], 'go_fid': None, 'in_ch': False}
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                l = lines[i]
                gm = re.match(r'\s+m_GameObject: \{fileID: (\d+)\}', l)
                if gm: data['go_fid'] = gm.group(1)
                fm = re.match(r'\s+m_Father: \{fileID: (\d+)\}', l)
                if fm: data['father'] = fm.group(1); data['in_ch'] = False
                if 'm_Children:' in l:
                    data['in_ch'] = True
                elif data['in_ch']:
                    cm = re.match(r'\s+- \{fileID: (\d+)\}', l)
                    if cm: data['children'].append(cm.group(1))
                    elif re.match(r'\s+m_\w+:', l): data['in_ch'] = False
                pm = re.match(r'\s+m_AnchoredPosition: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}', l)
                if pm: data['pos'] = (float(pm.group(1)), float(pm.group(2)))
                sm = re.match(r'\s+m_SizeDelta: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}', l)
                if sm: data['size'] = (float(sm.group(1)), float(sm.group(2)))
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
            name, active = None, True
            i += 1
            while i < len(lines) and not lines[i].startswith('---'):
                l = lines[i]
                nm = re.match(r'\s+m_Name: (\S+)', l)
                if nm: name = nm.group(1)
                am = re.match(r'\s+m_IsActive: (\d)', l)
                if am: active = am.group(1) == '1'
                i += 1
            if name: gos[go_fid] = {'name': name, 'active': active}
        else:
            i += 1
    return gos

# Load scene
with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    scene = f.read()
rts = parse_rts(scene)
gos = parse_go_names(scene)
go_to_rt = {d['go_fid']: fid for fid, d in rts.items() if d['go_fid']}

def find_rt(name):
    for gf, go in gos.items():
        if go['name'] == name:
            return go_to_rt.get(gf)
    return None

def rt_name(fid):
    d = rts.get(fid, {})
    gf = d.get('go_fid')
    return gos[gf]['name'] if gf and gf in gos else 'RT#' + fid

def is_active(fid):
    d = rts.get(fid, {})
    gf = d.get('go_fid')
    return gos[gf]['active'] if gf and gf in gos else True

OVERLAYS = {'DragIcon','Tooltip','DragSlot','DragGhost','DragItem'}

def bounds(fid):
    d = rts.get(fid)
    if not d: return None
    px, py = d['pos']; sw, sh = d['size']
    return px-sw/2, py-sh/2, px+sw/2, py+sh/2

def check_panel_layout(panel_name, ew, eh):
    issues = []
    fid = find_rt(panel_name)
    if not fid:
        print('[SKIP] ' + panel_name); return []
    d = rts[fid]
    w, h = d['size']
    ok = abs(w-ew)<1 and abs(h-eh)<1
    label = 'PASS' if ok else 'FAIL'
    print('[{}] {} {}x{} (exp {}x{})'.format(label, panel_name, int(w), int(h), ew, eh))
    if not ok: issues.append('{} size {}x{}'.format(panel_name, int(w), int(h)))
    children = d.get('children', [])
    pb = bounds(fid)
    child_info = []
    for c in children:
        if not is_active(c) or rt_name(c) in OVERLAYS: continue
        cb = bounds(c)
        if not cb: continue
        cl,cbottom,cr,ct = cb
        if cl<pb[0]-2 or cbottom<pb[1]-2 or cr>pb[2]+2 or ct>pb[3]+2:
            cd = rts[c]
            print('  [FAIL-OOB] {} pos({:.0f},{:.0f}) size({:.0f}x{:.0f})'.format(
                rt_name(c), cd['pos'][0], cd['pos'][1], cd['size'][0], cd['size'][1]))
            issues.append('OOB ' + rt_name(c))
        child_info.append((c, cb, rt_name(c)))
    for i in range(len(child_info)):
        for j in range(i+1, len(child_info)):
            fa,ba,na = child_info[i]; fb,bb,nb = child_info[j]
            ol = min(ba[2],bb[2]) - max(ba[0],bb[0])
            ob = min(ba[3],bb[3]) - max(ba[1],bb[1])
            if ol>2 and ob>2:
                print('  [FAIL-OVL] {} x {}: {}x{}px'.format(na, nb, int(ol), int(ob)))
                issues.append('OVL {}/{}'.format(na, nb))
    if not issues: print('  [PASS] {} children OK'.format(len(children)))
    return issues

# ─────────────────────────────────────────────────────────────────────────────
all_issues = []

print('=' * 58)
print('1. PANEL LAYOUT')
print('=' * 58)
all_issues += check_panel_layout('InventoryPanel', 1000, 1000)
all_issues += check_panel_layout('SkillTreePanel', 1100, 720)
for col in ['MeleeColumn','RangedColumn','MagicColumn']:
    fid = find_rt(col)
    if fid:
        d = rts[fid]
        print('  {} pos({:.0f},{:.0f}) size({:.0f}x{:.0f})'.format(
            col, d['pos'][0], d['pos'][1], d['size'][0], d['size'][1]))
all_issues += check_panel_layout('DialoguePanel', 1000, 1000)

print()
print('=' * 58)
print('2. SKILLROW PREFAB')
print('=' * 58)
with open('Assets/Prefabs/UI/SkillRow.prefab') as f:
    prefab = f.read()
prts = parse_rts(prefab)
root = prts.get('6255708628353042259')
if root:
    w,h = root['size']
    print('[{}] Root {}x{}'.format('PASS' if abs(w-340)<1 and abs(h-60)<1 else 'FAIL', int(w), int(h)))
    if not (abs(w-340)<1 and abs(h-60)<1):
        all_issues.append('SkillRow root {}x{}'.format(int(w),int(h)))
icon = prts.get('2222222222222222222')
print('[{}] Icon child'.format('PASS' if icon else 'FAIL'))
if not icon: all_issues.append('SkillRow Icon RT missing')

print()
print('=' * 58)
print('3. SPRITE META AUDIT (Assets/Art/Sprites/)')
print('=' * 58)

# Expected settings for specific UI sprites we generated
EXPECTED = {
    'panel_bg.png':      {'w':64,'h':64,'border':(12,12,12,12)},
    'dialog_panel.png':  {'w':64,'h':64,'border':(12,12,12,12)},
    'slot_bg.png':       {'w':64,'h':64,'border':(0,0,0,0)},
    'slot_hover.png':    {'w':64,'h':64,'border':(0,0,0,0)},
    'button_normal.png': {'w':64,'h':20,'border':(4,4,4,4)},
    'button_hover.png':  {'w':64,'h':20,'border':(4,4,4,4)},
    'button_pressed.png':{'w':64,'h':20,'border':(4,4,4,4)},
    'separator.png':     {'w':64,'h':4, 'border':(2,0,2,0)},
    'conn_branch_left.png':  {'w':8,'h':32,'border':None},
    'conn_branch_right.png': {'w':8,'h':32,'border':None},
}

def audit_meta(meta_path):
    errs = []
    png_path = meta_path[:-5]
    fn = os.path.basename(meta_path)
    fn_png = fn[:-5]  # remove .meta -> .png

    # PNG must exist
    if not os.path.exists(png_path):
        return ['PNG missing: ' + png_path]

    with open(meta_path) as f:
        c = f.read()

    if 'textureType: 8' not in c:
        return []  # not a sprite, skip

    # PNG dimensions
    pw, ph = read_png_size(png_path)

    # filterMode
    if 'filterMode: 0' not in c:
        errs.append('filterMode!=0 (Point)')

    # PPU
    if 'spritePixelsToUnits: 32' not in c:
        errs.append('PPU!=32')

    # Compression on DefaultPlatform
    m = re.search(r'buildTarget: DefaultTexturePlatform.*?textureCompression: (\d+)', c, re.DOTALL)
    if m and m.group(1) != '0':
        errs.append('DefaultPlatform compression={} (need 0)'.format(m.group(1)))

    is_multi = 'spriteMode: 2' in c

    if not is_multi:
        # rect check
        rm = re.search(
            r'sprites:[\s\S]*?rect:\s+serializedVersion: \d+\s+'
            r'x: ([\d\.]+)\s+y: ([\d\.]+)\s+width: ([\d\.]+)\s+height: ([\d\.]+)',
            c)
        if rm and pw and ph:
            rx,ry,rw,rh = float(rm.group(1)),float(rm.group(2)),float(rm.group(3)),float(rm.group(4))
            if abs(rx)>0.5 or abs(ry)>0.5:
                errs.append('rect y-offset ({:.0f},{:.0f}) — should start at (0,0)'.format(rx,ry))
            if abs(rw-pw)>0.5 or abs(rh-ph)>0.5:
                errs.append('rect {:.0f}x{:.0f} != PNG {}x{}'.format(rw,rh,pw,ph))

    # Expected overrides for known sprites
    exp = EXPECTED.get(fn_png)
    if exp:
        if pw and ph and (pw != exp['w'] or ph != exp['h']):
            errs.append('PNG {}x{} != expected {}x{}'.format(pw,ph,exp['w'],exp['h']))
        if exp['border']:
            bx,by,bz,bw = exp['border']
            bs = 'spriteBorder: {{x: {}, y: {}, z: {}, w: {}}}'.format(bx,by,bz,bw)
            if bs not in c:
                errs.append('border != {}'.format(exp['border']))

    # 9-slice border sanity: border must not exceed half the dimension
    bm = re.search(r'spriteBorder: \{x: ([\d\.]+), y: ([\d\.]+), z: ([\d\.]+), w: ([\d\.]+)\}', c)
    if bm and pw and ph:
        bx,by,bz,bw2 = float(bm.group(1)),float(bm.group(2)),float(bm.group(3)),float(bm.group(4))
        if bx+bz > pw: errs.append('border left+right ({:.0f}) > PNG width ({})'.format(bx+bz,pw))
        if by+bw2 > ph: errs.append('border bottom+top ({:.0f}) > PNG height ({})'.format(by+bw2,ph))

    return errs

meta_fails = []
total = 0
for root, dirs, files in os.walk('Assets/Art/Sprites'):
    for fn in sorted(files):
        if not fn.endswith('.meta'): continue
        p = os.path.join(root, fn)
        with open(p) as f:
            c_peek = f.read(200)
        if 'textureType: 8' not in c_peek and 'TextureImporter' not in c_peek:
            # quick skip non-texture metas
            continue
        errs = audit_meta(p)
        if errs:
            rel = p.replace('\\', '/').replace('Assets/', 'Assets/')
            meta_fails.append((os.path.basename(p), errs))
            print('[FAIL] {}'.format(os.path.basename(p)))
            for e in errs:
                print('       ' + e)
        total += 1 if errs is not None else 0

if not meta_fails:
    n = sum(1 for r,d,fs in os.walk('Assets/Art/Sprites') for fn in fs if fn.endswith('.meta'))
    print('[PASS] All {} sprite metas OK'.format(n))
    total = n
else:
    all_issues += ['{}: {}'.format(f, '; '.join(e)) for f, e in meta_fails]

print()
print('=' * 58)
if all_issues:
    print('ISSUES REMAIN ({})'.format(len(all_issues)))
    for i in all_issues:
        print('  - ' + i)
else:
    print('ALL PASS - {} sprite metas, all panels, prefab OK'.format(
        sum(1 for r,d,fs in os.walk('Assets/Art/Sprites') for fn in fs if fn.endswith('.meta'))))
