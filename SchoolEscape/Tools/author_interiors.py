"""Large isolated room spaces from the user's map backup.

Base floor room footprints stay opaque and closed. Each furnished room has its
own navigation grid, fixed-size furniture and south exit. Only music's practice
room / connecting passage / instrument store share a continuous room space.
"""
import re
T=1.25
WOOD='#80664b';METAL='#48585a';PAPER='#c6bda4'
DIRECTIONS={'north':(0,1),'south':(0,-1),'west':(-1,0),'east':(1,0)}
DIMENSIONS={'classroom':(18,18),'music':(36,34),'art':(26,24),'artStorage':(18,14),'library':(34,32),
            'infirmary':(24,22),'administration':(26,24),'staff':(28,26),'computer':(28,26),
            'broadcast':(15,15),'studentRestroom':(16,18),'staffRestroom':(14,16),
            'storage':(22,20),'assembly':(32,28),'maintenance':(22,20),'seminar':(26,24),
            'preparation':(22,20),'archives':(26,24),'lounge':(26,22),'counseling':(24,22),
            'meeting':(28,24),'principal':(28,24),'control':(26,24)}

def room_kind(label):
    if re.search(r'학년 \d반|빈교실|자습실',label):return 'classroom'
    if label.endswith('교무실'):return 'staff'
    if label.startswith('학생 화장실'):return 'studentRestroom'
    if label.startswith('교직원 화장실'):return 'staffRestroom'
    if label=='방송실':return 'broadcast'
    return {'음악실':'music','미술실':'art','미술실 창고':'artStorage','도서실':'library',
            '보건실':'infirmary','양호실':'infirmary','행정실':'administration','컴퓨터실':'computer',
            '창고':'storage','대회의실':'assembly','설비관리실':'maintenance','세미나실':'seminar',
            '준비실 1':'preparation','자료실':'archives','교직원 휴게실':'lounge',
            '학부모상담실':'counseling','메인회의실':'meeting','교장실':'principal','중앙통제실':'control'}.get(label,'')

class Interior:
    def __init__(self,base,room,index):
        self.room=room;self.kind=room['interiorKind'];self.label=room['label']
        self.width,self.height=DIMENSIONS[self.kind];self.cx=self.width//2
        self.id=f'room-f{base["number"]}-{index}'
        self.fixtures=[];self.zones=[]
        dx,dy=DIRECTIONS[room['facing']]
        self.map=dict(id=self.id,label=self.label,kind=self.kind,isRoom=True,number=base['number'],sourceFloor=base['number'],
            subtitle=self.label+' · 독립 실내 공간',width=self.width,height=self.height,
            spawnX=self.cx,spawnY=3,exitX=self.cx,exitY=1,
            returnX=room['doorX']+dx,returnY=room['doorY']+dy,
            rows=[''.join('.' if 1<=x<self.width-1 and 2<=y<self.height-1 else '#' for x in range(self.width)) for y in range(self.height)],
            rooms=[],stairs=[],barriers=[],landmarks=[dict(label='입구',x=self.cx,y=3)],fixtures=self.fixtures,interiorZones=self.zones)
        self.zone('입구',self.cx,3)
    def obj(self,kind,x,y,w,d,h,color=WOOD,base=0,solid=False,label='',shape='box',assembly=''):
        item=dict(id=f'{self.id}-{len(self.fixtures):04d}',room=self.label,kind=kind,label=label,
            x=round(x,3),y=round(y,3),width=round(w,3),depth=round(d,3),height=round(h,3),elevation=round(base,3),
            color=color,solid=solid,shape=shape)
        if assembly:item['assembly']=assembly
        self.fixtures.append(item)
        return item
    def zone(self,label,x,y):self.zones.append(dict(room=self.label,label=label,x=x,y=y))
    def table(self,x,y,w=.78,d=.54,kind='student-desk'):
        self.obj(kind,x,y,w,d,.1,base=.70,solid=True)
        self.obj('desk-drawer',x,y,w*.78,d*.8,.16,'#5e5141',base=.51)
        for dx in [-1,1]:
            for dy in [-1,1]:self.obj('desk-leg',x+dx*(w/2-.055)/T,y+dy*(d/2-.05)/T,.05,.05,.70,METAL)
    def chair(self,x,y):
        self.obj('chair-seat',x,y,.40,.40,.08,base=.42,solid=True)
        self.obj('chair-back',x,y-.14,.42,.055,.36,base=.50)
        for dx in [-.14,.14]:
            for dy in [-.14,.14]:self.obj('chair-leg',x+dx/T,y+dy/T,.035,.035,.42,METAL)
    def shelf(self,x,y,w=2.0,d=.65,h=1.7,books=False):
        """Open-backed shelf construction: no opaque box through the books.

        Books have at least 25mm vertical clearance and 30mm rear clearance;
        their spines sit forward of the back panel and apart from side panels.
        No coincident book/body planes can fight for depth as the camera moves.
        """
        assembly=f'{self.id}-shelf-{len(self.fixtures):04d}'
        self.obj('shelf-body',x,y+(d/2-.025)/T,w-.12,.05,h,'#4d5148',solid=True,assembly=assembly)
        for side in [-1,1]:self.obj('shelf-side',x+side*(w/2-.03)/T,y,.06,d,h,solid=True,assembly=assembly)
        for level,z in enumerate([.05,.50,.95,1.40]):
            self.obj('shelf-board',x,y-.015/T,w-.12,d-.03,.05,base=z,solid=True,assembly=assembly)
            if level==3:continue
            columns=8 if books else 4
            inside=w-.20;spacing=inside/columns
            for i in range(columns):
                xx=x+((i+.5)*spacing-inside/2)/T
                height=(.28+.018*(i%3)) if books else .26
                self.obj('books' if books else 'supply-box',xx,y-.015/T,spacing*.72,d-.18,height,
                    ['#757c69','#8a6652','#637883','#a49978'][i%4],base=z+.075,assembly=assembly)
        self.obj('shelf-top',x,y,w-.12,d-.03,.05,base=h-.05,assembly=assembly)
    def bust(self,x,y):
        self.obj('plaster-plinth',x,y,.45,.42,.65,'#6e756c',solid=True)
        self.obj('plaster-shoulders',x,y,.43,.23,.18,'#d3cfc0',base=.65)
        self.obj('plaster-neck',x,y,.10,.10,.12,'#d3cfc0',base=.83,shape='cylinder')
        self.obj('plaster-head',x,y,.24,.25,.32,'#ded9ca',base=.91,shape='sphere')
        self.obj('plaster-nose',x,y-.1,.055,.075,.08,'#ded9ca',base=1.04)
    def easel(self,x,y):
        self.obj('easel-foot',x,y,.56,.50,.06,solid=True)
        self.obj('easel-mast',x,y,.065,.07,1.35)
        self.obj('canvas-frame',x,y,.68,.095,.65,base=.58)
        self.obj('canvas',x,y-.06,.59,.025,.55,PAPER,base=.63)
        self.obj('unfinished-painting',x-.06,y-.078,.24,.012,.22,'#73877a',base=.80)
    def lockers(self,x,y):
        self.obj('locker-bank',x,y,1.40,.44,1.67,'#536865',solid=True)
        for col in range(4):
            for row in range(8):
                xx=x+(col-1.5)*.335/T
                self.obj('locker-compartment',xx,y-.19,.31,.035,.185,['#71857b','#7e8b7b'][(col+row)%2],base=.06+row*.198)
                self.obj('locker-handle',xx+.085,y-.212,.025,.025,.035,'#b8b7a0',base=.13+row*.198)
    def classroom(self):
        self.obj('blackboard-frame',9,16.4,6.2,.10,.95,base=.72)
        self.obj('long-blackboard',9,16.34,6.0,.025,.78,'#27493f',base=.80)
        self.obj('chalk-tray',9,16.24,6.0,.12,.045,'#aaa993',base=.76)
        self.obj('ceiling-TV-arm',15,16,.08,.08,.35,METAL,base=1.60)
        self.obj('ceiling-TV',15,15.8,.72,.40,.48,'#273233',base=1.30)
        self.obj('TV-glass',15,15.62,.54,.025,.32,'#657d7c',base=1.38)
        self.table(6,15,1.20,.70,'teacher-desk')
        for x in [3,5,7,11,13,15]:
            for y in [5,7,9,11,13]:self.table(x,y,.75,.55);self.chair(x,y-.56)
        from interior_dressing import locker_banks
        locker_banks(self)
        self.zone('중앙 통로',9,9);self.zone('칠판 앞',9,15);self.zone('뒤쪽 사물함',5,4)
    def music(self):
        from music_layout import build_music
        build_music(self)
    def art(self):
        for x in [4,8,18,22]:
            for y in [6,10,14]:self.table(x,y,1.80,.85,'art-worktable')
        for x,y in [(3,20),(8,20),(18,20)]:self.bust(x,y)
        for x,y in [(3,8),(9,17),(18,8),(22,17)]:self.easel(x,y)
        self.table(13,21,2.2,.8,'art-teacher-workbench')
        self.obj('sink-cabinet',23,3.7,.70,2.4,.80,'#8f9c90',solid=True)
        for yy in [3.1,3.7,4.3]:
            self.obj('sink-rim',23,yy,.73,.60,.06,'#c3c4b5',base=.80)
            self.obj('sink-basin',23,yy,.50,.42,.025,'#596d69',base=.87)
            self.obj('sink-tap',23.23,yy,.045,.045,.22,METAL,base=.87)
        # Original geometric plaster standing figure and hand/foot casts.
        self.obj('plaster-statue-base',22,20,.65,.60,.48,'#777d70',solid=True)
        for dx in [-.12,.12]:self.obj('plaster-statue-leg',22+dx/T,20,.14,.18,.48,'#d3cfc0',base=.48)
        self.obj('plaster-statue-torso',22,20,.42,.24,.48,'#d3cfc0',base=.96)
        self.obj('plaster-statue-head',22,20,.24,.25,.31,'#ded9ca',base=1.47,shape='sphere')
        for dx in [-.29,.29]:self.obj('plaster-statue-arm',22+dx/T,20,.11,.15,.52,'#d3cfc0',base=.98)
        self.table(6,18,1.4,.7,'plaster-display-table')
        self.obj('plaster-hand',5.75,18,.15,.24,.06,'#ded9ca',base=.81)
        for i in range(4):self.obj('plaster-finger',5.75+(i-1.5)*.035/T,18+.12,.025,.12,.055,'#ded9ca',base=.81)
        self.obj('plaster-foot',6.25,18,.13,.30,.085,'#ded9ca',base=.81)
        self.zone('작업실 중앙',13,11);self.zone('석고상 구역',13,19);self.zone('세면대 앞',21,4)
    def artStorage(self):
        for xx in [3,6,12,15]:self.shelf(xx,11,1.8,.6)
        for xx in [3,6,12,15]:self.easel(xx,6)
        for xx in [3,5,13,15]:self.obj('stacked-canvas',xx,9,.95,.22,1.25,PAPER,solid=True)
        for xx in [4,14]:self.obj('art-supply-crate',xx,3.5,.70,.60,.50,'#84775b',solid=True)
        self.zone('미술 재료창고',9,7);self.zone('보관 선반 앞',9,10)
    def library(self):
        for x in [4,8,12,22,26,30]:
            for y in [8,12,16,20,24]:self.shelf(x,y,2.0,.65,books=True)
        self.table(23,4,2.2,.75,'loan-counter')
        self.obj('loan-register',23,4,.48,.38,.25,'#38453d',base=.82)
        for x in [6,12,23,29]:
            self.table(x,28,1.60,.85,'reading-table');self.chair(x-.45,27.3);self.chair(x+.45,27.3)
        for x in [3,6,29,32]:self.table(x,5,.85,.55,'individual-reading-desk')
        self.zone('대출 데스크 앞',17,5);self.zone('서가 중앙 통로',17,16);self.zone('안쪽 열람 구역',17,28)
        self.zone('서쪽 서가 통로',2,16);self.zone('동쪽 서가 통로',32,16)
    def build(self):
        if self.kind in {'infirmary','administration','staff','computer'}:
            from support_rooms import build_support
            build_support(self)
        elif self.kind in {'broadcast','studentRestroom','staffRestroom'}:
            from facility_rooms import build_broadcast,build_restroom
            (build_broadcast if self.kind=='broadcast' else build_restroom)(self)
        elif self.kind in {'storage','assembly','maintenance','seminar','preparation','archives',
                           'lounge','counseling','meeting','principal','control'}:
            from remaining_rooms import build_remaining
            build_remaining(self)
        else:getattr(self,self.kind)()
        from interior_dressing import add_details
        add_details(self)
        return self.map

def populate(floors):
    room_maps=[]
    for base in floors:
        base.update(id=f'floor-{base["number"]}',label=f'본관 {base["number"]}층',kind='corridor',isRoom=False,sourceFloor=base['number'],
            spawnX=base['landmarks'][0]['x'],spawnY=base['landmarks'][0]['y'],fixtures=[],interiorZones=[])
        for index,room in enumerate(base['rooms']):
            room['interiorKind']=room_kind(room['label']);room['interiorOpen']=False;room['interiorId']=''
            if room['interiorKind']:
                interior=Interior(base,room,index);room['interiorId']=interior.id;room_maps.append(interior.build())
    return room_maps
