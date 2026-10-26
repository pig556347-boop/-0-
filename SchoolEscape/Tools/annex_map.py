"""Original annex from the user's 2026-10-26 sketch; no sibling assets."""
from author_interiors import Interior, DIMENSIONS
from facility_rooms import build_restroom

D={'north':(0,1),'south':(0,-1),'east':(1,0),'west':(-1,0)}

def make(id,label,kind,w,h,floor=2):
    DIMENSIONS[kind]=(w,h)
    o=Interior({'number':floor},{'label':label,'interiorKind':kind,'doorX':0,'doorY':0,'facing':'north'},0)
    o.id=id;o.map.update(id=id,building='annex',label=label,subtitle=f'별관 {floor}층 · '+label,returnSpaceId='annex-2',returnX=21,returnY=4,portals=[])
    return o

def attach(parent,child,x,y,facing):
    dx,dy=D[facing]
    parent.map['portals'].append(dict(id=parent.id+'-to-'+child.id,label=child.label,doorX=x,doorY=y,facing=facing,targetId=child.id,arrivalX=child.cx,arrivalY=3))
    child.map.update(returnSpaceId=parent.id,returnX=x+dx,returnY=y+dy,exitLabel=parent.label+'로 나가기')
    parent.zone(child.label+' 앞',x+dx,y+dy)

def build_annex(f2):
    hub=make('annex-2','별관 2층 공용 복도','annexCorridor',42,28)
    hub.map.update(returnSpaceId='floor-2',returnX=97,returnY=13,exitLabel='본관 2층 연결통로로 나가기')
    f2['barriers']=[p for p in f2['barriers'] if p['x']!=98]
    grid=[list(row) for row in f2['rows']]
    for y in range(12,15):grid[y][98]='#'
    f2['rows']=[''.join(row) for row in grid]
    f2.setdefault('portals',[]).append(dict(id='main-to-annex',label='별관 2층 · 체육관 연결부',doorX=98,doorY=13,facing='west',targetId=hub.id,arrivalX=21,arrivalY=3))
    for p in f2['landmarks']:
        if p['x']==97:p['label']='별관 2층 출입문 앞'
    for x in [6,16,26,36]:
        hub.obj('annex-bench',x,23,2.8,.6,.5,'#736a52',solid=True)
    for x in [8,29]:
        hub.obj('sports-display-cabinet',x,12,3.0,.9,1.65,'#56655c',solid=True)
        for dx in [-.6,0,.6]:hub.obj('school-trophy-cup',x+dx,12,.22,.22,.35,'#b7a369',base=1.66,shape='cylinder')
    hub.zone('중앙 통행로',21,14);hub.zone('체육관 앞',6,24)
    gym=make('annex-gym','체육관 · 2~3층 통층','gym',46,38)
    gym.map.update(doubleHeight=True,ceilingHeightMeters=7.6,occupiedFloors=[2,3])
    # 30 x 20 m court, with broad escape aisles around benches and stage.
    for n in range(50):gym.obj('gym-parquet-strip',11+(n+.5)*24/50,19,30/50-.012,25,.015,['#927c50','#9a8355','#8c784e'][n%3],base=.024)
    gym.obj('court-center-circle',23,19,4.8,4.8,.07,'#d8d0ad',base=.046,shape='torus')
    for x in [11,35]:gym.obj('court-line',x,19,.065,26,.012,'#c3bba1',base=.048)
    for y in [9,29]:gym.obj('court-line',23,y,30,.065,.012,'#c3bba1',base=.048)
    gym.obj('court-half-line',23,19,.065,25,.012,'#c3bba1',base=.048)
    for x in [10,36]:
        gym.obj('basketball-post',x,19,.22,.22,3.3,'#435c59',solid=True)
        gym.obj('basketball-board',x,19,.16,1.8,1.05,'#c2c7b7',base=2.7)
        gym.obj('basketball-rim',x+(.7 if x==10 else -.7),19,.55,.55,.055,'#b17442',base=2.9,shape='torus')
    gym.obj('gym-stage',23,34,15,3.2,.6,'#7e6950',solid=True)
    gym.obj('gym-curtain',23,36,16,.15,3.8,'#52383a',base=.6)
    for x in [3,42]:
        for y in [10,17,24]:gym.obj('gym-bleacher',x,y,2.2,4,.75,'#77745e',solid=True)
    gym.obj('gym-high-back-wall',23,37,55,.22,7.6,'#3a4e50')
    for x in [5,12,19,26,33,40]:
        for z in [2.7,5.2]:gym.obj('gym-high-window',x,36.88,4,.08,1.3,'#809f9a',base=z)
    gym.zone('코트 중앙',23,19);gym.zone('무대 앞',23,30);gym.zone('서쪽 우회로',7,19);gym.zone('동쪽 우회로',39,19)
    fitness=make('annex-fitness','체력단련실','fitness',24,22)
    for x in [5,10,16,20]:
        for y in [8,15]:
            fitness.obj('fitness-bench',x,y,.7,1.8,.55,'#404d51',solid=True)
            fitness.obj('weight-rack',x,y+1.2,1.3,.3,1.5,'#626b66',solid=True)
            fitness.obj('barbell',x,y+1.2,1.8,.08,.08,'#b0b6ab',base=1.25)
            for dx in [-.65,.65]:fitness.obj('weight-disc',x+dx,y+1.2,.16,.5,.5,'#2f383b',base=1,shape='cylinder')
    fitness.shelf(5,19,3);fitness.zone('기구 사이 통로',13,11);fitness.zone('안쪽 스트레칭',13,19)
    store=make('annex-equipment','체육용품 창고','sportsStorage',20,18)
    for x in [4,9,15]:store.shelf(x,14,2.4)
    for x in [4,15]:
        store.obj('sports-equipment-crate',x,7,1.8,1.4,.7,'#65705a',solid=True)
        for dx in [-.4,.4]:store.obj('basketball',x+dx,7,.4,.4,.4,'#a47944',base=.72,shape='sphere')
    store.zone('보관 통로',10,9);store.zone('선반 앞',10,12)
    control=make('annex-gym-control','체육관 제어실','gymControl',9,9)
    control.table(4,6,2.6,.8,'gym-control-desk');control.obj('gym-control-monitor',4,6,.8,.1,.48,'#91aaa3',base=.85)
    control.obj('gym-control-switchboard',7,5,.3,2,1.5,'#48625d',solid=True)
    control.zone('조작 패널 앞',5,4)
    male=make('annex-toilet-m','학생 화장실 남','studentRestroom',16,18);build_restroom(male)
    female=make('annex-toilet-f','학생 화장실 여','studentRestroom',16,18);build_restroom(female)
    lounge=make('annex-student-lounge','학생휴게실','studentLounge',22,18)
    for x in [5,16]:
        for y in [7,13]:lounge.obj('student-lounge-sofa',x,y,2.4,.9,.8,'#55675e',solid=True)
    lounge.table(10,10,2,.8,'lounge-table');lounge.shelf(5,15,2.5,books=True);lounge.zone('중앙 휴게 공간',12,8)
    theory=make('annex-sports-class','체육 이론 교실','sportsClass',22,22)
    for x in [5,10,16]:
        for y in [7,11,15]:theory.table(x,y,1.2,.7);theory.chair(x,y-.7)
    theory.obj('sports-tactics-board',10,20,8,.12,1.25,'#385c51',base=.8)
    theory.table(15,18,2,.7,'teacher-desk');theory.zone('전술 수업 앞',10,18)
    practice=make('annex-practice','체조·무용 실습실','sportsPractice',24,22)
    for x in [6,12,18]:
        for y in [8,14]:practice.obj('gymnastics-mat',x,y,4,3,.055,'#626f67',base=.025)
    practice.obj('dance-barre',12,19,21,.08,.08,'#a39877',base=1.0)
    practice.shelf(4,4,2);practice.zone('실습 중앙',12,11);practice.zone('안쪽 연습 구역',12,17)
    for obj,x,y,face in [(gym,6,26,'south'),(fitness,16,26,'south'),(store,26,26,'south'),(control,36,26,'south'),(male,1,10,'east'),(female,1,20,'east'),(lounge,40,8,'west'),(theory,40,18,'west'),(practice,11,1,'north')]:attach(hub,obj,x,y,face)
    cafe=make('annex-cafeteria','별관 1층 급식실','cafeteria',38,32,1);cafe.map['noExit']=True
    for x in [6,14,23,31]:
        for y in [8,13,18]:
            cafe.table(x,y,3.0,1.2,'cafeteria-table')
            for dy in [-1,1]:cafe.obj('cafeteria-bench',x,y+dy,2.8,.45,.48,'#7b7861',solid=True)
    cafe.obj('cafeteria-serving-counter',14,25,20,1.3,.95,'#87948b',solid=True)
    for x in [7,11,15,19]:cafe.obj('cafeteria-food-tray',x,25,1,.8,.15,'#c1c3af',base=.97)
    cafe.zone('식탁 사이 중앙 동선',19,13);cafe.zone('배식대 앞',19,23);cafe.zone('동쪽 귀환 동선',34,18)
    food=make('annex-food-store','급식실 창고','foodStorage',18,16,1)
    for x in [4,8,13]:food.shelf(x,12,2)
    for x in [4,13]:food.obj('food-supply-crate',x,7,1.5,1.2,.85,'#8b805d',solid=True)
    food.zone('식자재 통로',9,8);attach(cafe,food,36,25,'west')
    hub.map['stairs']=[dict(id='annex-down',x=35,y=4,targetId=cafe.id,target=1,arrivalX=32,arrivalY=25,label='급식실 1층 ↓')]
    cafe.map['stairs']=[dict(id='annex-up',x=32,y=28,targetId=hub.id,target=2,arrivalX=35,arrivalY=7,label='체육관 연결부 2층 ↑')]
    hub.zone('급식실 계단 앞',35,7);cafe.zone('2층 계단 앞',32,25)
    # Portal cells are sealed walls; interact from the corridor-facing side.
    allrooms=[hub,gym,fitness,store,control,male,female,lounge,theory,practice,cafe,food]
    for obj in allrooms:
        grid=[list(r) for r in obj.map['rows']]
        for p in obj.map['portals']:grid[p['doorY']][p['doorX']]='#'
        obj.map['rows']=[''.join(r) for r in grid]
    return [o.map for o in allrooms]
