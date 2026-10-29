"""0.17 authored map edits. Only independent SchoolEscape modules are reused."""
from copy import deepcopy
from annex_map import make, attach
from expansion_013 import wrap, office

def apply_017(data):
    rooms=data['roomMaps']; maps={r['id']:r for r in rooms}; yard=data['outdoorMaps'][0]; walk=data['outdoorMaps'][1]
    # Outdoor ring is contiguous geometry: no hidden teleport/door.
    ox,oy=68,22;w=ox+walk['width'];h=max(yard['height'],oy+walk['height'])
    grid=[['#']*w for _ in range(h)]
    for y,row in enumerate(yard['rows']):grid[y][:len(row)]=row
    for y,row in enumerate(walk['rows']):grid[y+oy][ox:ox+len(row)]=row
    for y in range(24,28):
        for x in range(62,73):grid[y][x]='.'
    yard.update(width=w,height=h,rows=[''.join(r) for r in grid])
    yard['portals']=[p for p in yard['portals'] if p['targetId']!='school-walkway']
    for f in walk['fixtures']:
        f=deepcopy(f);f['x']+=ox;f['y']+=oy;yard['fixtures'].append(f)
    for z in walk.get('landmarks',[])+walk.get('interiorZones',[]):
        z=deepcopy(z);z['x']+=ox;z['y']+=oy;yard['landmarks'].append(z)
    yard['landmarks'].append(dict(label='산책로 연결부 · 도보',x=69,y=25))
    data['outdoorMaps']=[yard]
    # Rename the west service rooms and remove cooking/freezer-specific props.
    for id,label,kind in [('west-dining','직원 카페테리아','staffCafe'),('west-freezer','비품창고','storage'),('west-prep','시설관리 작업실','maintenance')]:
        r=maps[id];old=r['label'];r.update(label=label,kind=kind,subtitle='서관 1층 · '+label)
        for f in r['fixtures']:
            f['room']=label
            if any(t in f['kind'] for t in ['freezer','stove','cooking']):f['kind']='maintenance-supply-cabinet';f['color']='#68766b';f['label']='시설 비품'
        if id=='west-prep':
            tables=[f for f in r['fixtures'] if f['kind']=='prep-table']
            for f in tables:f.update(kind='maintenance-workbench',label='수리 작업대')
            cabinets=[f for f in r['fixtures'] if f['kind']=='kitchen-storage-cabinet']
            for f,k in zip(cabinets,['maintenance-panel-cabinet','spare-parts-cabinet','spare-parts-cabinet']):f.update(kind=k,label='시설 부품 보관함')
            f=deepcopy(tables[0]);f.update(id=id+'-pump',kind='maintenance-pump',label='수리 중인 펌프',width=.45,depth=.4,height=.45,elevation=.8,solid=False,color='#697a74');r['fixtures'].append(f)
        if id=='west-freezer':
            cabinets=[f for f in r['fixtures'] if f['kind']=='maintenance-supply-cabinet']
            for f in cabinets:f.update(kind='storage-cabinet',label='비품 수납장')
            boxes=[f for f in r['fixtures'] if f['kind']=='supply-box']
            for f in boxes:f.update(kind='equipment-crate',label='시설 비품 상자')
            f=deepcopy(cabinets[0]);f.update(id=id+'-sorting-top',kind='storage-sorting-table',label='비품 분류대',height=.08,elevation=1.75,solid=False);r['fixtures'].append(f)
        for space in data['floors']+rooms:
            for p in space.get('portals',[]):
                if p.get('targetId')==id:p['label']=label
    # Match visible doors and clickable owner slots; no tiny 32-compartment unit.
    for r in rooms:
        banks=[f for f in r['fixtures'] if f['kind']=='locker-bank']
        if r['kind'] in ['guardDorm','gatehouse','changing']:
            count=2 if r['kind'] in ['guardDorm','gatehouse'] else 4
            if not banks and r['kind']=='gatehouse':
                o=wrap(r);o.obj('locker-bank',2,8,1.25,.55,1.65,'#5f756c',solid=True);banks=[r['fixtures'][-1]]
            r['fixtures'][:]=[f for f in r['fixtures'] if not f['kind'].startswith('locker-') or f in banks[:1]]
            for b in banks[:1]:
                b.update(compartments=count,width=count*.55,height=1.75)
                o=wrap(r)
                for n in range(count):
                    o.obj('locker-door',b['x']+(n-(count-1)/2)*.55/1.25,b['y']-.24,.50,.04,1.58,'#819084',base=.08)
                    o.obj('locker-handle',b['x']+(n-(count-1)/2)*.55/1.25+.12,b['y']-.27,.035,.04,.15,'#beb69a',base=.85)
    # Thirty actual student owners, four rear banks (8 + 8 + 8 + 6).
    for r in rooms:
        if r['kind']!='classroom':continue
        banks=[f for f in r['fixtures'] if f['kind']=='locker-bank']
        r['fixtures'][:]=[f for f in r['fixtures'] if not f['kind'].startswith('locker-') or f in banks]
        for bank_index,b in enumerate(banks):
            count=8 if bank_index<3 else 6;b.update(compartments=count,height=1.82)
            o=wrap(r)
            for n in range(count):
                col,row=n%4,n//4
                o.obj('locker-compartment',b['x']+(col-1.5)*.64/1.25,b['y']-.32/1.25,.60,.04,.78,'#71857b',base=.08+row*.84,assembly=b.get('assembly'))
                o.obj('locker-handle',b['x']+(col-1.5)*.64/1.25+.13,b['y']-.35/1.25,.03,.04,.12,'#bbb499',base=.4+row*.84,assembly=b.get('assembly'))
    # Real one-person toilet, accessed inside gatehouse.
    toilet=make('gatehouse-toilet','경비실 화장실','guardToilet',7,8,1);toilet.map['building']='grounds'
    toilet.obj('toilet-base',2,5,.55,.8,.65,'#bdc6b6',solid=True)
    toilet.obj('wash-basin',4.8,5,.7,.5,.8,'#bdc6b6',solid=True)
    toilet.obj('mirror-surface',4.8,6,.9,.05,1,'#91aaa6',base=1)
    maps['gatehouse'].setdefault('portals',[])
    grid=[list(row) for row in maps['gatehouse']['rows']];grid[7][10]='#';maps['gatehouse']['rows']=[''.join(row) for row in grid]
    attach(wrap(maps['gatehouse']),toilet,10,7,'west');rooms.append(toilet.map)
    # Two extra investigation rooms alongside the existing three, upstairs.
    gallery=wrap(maps['annex-gallery'])
    for id,label,x in [('coach-room','체육 지도교사실',12),('equipment-records','운동장비 관리실',31)]:
        o=make('annex-'+id,label,'sportsOffice',16,15,3);office(o)
        # Office authoring assumes 20x18: retain only fixtures within smaller room.
        o.fixtures[:]=[f for f in o.fixtures if 1<f['x']<14 and 3<f['y']<13]
        o.map['interiorZones']=[dict(room=label,label='중앙 조사 통로',x=8,y=9)];attach(gallery,o,x,1,'north');rooms.append(o.map)
    # Keep the protected court size. Room-specific enemies have fixed domains.
    maps['annex-gym']['subjectEvent']='gym'
    next(r for r in rooms if r['kind']=='art')['subjectEvent']='art'
    # Compact everyday kitchen + service passage revealed for the set-piece.
    chase=maps['annex-kitchen'];chase['id']='annex-kitchen-chase';chase['label']='조리실 비상 작업 통로';chase['subtitle']='붕괴로 열린 비상 작업 통로'
    chase['fixtures']=[f for f in chase['fixtures'] if f['kind']!='kitchen-reward-crate']
    chase['kitchenSequence']['leadSeconds']=1.4
    compact=make('annex-kitchen','급식실 조리실','kitchen',24,20,1);compact.map.update(returnSpaceId='annex-cafeteria',returnX=2,returnY=25,exitLabel='급식실로 돌아가기',noLoot=True)
    for x in [4,11,18]:
        for y in [7,12]:compact.obj('kitchen-prep-counter',x,y,2.5,1.1,.95,'#9ba99f',solid=True)
    compact.obj('kitchen-reward-crate',19,16,1.3,.8,.85,'#897952',solid=True)
    compact.zone('길 끝 보급 상자 앞',19,15)
    compact.map['portals']=[]
    compact.map['eventTransitions']=[dict(targetId=chase['id'],condition='상자 조사 후 귀환 · 붕괴 추격')]
    for p in maps['annex-cafeteria']['portals']:
        if p['targetId']=='annex-kitchen':p.update(arrivalX=compact.map['spawnX'],arrivalY=compact.map['spawnY'])
    rooms.append(compact.map)
    data['revision']='unity-port-017-2026-10-29'
