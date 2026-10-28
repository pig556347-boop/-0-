"""Static map/portal/furniture checks, distinct from Unity or browser execution."""
import json
from collections import deque
from math import floor, ceil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
data=json.loads((ROOT/'Assets/Resources/SchoolMap.json').read_text())
errors=[];checks=0

def check(ok,message):
    global checks
    checks+=1
    if not ok:errors.append(message)

spaces=data['floors']+data.get('roomMaps',[])+data.get('outdoorMaps',[])
spaces_by_id={s['id']:s for s in spaces}
rooms_by_id={s['id']:s for s in data.get('roomMaps',[])}
completion_rooms={
    (1,'창고'):'storage',(1,'대회의실'):'assembly',(1,'설비관리실'):'maintenance',(1,'세미나실'):'seminar',
    (3,'준비실 1'):'preparation',(3,'자료실'):'archives',(3,'창고'):'storage',
    (1,'교직원 휴게실'):'lounge',(4,'학부모상담실'):'counseling',(4,'메인회의실'):'meeting',
    (4,'교장실'):'principal',(4,'중앙통제실'):'control',
}
geometry={}
for f in spaces:
    geometry[f['id']]=dict(cells={(x,y) for y,row in enumerate(f['rows']) for x,c in enumerate(row) if c=='.'},solids=[o for o in f.get('fixtures',[]) if o.get('solid')])

def cells(f):return geometry[f['id']]['cells']
def blocked(f,x,y):
    radius=data['playerRadius']/data['tileSize']
    def overlaps(rect):
        cx=max(rect[0],min(x,rect[2]));cy=max(rect[1],min(y,rect[3]))
        return (x-cx)**2+(y-cy)**2<radius*radius-1e-10
    for iy in range(floor(y-radius-.5),ceil(y+radius+.5)+1):
        for ix in range(floor(x-radius-.5),ceil(x+radius+.5)+1):
            if (ix,iy) not in cells(f) and overlaps((ix-.5,iy-.5,ix+.5,iy+.5)):return True
    for o in geometry[f['id']]['solids']:
        if overlaps((o['x']-o['width']/data['tileSize']/2,o['y']-o['depth']/data['tileSize']/2,o['x']+o['width']/data['tileSize']/2,o['y']+o['depth']/data['tileSize']/2)):return True
    return False

def edge_clear(f,a,b):
    steps=max(8,ceil(((b[0]-a[0])**2+(b[1]-a[1])**2)**.5*data['tileSize']/.1))
    return all(not blocked(f,a[0]+(b[0]-a[0])*i/steps,a[1]+(b[1]-a[1])*i/steps) for i in range(1,steps+1))

def reachable(open,start,edge=None):
    if start not in open:return set()
    seen={start};q=deque([start])
    while q:
        x,y=q.popleft()
        for n in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
            if n in open and n not in seen and (edge is None or edge((x,y),n)):seen.add(n);q.append(n)
    return seen

def fixture_bounds(o):
    return (o['x']*data['tileSize']-o['width']/2,o['y']*data['tileSize']-o['depth']/2,o.get('elevation',0),o['x']*data['tileSize']+o['width']/2,o['y']*data['tileSize']+o['depth']/2,o.get('elevation',0)+o['height'])

def volume_overlap(a,b):return all(min(a[i+3],b[i+3])-max(a[i],b[i])>.001 for i in range(3))

check(data.get('schemaVersion',0)>=4,'Portal room map schema required')
check(len({f['id'] for f in spaces})==len(spaces),'Duplicate space ids')
check(bool(data.get('roomMaps')),'No independent room maps')
portal_ids=[]
for f in spaces:
    sid=f['id'];is_room=bool(f.get('isRoom'));open=cells(f)
    check(len(f['rows'])==f['height'] and all(len(row)==f['width'] for row in f['rows']),f'{sid} invalid row dimensions')
    if is_room:start=(f['spawnX'],f['spawnY'])
    else:start=(f['landmarks'][0]['x'],f['landmarks'][0]['y'])
    seen=reachable(open,start)
    check(seen==open,f'{sid} disconnected walkable cells: {len(open-seen)}')
    check(all(0<x<f['width']-1 and 0<y<f['height']-1 for x,y in open),f'{sid} open outer boundary')
    clear={p for p in open if not blocked(f,*p)}
    furniture_reached=reachable(clear,start,lambda a,b:edge_clear(f,a,b))
    check(start in clear,f'{sid} obstructed spawn/start')
    for m in f.get('landmarks',[]):check(not blocked(f,m['x'],m['y']) and any(abs(x-m['x'])<1.5 and abs(y-m['y'])<1.5 and edge_clear(f,(x,y),(m['x'],m['y'])) for x,y in furniture_reached),f"{sid} unreachable landmark {m['label']}")
    for z in f.get('interiorZones',[]):check(not blocked(f,z['x'],z['y']) and any(abs(x-z['x'])<1.5 and abs(y-z['y'])<1.5 and edge_clear(f,(x,y),(z['x'],z['y'])) for x,y in furniture_reached),f"{sid} furniture blocks interior zone {z['label']}")
    fixture_ids=set()
    for o in f.get('fixtures',[]):
        check(o['id'] not in fixture_ids,f"{sid} duplicate fixture id {o['id']}");fixture_ids.add(o['id'])
        check(all(o[k]>0 for k in ['width','depth','height']),f"{sid} invalid fixture dimensions {o['id']}")
        check(o.get('elevation',0)>=0,f"{sid} fixture below floor {o['id']}")
    if is_room:
        check(not f.get('rooms') and (not f.get('stairs') or (f.get('building') in {'annex','west'} or f.get('kind')=='rooftop')) and not f.get('barriers'),f'{sid} contains another building space or staircase')
        check(bool(f.get('fixtures')) and bool(f.get('interiorZones')),f'{sid} missing interior content or landmarks')
        check((f['exitX'],f['exitY']) not in open,f'{sid} exit door must block walking before interaction')
        check((f['exitX'],f['exitY']+1) in furniture_reached,f'{sid} return door approach unreachable')
        if not f.get('noExit'):
            base=spaces_by_id.get(f.get('returnSpaceId')) or data['floors'][f['sourceFloor']-1]
            check((f['returnX'],f['returnY']) in cells(base) and not blocked(base,f['returnX'],f['returnY']),f'{sid} unsafe corridor return')
        kinds={o['kind'] for o in f['fixtures']}
        if (f['sourceFloor'],f['label']) in completion_rooms:
            check(f['kind']==completion_rooms[(f['sourceFloor'],f['label'])],f'{sid} newly furnished room has wrong functional layout')
            check(f['width']>=18 and f['height']>=18,f'{sid} newly furnished room must retain generous exploration scale')
            check(len(f['interiorZones'])>=5,f'{sid} newly furnished room needs multiple explorable work/supply zones')
            check(len([o for o in f['fixtures'] if o.get('solid')])>=10,f'{sid} newly furnished room lacks substantial furnishings')
        required={
            'classroom':{'ceiling-TV','long-blackboard','teacher-desk','student-desk'},
            'music':{'piano-body','composer-frame','old-vocal-desk','instrument-case'},
            'art':{'plaster-head','easel-mast','art-worktable','sink-basin'},
            'library':{'shelf-body','reading-table','loan-counter'},
            'infirmary':{'infirmary-bed','medicine-cabinet','nurse-desk','curtain-post'},
            'administration':{'admin-desk','reception-counter','archive-cabinet','office-safe'},
            'staff':{'staff-desk','teacher-nameplate','staff-record-cabinet','staff-timetable-board'},
            'computer':{'computer-workstation','computer-monitor','computer-teacher-desk','network-rack'},
            'broadcast':{'broadcast-console','broadcast-mixer','broadcast-microphone','broadcast-equipment-rack','broadcast-announcer-desk','broadcast-acoustic-window','broadcast-storage-cabinet'},
            'studentRestroom':{'toilet-base','washbasin-bowl','restroom-stall-partition','mirror-surface'},
            'staffRestroom':{'toilet-base','washbasin-bowl','restroom-stall-partition','mirror-surface'},
            'storage':{'storage-cabinet','shelf-body','storage-sorting-table','equipment-crate'},
            'assembly':{'assembly-stage','assembly-projection-screen','assembly-podium','chair-seat'},
            'maintenance':{'maintenance-panel-cabinet','maintenance-workbench','maintenance-pump','spare-parts-cabinet'},
            'seminar':{'seminar-worktable','seminar-instructor-desk','seminar-projection-screen'},
            'preparation':{'preparation-worktable','preparation-cabinet','preparation-sample-jar','medical-sink-cabinet'},
            'archives':{'shelf-body','archives-file-cabinet','archive-catalog-desk','archive-sorting-table'},
            'lounge':{'lounge-sofa','lounge-tea-counter','lounge-water-dispenser'},
            'counseling':{'counselor-desk','counseling-low-divider','counseling-record-cabinet','lounge-sofa'},
            'meeting':{'meeting-table','meeting-document-cabinet','meeting-projection-screen'},
            'principal':{'principal-desk','principal-trophy-cabinet','school-trophy-cup','lounge-sofa'},
            'control':{'control-monitor-panel','control-console-desk','control-server-rack','control-status-led'},
        }.get(f.get('kind'),set())
        check(required<=kinds,f"{sid} missing reference furniture: {sorted(required-kinds)}")
        if f.get('kind') in {'infirmary','administration','staff','computer'}:
            check(f['width']>=18 and f['height']>=18,f'{sid} support room must retain generous exploration scale')
            count=lambda kind:sum(o['kind']==kind for o in f['fixtures'])
            if f['kind']=='infirmary':
                check(count('infirmary-bed')==4,f'{sid} infirmary needs four beds')
                check(count('curtain-post')>=4,f'{sid} infirmary needs bedside privacy screens')
            if f['kind']=='administration':
                check(count('admin-desk')==18,f'{sid} administration needs eighteen staffed workstations')
                check(count('reception-counter')==2,f'{sid} administration needs two connected reception counter arms')
            if f['kind']=='staff':
                check(count('staff-desk')==10,f'{sid} staff room needs six homeroom plus four subject teachers')
                check(len(f.get('teacherSeats',[]))==10,f'{sid} teacher assignments missing')
            if f['kind']=='computer':
                workstations=[o for o in f['fixtures'] if o['kind']=='computer-workstation']
                check(len(workstations)==24 and count('computer-monitor')==24,f'{sid} computer room needs 24 complete student stations')
                cx=f['width']//2
                check(sum(o['x']<cx for o in workstations)==12 and sum(o['x']>cx for o in workstations)==12,f'{sid} computer seats must flank the central aisle')
                check(len({o['y'] for o in workstations})==3,f'{sid} computer seats must form three rows')
                # A continuous physical corridor, including player radius, is
                # required between the entrance and the teaching area.
                check(all(not blocked(f,cx,3+i/4) for i in range((f['height']-7)*4+1)),f'{sid} computer central aisle obstructed')
        if f.get('kind')=='broadcast':
            reference_open=(18-2)*(18-3)
            check(.60<=len(open)/reference_open<=.70,f'{sid} broadcasting interior must remain60-70% of a classroom')
            check(any('방송' in z['label'] for z in f['interiorZones']),f'{sid} broadcasting work zones missing')
        if f.get('kind') in {'studentRestroom','staffRestroom'}:
            student=f['kind']=='studentRestroom';male=f['label'].endswith(' 남')
            count=lambda kind:sum(o['kind']==kind for o in f['fixtures'])
            expected_stalls=(2 if male else 3) if student else (2 if male else 3)
            check(count('toilet-base')==expected_stalls,f'{sid} incorrect toilet stall count for room type/sex')
            check(count('urinal-body')==((3 if student else 2) if male else 0),f'{sid} incorrect urinal facilities for room type/sex')
            check(count('washbasin-bowl')==(4 if student else 2),f'{sid} student/staff basin counts should differ')
            mirrors=[o for o in f['fixtures'] if o['kind']=='mirror-surface']
            check(len(mirrors)==1,f'{sid} requires one large wall mirror')
            check(all(o['width']>=(6.0 if student else 3.8) and o['height']>=1.0 and o['depth']<=.08 for o in mirrors),f'{sid} mirror must be a broad thin wall surface at human scale')
            check(any('거울' in z['label'] for z in f['interiorZones']),f'{sid} mirror needs an accessible viewing zone')
        if f.get('kind')=='classroom':
            lockers=[o for o in f['fixtures'] if o['kind']=='locker-compartment']
            banks=[o for o in f['fixtures'] if o['kind']=='locker-bank']
            check(len(banks)>=3,f'{sid} needs multiple rear locker banks')
            check(len({o['y'] for o in banks})==1,f'{sid} rear locker banks must align in one row')
            for bank in banks:
                assembly=bank.get('assembly')
                compartments=[o for o in lockers if assembly and o.get('assembly')==assembly]
                check(bool(assembly),f"{sid} locker bank needs an assembly id {bank['id']}")
                check(len(compartments)==32 and len({o['x'] for o in compartments})==4 and len({o['elevation'] for o in compartments})==8,f"{sid} locker bank must be4columns x8rows {bank['id']}")
                check(all(o['width']>=.5 and o['height']>=.18 for o in compartments),f"{sid} locker compartments too small {bank['id']}")
            check(len(lockers)==len(banks)*32,f'{sid} ungrouped locker compartments')
        if f.get('kind')=='music':
            labels={z['label'] for z in f['interiorZones']}
            check({'보컬 연습실','악기보관창고','연결 통로'}<=labels,f'{sid} music suite zones missing')
            check(len(f['interiorZones'])>=4,f'{sid} music requires four linked zones')
            check(f['width']!=f['height'],f'{sid} music should not repeat the square footprint')
            full_inner=(f['width']-2)*(f['height']-3)
            check(len(open)<full_inner*.95,f'{sid} music footprint should include a nonrectangular main/passage/suite arrangement')
        if f.get('kind')=='library':
            books=[o for o in f['fixtures'] if o['kind']=='books'];bodies=[o for o in f['fixtures'] if o['kind']=='shelf-body'];frames=[o for o in f['fixtures'] if o['kind'] in ['shelf-body','shelf-side','shelf-board','shelf-top']]
            check(bool(books) and bool(bodies),f'{sid} library books/shelves missing')
            check(all(not volume_overlap(fixture_bounds(book),fixture_bounds(body)) for book in books for body in frames),f'{sid} book meshes intersect shelf frames')
            check(len({(o['x'],o['y'],o.get('elevation',0)) for o in books})==len(books),f'{sid} duplicate coplanar book meshes')
    elif f.get('id')=='school-walkway':
        check(len(open)<f['width']*f['height']*.6,f'{sid} walkway must wrap a planted central area')
        check(len(f.get('portals',[]))==1,f'{sid} needs a return to the yard')
    elif f.get('isOutdoor'):
        check(f['id']=='playground' and not f.get('rooms') and not f.get('stairs'),f'{sid} outdoor map must remain a separate yard without another building or floor staircase')
        check(f['width']>=69 and f['height']>=55,f'{sid} yard must retain planned84x66m usable scale')
        check(bool(f.get('fixtures')) and len(f.get('landmarks',[]))>=5,f'{sid} missing outdoor structures or exploration landmarks')
        covers=[o for o in f.get('fixtures',[]) if o['kind']=='yard-cover-planter']
        check(len(covers)==2 and all(o['solid'] and o['width']>=7 and o['depth']>=2.5 for o in covers),f'{sid} needs two substantial staggered cover obstacles')
        check(len({o['x'] for o in covers})==2 and len({o['y'] for o in covers})==2,f'{sid} cover obstacles must be staggered')
        sources=[o for o in f.get('fixtures',[]) if o.get('solid') and o['kind'] in ['yard-trash-bin','yard-parcel-box','yard-water-station']]
        check(len(sources)>=6,f'{sid} needs distributed real search furniture for outdoor keys')
        routes=f.get('explorationRoutes',[])
        check({r['id'] for r in routes}=={'yard-central','yard-west','yard-east'},f'{sid} needs central and both side exploration routes')
        for route in routes:
            check(len(route.get('points',[]))>=3,f"{sid} route needs multiple waypoints: {route['id']}")
            check(all((p['x'],p['y']) in furniture_reached for p in route.get('points',[])),f"{sid} route waypoint unreachable: {route['id']}")
            check(all(edge_clear(f,(a['x'],a['y']),(b['x'],b['y'])) for a,b in zip(route['points'],route['points'][1:])),f"{sid} route segment lacks continuous player clearance: {route['id']}")
    else:
        check(not f.get('fixtures') and not f.get('interiorZones'),f'{sid} corridor must not contain room furniture')
        for r in f['rooms']:
            dx,dy={'south':(0,-1),'north':(0,1),'west':(-1,0),'east':(1,0)}[r['facing']]
            footprint={(x,y) for x in range(r['x0'],r['x1']+1) for y in range(r['y0'],r['y1']+1)}
            check(not r.get('interiorOpen') and not(footprint & open),f"{sid} corridor exposes room interior {r['label']}")
            check((r['doorX'],r['doorY']) not in open,f"{sid} door not closed {r['label']}")
            check((r['doorX']+dx,r['doorY']+dy) in furniture_reached,f"{sid} unreachable doorway {r['label']}")
            check(bool(r.get('interiorId')) and bool(r.get('interiorKind')),f"{sid} main-building room still lacks an enterable interior {r['label']}")
            if r.get('interiorId'):
                portal_ids.append(r['interiorId']);dest=rooms_by_id.get(r['interiorId'])
                check(dest is not None,f"{sid} missing portal destination {r['label']}")
                if dest:
                    check(dest['sourceFloor']==f['number'] and dest['label']==r['label'],f"{sid} mismatched room destination {r['label']}")
                    check(dest['returnX']==r['doorX']+dx and dest['returnY']==r['doorY']+dy,f"{sid} incorrect room return point {r['label']}")
        for i,a in enumerate(f['rooms']):
            for b in f['rooms'][i+1:]:check(a['x1']<b['x0'] or b['x1']<a['x0'] or a['y1']<b['y0'] or b['y1']<a['y0'],f"Overlapping rooms {a['label']} / {b['label']}")
        for passage in f.get('connectionPassages',[]):
            entry=(passage['entryX'],passage['entryY']);end=(passage['endX'],passage['endY'])
            check(entry in furniture_reached and end in furniture_reached,f"{sid} unreachable wing passage {passage['id']}")
            route=reachable(clear,entry,lambda a,b:edge_clear(f,a,b))
            check(end in route,f"{sid} wing passage not connected to main corridor {passage['id']}")
            direct=((end[0]-entry[0])**2+(end[1]-entry[1])**2)**.5*data['tileSize']
            check(direct>=passage.get('minLengthMeters',20),f"{sid} wing passage too short {passage['id']}")
        if f['number']==2:check(len(f.get('connectionPassages',[]))==2,'2F needs west and east walking passages')
        if f['number']==2:
            corrections=f.get('corridorCorrections',[])
            check(len(corrections)==1 and corrections[0].get('id')=='southwest-l-corridor','2F needs the user-drawn southwest L corridor')
            for correction in corrections:
                vertical=correction['vertical'];horizontal=correction['horizontal']
                check(correction['widthCells']==4 and vertical[2]-vertical[0]+1==4 and horizontal[3]-horizontal[1]+1==4,'2F southwest corridor must be four cells wide on both legs')
                for label,rect in [('vertical',vertical),('horizontal',horizontal)]:
                    x0,y0,x1,y1=rect
                    check(all((x,y) in open and not blocked(f,x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)),f'2F southwest {label} corridor contains a wall or narrow obstruction')
                    # Test the player's full radius near both edges, so a
                    # visible four-cell rectangle also provides that clearance.
                    inset=data['playerRadius']/data['tileSize']+.02
                    if label=='vertical':
                        samples=[(x,y) for x in [x0-.5+inset,x1+.5-inset] for y in range(y0,y1+1)]
                    else:samples=[(x,y) for y in [y0-.5+inset,y1+.5-inset] for x in range(x0,x1+1)]
                    check(all(not blocked(f,*p) for p in samples),f'2F southwest {label} corridor lacks player-radius edge clearance')
                classroom=next((r for r in f['rooms'] if r['label']=='1학년 5반'),None)
                check(bool(classroom) and classroom['facing']=='east' and classroom['doorX']==correction['classroomDoorX'] and classroom['doorY']==correction['classroomDoorY'] and classroom['doorX']<vertical[0],'2F empty classroom A must sit west of the vertical corridor and open east')
                points=[(correction['classroomDoorX']+1,correction['classroomDoorY']),
                        (correction['maleApproachX'],correction['maleApproachY']),
                        (correction['femaleApproachX'],correction['femaleApproachY']),
                        (correction['turnX'],correction['turnY']),
                        (vertical[2],vertical[3]-1)]
                check(all(p in furniture_reached for p in points),'2F classroom/toilets/north wing join must share the corrected corridor')
                check(all(edge_clear(f,a,b) for a,b in zip(points,points[1:])),'2F southwest corridor route segments need continuous player clearance')
        for s in f['stairs']:
            check((s['x'],s['y']) in furniture_reached,f"Unreachable stairs {s['id']}")
            dest=spaces_by_id.get(s.get('targetId')) or data['floors'][s['target']-1];point=(s['arrivalX'],s['arrivalY'])
            check(point in cells(dest),f"Blocked landing {s['id']}")
            for other in dest['stairs']:
                distance=((other['x']-point[0])**2+(other['y']-point[1])**2)**.5*data['tileSize']
                check(distance>1.55,f"Landing inside stair trigger {s['id']}")
    for portal in f.get('portals',[]):
        target=spaces_by_id.get(portal['targetId'])
        dx,dy={'south':(0,-1),'north':(0,1),'west':(-1,0),'east':(1,0)}[portal['facing']]
        approach=(portal['doorX']+dx,portal['doorY']+dy)
        check((portal['doorX'],portal['doorY']) not in open,f"{sid} external portal must block movement before E: {portal['id']}")
        check(approach in furniture_reached and not blocked(f,*approach),f"{sid} external portal approach is blocked: {portal['id']}")
        if portal.get('locked'):continue
        check(bool(target),f"{sid} missing external portal target: {portal['id']}")
        if target:
            arrival=(portal['arrivalX'],portal['arrivalY'])
            check(arrival in cells(target) and not blocked(target,*arrival),f"{sid} external portal arrival is unsafe: {portal['id']}")
            check(any(p['targetId']==sid for p in target.get('portals',[])) or target.get('returnSpaceId')==sid,f"{sid} external portal has no return connection: {portal['id']}")
    # Building diagrams show closed-room outlines; isolated interiors get separate diagrams.
    w,h=f['width']*16,f['height']*16+80;top=f['height']-1
    title=f.get('label',f"본관{f['number']}F")
    parts=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="#101d26"/><style>text{{font:10px sans-serif;fill:#d6dacf}}</style><text x="16" y="24" style="font-size:18px">폐교탈출 · {title}</text>']
    for x,y in sorted(open):parts.append(f'<rect x="{x*16}" y="{(top-y)*16+50}" width="16" height="16" fill="#62736c"/>')
    for r in f.get('rooms',[]):
        parts.append(f'<rect x="{r["x0"]*16}" y="{(top-r["y1"])*16+50}" width="{(r["x1"]-r["x0"]+1)*16}" height="{(r["y1"]-r["y0"]+1)*16}" fill="#263a41" stroke="#52645c"/>')
        x=r['doorX']*16;y=(top-r['doorY'])*16+50
        parts.append(f'<rect x="{x}" y="{y}" width="16" height="16" fill="#ad8551"/><text x="{x-12}" y="{y-4}">{r["label"]}</text>')
    for o in f.get('fixtures',[]):
        if o.get('solid'):parts.append(f'<rect x="{(o["x"]-.5*o["width"]/data["tileSize"]+.5)*16}" y="{(top-o["y"]-.5*o["depth"]/data["tileSize"]+.5)*16+50}" width="{o["width"]/data["tileSize"]*16}" height="{o["depth"]/data["tileSize"]*16}" fill="#ac9a72"/>')
    for s in f.get('stairs',[]):
        x=s['x']*16;y=(top-s['y'])*16+50
        parts.append(f'<rect x="{x}" y="{y}" width="16" height="16" fill="#dbbb74"/><text x="{x+20}" y="{y+12}">{s["label"]}</text>')
    for portal in f.get('portals',[]):
        x=portal['doorX']*16;y=(top-portal['doorY'])*16+50
        parts.append(f'<rect x="{x}" y="{y}" width="16" height="16" fill="#8ac7c0"/><text x="{x+20}" y="{y+12}">{portal["label"]}</text>')
    parts.append('</svg>')
    # Keep generated artifacts deterministic and avoid filenames from Korean room labels.
    dest=ROOT/(f'Docs/RoomPlans/{sid}.svg' if is_room else (('Docs/Playground.svg' if sid=='playground' else f'Docs/{sid}.svg') if f.get('isOutdoor') else f'Docs/Floor{f["number"]}.svg'))
    dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(''.join(parts))
check(set(portal_ids)=={id for id,r in rooms_by_id.items() if r.get('building') not in {'annex','west','grounds'} and r.get('kind')!='rooftop'},'Unlinked isolated room maps')
check(len(portal_ids)==len(set(portal_ids)),'Two entrances accidentally share one room instance')
for kind,floors in {'infirmary':[1],'administration':[1],'staff':[2,3,4],'computer':[4],'broadcast':[1],
                    'studentRestroom':[1,1,2,2,3,3,4,4],'staffRestroom':[1,1,2,2,3,3,4,4],'library':[3]}.items():
    actual=sorted(r['sourceFloor'] for r in rooms_by_id.values() if r.get('kind')==kind and r.get('building') not in {'annex','west','grounds'} and r.get('kind')!='rooftop')
    check(actual==floors,f'Requested support room instances missing or misplaced: {kind} {actual}')
door_count=sum(len(f['rooms']) for f in data['floors'])
check(len([r for r in rooms_by_id.values() if r.get('building') not in {'annex','west','grounds'} and r.get('kind')!='rooftop'])==door_count,'Every authored main-building room must link to its own independent furnished interior')
check(door_count==67,'Expected 67 main-building room entrances after four-storey expansion')
for (number,label),kind in completion_rooms.items():
    matches=[r for r in data['floors'][number-1]['rooms'] if r['label']==label]
    check(len(matches)==1,f'{number}F newly requested room missing or duplicated: {label}')
    dest=rooms_by_id.get(matches[0].get('interiorId')) if matches else None
    check(bool(dest) and dest['sourceFloor']==number and dest['label']==label and dest['kind']==kind,f'{number}F newly requested room not furnished and linked: {label}')
for f in data['floors']:
    for category in ['학생','교직원']:
        for sex in ['남','여']:
            matches=[r for r in f['rooms'] if r['label']==f'{category} 화장실 {sex}']
            check(len(matches)==1 and matches[0].get('interiorId') in rooms_by_id,f"{f['number']}F missing separate {category} {sex} toilet portal")
for grade in range(1,4):
    f=data['floors'][grade]
    check(all(any(r['label']==f'{grade}학년 {n}반' for r in f['rooms']) for n in range(1,7)),f'Grade {grade} needs six classes')
check(sum(r['label'].startswith('빈교실') for f in data['floors'] for r in f['rooms'])==2,'Only two vacant classrooms')
for kind,number in [('science',3),('music',3),('computer',4),('art',4)]:
    check(any(r['kind']==kind and r['sourceFloor']==number for r in rooms_by_id.values()),f'{kind} misplaced')
yard=spaces_by_id.get('playground')
gate=data.get('escapeGate',{})
check(len(data.get('outdoorMaps',[]))==2 and bool(yard) and yard.get('isOutdoor'),'Expected playground and walking path')
check(gate.get('spaceId')=='playground','Main escape gate must be only in the playground')
if yard:
    start=(yard['spawnX'],yard['spawnY']);clear={p for p in cells(yard) if not blocked(yard,*p)}
    reached=reachable(clear,start,lambda a,b:edge_clear(yard,a,b))
    check((gate.get('x'),gate.get('y')) in reached,'Outdoor escape gate interaction position must be reachable from the main entrance')
    check(not any(b['x']==24 and b['y']==1 for b in data['floors'][0]['barriers']),'Old 1F temporary exit barrier must not seal the playground door')
    graph={s['id']:[] for s in spaces}
    for s in spaces:
        graph[s['id']]+=[p['targetId'] for p in s.get('portals',[]) if not p.get('locked')]
        graph[s['id']]+=[r['interiorId'] for r in s.get('rooms',[]) if r.get('interiorId')]
        graph[s['id']]+=[st.get('targetId',f"floor-{st['target']}") for st in s.get('stairs',[])]
        if s.get('isRoom') and not s.get('noExit'):graph[s['id']].append(s.get('returnSpaceId',f"floor-{s['sourceFloor']}"))
    seen={'floor-1'};q=['floor-1']
    for sid in q:
        for target in graph[sid]:
            if target not in seen:seen.add(target);q.append(target)
    check(seen==set(graph),'Main building, all 53 rooms and playground must form one returnable space graph')
result={'kind':'static main building, annex, playground, portal rooms and continuous furniture clearance; not Unity execution','checks':checks,'passed':checks-len(errors),'errors':errors}
print(json.dumps(result,ensure_ascii=False,indent=2));raise SystemExit(bool(errors))
