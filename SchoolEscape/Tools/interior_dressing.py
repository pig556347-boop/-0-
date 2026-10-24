"""Original static school furnishings for the exploration maps.

No gameplay pickups, random placement or interactions are added here. Geometry
uses the same metre-sized fixture format as the rest of the school authoring.
"""
from math import hypot

T=1.25
WOOD='#80664b'
METAL='#48585a'
PAPER='#c6bda4'
GREEN='#677e70'


def locker_banks(interior):
    """Four aligned, substantial 4-column by 2-row banks, exit aisle clear."""
    for bank, x in enumerate([2.65,5.55,12.45,15.35]):
        y=2.65
        group=f'{interior.id}-rear-locker-{bank}'
        interior.obj('locker-bank',x,y,2.65,.55,1.22,'#536865',solid=True,assembly=group)
        interior.obj('locker-toe-kick',x,y-.02,2.55,.48,.10,'#3f5452',assembly=group)
        interior.obj('locker-top',x,y,2.72,.60,.07,'#8c9b87',base=1.22,assembly=group)
        # Front panels sit 25mm in front of the opaque body, not coplanar.
        front=y-.32/T
        for col in range(4):
            for row in range(2):
                xx=x+(col-1.5)*.64/T
                base=.14+row*.51
                interior.obj('locker-compartment',xx,front,.60,.04,.46,
                             ['#71857b','#7e8b7b'][(col+row+bank)%2],base=base,assembly=group)
                interior.obj('locker-handle',xx+.17/T,front-.055/T,.045,.045,.13,
                             '#bdbaa2',base=base+.17,assembly=group)
                interior.obj('locker-nameplate',xx-.09/T,front-.04/T,.20,.012,.065,
                             '#c5c1a7',base=base+.36,assembly=group)
                for slat in range(3):
                    interior.obj('locker-vent',xx-.025/T,front-.04/T,.24,.012,.018,
                                 '#405753',base=base+.09+slat*.045,assembly=group)


def _bounds(o):
    return (o['x']-o['width']/T/2,o['y']-o['depth']/T/2,
            o['x']+o['width']/T/2,o['y']+o['depth']/T/2)


def _near_rect(x,y,b,r):
    return hypot(x-max(b[0],min(x,b[2])),y-max(b[1],min(y,b[3])))<r


def _assembly(interior,build,margin=.04):
    """Reject a whole new furnishing if it masks a landmark or solid fixture.

    Existing collision footprints and zone centres retain their original
    circulation. The parent runs continuous path checks after all additions.
    """
    start=len(interior.fixtures)
    existing=[o for o in interior.fixtures if o.get('solid')]
    build()
    added=interior.fixtures[start:]
    solids=[o for o in added if o.get('solid')]
    valid=True
    for o in solids:
        b=_bounds(o)
        if b[0]<.53 or b[2]>interior.width-1.53 or b[1]<1.53 or b[3]>interior.height-1.53:
            valid=False;break
        for z in interior.zones:
            if _near_rect(z['x'],z['y'],b,(.27+margin)/T):valid=False;break
        if not valid:break
        for old in existing:
            q=_bounds(old)
            if min(b[2],q[2])-max(b[0],q[0])>margin/T and min(b[3],q[3])-max(b[1],q[1])>margin/T:
                valid=False;break
        if not valid:break
    if not valid:del interior.fixtures[start:]
    return valid


def _cabinet(i,x,y,w=1.50,d=.48,h=1.65,kind='school-cabinet'):
    i.obj(kind,x,y,w,d,h,'#63736b',solid=True)
    for side in [-1,1]:
        xx=x+side*w*.245/T
        i.obj('cabinet-door',xx,y-(d/2+.025)/T,w*.46,.035,h*.85,
              '#7f8b7b',base=.08)
        i.obj('cabinet-handle',x+side*.055/T,y-(d/2+.062)/T,.025,.035,.16,
              '#bbb9a1',base=h*.47)
    i.obj('cabinet-top',x,y,w+.05,d+.04,.055,'#8d9583',base=h)


def _bench(i,x,y,w=1.8):
    i.obj('school-bench-seat',x,y,w,.43,.08,WOOD,base=.43,solid=True)
    for side in [-1,1]:i.obj('bench-leg',x+side*(w*.38)/T,y,.10,.35,.43,METAL)
    i.obj('school-bench-back',x,y+.14/T,w,.065,.42,WOOD,base=.51)


def _radiator(i,x,y):
    i.obj('radiator-body',x,y,1.55,.25,.65,'#a0a497',solid=True)
    for k in range(9):i.obj('radiator-fin',x+(k-4)*.15/T,y-.15/T,.09,.10,.59,'#c0c1ad',base=.03)
    i.obj('radiator-pipe',x-.72/T,y,.045,.045,.88,METAL)


def _trash(i,x,y):
    i.obj('school-trash-bin',x,y,.38,.36,.57,'#69736c',solid=True)
    i.obj('trash-bin-lid',x,y,.41,.39,.06,'#45584f',base=.57)
    i.obj('trash-bin-opening',x,y-.205/T,.23,.016,.09,'#233e37',base=.40)


def _bulletin(i,x,y,w=2.4):
    # A wall display: visible but not an extra walking obstacle.
    i.obj('bulletin-board-frame',x,y,w,.11,1.05,WOOD,base=.73)
    i.obj('bulletin-board-cork',x,y-.07/T,w-.16,.03,.89,'#8a7958',base=.81)
    for index in range(6):
        xx=x+((index%3)-1)*w*.25/T
        z=.91+(index//3)*.34
        i.obj('notice-paper',xx,y-.095/T,.34,.015,.27,
              ['#c0bda3','#b4bea7','#a7b7b1'][index%3],base=z)
        i.obj('notice-paper-heading',xx,y-.108/T,.23,.008,.025,'#697569',base=z+.19)


def _tabletop_details(i):
    """Books, pencil pots, paper and static bags make table groups lived in."""
    tables=[o for o in i.fixtures if o['kind'] in [
        'student-desk','teacher-desk','music-desk','art-worktable',
        'art-teacher-workbench','reading-table','individual-reading-desk','loan-counter']]
    for n,o in enumerate(tables):
        if o['kind']=='student-desk' and n%3==2:continue
        x,y=o['x'],o['y'];top=o['elevation']+o['height']+.015
        i.obj('desk-notebook',x-.11/T,y-.035/T,.24,.30,.025,
              ['#a9b59f','#a98c73','#8eaaa7'][n%3],base=top)
        i.obj('notebook-page',x-.11/T,y-.035/T,.21,.27,.008,PAPER,base=top+.025)
        if n%2==0:
            i.obj('pencil-cup',x+.19/T,y+.08/T,.10,.10,.13,'#677c73',base=top,shape='cylinder')
            for k in range(3):i.obj('pencil',x+(.16+k*.026)/T,y+.08/T,.012,.012,.20,
                                   ['#aa8d50','#6c8170','#8d6c5e'][k],base=top+.03)
        if o['kind'] in ['student-desk','music-desk'] and n%4==0:
            i.obj('school-bag',x+.31/T,y+.12/T,.25,.16,.34,
                  ['#4a5c61','#67614d','#62526a'][n%3],base=.05)
            i.obj('bag-pocket',x+.31/T,y+.025/T,.17,.035,.17,'#78857a',base=.09)


def _classroom(i):
    # The two side strips and front corners were previously mostly unused.
    for y in [6,10]:_assembly(i,lambda y=y:_radiator(i,1.2,y))
    _assembly(i,lambda:_cabinet(i,1.25,14.8,1.40,.48,1.68,'teaching-supply-cabinet'))
    _assembly(i,lambda:_cabinet(i,15.8,14.9,1.15,.50,1.75,'cleaning-closet'))
    _assembly(i,lambda:i.shelf(16,8,1.1,.48,books=True))
    _assembly(i,lambda:i.shelf(1.4,3.9,1.45,.50,books=True))
    _assembly(i,lambda:_trash(i,15.9,3.8))
    _assembly(i,lambda:_bench(i,12.6,15.6,1.75))
    _bulletin(i,3.3,16.35,2.5)
    i.obj('classroom-clock',12.1,16.30,.48,.08,.48,'#c0c1ac',base=1.28,shape='cylinder')
    i.obj('clock-centre',12.1,16.24,.035,.045,.035,'#45574d',base=1.50)
    i.obj('clock-hand-horizontal',12.16,16.24,.16,.03,.022,'#45574d',base=1.50)
    i.obj('clock-hand-vertical',12.1,16.24,.022,.03,.13,'#45574d',base=1.50)
    for x in [15.55,15.78]:
        i.obj('cleaning-broom-handle',x,14.5,.035,.035,1.4,'#9a8861')
        i.obj('cleaning-broom-head',x,14.5,.24,.065,.13,'#a89a71',base=.02)
    # Chalk and an eraser sit on the existing board tray.
    i.obj('chalk-eraser',8.0,16.22,.16,.07,.055,'#7d7962',base=.81)
    for x in [8.3,8.44]:i.obj('chalk-stick',x,16.22,.075,.02,.02,'#d5d3ba',base=.81)


def _art(i):
    # Freestanding work/display islands break up the broad central void.
    _assembly(i,lambda:i.table(11,7,1.6,.85,'art-sculpture-worktable'))
    _assembly(i,lambda:i.bust(11,13))
    _assembly(i,lambda:i.easel(16,9))
    def drying_rack():
        i.obj('canvas-drying-rack',16,14,1.45,.55,1.25,'#646f62',solid=True)
        for level in range(4):i.obj('drying-rack-shelf',16,14-.04,1.52,.62,.045,'#a09373',base=.14+level*.29)
        for offset in [-.35,0,.35]:i.obj('drying-canvas',16+offset/T,14-.08,.24,.48,.035,'#c8bca0',base=1.02)
    _assembly(i,drying_rack)
    for x,y in [(2,4),(2,12),(23.7,11)]:_assembly(i,lambda x=x,y=y:_cabinet(i,x,y,1.65,.55,1.6,'art-material-cabinet'))
    for x,y in [(6,3.8),(18,3.8),(11,18.1),(20,19)]:
        def cart(x=x,y=y):
            i.table(x,y,1.05,.58,'art-supply-cart')
            for k in range(5):
                i.obj('paint-pot',x+(k-2)*.15/T,y,.10,.10,.12,
                      ['#a89d66','#6d8c7a','#9b675b','#6c8290','#beb5a3'][k],base=.81,shape='cylinder')
        _assembly(i,cart)
    for x,y in [(4,17),(21,6),(16,21)]:
        _assembly(i,lambda x=x,y=y:i.obj('stacked-art-boards',x,y,1.1,.28,1.15,'#b8af91',solid=True))
    for x,y in [(5,21),(10,21),(18,21)]:_assembly(i,lambda x=x,y=y:i.bust(x,y))
    for x,y in [(10,4),(16,4),(11,16)]:_assembly(i,lambda x=x,y=y:i.easel(x,y))
    _bulletin(i,4.6,22.25,3.0)
    _assembly(i,lambda:_trash(i,23.5,7))
    # Brushes and paint palettes stand on existing work tables, not the aisles.
    for n,o in enumerate([o for o in i.fixtures if o['kind']=='art-worktable']):
        if n%2:continue
        i.obj('paint-palette',o['x']+.40/T,o['y'],.30,.22,.028,'#b3a582',base=.82)
        for k in range(4):i.obj('paint-dab',o['x']+(.30+k*.055)/T,o['y'],.042,.042,.018,
                               ['#9c675d','#64816b','#a8985e','#657e8e'][k],base=.85,shape='cylinder')
        i.obj('brush-jar',o['x']-.4/T,o['y']+.12/T,.13,.13,.18,'#7c8c79',base=.82,shape='cylinder')
        for k in range(3):i.obj('paintbrush',o['x']+(-.43+k*.026)/T,o['y']+.12/T,.012,.012,.30,'#a39169',base=.90)


def _storage(i):
    for x,y in [(2,2.7),(15.5,8),(7,10.5)]:_assembly(i,lambda x=x,y=y:i.bust(x,y))
    for x,y in [(2.3,7.9),(6.2,3.3),(12.5,3.3)]:
        _assembly(i,lambda x=x,y=y:i.obj('stacked-material-boxes',x,y,.78,.65,.66,'#8f805e',solid=True))
    _assembly(i,lambda:_cabinet(i,15.7,2.6,1.2,.50,1.5,'art-tool-cabinet'))
    for x,y in [(7.4,11.7),(10.6,11.7)]:
        i.obj('rolled-paper-bundle',x,y,.35,.35,.85,'#bcb99d',shape='cylinder')
    _bulletin(i,3.5,12.3,1.8)


def _library(i):
    # Low display islands and seating occupy the broad centre without closing
    # the main x17 spine or the cross aisles used by the player.
    _assembly(i,lambda:i.shelf(15,10,1.65,.55,books=True))
    _assembly(i,lambda:i.shelf(19,21,1.65,.55,books=True))
    _assembly(i,lambda:_bench(i,15,16,1.8))
    def book_display():
        i.table(19,12,1.45,.75,'library-display-table')
        for n in range(3):i.obj('displayed-book',19+(n-1)*.36/T,12,.29,.40,.055,
            ['#8a715c','#678174','#728c96'][n],base=.81)
    _assembly(i,book_display)
    # A reading/returns zone is a cluster of furniture, with the central spine
    # and north/south cross aisles kept clear.
    for x,y in [(5,3),(11,3),(27.5,3)]:_assembly(i,lambda x=x,y=y:_bench(i,x,y,2.1))
    for x,y in [(14,4),(29,4)]:
        def cart(x=x,y=y):
            i.table(x,y,.90,.55,'book-return-cart')
            for level in range(3):
                i.obj('returned-book-stack',x,y,.48,.33,.065,
                      ['#84927b','#9b7d63','#6e8790'][level],base=.81+level*.065)
        _assembly(i,cart)
    for x,y in [(3,29.7),(30.5,29.7)]:_assembly(i,lambda x=x,y=y:_cabinet(i,x,y,1.8,.48,1.6,'library-document-cabinet'))
    for x,y in [(2.0,10),(32.0,10),(2,22),(32,22)]:_assembly(i,lambda x=x,y=y:_radiator(i,x,y))
    for x,y in [(15,27),(20,27)]:_assembly(i,lambda x=x,y=y:i.shelf(x,y,1.65,.55,books=True))
    for x,y in [(6,30),(12,30),(23,30),(29,30)]:
        _assembly(i,lambda x=x,y=y:i.table(x,y,1.45,.70,'library-study-table'))
    _bulletin(i,9,30.35,3.3)
    _bulletin(i,25,30.35,3.3)
    _assembly(i,lambda:_trash(i,30.6,6.5))
    # An old search terminal and desk lamp make the loan desk read as service
    # furniture, with no computer interaction or pickups implemented.
    i.obj('library-search-monitor',23.55,4,.48,.16,.38,'#3d4b46',base=.91)
    i.obj('library-search-screen',23.55,3.925,.38,.025,.27,'#829186',base=.97)
    i.obj('library-desk-lamp-base',22.45,4,.16,.16,.045,METAL,base=.81,shape='cylinder')
    i.obj('library-desk-lamp-stem',22.45,4,.03,.03,.39,METAL,base=.85)
    i.obj('library-desk-lamp-shade',22.45,4,.30,.22,.14,'#9ba98d',base=1.20)


def add_details(interior):
    """Enrich existing authored rooms, preserving each room's open circulation."""
    handlers={'classroom':_classroom,'art':_art,'artStorage':_storage,'library':_library}
    handler=handlers.get(interior.kind)
    if handler:handler(interior)
    _tabletop_details(interior)
