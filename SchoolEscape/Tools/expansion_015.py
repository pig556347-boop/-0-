"""Four-storey main building. Authored here, only SchoolEscape source/assets.
All transforms are deterministic; the three expressly protected maps stay intact.
"""
from copy import deepcopy
from author_interiors import Interior, DIMENSIONS, room_kind
from annex_map import make, attach
from support_rooms import _paperwork, _monitor, _keyboard, _cabinet, _board, _printer
from interior_dressing import locker_banks

D={'north':(0,1),'south':(0,-1),'east':(1,0),'west':(-1,0)}

def class_room(base,door,index):
    DIMENSIONS['classroom']=(18,16)
    i=Interior(base,door,index)
    i.obj('blackboard-frame',9,14.7,6.2,.10,.95,base=.72)
    i.obj('long-blackboard',9,14.64,6,.025,.78,'#27493f',base=.80)
    i.obj('chalk-tray',9,14.54,6,.12,.045,'#aaa993',base=.76)
    i.table(6,13.6,1.20,.70,'teacher-desk');_paperwork(i,6,13.6)
    for col,x in enumerate([3,5,7,11,13,15]):
        for row,y in enumerate([11.7,9.9,8.1,6.3,4.5]):
            i.table(x,y,.75,.55);i.fixtures[-6]['seatRow']=row;i.fixtures[-6]['seatCol']=col
            i.chair(x,y-.56)
            i.obj('student-notebook',x-.1,y,.23,.29,.025,'#b8b69a',base=.81)
            i.obj('student-pencil',x+.14,y,.018,.18,.015,'#a89669',base=.82)
    locker_banks(i)
    for bank in [o for o in i.fixtures if o['kind']=='locker-bank']:
        assembly=bank['assembly'];bank.update(height=1.82,compartments=32)
        i.fixtures[:]=[o for o in i.fixtures if o.get('assembly')!=assembly or o is bank]
        for row in range(8):
            for col in range(4):
                i.obj('locker-compartment',bank['x']+(col-1.5)*.64/1.25,bank['y']-.32/1.25,.6,.04,.19,'#71857b',base=.08+row*.215,assembly=assembly)
    i.obj('ceiling-TV',15,14.2,.72,.4,.48,'#273233',base=1.3)
    i.obj('TV-glass',15,14.02,.54,.025,.32,'#657d7c',base=1.38)
    i.obj('class-clock',9,14.8,.45,.08,.45,'#b9baa6',base=1.9)
    i.obj('class-cleaning-cabinet',1.35,12,.6,1.3,1.7,'#65766c',solid=True)
    i.obj('class-book-cabinet',16.2,12,.55,1.4,1.2,'#7b765d',solid=True)
    for x in [1.1,16.85]:
        for y in [6,10]:
            i.obj('class-window-frame',x,y,.10,2,1.3,'#59736d',base=.8)
            i.obj('class-window-glass',x-.06,y,.02,1.8,1.12,'#7b9390',base=.88)
    i.zone('중앙 통로',9,8);i.zone('칠판 앞',9,13.5);i.zone('사물함 앞',9,4)
    for serial,obj in enumerate(i.fixtures):obj['id']=f'{i.id}-015-{serial:04d}'
    i.map['seatColumns']=[3,5,7,11,13,15];i.map['seatRows']=[11.7,9.9,8.1,6.3,4.5]
    return i.map

def staff_room(base,door,index):
    DIMENSIONS['staff']=(18,18);i=Interior(base,door,index)
    grade=int(door['label'][0]);subjects={1:['기술·가정','국어','영어','체육'],2:['컴퓨터','수학','과학','음악'],3:['지리','사회','미술','진로']}[grade]
    roles=[f'{grade}학년 {n}반 담임' for n in range(1,7)]+[s+' 교사' for s in subjects]
    i.map['teacherSeats']=[]
    for n,role in enumerate(roles):
        x=[4,7,11,14][n%4];y=[6,10,14][n//4]
        i.table(x,y,1.65,.8,'staff-desk');desk=next(o for o in reversed(i.fixtures) if o['kind']=='staff-desk');desk['label']=role
        i.chair(x,y-.72);_monitor(i,x,y+.12);_keyboard(i,x,y-.2)
        i.obj('teacher-nameplate',x+.55,y-.2,.4,.10,.12,'#bcb99d',base=.82,label=role)
        i.map['teacherSeats'].append(dict(role=role,x=x,y=y,fixtureId=desk['id']))
    for x in [3,7,12,15]:_cabinet(i,x,16,2,.55,1.7,'staff-record-cabinet')
    _printer(i,2,3.3);_board(i,9,16.8,4,'staff-timetable-board')
    i.table(13,3.2,2,.65,'staff-tea-counter');i.zone('중앙 통로',9,9);i.zone('담임 좌석',9,6);i.zone('과목 교사 좌석',9,14)
    return i.map

def special(base,door,index,kind):
    sizes={'science':(22,20),'technology':(22,20),'studentCouncil':(16,15),'gatehouse':(12,12)}
    DIMENSIONS[kind]=sizes[kind];door['interiorKind']=kind;i=Interior(base,door,index)
    if kind=='science':
        for x in [5,10,16]:
            for y in [7,12]:
                i.table(x,y,2.6,1.1,'science-lab-workbench')
                for dx in [-.65,.65]:
                    i.obj('lab-flask',x+dx,y,.16,.16,.32,'#a7c1b6',base=.82,shape='cylinder')
                    i.chair(x+dx,y-.9)
                i.obj('lab-sink',x,y,.55,.45,.08,'#b0bcb2',base=.81)
                i.obj('lab-tap',x,y+.2,.06,.06,.35,'#96aaa6',base=.89)
        i.table(10,17,3,.9,'science-teacher-desk');_board(i,10,18.5,7,'science-board')
        for x in [3,18]:_cabinet(i,x,17,2,.6,1.8,'science-equipment-cabinet')
        i.zone('실험대 사이 통로',13,10);i.zone('교탁 앞',10,15)
    elif kind=='technology':
        for x in [5,11,17]:
            for y in [7,12]:
                i.table(x,y,2.5,1.1,'technology-workbench');i.chair(x,y-.9)
                i.obj('sewing-machine-base',x,y,.6,.35,.08,'#b3b8a3',base=.81)
                i.obj('sewing-machine-arm',x-.15,y,.15,.18,.4,'#a4b3aa',base=.89)
                i.obj('sewing-machine-top',x,y,.45,.18,.12,'#a4b3aa',base=1.2)
        for x in [4,9,16]:_cabinet(i,x,17,2,.6,1.7,'technology-tool-cabinet')
        i.table(18,3.5,3,.7,'home-economics-counter');i.obj('home-economics-stove',18,3.5,.65,.6,.05,'#333e3e',base=.82)
        _board(i,10,18.5,5);i.zone('작업 통로',8,10)
    elif kind=='studentCouncil':
        for x in [5,10]:
            for y in [7,10]:i.table(x,y,1.8,.8,'council-desk');i.chair(x,y-.7);_paperwork(i,x,y)
        for x in [3,7,12]:_cabinet(i,x,12.5,1.8,.6,1.7,'council-record-cabinet')
        _board(i,8,13.6,5);i.zone('회의 동선',8,8)
    else:
        i.table(4,7,2,.8,'guard-desk');i.chair(4,6.3);_monitor(i,4,7);_paperwork(i,4,7)
        _cabinet(i,8,9,2,.6,1.7,'guard-key-cabinet');i.obj('guard-radio',4.6,7,.18,.15,.3,'#444f4b',base=.82)
        i.obj('guard-bench',8,5,2,.6,.5,'#747b66',solid=True);i.zone('근무 일지 앞',4,5)
    return i.map

def compact(r):
    """Move rigid furniture groups closer; retain metre dimensions and elevations.
    Purpose-built routes/portals, music, gym and small spaces are excluded.
    """
    if r['kind'] in {'classroom','staff','music','gym','rooftop','studentRestroom','staffRestroom'} or r.get('portals') or r.get('stairs') or r.get('kitchenSequence') or r.get('voidRects'):return
    w,h=r['width'],r['height']
    if w<20 or h<20:return
    nw,nh=max(18,round(w*.85)),max(18,round(h*.85));sx=(nw-3)/(w-3);sy=(nh-4)/(h-4)
    fx=lambda x:1+(x-1)*sx
    fy=lambda y:2+(y-2)*sy
    originals=deepcopy(r['fixtures']);solids=[o for o in originals if o.get('solid')]
    anchors={}
    for o in originals:
        if o.get('assembly') and o['assembly'] not in anchors:anchors[o['assembly']]=o
    for o,old in zip(r['fixtures'],originals):
        anchor=anchors.get(old.get('assembly'))
        if anchor is None and solids:
            near=min(solids,key=lambda s:(s['x']-old['x'])**2+(s['y']-old['y'])**2)
            if abs(near['x']-old['x'])<.95 and abs(near['y']-old['y'])<.95:anchor=near
        anchor=anchor or old
        o['x']=round(fx(anchor['x'])+old['x']-anchor['x'],3);o['y']=round(fy(anchor['y'])+old['y']-anchor['y'],3)
    r.update(width=nw,height=nh,spawnX=nw//2,spawnY=3,exitX=nw//2,exitY=1,
        rows=[''.join('.' if 1<=x<nw-1 and 2<=y<nh-1 else '#' for x in range(nw)) for y in range(nh)])
    for p in r['landmarks']+r['interiorZones']:p.update(x=round(fx(p['x']),2),y=round(fy(p['y']),2))
    r['sizeAdjustment']=dict(before=[w,h],after=[nw,nh],rigidFurniture=True)

def expand_015(floors,rooms,yard):
    old2,old3=floors[1],floors[2]
    new2=deepcopy(old2)
    # Preserve all original interior IDs to avoid remapping fixture assemblies.
    for f in [old2,old3]:
        n=f['number']+1;f.update(number=n,sourceFloor=n,id=f'floor-{n}',label=f'본관 {n}층')
    old2['portals']=[];old2['connectionPassages']=[];old2['barriers']=[]
    grid=[list(row) for row in old2['rows']]
    for row in grid:
        for x in list(range(0,28))+list(range(73,len(row))):row[x]='#'
    old2['rows']=[''.join(row) for row in grid]
    old2['landmarks']=[p for p in old2['landmarks'] if 28<=p['x']<=72]
    new2.update(number=2,id='floor-2',sourceFloor=2,label='본관 2층',subtitle='1학년 · 학생회 · 기술가정 · 서관/별관 연결')
    chosen=[(0,'1학년 1반'),(1,'1학년 2반'),(2,'1학년 3반'),(3,'1학년 4반'),(6,'1학년 5반'),(7,'1학년 6반'),(8,'학생회실'),(9,'기술가정실'),(13,'학생 화장실 남'),(14,'학생 화장실 여'),(19,'교직원 화장실 남'),(20,'교직원 화장실 여'),(21,'1학년 교무실')]
    # Use labels to select restroom footprints (indices follow authored source).
    chosen=[(i,l) for i,l in chosen if i<len(new2['rooms'])]
    new2['rooms']=[dict(deepcopy(new2['rooms'][idx]),label=label,interiorId='',interiorKind='') for idx,label in chosen]
    replacements={1:{'빈교실 2':'학생 상담실','빈교실 3':'교직원 휴게실'},3:{'빈교실 A':'2학년 5반','빈교실 B':'2학년 6반','빈교실 C':'과학실','빈교실 D':'과학 준비실','미술실':'학생 연구실','미술실 창고':'약품 보관실','컴퓨터실':'과학 자료실','자습실':'독서 토론실'},4:{'빈교실 1':'3학년 5반','빈교실 2':'3학년 6반','빈교실 3':'미술실','교직원 휴게실':'컴퓨터실'}}
    byid={r['id']:r for r in rooms}
    for f in [floors[0],old2,old3]:
        for idx,d in enumerate(f['rooms']):
            oldlabel=d['label'];d['label']=replacements.get(f['number'],{}).get(oldlabel,oldlabel)
            r=byid[d['interiorId']];r.update(number=f['number'],sourceFloor=f['number'],label=d['label'],subtitle=d['label']+' · 독립 실내 공간')
            for o in r['fixtures']:o['room']=d['label']
            rebuild=d['label']!=oldlabel or d['interiorKind'] in {'classroom','staff'}
            if not rebuild:continue
            kind=room_kind(d['label']) or {'학생 상담실':'counseling','과학 준비실':'preparation','약품 보관실':'storage','과학 자료실':'archives','학생 연구실':'seminar','독서 토론실':'seminar','과학실':'science'}[d['label']]
            d['interiorKind']=kind
            rid=r['id']
            if kind=='classroom':replacement=class_room(f,d,idx)
            elif kind=='staff':replacement=staff_room(f,d,idx)
            elif kind=='science':replacement=special(f,d,idx,kind)
            else:replacement=Interior(f,d,idx).build()
            prefix=replacement['id']
            for obj in replacement['fixtures']:
                obj['id']=obj['id'].replace(prefix,rid,1)
                if obj.get('assembly'):obj['assembly']=obj['assembly'].replace(prefix,rid,1)
            for seat in replacement.get('teacherSeats',[]):seat['fixtureId']=seat['fixtureId'].replace(prefix,rid,1)
            replacement['id']=rid;rooms[rooms.index(r)]=replacement;byid[rid]=replacement
    # Second floor uses new unique IDs, not any room formerly on floor 2.
    for idx,d in enumerate(new2['rooms']):
        kind=room_kind(d['label']) or {'학생회실':'studentCouncil','기술가정실':'technology'}[d['label']];d['interiorKind']=kind
        base=dict(new2,number=20)
        if kind=='classroom':r=class_room(base,d,idx)
        elif kind=='staff':r=staff_room(base,d,idx)
        elif kind in {'technology','studentCouncil'}:r=special(base,d,idx,kind)
        else:r=Interior(base,d,idx).build()
        r.update(number=2,sourceFloor=2);d['interiorId']=r['id'];rooms.append(r)
    # Exactly two vacant classrooms; first-floor former classroom is retained,
    # second is a compact independent classroom adjoining the ground-floor foyer.
    f1=floors[0]
    extra=dict(label='빈교실 2',x0=29,y0=2,x1=35,y1=6,doorX=29,doorY=5,facing='west',interiorKind='classroom',interiorOpen=False)
    r=class_room(f1,extra,99);extra['interiorId']=r['id'];f1['rooms'].append(extra);rooms.append(r)
    f1['stairs']=[dict(id='f1-up',x=26,y=29,target=2,arrivalX=49,arrivalY=5,label='2층 ↑')]
    new2['stairs']=[dict(id='f2-down',x=49,y=3,target=1,arrivalX=26,arrivalY=27,label='1층 ↓'),dict(id='f2-up',x=50,y=35,target=3,arrivalX=49,arrivalY=5,label='3층 ↑')]
    old2['stairs']=[dict(id='f3-down',x=49,y=3,target=2,arrivalX=50,arrivalY=32,label='2층 ↓'),dict(id='f3-up',x=50,y=35,target=4,arrivalX=24,arrivalY=5,label='4층 ↑')]
    old3['stairs']=[dict(id='f4-down',x=24,y=3,target=3,arrivalX=50,arrivalY=32,label='3층 ↓'),dict(id='f4-roof',x=24,y=35,targetId='main-rooftop',target=5,arrivalX=14,arrivalY=5,label='옥상 ↑')]
    roof=byid['main-rooftop'];roof.update(number=5,sourceFloor=5,returnSpaceId='floor-4')
    roof['stairs']=[dict(id='roof-f4',x=14,y=2,targetId='floor-4',target=4,arrivalX=24,arrivalY=32,label='본관 4층 ↓')]
    roof['subtitle']='본관 4층 위 옥상 · 설비 점검 구역'
    for idx,(x,y) in enumerate([(8,16),(18,16),(8,8),(18,8)]):
        roof['fixtures'].append(dict(id=f'roof-015-{idx}',kind='roof-duct',x=x,y=y,width=3,depth=1.3,height=.65,elevation=0,color='#607572',solid=True,shape='box'))
    floors[:]=[f1,new2,old2,old3]
    for r in rooms:compact(r)
    # The original yard dimensions and sports/music routes remain untouched.
    walk=make('school-walkway','학교 뒤 산책로','walkway',40,26,0)
    walk.map.update(building='grounds',isOutdoor=True,isRoom=False,number=0,sourceFloor=0,noExit=True,returnSpaceId='playground')
    grid=[['#']*40 for _ in range(26)]
    for y in range(2,24):
        for x in range(1,39):
            if y<=5 or y>=20 or x<=5 or x>=34:grid[y][x]='.'
    walk.map['rows']=[''.join(row) for row in grid];walk.map.update(spawnX=3,spawnY=3)
    walk.map['portals']=[dict(id='walk-to-yard',label='운동장으로 돌아가기',doorX=1,doorY=3,facing='east',targetId='playground',arrivalX=64,arrivalY=25)]
    yard['portals'].append(dict(id='yard-to-walk',label='학교 뒤 산책로',doorX=65,doorY=25,facing='west',targetId=walk.id,arrivalX=3,arrivalY=3))
    for x in [11,22,30]:
        walk.obj('walkway-bench',x,3,2,.55,.5,'#7a795f',solid=True)
        walk.obj('yard-trash-bin',x,22,.7,.7,.9,'#526858',solid=True,shape='cylinder')
    for x in [9,16,24,31]:
        for y in [8,17]:
            walk.obj('walkway-tree-trunk',x,y,.4,.4,2.1,'#655d49',solid=True,shape='cylinder')
            walk.obj('walkway-tree-crown',x,y,3,3,1.8,'#3b554b',base=1.7,shape='sphere')
    walk.map['landmarks']=[dict(label='산책로 입구',x=3,y=3),dict(label='서쪽 산책길',x=3,y=13),dict(label='동쪽 산책길',x=36,y=13),dict(label='뒤쪽 산책길',x=20,y=22)]
    walk.zone('서쪽 산책길',3,13);walk.zone('동쪽 산책길',36,13);walk.zone('뒤쪽 산책길',20,22)
    door=dict(label='교문 경비실',interiorKind='gatehouse',doorX=6,doorY=5,facing='east')
    guard=special({'number':0},door,201,'gatehouse');guard.update(id='gatehouse',building='grounds',returnSpaceId='playground',returnX=7,returnY=5,exitLabel='운동장으로 나가기')
    yard['portals'].append(dict(id='yard-to-gatehouse',label='교문 경비실',doorX=6,doorY=5,facing='east',targetId='gatehouse',arrivalX=guard['spawnX'],arrivalY=3))
    rooms.append(guard)
    for space,points in [(yard,[(6,5),(65,25)]),(walk.map,[(1,3)])]:
        rows=[list(row) for row in space['rows']]
        for x,y in points:rows[y][x]='#'
        space['rows']=[''.join(row) for row in rows]
    for idx,(kind,x,y,w,d,h,z,color) in enumerate([('gatehouse-exterior',3.8,5,4,6,2.5,0,'#465d59'),('gatehouse-roof',3.8,5,4.3,6.3,.18,2.5,'#273d3f'),('gatehouse-window',5.43,6,.06,1.4,.8,1,'#92a9a1')]):
        yard['fixtures'].append(dict(id=f'gatehouse-exterior-{idx}',kind=kind,x=x,y=y,width=w,depth=d,height=h,elevation=z,color=color,solid=idx==0,shape='box'))
    # Yard main-building silhouette is now four storeys (other exteriors kept).
    for o in yard['fixtures']:
        if o['kind']=='school-main-facade':o['height']=10.2
    from school_shop import add_school_shop
    add_school_shop(rooms)
    return walk.map
