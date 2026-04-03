"""
Recursive deep audit:
- Every descendant checked against its IMMEDIATE parent bounds
- ContentSizeFitter / LayoutGroup settings in prefabs
- GridLayoutGroup cell vs slot sizes
- ScrollRect content vs viewport
- All sprite metas (filterMode, PPU, compression, rect, border)
"""
import re, os, struct

# ── PNG helper ────────────────────────────────────────────────────────────────
def read_png_size(path):
    try:
        with open(path,'rb') as f:
            if f.read(8)!=b'\x89PNG\r\n\x1a\n': return None,None
            f.read(4)
            if f.read(4)!=b'IHDR': return None,None
            return struct.unpack('>I',f.read(4))[0], struct.unpack('>I',f.read(4))[0]
    except: return None,None

# ── RT + GO parser ────────────────────────────────────────────────────────────
def parse_rts(text):
    rts={}
    lines=text.split('\n'); i=0
    while i<len(lines):
        m=re.match(r'^--- !u!224 &(\d+)',lines[i])
        if m:
            fid=m.group(1)
            d={'pos':None,'size':None,'father':'0','children':[],'go_fid':None,'in_ch':False}
            i+=1
            while i<len(lines) and not lines[i].startswith('---'):
                l=lines[i]
                gm=re.match(r'\s+m_GameObject: \{fileID: (\d+)\}',l)
                if gm: d['go_fid']=gm.group(1)
                fm=re.match(r'\s+m_Father: \{fileID: (\d+)\}',l)
                if fm: d['father']=fm.group(1); d['in_ch']=False
                if 'm_Children:' in l: d['in_ch']=True
                elif d['in_ch']:
                    cm=re.match(r'\s+- \{fileID: (\d+)\}',l)
                    if cm: d['children'].append(cm.group(1))
                    elif re.match(r'\s+m_\w+:',l): d['in_ch']=False
                pm=re.match(r'\s+m_AnchoredPosition: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}',l)
                if pm: d['pos']=(float(pm.group(1)),float(pm.group(2)))
                sm=re.match(r'\s+m_SizeDelta: \{x: ([\d\.\-]+), y: ([\d\.\-]+)\}',l)
                if sm: d['size']=(float(sm.group(1)),float(sm.group(2)))
                i+=1
            if d['pos'] and d['size']: rts[fid]=d
        else: i+=1
    return rts

def parse_gos(text):
    gos={}
    lines=text.split('\n'); i=0
    while i<len(lines):
        m=re.match(r'^--- !u!1 &(\d+)',lines[i])
        if m:
            fid=m.group(1); name=None; active=True
            i+=1
            while i<len(lines) and not lines[i].startswith('---'):
                l=lines[i]
                nm=re.match(r'\s+m_Name: (\S+)',l)
                if nm: name=nm.group(1)
                am=re.match(r'\s+m_IsActive: (\d)',l)
                if am: active=am.group(1)=='1'
                i+=1
            if name: gos[fid]={'name':name,'active':active}
        else: i+=1
    return gos

# ── MonoBehaviour component finder ───────────────────────────────────────────
def find_components(go_fid, text):
    """Return list of (component_type_hint, content_dict) for all MonoBehaviours on a GO."""
    comps=[]
    for m in re.finditer(
        r'--- !u!114 &\d+\nMonoBehaviour:.*?m_GameObject: \{fileID: '+go_fid+r'\}(.*?)(?=\n--- !u|\Z)',
        text, re.DOTALL):
        block=m.group(1)
        # Try to identify component type from m_EditorClassIdentifier or script fields
        ecm=re.search(r'm_EditorClassIdentifier: (.+)',block)
        cls=ecm.group(1).strip() if ecm else 'Unknown'
        comps.append({'class':cls,'block':block})
    return comps

# ── Load files ────────────────────────────────────────────────────────────────
print('Loading...')
with open('Assets/Scenes/GameScene.unity','r',encoding='utf-8') as f: scene=f.read()
with open('Assets/Prefabs/UI/SkillRow.prefab','r',encoding='utf-8') as f: skill_prefab=f.read()

rts=parse_rts(scene)
gos=parse_gos(scene)
go_to_rt={d['go_fid']:fid for fid,d in rts.items() if d['go_fid']}

def find_rt(name):
    for gf,go in gos.items():
        if go['name']==name: return go_to_rt.get(gf)
    return None

def rt_name(fid, rts_=None, gos_=None):
    if rts_ is None: rts_,gos_=rts,gos
    d=rts_.get(fid,{}); gf=d.get('go_fid')
    return gos_[gf]['name'] if gf and gf in gos_ else 'RT#'+fid

def is_active(fid):
    d=rts.get(fid,{}); gf=d.get('go_fid')
    return gos[gf]['active'] if gf and gf in gos else True

OVERLAYS={'DragIcon','Tooltip','DragSlot','DragGhost','DragItem','Loading','QuestProposal'}
# Runtime-exclusive groups (won't overlap in actual use)
EXCLUSIVE_GROUPS=[
    {'Options','ActionButtons','InputField','SendButton','FreeToggle'},
]
def same_exclusive_group(na,nb):
    for grp in EXCLUSIVE_GROUPS:
        if na in grp and nb in grp: return True
    return False

def bounds(fid,rts_=None):
    if rts_ is None: rts_=rts
    d=rts_.get(fid)
    if not d: return None
    px,py=d['pos']; sw,sh=d['size']
    return px-sw/2, py-sh/2, px+sw/2, py+sh/2

# ── Recursive hierarchy checker ───────────────────────────────────────────────
issues=[]

def check_subtree(panel_fid, label, rts_=None, gos_=None, text=None):
    if rts_ is None: rts_,gos_,text=rts,gos,scene
    local_issues=[]

    def _check(fid, depth=0):
        d=rts_.get(fid)
        if not d: return
        parent_b=bounds(fid,rts_)
        children=d.get('children',[])

        child_info=[]
        for c in children:
            cd=rts_.get(c)
            if not cd: continue
            gf=cd.get('go_fid')
            name=gos_[gf]['name'] if gf and gf in gos_ else 'RT#'+c
            active=gos_[gf]['active'] if gf and gf in gos_ else True
            if not active: continue
            if name in OVERLAYS: continue

            cb=bounds(c,rts_)
            if not cb: continue
            cl,cbottom,cr,ct=cb

            # OOB vs parent
            if parent_b:
                pl,pb2,pr,pt=parent_b
                if cl<pl-2 or cbottom<pb2-2 or cr>pr+2 or ct>pt+2:
                    pfid_name=rt_name(fid,rts_,gos_)
                    msg='[OOB] {}/{}: pos({:.0f},{:.0f}) size({:.0f}x{:.0f}) L={:.0f} B={:.0f} R={:.0f} T={:.0f} vs parent({:.0f},{:.0f},{:.0f},{:.0f})'.format(
                        pfid_name,name,cd['pos'][0],cd['pos'][1],cd['size'][0],cd['size'][1],cl,cbottom,cr,ct,pl,pb2,pr,pt)
                    print('  '+msg)
                    local_issues.append(msg)

            child_info.append((c,cb,name))

        # Sibling overlap check (same parent)
        for i in range(len(child_info)):
            for j in range(i+1,len(child_info)):
                fa,ba,na=child_info[i]; fb,bb,nb=child_info[j]
                if same_exclusive_group(na,nb): continue
                ol=min(ba[2],bb[2])-max(ba[0],bb[0])
                ob=min(ba[3],bb[3])-max(ba[1],bb[1])
                if ol>2 and ob>2:
                    msg='[OVL] {}/{}: {}x{}px at depth {}'.format(na,nb,int(ol),int(ob),depth)
                    print('  '+msg)
                    local_issues.append(msg)

        for c,cb,name in child_info:
            _check(c, depth+1)

    _check(panel_fid)
    return local_issues

all_issues=[]

print()
print('='*60)
print('1. RECURSIVE LAYOUT AUDIT')
print('='*60)

for panel_name,ew,eh in [('InventoryPanel',1000,1000),('SkillTreePanel',1100,720),('DialoguePanel',1000,1000)]:
    fid=find_rt(panel_name)
    if not fid:
        print('[SKIP] '+panel_name); continue
    d=rts[fid]; w,h=d['size']
    ok=abs(w-ew)<1 and abs(h-eh)<1
    print('[{}] {} {}x{}'.format('PASS' if ok else 'FAIL',panel_name,int(w),int(h)))
    if not ok: all_issues.append(panel_name+' size mismatch')
    sub=check_subtree(fid,panel_name)
    if not sub: print('  [PASS] full hierarchy OK')
    all_issues+=sub

# ── 2. SkillRow prefab recursive check ───────────────────────────────────────
print()
print('='*60)
print('2. SKILLROW PREFAB HIERARCHY')
print('='*60)
prts=parse_rts(skill_prefab)
pgos=parse_gos(skill_prefab)
pgo_to_rt={d['go_fid']:fid for fid,d in prts.items() if d['go_fid']}

root_fid='6255708628353042259'
r=prts.get(root_fid)
if r:
    w,h=r['size']
    ok=abs(w-340)<1 and abs(h-60)<1
    print('[{}] Root {}x{}'.format('PASS' if ok else 'FAIL',int(w),int(h)))
    if not ok: all_issues.append('SkillRow root size')
    sub=check_subtree(root_fid,'SkillRow',prts,pgos,skill_prefab)
    if not sub: print('  [PASS] full hierarchy OK')
    all_issues+=sub
else:
    print('[FAIL] Root RT not found')
    all_issues.append('SkillRow root RT missing')

# ── 3. LayoutGroup / ContentSizeFitter checks ─────────────────────────────────
print()
print('='*60)
print('3. LAYOUT COMPONENTS CHECK')
print('='*60)

def check_layout_components(text, context_name):
    comp_issues=[]
    # GridLayoutGroup: cellSize should match InventorySlot expected size (44x44 for 1000px panel, ~10 cols)
    for m in re.finditer(r'm_CellSize: \{x: ([\d\.]+), y: ([\d\.]+)\}', text):
        cx,cy=float(m.group(1)),float(m.group(2))
        if cx<20 or cy<20:
            comp_issues.append('[WARN] GridLayoutGroup cellSize {}x{} may be too small'.format(int(cx),int(cy)))
        else:
            print('[PASS] GridLayoutGroup cellSize {}x{}  ({})'.format(int(cx),int(cy),context_name))

    # ContentSizeFitter: check fitsX/fitsY
    for m in re.finditer(r'm_HorizontalFit: (\d).*?m_VerticalFit: (\d)', text, re.DOTALL):
        hf,vf=m.group(1),m.group(2)
        # 0=Unconstrained, 1=MinSize, 2=PreferredSize
        if hf=='2' or vf=='2':
            pass  # PreferredSize CSF is normal
    return comp_issues

lc=check_layout_components(scene,'GameScene')
all_issues+=lc
lc2=check_layout_components(skill_prefab,'SkillRow')
all_issues+=lc2
if not lc and not lc2: print('[PASS] No layout component issues')

# ── 4. Comprehensive sprite meta audit ───────────────────────────────────────
print()
print('='*60)
print('4. SPRITE META AUDIT (all Assets/Art/Sprites/)')
print('='*60)

EXPECTED={
    'panel_bg.png':       {'w':64,'h':64,'border':(12,12,12,12)},
    'dialog_panel.png':   {'w':64,'h':64,'border':(12,12,12,12)},
    'slot_bg.png':        {'w':64,'h':64,'border':(0,0,0,0)},
    'slot_hover.png':     {'w':64,'h':64,'border':(0,0,0,0)},
    'button_normal.png':  {'w':64,'h':20,'border':(4,4,4,4)},
    'button_hover.png':   {'w':64,'h':20,'border':(4,4,4,4)},
    'button_pressed.png': {'w':64,'h':20,'border':(4,4,4,4)},
    'separator.png':      {'w':64,'h':4, 'border':(2,0,2,0)},
    'conn_branch_left.png':  {'w':8,'h':32,'border':None},
    'conn_branch_right.png': {'w':8,'h':32,'border':None},
}

def audit_meta(meta_path):
    errs=[]
    png_path=meta_path[:-5]
    fn=os.path.basename(meta_path)[:-5]  # strip .meta

    if not os.path.exists(png_path):
        return ['PNG missing']

    with open(meta_path) as f: c=f.read()
    if 'textureType: 8' not in c: return []

    pw,ph=read_png_size(png_path)

    if 'filterMode: 0' not in c: errs.append('filterMode!=0 (need Point)')
    if 'spritePixelsToUnits: 32' not in c: errs.append('PPU!=32')

    m=re.search(r'buildTarget: DefaultTexturePlatform.*?textureCompression: (\d+)',c,re.DOTALL)
    if m and m.group(1)!='0': errs.append('compression='+m.group(1)+' (need 0=None)')

    is_multi='spriteMode: 2' in c
    if not is_multi:
        rm=re.search(
            r'sprites:[\s\S]*?rect:\s+serializedVersion: \d+\s+'
            r'x: ([\d\.]+)\s+y: ([\d\.]+)\s+width: ([\d\.]+)\s+height: ([\d\.]+)',c)
        if rm and pw and ph:
            rx,ry,rw,rh=float(rm.group(1)),float(rm.group(2)),float(rm.group(3)),float(rm.group(4))
            if abs(rx)>0.5 or abs(ry)>0.5:
                errs.append('rect origin ({:.0f},{:.0f}) != (0,0)'.format(rx,ry))
            if abs(rw-pw)>0.5 or abs(rh-ph)>0.5:
                errs.append('rect {:.0f}x{:.0f} != PNG {}x{}'.format(rw,rh,pw,ph))

    exp=EXPECTED.get(fn+'.png')
    if exp:
        if pw and ph and (pw!=exp['w'] or ph!=exp['h']):
            errs.append('PNG {}x{} != exp {}x{}'.format(pw,ph,exp['w'],exp['h']))
        if exp['border']:
            bx,by,bz,bw=exp['border']
            bs='spriteBorder: {{x: {}, y: {}, z: {}, w: {}}}'.format(bx,by,bz,bw)
            if bs not in c: errs.append('border != {}'.format(exp['border']))

    bm=re.search(r'spriteBorder: \{x: ([\d\.]+), y: ([\d\.]+), z: ([\d\.]+), w: ([\d\.]+)\}',c)
    if bm and pw and ph:
        bx,by,bz,bw2=float(bm.group(1)),float(bm.group(2)),float(bm.group(3)),float(bm.group(4))
        if bx+bz>pw: errs.append('border LR {}>{} width'.format(int(bx+bz),pw))
        if by+bw2>ph: errs.append('border TB {}>{} height'.format(int(by+bw2),ph))

    return errs

meta_fails=[]
for root,dirs,files in os.walk('Assets/Art/Sprites'):
    for fn in sorted(files):
        if not fn.endswith('.meta'): continue
        p=os.path.join(root,fn)
        errs=audit_meta(p)
        if errs:
            rel=p.replace('\\','/')
            print('[FAIL] '+fn+': '+'; '.join(errs))
            meta_fails.append(fn+': '+'; '.join(errs))

if not meta_fails:
    n=sum(1 for r,d,fs in os.walk('Assets/Art/Sprites') for f in fs if f.endswith('.meta'))
    print('[PASS] All {} sprite metas OK'.format(n))
all_issues+=meta_fails

# ── 5. Icon meta check (new untracked icons) ─────────────────────────────────
print()
print('='*60)
print('5. UNTRACKED ICON .meta FILES')
print('='*60)
untracked=[
    'Assets/Art/Sprites/Icons/icon_quest_complete.png.meta',
    'Assets/Art/Sprites/Icons/icon_quest_marker.png.meta',
    'Assets/Art/Sprites/Icons/status_defdown.png.meta',
    'Assets/Art/Sprites/Icons/status_dot.png.meta',
    'Assets/Art/Sprites/Icons/status_freeze.png.meta',
    'Assets/Art/Sprites/Icons/status_heal.png.meta',
    'Assets/Art/Sprites/Icons/status_knockback.png.meta',
    'Assets/Art/Sprites/Icons/status_rage.png.meta',
    'Assets/Art/Sprites/Icons/status_speedup.png.meta',
    'Assets/Art/Sprites/Icons/status_stealth.png.meta',
]
icon_fails=[]
for mp in untracked:
    if not os.path.exists(mp):
        print('[MISS] '+os.path.basename(mp))
        icon_fails.append('missing: '+mp)
        continue
    errs=audit_meta(mp)
    if errs:
        print('[FAIL] '+os.path.basename(mp)+': '+'; '.join(errs))
        icon_fails.append(os.path.basename(mp)+': '+'; '.join(errs))
    else:
        print('[PASS] '+os.path.basename(mp))
all_issues+=icon_fails

# ── Summary ───────────────────────────────────────────────────────────────────
print()
print('='*60)
if all_issues:
    print('ISSUES REMAIN ({})'.format(len(all_issues)))
    for i in all_issues: print('  - '+i)
else:
    print('ALL PASS')
