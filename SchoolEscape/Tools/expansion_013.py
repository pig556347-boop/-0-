"""User sketch: gym gallery, west wing and reversible kitchen collapse."""
from annex_map import make, attach
from author_interiors import Interior
from facility_rooms import build_restroom

def wrap(m):
    o=object.__new__(Interior);o.map=m;o.id=m['id'];o.label=m['label'];o.kind=m['kind'];o.width=m['width'];o.height=m['height'];o.cx=o.width//2;o.fixtures=m['fixtures'];o.zones=m['interiorZones'];return o

def seal(o):
    rows=[list(r) for r in o.map['rows']]
    for p in o.map.get('portals',[]):rows[p['doorY']][p['doorX']]='#'
    o.map['rows']=[''.join(r) for r in rows]

def locked(o,id,label,x,y,face):
    o.map.setdefault('portals',[]).append(dict(id=id,label=label+' · 단단히 잠김',doorX=x,doorY=y,facing=face,targetId='',arrivalX=0,arrivalY=0,locked=True))
    # Heavy crossbars and a padlock are actual visible geometry.
    horizontal=face in ['north','south']
    dx,dy={'north':(0,1),'south':(0,-1),'east':(1,0),'west':(-1,0)}[face]
    for z in [.65,1.25]:o.obj('locked-door-crossbar',x+dx*.58,y+dy*.58,1.3 if horizontal else .12,.12 if horizontal else 1.3,.10,'#68736d',base=z)
    o.obj('locked-door-padlock',x+dx*.65,y+dy*.65,.19,.16,.25,'#9b8c5f',base=.88)
    seal(o)

def stairs(a,b,ax,ay,bx,by):
    a.map['stairs'].append(dict(id=a.id+'-to-'+b.id,x=ax,y=ay,targetId=b.id,target=b.map['sourceFloor'],arrivalX=bx,arrivalY=by-3,label=b.label+' ↑↓'))
    b.map['stairs'].append(dict(id=b.id+'-to-'+a.id,x=bx,y=by,targetId=a.id,target=a.map['sourceFloor'],arrivalX=ax,arrivalY=ay-3,label=a.label+' ↑↓'))
    a.zone(b.label+' 계단 앞',ax,ay-3);b.zone(a.label+' 계단 앞',bx,by-3)

def office(o,theme='자료'):
    for x in [4,o.width-5]:
        for y in [6,11]:o.table(x,y,1.6,.8,theme+'-desk');o.chair(x,y-.7)
    for x in [4,o.width-5]:o.shelf(x,o.height-3,2.2,books=True)
    o.zone('중앙 조사 동선',o.cx,9);o.zone('자료 선반 앞',o.cx,o.height-4)

def expand_school(floors,rooms,yard):
    added=[];maps={r['id']:wrap(r) for r in rooms};gym=maps['annex-gym'];cafe=maps['annex-cafeteria'];practice=maps['annex-practice']
    # Correct basketball proportions: 28 x 15m playing area, complete key boxes.
    gym.fixtures[:]=[f for f in gym.fixtures if not f['kind'].startswith('court-') and f['kind']!='gym-parquet-strip']
    for n in range(56):gym.obj('gym-parquet-strip',23+(n-27.5)*.4,19,.49,15,.015,['#927c50','#9a8355','#8c784e'][n%3],base=.024)
    for x in [11.8,34.2]:gym.obj('court-line',x,19,.06,15,.012,'#ddd7b9',base=.048)
    for y in [13,25]:gym.obj('court-line',23,y,28,.06,.012,'#ddd7b9',base=.048)
    gym.obj('court-half-line',23,19,.06,15,.012,'#ddd7b9',base=.048)
    gym.obj('court-center-circle',23,19,3.6,3.6,.08,'#ddd7b9',base=.048,shape='courtRing')
    for x in [15.5,30.5]:
        gym.obj('court-free-throw-circle',x,19,3.6,3.6,.08,'#ddd7b9',base=.048,shape='courtRing')
        for y in [17.04,20.96]:gym.obj('court-key-line',(x+(11.8 if x<23 else 34.2))/2,y,4.625,.06,.012,'#ddd7b9',base=.048)
        gym.obj('court-key-line',x,19,.06,4.9,.012,'#ddd7b9',base=.048)
    for f in gym.fixtures:
        if f['kind'].startswith('basketball-'):
            left={'basketball-post':10.8,'basketball-board':12.76,'basketball-rim':13.06}[f['kind']]
            f['x']=left if f['x']<23 else 46-left
    for x,shape in [(13.06,'courtArcEast'),(32.94,'courtArcWest')]:gym.obj('court-three-point-arc',x,19,13.5,13.5,.06,'#ddd7b9',base=.048,shape=shape)
    gym.obj('sports-equipment-crate',7,25,1.6,1.2,.8,'#697770',solid=True)
    for dx in [-.4,0,.4]:gym.obj('basketball',7+dx,25,.4,.4,.4,'#a66f3d',base=.8,shape='sphere')
    gym.obj('gym-scoreboard',23,36.7,5,.12,1.5,'#182c31',base=3)
    for x in [21.7,24.3]:gym.obj('scoreboard-digit',x,36.6,.55,.05,.75,'#b9a478',base=3.4)
    gallery=make('annex-gallery','체육관 3층 관람통로','gymGallery',46,38,3);gallery.map.update(noExit=True,voidRects=[[6,6,39,31]],courtView='annex-gym')
    rows=[list(r) for r in gallery.map['rows']]
    for y in range(6,32):
        for x in range(6,40):rows[y][x]='#'
    gallery.map['rows']=[''.join(r) for r in rows]
    for x in [5.5,39.5]:gallery.obj('gallery-railing',x,18.5,.1,32.5,1,'#6c7e77',solid=True)
    for y in [5.5,31.5]:gallery.obj('gallery-railing',22.5,y,42.5,.1,1,'#6c7e77',solid=True)
    gallery.map['spawnX']=42;gallery.map['spawnY']=29;gallery.map['interiorZones'][:]=[]
    gallery.zone('동쪽 관람통로',42,20);gallery.zone('서쪽 관람통로',3,20);gallery.zone('북쪽 조사실 앞',23,34)
    stairs(gym,gallery,42,32,44,10)
    gym.map['stairs'][-1].update(arrivalX=42,arrivalY=10)
    gallery.zones[-1].update(x=42,y=10)
    for id,label,x in [('sports-records','체육 기록실',10),('video-analysis','경기 영상분석실',23),('sports-club','스포츠 동아리실',36)]:
        o=make('annex-'+id,label,'sportsOffice',20,18,3);office(o);attach(gallery,o,x,36,'south');added.append(o)
    added.append(gallery)
    changing=make('annex-changing','체조·무용실 탈의실','changing',10,10)
    changing.lockers(3,7);changing.obj('changing-bench',6,5,2,.5,.5,'#817655',solid=True);changing.zone('탈의 공간',5,4);attach(practice,changing,22,12,'west');added.append(changing)
    locked(cafe,'cafeteria-exterior','급식실 외부 출입구',7,1,'north')
    yo=wrap(yard);locked(yo,'yard-annex-locked','별관 외부 출입구',57,53,'south');locked(yo,'yard-west-locked','서관 외부 출입구',10,53,'south')
    # Wide kitchen initially; collapse later leaves the authored one-cell route.
    kitchen=make('annex-kitchen','급식실 조리실','kitchen',53,38,1);kitchen.map.update(noExit=True,noLoot=True,spawnX=24,spawnY=34)
    attach(cafe,kitchen,1,25,'east');cafe.map['portals'][-1].update(arrivalX=24,arrivalY=34)
    for id,x,y,face in [('kitchen-entry',24,36,'south'),('kitchen-emergency',1,20,'east')]:kitchen.map['portals'].append(dict(id=id,label='급식실로 돌아가기' if id=='kitchen-entry' else '서쪽 비상 출입문',doorX=x,doorY=y,facing=face,targetId=cafe.id,arrivalX=2,arrivalY=25))
    for x,y,w,d in [(8,30,10,5),(34,23,7,4),(7,7,7,4),(26,20,8,5)]:
        kitchen.obj('kitchen-prep-counter',x,y,w,d,.95,'#8c9991',solid=True)
        kitchen.obj('kitchen-counter-top',x,y,w+.05,d+.05,.07,'#bbc1ae',base=.95)
    for x in [31,35,39,43]:kitchen.obj('cooking-boiler',x,35,1.4,1.4,1.3,'#576d69',solid=True,shape='cylinder')
    kitchen.map['interiorZones'][:]=[];kitchen.zone('입구',24,34);kitchen.zone('중앙 작업 구역',20,24);kitchen.zone('서쪽 비상문 앞',2,20)
    waypoints=[[24,35],[24,28],[28,28],[28,33],[46,33],[46,28],[42,28],[42,19],[48,19],[48,15],[40,15],[40,11],[35,11],[35,15],[30,15],[30,10],[24,10],[24,13],[20,13],[20,10],[15,10],[15,15],[11,15],[11,20],[2,20]]
    path=[waypoints[0]]
    for target in waypoints[1:]:
        x,y=path[-1]
        while [x,y]!=target:
            x+=(target[0]>x)-(target[0]<x);y+=(target[1]>y)-(target[1]<y);path.append([x,y])
    kitchen.obj('kitchen-reward-crate',3,22,1.3,.8,.85,'#8a7959',solid=True)
    kitchen.zone('조사할 보급 상자',3,21)
    kitchen.map['kitchenSequence']=dict(rewardX=3,rewardY=22,path=path,triggerPortal='kitchen-entry',escapePortal='kitchen-emergency',monster='배식장',leadSeconds=4)
    added.append(kitchen)
    # West wing uses the saved building program, with an independent staff kitchen.
    def west(id,label,kind,w=24,h=22,floor=2):
        o=make('west-'+id,label,kind,w,h,floor);o.map.update(building='west',subtitle=f'서관 {floor}층 · '+label);added.append(o);return o
    hubs=[]
    for n in [1,2,3]:
        h=west('floor-'+str(n),'서관 '+str(n)+'층 공용 복도','westCorridor',36,26,n);h.map['noExit']=n!=2;h.map.update(returnSpaceId='floor-2',returnX=3,returnY=13,exitLabel='본관 2층 연결통로')
        for x in [7,27]:h.obj('west-corridor-bench',x,17,3,.7,.5,'#5e7163',solid=True)
        hubs.append(h)
    f2=floors[1];f2['barriers']=[p for p in f2['barriers'] if p['x']!=2]
    f2['portals'].append(dict(id='main-to-west',label='서관 2층 연결부',doorX=2,doorY=13,facing='east',targetId=hubs[1].id,arrivalX=18,arrivalY=3))
    rows=[list(r) for r in f2['rows']]
    for y in range(12,15):rows[y][2]='#'
    f2['rows']=[''.join(r) for r in rows]
    stairs(hubs[0],hubs[1],30,7,30,7);stairs(hubs[1],hubs[2],30,21,30,21)
    programs=[(0,'dining','서관 직원급식실','staffDining'),(0,'prep','서관 준비조리실','prepKitchen'),(0,'freezer','냉동창고','freezer'),(1,'dorm','경비원 기숙사실','guardDorm'),(1,'pantry','탕비실','pantry'),(1,'restroom','교직원 화장실','westRestroom'),(1,'control','서관 통제실','westControl'),(2,'records','통제구역 기록실','restrictedRecords'),(2,'surveillance','감시실','surveillance'),(2,'server','서버실','serverRoom')]
    doorpos=[(6,24,'south'),(18,24,'south'),(1,12,'east'),(34,12,'west')];count={0:0,1:0,2:0}
    for n,id,label,kind in programs:
        o=west(id,label,kind,floor=n+1);i=count[n];count[n]+=1;attach(hubs[n],o,*doorpos[i])
        if id=='dining':
            for x in [5,12,19]:
                for y in [7,13]:o.table(x,y,2.6,1,'dining-table');o.chair(x,y-.8)
        elif id in ['prep','pantry']:
            for x in [5,12,19]:o.obj('kitchen-storage-cabinet',x,17,2.2,.8,1.6,'#6b8179',solid=True)
            for x in [6,17]:o.table(x,10,3,1.3,'prep-table')
        elif id=='freezer':
            for x in [4,9,16,21]:o.shelf(x,17,2)
            for x in [5,18]:o.obj('freezer-crate',x,8,2,2,.8,'#82989a',solid=True)
        elif id=='dorm':
            for x in [4,19]:
                for y in [8,15]:o.obj('guard-bed',x,y,1.6,2.6,.65,'#6c7d6b',solid=True)
            o.lockers(10,19);o.table(12,10,2,.8,'guard-desk')
        elif id=='restroom':
            o.kind='staffRestroom';o.map['kind']='staffRestroom';o.label+=' 남';o.map['label']=o.label;build_restroom(o)
        else:
            office(o)
            if id in ['control','surveillance','server']:
                for x in [5,10,15,20]:o.obj('surveillance-monitor',x,19,1.3,.16,.8,'#668f87',base=1.1)
        o.zone('중앙 동선',12,9);o.zone('안쪽 동선',12,18)
    locked(hubs[0],'west-exterior','서관 외부 출입구',7,1,'north')
    for o in [*maps.values(),*added]:seal(o)
    for i,f in enumerate(gym.fixtures):f['id']=f'{gym.id}-013-{i:04d}'
    rooms.extend(o.map for o in added)
