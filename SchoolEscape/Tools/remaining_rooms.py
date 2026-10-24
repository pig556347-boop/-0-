"""Original main-building rooms completing the remaining closed room labels.

Use the same 1.25m grid and fixed metre furniture as our earlier school rooms.
The south entrance, side loops and named exploration zones stay clear; these
are independent E-door interiors, not newly exposed floor footprints.
"""
from support_rooms import (_cabinet, _chair, _paperwork, _monitor, _keyboard,
                           _bench, _printer, _board, _bin, _plant, _sink)

T=1.25
WOOD='#827052'; METAL='#4c615e'; PAPER='#d1c9ad'
KINDS={'storage','assembly','maintenance','seminar','preparation','archives',
       'lounge','counseling','meeting','principal','control'}


def _sofa(i,x,y,w=3.0,front=-1):
    i.obj('lounge-sofa',x,y,w,.85,.30,'#576d65',base=.18,solid=True)
    i.obj('sofa-seat-cushion',x,y+front*.11/T,w-.26,.60,.12,'#7c8b78',base=.48)
    i.obj('sofa-back',x,y-front*.34/T,w,.15,.65,'#62776a',base=.40)
    for side in [-1,1]:i.obj('sofa-arm',x+side*(w/2-.10)/T,y,.20,.84,.47,'#6c8070',base=.31)
    for n in [-1,0,1]:i.obj('sofa-seam',x+n*w*.24/T,y+front*.12/T,.012,.56,.008,'#4c655b',base=.606)


def _coffee(i,x,y):
    i.table(x,y,1.8,.80,'lounge-coffee-table')
    i.obj('coffee-table-magazines',x-.3/T,y,.28,.36,.045,PAPER,base=.815)
    i.obj('coffee-table-mug',x+.4/T,y,.09,.09,.11,'#aab59d',base=.815,shape='cylinder')


def _rack(i,x,y,w=2.3,kind='storage-cabinet',variant=0):
    _cabinet(i,x,y,w,.65,1.65,kind)
    for n in range(3):
        xx=x+(n-1)*.48/T
        i.obj('labelled-storage-box',xx,y,.40,.46,.26,
              ['#a49370','#788d7a','#7f9192'][(variant+n)%3],base=1.72)
        i.obj('storage-box-label',xx,y-.25/T,.18,.02,.09,PAPER,base=1.79)


def _shell(i):
    """Window/lamp modules at valid side-wall heights, no new obstacles."""
    for side,x in [(-1,1.0),(1,i.width-2.0)]:
        for y in range(6,i.height-3,6):
            face=x-side*.08/T
            i.obj('support-window-frame',x,y,.12,2.7,1.35,'#586d63',base=1.03)
            i.obj('support-window-glass',face,y,.025,2.48,1.12,'#557274',base=1.15)
            i.obj('support-window-mullion',face-side*.02/T,y,.03,.055,1.14,'#a2ad98',base=1.14)
            i.obj('support-window-sill',x,y,.24,2.83,.075,'#a1aa93',base=.95)
        for y in range(4,i.height-3,8):
            i.obj('support-wall-lamp-back',x,y,.16,.38,.44,'#4e655d',base=1.32)
            i.obj('support-wall-lamp-glass',x-side*.11/T,y,.07,.30,.32,'#c9c8a4',base=1.38)
    _board(i,i.cx,i.height-1.65,2.5,'room-information-board')
    _bin(i,i.width-3,3.5)


def build_storage(i):
    # Two small rack islands, four rear racks and perimeter equipment leave
    # a wide centre spine and three transverse aisles. Upper-floor storage
    # visibly holds classroom equipment, ground-floor storage bulky supplies.
    upper=i.map['sourceFloor']==2
    for x in [4,8,14,18]:_rack(i,x,17,2.4,'storage-cabinet',int(upper))
    for x in [5,17]:
        for y in [7,12]:i.shelf(x,y,2.8,.70,books=False)
    i.table(5,3.8,2.8,.90,'storage-sorting-table');_paperwork(i,5,3.8)
    _cabinet(i,18,3.9,2.3,.65,1.8,'equipment-cabinet')
    for x,y in [(3,9.5),(18.5,9.5),(5,15),(17,15)]:
        i.obj('equipment-crate',x,y,.85,.70,.65,'#82765c',solid=True)
        i.obj('crate-lid',x,y,.90,.75,.055,'#aa9974',base=.65)
        for xx in [-.27,.27]:i.obj('crate-band',x+xx/T,y,.055,.76,.025,'#4d645a',base=.71)
    for x in [4.5,5.0,5.5]:
        if upper:
            i.obj('stored-projector',x,17,.36,.50,.20,'#a7afa0',base=2.01)
        else:
            i.obj('stored-cleaning-pole',x,15,.04,.04,1.55,'#9e8e6b')
    for label,x,y in [('중앙 보관 통로',11,10),('남쪽 정리대 앞',8,5),
                       ('서쪽 선반 통로',3,12),('동쪽 선반 통로',19,12),
                       ('안쪽 보관함 앞',11,15),('뒤쪽 가로 통로',11,18)]:i.zone(label,x,y)


def build_assembly(i):
    # Broad central aisle, two seating blocks and both side aisles. The stage
    # is an obstacle to walk around; no zone silently requires a jump onto it.
    i.obj('assembly-stage',16,24,22.0,2.8,.30,'#75654f',solid=True)
    i.obj('assembly-stage-front',16,22.84,22.0,.08,.28,'#948565')
    i.obj('assembly-projection-screen',16,26.3,8.0,.10,2.0,'#bac3ad',base=.58)
    i.obj('projection-screen-border',16,26.36,8.18,.06,2.18,'#486158',base=.49)
    for x in [4,28]:
        i.obj('assembly-speaker',x,24,.85,.70,1.50,'#354c48',solid=True)
        for z in [.35,.95]:i.obj('assembly-speaker-cone',x,23.7,.52,.035,.45,'#526c60',base=z,shape='cylinder')
    i.table(7,20,3.0,.95,'assembly-registration-table');_paperwork(i,7,20)
    i.obj('assembly-podium',25,20.5,.90,.75,1.20,WOOD,solid=True)
    i.obj('podium-reading-top',25,20.5,1.05,.83,.07,'#a0916c',base=1.20)
    i.obj('podium-microphone-stem',25.2,20.5,.025,.025,.35,'#65796c',base=1.27)
    for y in [7,10,13,16]:
        for x in [5,8,11,21,24,27]:_chair(i,x,y,facing=-1)
    for x in [4,8,24,28]:_rack(i,x,3.8,2.5,'assembly-supply-cabinet')
    for label,x,y in [('중앙 관람 통로',16,12),('좌측 좌석 옆',3,12),
                       ('우측 좌석 옆',29,12),('좌석 뒤 가로 통로',16,5),
                       ('무대 앞 공간',16,21),('서쪽 무대 옆',5,21),
                       ('동쪽 무대 옆',28,21)]:i.zone(label,x,y)


def build_maintenance(i):
    for x in [4,8,14,18]:
        _cabinet(i,x,17,2.4,.70,1.90,'maintenance-panel-cabinet')
        for n in range(3):
            i.obj('electrical-gauge',x+(n-1)*.45/T,16.69,.24,.035,.24,'#b5bca5',base=1.20,shape='cylinder')
            i.obj('electrical-switch',x+(n-1)*.45/T,16.67,.07,.045,.15,'#728d78',base=.65)
    i.table(5.5,12,4.8,1.1,'maintenance-workbench')
    for n in range(5):
        i.obj('tool-tray',5.5+(n-2)*.65/T,12,.48,.55,.07,'#556e60',base=.815)
        i.obj('workshop-tool',5.5+(n-2)*.65/T,12,.10,.35,.04,'#a4aa96',base=.89)
    for y in [6,10,14]:_rack(i,17.5,y,2.8,'spare-parts-cabinet')
    for x in [3.4,6.5]:
        i.obj('maintenance-pump',x,6,.78,.80,.68,'#52766f',solid=True)
        i.obj('pump-motor',x,6,.40,.65,.34,'#8e9e8e',base=.68,shape='cylinder')
    i.obj('maintenance-pipe-riser',2.3,15,.12,.12,2.0,'#7e9589')
    i.obj('maintenance-pipe-run',5.8,17.8,8.8,.12,.12,'#80988a',base=1.97)
    for label,x,y in [('설비 중앙 통로',11,10),('작업대 앞',6,10),
                       ('배전반 앞',11,15),('예비 부품 통로',15,10),
                       ('펌프 점검 구역',5,8),('서쪽 우회 통로',2,10)]:i.zone(label,x,y)


def build_seminar(i):
    for x in [6,20]:
        for y in [8,14]:
            i.table(x,y,3.3,1.1,'seminar-worktable');_paperwork(i,x,y)
            for dx in [-.7,.7]:
                _chair(i,x+dx,y-1.15);_chair(i,x+dx,y+1.15,facing=-1)
    i.table(13,20,3.8,1.05,'seminar-instructor-desk');_monitor(i,13.7,20.12)
    _chair(i,13,21.15,facing=-1)
    i.obj('seminar-projection-screen',13,22.3,7.0,.08,1.7,'#bfc7b2',base=.58)
    i.obj('seminar-projector',13,17,.44,.58,.21,'#a2afa0',base=2.05)
    for x in [3.5,7,19,22]:_rack(i,x,3.8,2.5,'seminar-supply-cabinet')
    _board(i,5,22.3,3.2,'seminar-whiteboard')
    for label,x,y in [('세미나 중앙 통로',13,11),('왼쪽 토론 구역',6,11),
                       ('오른쪽 토론 구역',20,11),('강사석 앞',13,18),
                       ('서쪽 우회 통로',3,14),('동쪽 우회 통로',23,14)]:i.zone(label,x,y)


def build_preparation(i):
    for x in [4,8,14,18]:_rack(i,x,17,2.4,'preparation-cabinet')
    for x in [5.5,16.5]:
        i.table(x,8,3.8,1.2,'preparation-worktable')
        for n in range(4):
            i.obj('preparation-tray',x+(n-1.5)*.55/T,8,.42,.55,.055,'#8c9c85',base=.815)
            i.obj('preparation-sample-jar',x+(n-1.5)*.55/T,8,.14,.14,.25,
                  ['#8eaa9d','#a79d7a','#a4b3a2'][n%3],base=.875,shape='cylinder')
        _chair(i,x,6.7)
    _sink(i,4.5,14)
    _cabinet(i,17.5,13.5,2.5,.70,1.65,'teaching-material-cabinet')
    i.table(6.0,3.8,3.0,.85,'preparation-record-desk');_paperwork(i,6,3.8)
    for label,x,y in [('준비실 중앙 통로',11,10),('서쪽 준비대 앞',6,10),
                       ('동쪽 준비대 앞',16,10),('세척대 앞',5,12),
                       ('안쪽 교구장 앞',11,15),('동쪽 교구장 옆',19,14)]:i.zone(label,x,y)


def build_archives(i):
    for x in [4,8,18,22]:
        for y in [7,11,15]:i.shelf(x,y,2.8,.70,books=True)
    for x in [4,8,18,22]:_rack(i,x,20.5,2.7,'archives-file-cabinet')
    i.table(13,20,3.4,1.05,'archive-catalog-desk');_paperwork(i,12.5,20);_monitor(i,13.6,20.12)
    _printer(i,22,3.8,'archives-document-scanner')
    for x in [5,9,18]:
        i.table(x,3.8,2.4,.85,'archive-sorting-table');_paperwork(i,x,3.8)
    for label,x,y in [('자료실 중앙 통로',13,11),('남쪽 분류대 앞',13,6),
                       ('서쪽 문서 통로',2,11),('동쪽 문서 통로',24,11),
                       ('안쪽 가로 통로',13,18),('문서 검색대 앞',13,22)]:i.zone(label,x,y)


def build_lounge(i):
    for y,front in [(8,1),(12,-1)]:_sofa(i,6,y,3.8,front)
    _coffee(i,6,10)
    for y,front in [(7,1),(11,-1)]:_sofa(i,20,y,3.1,front)
    _coffee(i,20,9)
    _cabinet(i,4,18,2.8,.65,1.55,'lounge-personal-cabinet')
    _cabinet(i,8,18,2.8,.65,1.55,'lounge-personal-cabinet')
    i.obj('lounge-tea-counter',19,18,5.4,.85,.85,WOOD,solid=True)
    i.obj('tea-counter-worktop',19,18,5.5,.91,.055,'#b4b99f',base=.85)
    i.obj('lounge-kettle',18,18,.25,.25,.31,'#bfc6b0',base=.91,shape='cylinder')
    for x in [19,19.3,19.6]:i.obj('lounge-mug',x,18,.10,.10,.12,'#a3b69e',base=.91,shape='cylinder')
    i.obj('lounge-water-dispenser',23,16,.55,.55,1.0,'#a4b39e',solid=True)
    i.obj('water-dispenser-bottle',23,16,.40,.40,.50,'#73958f',base=1.0,shape='cylinder')
    i.obj('lounge-television',13,20.2,2.6,.23,1.3,'#3b514b',base=.75)
    i.obj('lounge-television-glass',13,20.09,2.35,.025,1.05,'#5e7973',base=.87)
    _plant(i,2.5,3.8);_plant(i,23,19)
    for label,x,y in [('휴게실 중앙 통로',13,10),('서쪽 소파 옆',3,10),
                       ('동쪽 소파 옆',23,9),('휴식 공간 뒤',13,14),
                       ('개인 보관함 앞',7,16),('탕비대 앞',19,16)]:i.zone(label,x,y)


def build_counseling(i):
    i.table(6,16,3.2,1.2,'counselor-desk');_paperwork(i,5.5,16);_monitor(i,6.6,16.15)
    _chair(i,6,17.2,facing=-1)
    for x in [5,7]:_chair(i,x,14.8)
    for y,front in [(8,1),(12,-1)]:_sofa(i,17,y,3.8,front)
    _coffee(i,17,10)
    i.obj('counseling-low-divider',11,15,.15,3.4,1.15,'#778b7a',solid=True)
    i.obj('divider-top-trim',11,15,.20,3.43,.06,'#a1ad95',base=1.15)
    for x in [4,8,17,20]:_cabinet(i,x,19,2.4,.60,1.7,'counseling-record-cabinet')
    _bench(i,5,5,3.4);_plant(i,2.5,4);_plant(i,20,16)
    for label,x,y in [('상담실 중앙 통로',12,9),('대기석 앞',5,7),
                       ('상담 책상 앞',6,13),('대화 소파 옆',20,10),
                       ('가림막 동쪽 통로',13,15),('기록 보관함 앞',12,18)]:i.zone(label,x,y)


def build_meeting(i):
    # One long table forces a meaningful split at the entrance, with both
    # sides reconnecting behind it; meetings use a different topology than
    # the paired seminar islands or theatre-style large assembly room.
    i.table(14,12,3.2,8.2,'meeting-table')
    for y in [9,12,15]:
        _chair(i,11.5,y);_chair(i,16.5,y)
        _paperwork(i,14,y)
    for y in [7.6,16.4]:_chair(i,14,y,facing=1 if y<12 else -1)
    for x in [4,8,20,24]:_rack(i,x,20.5,2.6,'meeting-document-cabinet')
    i.obj('meeting-projection-screen',14,22.3,7.5,.08,1.7,'#bdc5ae',base=.58)
    i.obj('meeting-projector',14,17.5,.45,.60,.22,'#a4afa0',base=2.05)
    _printer(i,3.5,16,'meeting-print-station')
    _bench(i,5,4,3.0);_bench(i,23,4,3.0)
    for label,x,y in [('회의실 남쪽 분기',14,5),('회의실 중앙 서쪽 통로',9,12),
                       ('동쪽 회의 통로',19,12),('북쪽 연결 통로',14,19),
                       ('왼쪽 문서 보관함 앞',6,19),('오른쪽 문서 보관함 앞',23,19)]:i.zone(label,x,y)


def build_principal(i):
    i.table(14,18,4.4,1.6,'principal-desk');_paperwork(i,13,18);_monitor(i,15,18.15)
    _chair(i,14,20,facing=-1)
    for x in [12.5,15.5]:_chair(i,x,16.2)
    for x in [4,8,20,24]:_cabinet(i,x,21,2.6,.65,1.85,'principal-record-cabinet')
    for y,front in [(7.5,1),(12,-1)]:_sofa(i,6,y,4.0,front)
    _coffee(i,6,9.7)
    for y in [8,12]:
        _cabinet(i,23.5,y,2.0,.65,1.35,'principal-trophy-cabinet')
        for x in [23.1,23.5,23.9]:
            i.obj('school-trophy-base',x,y,.20,.20,.07,'#60796b',base=1.42)
            i.obj('school-trophy-stem',x,y,.045,.045,.22,'#b7a16a',base=1.49)
            i.obj('school-trophy-cup',x,y,.22,.20,.18,'#bca978',base=1.69,shape='cylinder')
    i.obj('principal-wall-frame',14,22.3,4.0,.10,1.0,'#998b65',base=1.10)
    i.obj('principal-school-photo',14,22.23,3.8,.025,.82,'#718978',base=1.19)
    _plant(i,3,16);_plant(i,25,18);_bench(i,21,4,3.0)
    for label,x,y in [('교장실 중앙 공간',14,10),('접견 소파 옆',3,10),
                       ('수상 진열장 앞',21,10),('집무 책상 앞',14,14),
                       ('책상 뒤 통로',14,22),('동쪽 우회 통로',20,18)]:i.zone(label,x,y)


def build_control(i):
    for x in [5,10,15,20]:
        i.obj('control-monitor-panel',x,21.5,3.7,.20,1.4,'#384f48',base=.70)
        i.obj('control-monitor-glass',x,21.38,3.42,.025,1.13,'#496d68',base=.84)
        for n in range(3):
            i.obj('control-monitor-grid',x+(n-1)*.85/T,21.36,.025,.012,1.08,'#78988a',base=.865)
    for x in [6.5,19]:
        for y in [14,18]:
            i.table(x,y,4.0,1.2,'control-console-desk')
            for dx in [-.65,.65]:_monitor(i,x+dx,y+.15)
            _keyboard(i,x,y-.25);_chair(i,x,y-1.25)
    for x in [3,22.5]:
        for y in [6.5,10]:
            i.obj('control-server-rack',x,y,1.2,.95,1.85,'#354f48',solid=True)
            for z in [.20,.48,.76,1.04,1.32,1.60]:
                i.obj('control-server-unit',x,y-.41/T,1.0,.04,.18,'#5a7567',base=z)
                for n in range(3):i.obj('control-status-led',x+(.15+n*.12)/T,y-.44/T,.025,.018,.025,'#97b17f',base=z+.08)
    _cabinet(i,6.5,3.8,3.2,.65,1.5,'control-record-cabinet')
    _cabinet(i,19,3.8,3.2,.65,1.5,'control-equipment-cabinet')
    _printer(i,3.5,17,'control-log-printer')
    for label,x,y in [('통제실 중앙 통로',13,12),('서쪽 서버 앞',5,8),
                       ('동쪽 서버 앞',21,8),('남쪽 운영석 앞',13,10),
                       ('북쪽 운영석 앞',13,16),('감시 화면 앞',13,20)]:i.zone(label,x,y)


def build_remaining(i):
    globals()['build_'+i.kind](i)
    _shell(i)
