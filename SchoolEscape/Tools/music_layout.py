"""Music V3: main classroom -> bent side passage -> old vocal room.
The instrument store branches off the middle of that passage. All stay in one
isolated music space. This nonrectangular footprint follows the available text
backup; orientation defaults to option1 while the large image DOCX is unread.
"""
T=1.25
WOOD='#80664b';METAL='#48585a';PAPER='#c6bda4'

def build_music(s):
    areas=[(1,2,25,20),(26,17,30,18),(29,18,30,26),(28,27,34,32),(19,23,26,30),(27,24,28,25)]
    s.map['rows']=[''.join('.' if any(x0<=x<=x1 and y0<=y<=y1 for x0,y0,x1,y1 in areas) else '#' for x in range(s.width)) for y in range(s.height)]
    s.map['subtitle']='메인 음악실 · 꺾인 연결 통로 · 보컬룸 · 악기창고'
    # Stage and large rehearsal layout, with a central cross aisle and varied sections.
    s.obj('music-stage',8,18,11.0,2.5,.10,'#66533b')
    s.obj('music-board-frame',12,20.40,5.2,.10,1.0,base=.65)
    s.obj('music-board',12,20.34,5.0,.025,.82,'#b8b9a5',base=.74)
    for i in range(5):s.obj('stave-line',12,20.29,4.8,.018,.014,'#59675b',base=.94+i*.075)
    s.table(18.5,19,1.6,.7,'music-teacher-desk')
    px,py=5,17.5
    s.obj('piano-body',px,py,2.20,1.30,.91,'#252e2c',solid=True)
    s.obj('piano-lid',px,py+.25,2.20,.67,.06,'#383c35',base=.91)
    s.obj('piano-keyboard',px,py-.52,1.95,.26,.06,'#d7d1b9',base=.75)
    for i in range(16):s.obj('piano-black-key',px+(i-7.5)*.11/T,py-.49,.050,.12,.04,'#202a28',base=.82)
    s.chair(px,py-1)
    # Rehearsal chairs/desks broken into sections instead of a filled square grid.
    for x,ys in [(4,[6,9,12]),(7,[5.5,8.5,11.5]),(10,[5,8,11]),(17,[5,8,11]),(20,[5.5,8.5,11.5]),(23,[6,9,12])]:
        for y in ys:s.table(x,y,.85,.55,'music-desk');s.chair(x,y-.56)
    for x,y in [(6,14),(11,14),(17,14),(22,14)]:
        s.obj('music-stand-foot',x,y,.40,.40,.045,METAL,shape='cylinder')
        s.obj('music-stand-stem',x,y,.035,.035,1.0,METAL)
        s.obj('music-stand-sheet',x,y,.50,.10,.33,METAL,base=.95)
        s.obj('music-stand-score',x,y-.045,.42,.025,.25,PAPER,base=.99)
    # A percussion / ensemble island fills the centre-left while preserving
    # the main x14 cross route and the authored rehearsal landmarks.
    for x,y,w,h in [(13,7,.78,.72),(12,15.2,.62,.82),(15.5,4.8,.58,.62)]:
        s.obj('rehearsal-drum',x,y,w,w*.86,h,'#74634d',solid=True,shape='cylinder')
        s.obj('drum-head',x,y,w+.045,w*.86+.045,.045,'#c1b9a0',base=h,shape='cylinder')
        for side in [-1,1]:s.obj('drum-leg',x+side*w*.32/T,y,.035,.035,.30,METAL)
    s.obj('instrument-rack',1.7,14.8,1.30,.55,1.05,'#605b48',solid=True)
    for xx in [1.35,1.7,2.05]:
        s.obj('guitar-body',xx,14.8,.35,.16,.48,'#96734c',base=.22,shape='sphere')
        s.obj('guitar-neck',xx,14.8,.055,.06,.62,'#614a33',base=.65)
    # Music portraits along the classroom wall, not floating in the route.
    for xx in [3,8,16,22]:
        yy=20.4;s.obj('composer-frame',xx,yy,.66,.10,.79,'#73634e',base=1.12)
        s.obj('composer-paper',xx,yy-.06,.55,.025,.67,PAPER,base=1.17)
        s.obj('composer-silhouette-head',xx,yy-.078,.20,.04,.24,'#53574f',base=1.51,shape='sphere')
        s.obj('composer-silhouette-body',xx,yy-.078,.33,.04,.23,'#53574f',base=1.26)
    for xx in [3,6]:s.shelf(xx,3.2,1.8,.6,books=True)
    for yy in [6,10,14]:s.shelf(24.8,yy,1.0,.5)
    s.obj('music-cabinet',1.3,9,1.1,.55,1.45,'#63746b',solid=True)
    s.obj('music-trolley',21.8,3.3,.85,.65,.75,'#5e6c65',solid=True)
    for i in range(4):s.obj('music-score-stack',21.8+(i%2)*.2,3.3+(i//2)*.16,.20,.24,.06,PAPER,base=.76+i*.02)
    # Thresholds and open inner frames: walk through, no portal/E inside suite.
    for x,y,along_y in [(25.5,17.5,True),(28.5,24.5,True),(29.5,26.5,False)]:
        for side in [-1,1]:
            s.obj('music-inner-door-frame',x+(0 if along_y else side*.95),y+(side*.95 if along_y else 0),.10 if along_y else .14,.14 if along_y else .10,1.55,'#9da793')
        s.obj('music-inner-threshold',x,y,.22 if along_y else 2.15,2.15 if along_y else .22,.025,'#a69a7c')
    # Narrow bend: wall-mounted fittings keep the walking centre clear.
    s.obj('music-passage-notice',30.44,21,.04,.70,.55,'#a59b80',base=.75)
    # Old school vocal room at the far end; it is not a modern studio.
    s.table(28.9,31,.95,.55,'old-vocal-desk');s.chair(28.9,30.35)
    s.obj('vocal-absorber',27.6,30,.10,2.1,.85,'#666c5c',base=.65)
    s.obj('old-amplifier',33.5,31,.65,.45,.70,'#38413b',solid=True)
    s.obj('old-amplifier',33.5,28,.65,.45,.70,'#38413b',solid=True)
    s.obj('microphone-base',31.3,31,.32,.32,.045,METAL,shape='cylinder')
    s.obj('microphone-stand',31.3,31,.035,.035,1.35,METAL)
    s.obj('old-microphone',31.3,31,.10,.10,.16,'#b2b19d',base=1.28,shape='sphere')
    s.obj('vocal-score-stand',31.3,30.4,.46,.12,.38,METAL,base=.80)
    s.obj('vocal-score-sheet',31.3,30.34,.38,.02,.30,PAPER,base=.85)
    # Side store is a cluttered equipment room, not a display gallery.
    for xx in [20,23,25.5]:s.shelf(xx,29.8,1.7,.60)
    for xx in [20,23,25]:s.obj('instrument-case',xx,23.8,.55,1.05,.30,'#534e3f',solid=True)
    for xx,yy in [(19.2,26),(25.7,27.4)]:
        s.obj('instrument-crate',xx,yy,.85,.70,.55,'#84775b',solid=True)
        s.obj('instrument-crate-top',xx,yy,.86,.71,.035,'#b3a17c',base=.56)
    # Recognizable original geometric string instrument on its stand.
    s.obj('cello-stand',20,28,.6,.55,.06,METAL,solid=True)
    s.obj('cello-body',20,28,.55,.28,.75,'#8d5935',base=.18,shape='sphere')
    s.obj('cello-neck',20,28,.075,.10,.78,'#61462e',base=.82)
    for side in [-1,1]:s.obj('cello-peg',20+side*.07,28,.20,.07,.035,'#5c422b',base=1.42)
    s.obj('cleaning-broom',26.2,29,.05,.06,1.45)
    for xx in [21,24]:s.obj('rolled-music-banner',xx,23.6,.27,.55,.65,'#758476',solid=True,shape='cylinder')
    s.zone('메인 음악실',14,11);s.zone('연결 통로',29,22);s.zone('보컬 연습실',31,29);s.zone('악기보관창고',22,26)
    s.zone('통로 굴절부',29,18);s.zone('합주 공간',14,15);s.zone('피아노 앞',6,16)
