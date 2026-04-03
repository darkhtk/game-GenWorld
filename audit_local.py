"""
Audit using LOCAL coordinate space:
- Each child's m_AnchoredPosition is relative to its parent's center (anchor=0.5)
- OOB = child bounds exceed parent's half-size
- Sibling overlap compared in same parent-local space
"""
import re, os, struct

def read_png_size(path):
    try:
        with open(path,'rb') as f:
            if f.read(8)!=b'\x89PNG\r\n\x1a\n': return None,None
            f.read(4)
            if f.read(4)!=b'IHDR': return None,None
            return struct.unpack('>I',f.read(4))[0], struct.unpack('>I',f.read(4))[0]
    except: return None,None

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

with open('Assets/Scenes/GameScene.unity','r',encoding='utf-8') as f: scene=f.read()
with open('Assets/Prefabs/UI/SkillRow.prefab','r',encoding='utf-8') as f: prefab=f.read()

rts=parse_rts(scene); gos=parse_gos(scene)
go_to_rt={d['go_fid']:fid for fid,d in rts.items() if d['go_fid']}

def find_rt(name,rts_=None,gos_=None,g2r=None):
    if rts_ is None: rts_,gos_,g2r=rts,gos,go_to_rt
    for gf,go in gos_.items():
        if go['name']==name: return g2r.get(gf)
    return None

def rt_name(fid,rts_=None,gos_=None):
    if rts_ is None: rts_,gos_=rts,gos
    d=rts_.get(fid,{}); gf=d.get('go_fid')
    return gos_[gf]['name'] if gf and gf in gos_ else 'RT#'+fid

def is_active(fid,rts_=None,gos_=None):
    if rts_ is None: rts_,gos_=rts,gos
    d=rts_.get(fid,{}); gf=d.get('go_fid')
    return gos_[gf]['active'] if gf and gf in gos_ else True

OVERLAYS={'DragIcon','Tooltip','DragSlot','DragGhost','DragItem','Loading','QuestProposal'}
# Runtime-exclusive pairs that legitimately overlap in static YAML
EXCLUSIVE=[
    {'Options','ActionButtons','InputField','SendButton','FreeToggle'},
    {'Text','Placeholder'},  # InputField children
]
def excl(na,nb):
    for g in EXCLUSIVE:
        if na in g and nb in g: return True
    return False

# ── core checker ──────────────────────────────────────────────────────────────
def check_hierarchy(root_fid, label, rts_=None, gos_=None):
    if rts_ is None: rts_,gos_=rts,gos
    issues=[]

    def _check(fid):
        d=rts_.get(fid)
        if not d: return
        pw,ph=d['size']
        # parent local half-extents
        plx,ply=pw/2,ph/2

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

            cx,cy=cd['pos']; cw,ch=cd['size']
            cl=cx-cw/2; cb=cy-ch/2; cr=cx+cw/2; ct=cy+ch/2

            # OOB check in parent-local space
            if cl < -plx-2 or cb < -ply-2 or cr > plx+2 or ct > ply+2:
                pname=rt_name(fid,rts_,gos_)
                msg='[OOB] {}/{}: pos({:.0f},{:.0f}) size({:.0f}x{:.0f}) local L={:.0f} R={:.0f} T={:.0f} B={:.0f} vs parent {:.0f}x{:.0f}'.format(
                    pname,name,cx,cy,cw,ch,cl,cr,ct,cb,pw,ph)
                print('  '+msg)
                issues.append(msg)

            child_info.append((c,(cl,cb,cr,ct),name))

        # Sibling overlap in parent-local space
        for i in range(len(child_info)):
            for j in range(i+1,len(child_info)):
                fa,ba,na=child_info[i]; fb,bb,nb=child_info[j]
                if excl(na,nb): continue
                ol=min(ba[2],bb[2])-max(ba[0],bb[0])
                ob=min(ba[3],bb[3])-max(ba[1],bb[1])
                if ol>2 and ob>2:
                    pname=rt_name(fid,rts_,gos_)
                    msg='[OVL] {}/{} in {}: {}x{}px'.format(na,nb,pname,int(ol),int(ob))
                    print('  '+msg)
                    issues.append(msg)

        for c,_,_ in child_info:
            _check(c)

    _check(root_fid)
    return issues

# ── run ───────────────────────────────────────────────────────────────────────
all_issues=[]
print('='*55)
print('1. PANEL LAYOUT (LOCAL COORDS)')
print('='*55)

for name,ew,eh in [('InventoryPanel',1000,1000),('SkillTreePanel',1100,720),('DialoguePanel',1000,1000)]:
    fid=find_rt(name)
    if not fid: print('[SKIP] '+name); continue
    d=rts[fid]; w,h=d['size']
    ok=abs(w-ew)<1 and abs(h-eh)<1
    print('[{}] {} {}x{}'.format('PASS' if ok else 'FAIL',name,int(w),int(h)))
    if not ok: all_issues.append(name+' size')
    sub=check_hierarchy(fid,name)
    if not sub: print('  [PASS] full hierarchy OK')
    all_issues+=sub

print()
for col in ['MeleeColumn','RangedColumn','MagicColumn']:
    fid=find_rt(col)
    if fid:
        d=rts[fid]
        print('  {} pos({:.0f},{:.0f}) size({:.0f}x{:.0f})'.format(
            col,d['pos'][0],d['pos'][1],d['size'][0],d['size'][1]))

print()
print('='*55)
print('2. SKILLROW PREFAB')
print('='*55)
prts=parse_rts(prefab); pgos=parse_gos(prefab)
pgo_to_rt={d['go_fid']:fid for fid,d in prts.items() if d['go_fid']}
root=prts.get('6255708628353042259')
if root:
    w,h=root['size']
    ok=abs(w-340)<1 and abs(h-60)<1
    print('[{}] Root {}x{}'.format('PASS' if ok else 'FAIL',int(w),int(h)))
    if not ok: all_issues.append('SkillRow root size')
    sub=check_hierarchy('6255708628353042259','SkillRow',prts,pgos)
    if not sub: print('  [PASS] full hierarchy OK')
    all_issues+=sub
icon=prts.get('2222222222222222222')
print('[{}] Icon RT'.format('PASS' if icon else 'FAIL'))
if not icon: all_issues.append('SkillRow Icon RT missing')

print()
print('='*55)
print('3. SPRITE METAS')
print('='*55)

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

def audit_meta(p):
    errs=[]
    png=p[:-5]
    fn=os.path.basename(p)[:-5]
    if not os.path.exists(png): return ['PNG missing']
    with open(p) as f: c=f.read()
    if 'textureType: 8' not in c: return []
    pw,ph=read_png_size(png)
    if 'filterMode: 0' not in c: errs.append('filterMode!=0')
    if 'spritePixelsToUnits: 32' not in c: errs.append('PPU!=32')
    m=re.search(r'buildTarget: DefaultTexturePlatform.*?textureCompression: (\d+)',c,re.DOTALL)
    if m and m.group(1)!='0': errs.append('compression!=0')
    is_multi='spriteMode: 2' in c
    if not is_multi:
        rm=re.search(r'sprites:[\s\S]*?rect:\s+serializedVersion: \d+\s+x: ([\d\.]+)\s+y: ([\d\.]+)\s+width: ([\d\.]+)\s+height: ([\d\.]+)',c)
        if rm and pw and ph:
            rx,ry,rw,rh=float(rm.group(1)),float(rm.group(2)),float(rm.group(3)),float(rm.group(4))
            if abs(rx)>0.5 or abs(ry)>0.5: errs.append('rect origin ({:.0f},{:.0f})'.format(rx,ry))
            if abs(rw-pw)>0.5 or abs(rh-ph)>0.5: errs.append('rect {:.0f}x{:.0f}!=PNG {}x{}'.format(rw,rh,pw,ph))
    exp=EXPECTED.get(fn+'.png')
    if exp:
        if pw and ph and (pw!=exp['w'] or ph!=exp['h']): errs.append('PNG size {}x{}!=exp {}x{}'.format(pw,ph,exp['w'],exp['h']))
        if exp['border']:
            bx,by,bz,bw=exp['border']
            if 'spriteBorder: {{x: {}, y: {}, z: {}, w: {}}}'.format(bx,by,bz,bw) not in c:
                errs.append('border!={}'.format(exp['border']))
    bm=re.search(r'spriteBorder: \{x: ([\d\.]+), y: ([\d\.]+), z: ([\d\.]+), w: ([\d\.]+)\}',c)
    if bm and pw and ph:
        bx,by,bz,bw=float(bm.group(1)),float(bm.group(2)),float(bm.group(3)),float(bm.group(4))
        if bx+bz>pw: errs.append('border LR>width')
        if by+bw>ph: errs.append('border TB>height')
    return errs

meta_fails=[]
for root,dirs,files in os.walk('Assets/Art/Sprites'):
    for fn in sorted(files):
        if not fn.endswith('.meta'): continue
        p=os.path.join(root,fn)
        errs=audit_meta(p)
        if errs:
            print('[FAIL] '+fn+': '+'; '.join(errs))
            meta_fails.append(fn+': '+'; '.join(errs))

if not meta_fails:
    n=sum(1 for r,d,fs in os.walk('Assets/Art/Sprites') for f in fs if f.endswith('.meta'))
    print('[PASS] All {} metas OK'.format(n))
all_issues+=meta_fails

print()
print('='*55)
if all_issues:
    print('ISSUES REMAIN ({})'.format(len(all_issues)))
    for i in all_issues: print('  - '+str(i)[:120])
else:
    print('ALL PASS')
