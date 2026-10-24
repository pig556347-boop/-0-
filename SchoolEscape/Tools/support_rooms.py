"""Original furnishings for the four approved support-room concepts.

Coordinates are grid cells; dimensions and elevations are metres. Every room
keeps a clear south entrance and uses only static, noninteractive furnishings.
"""
T=1.25
WOOD='#827052'
METAL='#4c615e'
PALE='#bfc6b5'
PAPER='#d1c9ad'


def _cabinet(i,x,y,w=1.7,d=.58,h=1.75,kind='office-cabinet'):
    i.obj(kind,x,y,w,d,h,'#64746b',solid=True)
    for side in [-1,1]:
        xx=x+side*w*.245/T
        i.obj('support-cabinet-door',xx,y-(d/2+.025)/T,w*.46,.035,h-.17,
              '#929b87',base=.08)
        i.obj('support-cabinet-handle',x+side*.06/T,y-(d/2+.06)/T,.025,.035,.16,
              '#b9b9a4',base=h*.46)
    i.obj('support-cabinet-top',x,y,w+.04,d+.04,.05,'#a0a58e',base=h)


def _chair(i,x,y,facing=1):
    i.obj('chair-seat',x,y,.46,.44,.09,'#4c6465',base=.43,solid=True)
    i.obj('office-chair-back',x,y-facing*.18/T,.47,.07,.43,'#60767a',base=.52)
    i.obj('office-chair-stem',x,y,.075,.075,.41,METAL)
    i.obj('office-chair-cross',x,y,.52,.07,.045,METAL,base=.04)
    i.obj('office-chair-cross',x,y,.07,.50,.045,METAL,base=.04)


def _paperwork(i,x,y,top=.815):
    i.obj('document-stack',x-.25/T,y,.29,.35,.055,PAPER,base=top)
    i.obj('document-top-sheet',x-.25/T,y,.27,.33,.008,'#e0d8c0',base=top+.055)
    for z in [.06,.09,.12]:
        i.obj('document-spine',x+.27/T,y,.25,.32,.025,
              ['#617e76','#817663','#92765e'][round(z*100)%3],base=top+z)
    i.obj('pencil-holder',x+.51/T,y+.12/T,.10,.10,.13,'#687a70',base=top,shape='cylinder')
    for n in range(3):
        i.obj('office-pencil',x+(.48+n*.025)/T,y+.12/T,.011,.011,.20,
              '#aa9667',base=top+.03)


def _monitor(i,x,y,kind='office-monitor',front=-1):
    # Separate face sits beyond the casing, avoiding coplanar depth flicker.
    i.obj('monitor-base',x,y,.36,.23,.035,'#7d8780',base=.815)
    i.obj('monitor-neck',x,y,.075,.065,.20,'#727d75',base=.85)
    i.obj(kind,x,y,.60,.20,.45,'#a0a798',base=1.04)
    i.obj('computer-screen' if kind=='computer-monitor' else 'office-screen',
          x,y+front*.115/T,.49,.018,.34,'#243f43',base=1.10)
    i.obj('monitor-power-dot',x+.23/T,y+front*.13/T,.025,.018,.018,
          '#789d8b',base=1.06)


def _keyboard(i,x,y):
    i.obj('computer-keyboard',x,y,.48,.17,.035,'#a4aa9a',base=.815)
    for row in range(3):
        i.obj('keyboard-key-row',x,y+((row-1)*.045)/T,.40,.022,.012,
              '#7e8a80',base=.85)
    i.obj('computer-mouse',x+.36/T,y,.07,.10,.04,'#a9b1a2',base=.815)


def _bench(i,x,y,w=2.4):
    i.obj('waiting-bench',x,y,w,.52,.12,'#536c61',base=.43,solid=True)
    i.obj('waiting-bench-back',x,y+.20/T,w,.08,.47,'#647d6a',base=.55)
    for dx in [-.36,.36]:i.obj('waiting-bench-leg',x+dx*w/T,y,.10,.40,.43,METAL)


def _printer(i,x,y,kind='office-copier'):
    i.obj(kind,x,y,.85,.80,1.03,'#929e92',solid=True)
    i.obj('copier-top',x,y,.91,.84,.09,'#bcc1ad',base=1.03)
    i.obj('copier-glass',x,y-.02,.58,.51,.025,'#455e5b',base=1.12)
    i.obj('copier-controls',x+.23/T,y-.34/T,.20,.08,.04,'#2f5554',base=1.13)
    for z in [.22,.47,.72]:
        i.obj('copier-drawer',x,y-.42/T,.74,.035,.16,'#b0b5a1',base=z)
    i.obj('copier-paper-output',x+.46/T,y,.24,.40,.07,'#687b72',base=.75)


def _board(i,x,y,w=3.0,kind='notice-board'):
    i.obj(kind,x,y,w,.11,1.1,'#635b45',base=.74)
    i.obj('notice-board-face',x,y-.08/T,w-.14,.025,.95,'#526c5a',base=.81)
    for n in range(5):
        xx=x+(n-2)*w*.16/T
        i.obj('timetable-sheet',xx,y-.105/T,.32,.012,.60,PAPER,base=.99)
        for row in range(5):
            i.obj('timetable-rule',xx,y-.115/T,.27,.008,.012,'#7d8978',base=1.06+row*.09)


def _bin(i,x,y):
    i.obj('support-waste-bin',x,y,.40,.38,.60,'#5f7369',solid=True)
    i.obj('support-waste-bin-lid',x,y,.43,.41,.055,'#899580',base=.60)


def _room_shell_details(i):
    """Dusty side windows, fixed wall lamps and a clock, for these rooms only."""
    for side,x in [(-1,1.0),(1,i.width-2.0)]:
        for y in [7,13,19]:
            i.obj('support-window-frame',x,y,.12,2.7,1.35,'#586d63',base=1.03)
            face=x-side*.08/T
            i.obj('support-window-glass',face,y,.025,2.48,1.12,'#557274',base=1.15)
            i.obj('support-window-mullion',face-side*.02/T,y,.03,.055,1.14,'#a2ad98',base=1.14)
            i.obj('support-window-sill',x,y,.24,2.83,.075,'#a1aa93',base=.95)
            i.obj('gathered-window-curtain',face,y+1.20/T,.11,.23,1.35,'#788875',base=1.01)
        for y in [4,16,22]:
            if y>=i.height-2:continue
            i.obj('support-wall-lamp-back',x,y,.16,.38,.44,'#4e655d',base=1.32)
            i.obj('support-wall-lamp-glass',x-side*.11/T,y,.07,.30,.32,'#c9c8a4',base=1.38)
    x=i.width*.5;y=i.height-1.65
    i.obj('support-room-clock',x,y,.45,.08,.45,'#b6bda6',base=2.00)
    i.obj('support-clock-face',x,y-.05/T,.38,.025,.38,'#d2d3b9',base=2.035)
    i.obj('support-clock-hand',x+.06/T,y-.073/T,.15,.015,.018,'#566a5e',base=2.22)
    i.obj('support-clock-hand',x,y-.074/T,.018,.015,.13,'#566a5e',base=2.22)


def _plant(i,x,y):
    i.obj('plant-pot',x,y,.35,.35,.40,'#82745c',solid=True,shape='cylinder')
    i.obj('plant-stem',x,y,.04,.04,.54,'#536b4c',base=.38)
    for dx,dy,dz in [(-.16,0,.60),(.15,.07,.69),(-.1,-.08,.81),(.08,0,.94)]:
        i.obj('plant-leaves',x+dx/T,y+dy/T,.34,.22,.18,'#657d55',base=dz,shape='sphere')


def _sink(i,x,y):
    i.obj('medical-sink-cabinet',x,y,2.2,.75,.81,'#a7af9d',solid=True)
    i.obj('medical-sink-worktop',x,y,2.28,.80,.055,'#c8ccba',base=.81)
    i.obj('medical-sink-basin',x,y-.03,.67,.48,.025,'#526b65',base=.87)
    i.obj('medical-sink-tap',x,y+.23/T,.045,.045,.28,METAL,base=.88)
    i.obj('medical-sink-spout',x,y+.14/T,.045,.25,.045,METAL,base=1.12)
    i.obj('soap-bottle',x-.58/T,y,.11,.11,.22,'#c1cbb7',base=.88)
    i.obj('soap-pump',x-.58/T,y,.04,.08,.04,'#779182',base=1.10)


def _medical_cart(i,x,y):
    i.obj('medical-cart',x,y,.56,.52,.08,'#9cae9c',base=.72,solid=True)
    for z in [.18,.43]:i.obj('medical-cart-shelf',x,y,.56,.52,.045,'#7d9889',base=z)
    for dx in [-.22,.22]:
        for dy in [-.20,.20]:
            i.obj('medical-cart-leg',x+dx/T,y+dy/T,.035,.035,.72,METAL)
    for n in range(3):
        i.obj('medical-bottle',x+(n-1)*.12/T,y,.08,.08,.17,
              ['#b4c1a4','#8caba0','#ad907a'][n],base=.81,shape='cylinder')


def _bed(i,x,y):
    i.obj('infirmary-bed',x,y,1.05,2.12,.24,'#7f958c',base=.37,solid=True)
    i.obj('bed-mattress',x,y,1.00,2.02,.18,'#c6ccb9',base=.61)
    i.obj('bed-blanket',x,y-.26/T,.96,1.36,.07,'#a6b49f',base=.79)
    i.obj('bed-pillow',x,y+.73/T,.70,.40,.13,'#dfddca',base=.79)
    for yy in [-1.03,1.03]:
        i.obj('bed-end-frame',x,y+yy/T,1.10,.065,.68,'#abb7a7',base=.12)
        for dx in [-.48,.48]:i.obj('bed-leg',x+dx/T,y+yy/T,.055,.055,.40,METAL)
    # Open privacy frames with curtains gathered at outer corners. The middle
    # stays open to the overhead camera and the player's walking route.
    for dx in [-1.4,1.4]:
        for dy in [-1.65,1.65]:
            i.obj('curtain-post',x+dx/T,y+dy/T,.045,.045,1.82,'#9aab9c',solid=True)
        i.obj('curtain-side-rail',x+dx/T,y,.035,3.35,.035,'#a7b7a9',base=1.82)
    i.obj('curtain-head-rail',x,y+1.65/T,2.85,.035,.035,'#a7b7a9',base=1.82)
    for dx in [-1.32,1.32]:
        i.obj('gathered-privacy-curtain',x+dx/T,y+1.28/T,.14,.65,1.12,
              '#7f9c87',base=.57)
        for fold in [-.04,.04]:
            i.obj('privacy-curtain-fold',x+(dx+fold)/T,y+1.28/T,.026,.66,1.11,
                  '#98ad94',base=.57)
    _medical_cart(i,x+1.0,y-.4)
    i.obj('bedside-locker',x+1.0,y+.8,.50,.52,.72,'#95886c',solid=True)
    i.obj('bedside-locker-top',x+1.0,y+.8,.55,.55,.05,'#b1aa8e',base=.72)


def build_infirmary(i):
    for x in [16,20]:
        for y in [8,15]:_bed(i,x,y)
    for x in [3.0,6.0,9.0]:_cabinet(i,x,19,2.35,.65,1.7,'medicine-cabinet')
    for x in [3.0,6.0,9.0]:
        i.obj('medical-cross-vertical',x,18.70,.08,.035,.30,'#d0d7bb',base=1.00)
        i.obj('medical-cross-horizontal',x,18.70,.30,.035,.08,'#d0d7bb',base=1.11)
    _sink(i,4.2,16.5)
    i.table(6,10,3.0,1.0,'nurse-desk');_paperwork(i,5.6,10)
    _monitor(i,6.5,10.20)
    _chair(i,6,11.1,facing=-1);_chair(i,5,8.8);_chair(i,7,8.8)
    i.table(3.8,11.1,1.05,1.45,'medical-side-worktop')
    i.obj('medical-scale-base',3,14,.45,.50,.06,'#9ca995',solid=True)
    i.obj('medical-scale-column',3,14,.07,.07,1.55,'#85998b')
    i.obj('medical-scale-display',3,14,.32,.14,.17,'#b9c3ae',base=1.49)
    _board(i,9,20.15,2.3,'health-information-board')
    _bench(i,5,3.5,3.3);_bin(i,8,3.5);_plant(i,2,3.2)
    _plant(i,2,18);_bin(i,9.5,16.5)
    _medical_cart(i,9.5,13.5)
    for label,x,y in [('대기 공간',5,5),('진료 책상 앞',6,8),('약품장 앞',6,18),
                       ('세면대 앞',5,15),('병상 중앙 통로',12,12),
                       ('남쪽 병상 구역',18,8),('북쪽 병상 구역',18,15)]:i.zone(label,x,y)


def build_admin(i):
    # L counter is composed of two perpendicular solid modules.
    i.obj('reception-counter',6.0,5,5.7,.90,.92,WOOD,solid=True)
    i.obj('reception-counter',7.94,6.4,.85,2.50,.92,WOOD,solid=True)
    i.obj('reception-worktop',6.0,5,5.8,.96,.07,'#a18d69',base=.92)
    i.obj('reception-worktop',7.94,6.4,.91,2.52,.07,'#a18d69',base=.92)
    _paperwork(i,5,5,1.005)
    i.obj('reception-sign',6.9,4.57,.65,.08,.23,'#71867b',base=.99)
    _bench(i,5.5,3.1,3.8);_chair(i,5.5,6.4,facing=-1)
    # Three columns by three rows of paired desks give 18 proper work seats.
    # Keep the same metre-sized furniture; circulation remains between the
    # islands, ahead of reception and in front of the rear document cabinets.
    for cy in [9.5,13.5,17.5]:
        for cx in [5,12.5,20]:
            for offset in [-.43,.43]:
                y=cy+offset
                i.table(cx,y,2.25,.95,'admin-desk')
                _paperwork(i,cx-.45,y)
                _monitor(i,cx+.48,y+(.12 if offset<0 else -.12),front=-1 if offset<0 else 1)
                _chair(i,cx,cy+(-1.5 if offset<0 else 1.5),facing=1 if offset<0 else -1)
            i.obj('admin-desk-divider',cx,cy,2.10,.045,.36,'#7b9187',base=.81)
    for x in [3,6.5,10,18,21.5]:_cabinet(i,x,21,2.6,.65,1.85,'archive-cabinet')
    for x in [3,6.5,10]:
        for n in range(3):
            i.obj('archive-box',x+(n-1)*.44/T,21,.36,.43,.26,
                  '#a4926c',base=1.92)
    i.obj('office-safe',14.0,21,1.05,.86,1.22,'#485951',solid=True)
    i.obj('office-safe-door',14.0,20.63,.86,.04,.98,'#69786b',base=.12)
    i.obj('office-safe-wheel',14.22,20.59,.19,.06,.19,'#bcc0a6',base=.52,shape='cylinder')
    _printer(i,23,16);_printer(i,23,12,'office-document-scanner')
    _cabinet(i,23,7,1.8,.60,1.25,'admin-stationery-cabinet')
    i.table(19,5,2.2,.85,'admin-document-worktable');_paperwork(i,18.7,5)
    for x in [2,23]:_plant(i,x,3.4)
    _bin(i,21,5);_board(i,14,22.15,3.2,'admin-notice-board')
    for label,x,y in [('민원 접수 앞',8,3),('접수대 안쪽',6,8),('업무 책상 남쪽',13,7),
                       ('업무 책상 북쪽',13,20),('업무 책상 서쪽 통로',9,12),
                       ('업무 책상 동쪽 통로',17,16),('문서 보관 구역',12,20),
                       ('복사기 앞',21,16),('자료 정리대 앞',19,7)]:i.zone(label,x,y)


def _staff_island(i,cx,cy):
    for dx in [-.92,.92]:
        for dy in [-.43,.43]:
            x=cx+dx;y=cy+dy
            i.table(x,y,1.80,.95,'staff-desk')
            _paperwork(i,x-.22,y)
            _monitor(i,x+.41,y+(.11 if dy<0 else -.11),front=-1 if dy<0 else 1)
            _chair(i,x,cy+(-1.55 if dy<0 else 1.55),facing=1 if dy<0 else -1)
    i.obj('staff-island-divider',cx,cy,4.1,.045,.33,'#7c9184',base=.81)
    i.obj('staff-island-folder-rack',cx,cy,.33,.48,.38,'#596f64',base=.81)


def build_staff(i):
    for x in [6,21]:
        for y in [8.5,17]:_staff_island(i,x,y)
    i.table(14,23,3.2,1.05,'head-teacher-desk');_paperwork(i,13.2,23)
    _monitor(i,14.5,23.1);_chair(i,14,24,facing=-1)
    _board(i,14,24.35,7.0,'staff-timetable-board')
    i.table(14,13,1.9,3.0,'staff-meeting-table')
    for y in [12.2,13.8]:
        _chair(i,12.7,y);_chair(i,15.3,y)
    _paperwork(i,14,13)
    for x in [3.5,7,21,24.5]:_cabinet(i,x,23.2,2.3,.60,1.75,'staff-record-cabinet')
    for x in [4.5,8,20.5,24]:
        _cabinet(i,x,3.3,2.3,.65,1.05,'staff-low-file-cabinet')
        _paperwork(i,x,3.3,1.115)
    _printer(i,2,12);_printer(i,2,15,'staff-print-station')
    i.table(25,13,1.6,.75,'staff-tea-counter')
    i.obj('staff-kettle',25.3,13,.23,.23,.27,'#b7bfaa',base=.815,shape='cylinder')
    for x in [24.6,24.85]:i.obj('staff-mug',x,13,.09,.09,.11,'#a2b8a5',base=.815,shape='cylinder')
    i.obj('water-dispenser',25,15,.50,.50,.95,'#a0b0a0',solid=True)
    i.obj('water-dispenser-bottle',25,15,.39,.39,.48,'#688f88',base=.95,shape='cylinder')
    for x,y in [(2,3.3),(25.5,21)]:_plant(i,x,y)
    _bin(i,24,12)
    for label,x,y in [('입구 자료장 앞',14,5),('남쪽 교사 책상 사이',14,8),
                       ('중앙 회의 공간 서쪽',11,13),('중앙 회의 공간 동쪽',17,13),
                       ('북쪽 교사 책상 사이',14,18),('부장 책상 앞',14,21),
                       ('복사 공간',3,13),('탕비 공간',23,14)]:i.zone(label,x,y)


def build_computer(i):
    for row,y in enumerate([7,12,17]):
        for side,xs in enumerate([[4,6,8,10],[18,20,22,24]]):
            for x in xs:
                i.table(x,y,1.70,.80,'computer-workstation')
                _monitor(i,x,y+.17,kind='computer-monitor')
                _keyboard(i,x,y-.18)
                i.obj('computer-tower',x+.51/T,y+.02,.30,.43,.52,'#9ba797',base=.06)
                i.obj('computer-tower-slot',x+.51/T,y-.20,.20,.018,.035,'#4e625b',base=.43)
                _chair(i,x,y-.95)
            # Small, low cable trunk behind each four-seat group.
            mid=(xs[0]+xs[-1])/2
            i.obj('computer-cable-trunk',mid,y+.43,9.0,.11,.12,'#5d7168')
    i.table(14,23,2.8,1.05,'computer-teacher-desk')
    _monitor(i,14.2,23.1,kind='teacher-computer-monitor')
    _keyboard(i,14.2,22.84)
    _chair(i,14,24,facing=-1)
    i.obj('computer-projection-board',14,24.4,6.2,.13,1.55,'#385d4e',base=.45)
    i.obj('computer-projection-screen',14,24.30,4.15,.035,1.35,'#b8c3b3',base=.62)
    i.obj('computer-projector',14,21.5,.40,.52,.18,'#a2afa0',base=2.1)
    i.obj('computer-projector-lens',14,21.28,.12,.05,.12,'#3b6264',base=2.13,shape='cylinder')
    i.obj('network-rack',24.7,23,1.0,.82,1.93,'#304b45',solid=True)
    for z in [.22,.47,.72,.97,1.22,1.47,1.72]:
        i.obj('network-rack-unit',24.7,22.65,.80,.035,.16,'#526d5c',base=z)
        for n in range(3):i.obj('network-status-led',24.48+n*.10,22.62,.026,.016,.026,
                              '#9dba7c',base=z+.07)
    for x in [3,6,22]:_cabinet(i,x,23,2.0,.65,1.7,'computer-equipment-cabinet')
    for x in [4,7,21,24]:_cabinet(i,x,3.4,2.2,.62,1.05,'computer-supply-cabinet')
    _printer(i,2,10,'computer-room-printer')
    _board(i,5,24.35,2.5,'computer-instruction-board')
    for x,y in [(2,3.4),(25.5,20)]:_plant(i,x,y)
    _bin(i,25,5)
    for label,x,y in [('컴퓨터실 중앙 통로',14,12),('남쪽 좌석 가로 통로',14,5),
                       ('가운데 좌석 가로 통로',14,10),('북쪽 좌석 가로 통로',14,15),
                       ('교사 책상 앞',14,21),('서쪽 컴퓨터 구역',7,10),
                       ('동쪽 컴퓨터 구역',21,15),('네트워크 장비 앞',24,21)]:i.zone(label,x,y)


def build_support(i):
    {'infirmary':build_infirmary,'administration':build_admin,
     'staff':build_staff,'computer':build_computer}[i.kind](i)
    _room_shell_details(i)
