"""Original school broadcast and restroom furnishings for exploration only.

The rejected broadcast image is not reused. Its replacement is a small school
PA control room and a connected announcer booth, with static old equipment.
The latest user request overrides the backup's nearly identical toilet layouts:
student facilities have more fixtures; staff facilities have quieter storage and
separate vanity furniture. All dimensions are metres, coordinates are grid cells.
"""
T=1.25
METAL='#64766e'
CREAM='#b6bca6'
CERAMIC='#c4c9b7'
TILE='#73887f'
WOOD='#806b4e'


def _cabinet(i,x,y,w=1.5,d=.55,h=1.7,kind='facility-storage-cabinet'):
    i.obj(kind,x,y,w,d,h,'#758475',solid=True)
    for side in [-1,1]:
        i.obj('facility-cabinet-door',x+side*w*.245/T,y-(d/2+.026)/T,
              w*.46,.035,h-.16,'#9da58f',base=.08)
        i.obj('facility-cabinet-handle',x+side*.055/T,y-(d/2+.065)/T,
              .026,.035,.15,'#d1cdb0',base=h*.48)
    i.obj('facility-cabinet-top',x,y,w+.045,d+.05,.055,'#b2b59d',base=h)


def _bin(i,x,y,staff=False):
    i.obj('restroom-waste-bin',x,y,.36,.36,.57,
          '#738577' if staff else '#557365',solid=True)
    i.obj('restroom-bin-lid',x,y,.40,.40,.055,'#a8ae96',base=.57)


def _restroom_board(i,x,y,staff):
    i.obj('staff-restroom-notice' if staff else 'student-hygiene-notice',
          x,y,1.2,.075,.74,'#536d60',base=1.00)
    for n in range(3):
        i.obj('restroom-notice-paper',x+(n-1)*.31/T,y-.05/T,.25,.018,.51,
              '#c8c7aa',base=1.10)


def _toilet(i,x,y,staff):
    i.obj('toilet-base',x,y,.62,.85,.39,CERAMIC,solid=True)
    i.obj('toilet-pedestal',x,y-.1/T,.43,.51,.39,'#a3b29f')
    i.obj('toilet-seat',x,y-.12/T,.58,.68,.07,'#dde0cd',base=.39,shape='cylinder')
    i.obj('toilet-bowl-opening',x,y-.13/T,.36,.47,.012,'#576f64',base=.466,shape='cylinder')
    i.obj('toilet-cistern',x,y+.36/T,.61,.20,.77,CERAMIC,solid=True)
    i.obj('toilet-flush-button',x+.17/T,y+.245/T,.075,.025,.055,
          '#7f9485',base=.65)
    i.obj('toilet-paper-holder',x+.58/T,y+.08/T,.14,.12,.18,
          '#71877b',base=.56)
    i.obj('toilet-paper-roll',x+.58/T,y-.005/T,.10,.11,.13,
          '#d4d4bb',base=.585,shape='cylinder')
    if staff:
        i.obj('staff-stall-shelf',x-.36/T,y+.30/T,.28,.15,.055,
              '#9da38c',base=1.05)


def _stall(i,cx,cy,west,staff,index):
    """Open cubicle entrance faces the central aisle; solid doors are ajar.

    The 1.20m opening is unobstructed. The door lies perpendicular to its closed
    position, leaving actual walking access without adding an interaction.
    """
    w,d,opening=2.8,2.45,1.2
    color='#8f9e8d' if staff else '#6e887c'
    for dy in [-1,1]:
        i.obj('restroom-stall-partition',cx,cy+dy*(d/2)/T,w,.075,1.45,
              color,solid=True)
        i.obj('restroom-partition-cap',cx,cy+dy*(d/2)/T,w,.09,.05,
              '#b4baa1',base=1.45)
    sign=1 if west else -1
    back=cx-sign*w/2/T;front=cx+sign*w/2/T
    i.obj('restroom-stall-partition',back,cy,.075,d,1.45,color,solid=True)
    jamb=(d-opening)/2
    for dy in [-1,1]:
        i.obj('restroom-stall-partition',front,cy+dy*(opening+jamb)/2/T,
              .075,jamb,1.45,color,solid=True)
    i.obj('restroom-stall-door',front+sign*.49/T,cy+opening/2/T,
          .98,.045,1.30,'#a2ac94' if staff else '#819a8c',base=.10,solid=True)
    i.obj('restroom-stall-door-handle',front+sign*.82/T,cy+(opening/2-.04)/T,
          .085,.055,.055,'#c2c2a4',base=.78)
    _toilet(i,cx-sign*.40/T,cy,staff)
    outside=int(round(front+sign*1.0))
    inside=int(round(front-sign*.35))
    i.zone(f'{index}번 칸 앞',outside,int(cy))
    i.zone(f'{index}번 칸 안쪽',inside,int(cy))
    if i.label.endswith('여'):
        i.obj('sanitary-bin',cx-sign*.30/T,cy-.68/T,.23,.23,.40,
              '#7b8f7e',solid=True)


def _wash_area(i,staff):
    cx=i.width//2;count=2 if staff else 4
    width=4.0 if staff else 6.4
    mirror_y=i.height-1.65
    # Front plane is 40mm in front of its frame. Both runtimes replace this
    # dedicated surface with their real mirror renderer; it is not a flat prop.
    i.obj('mirror-frame',cx,mirror_y+.06/T,width+.22,.08,1.77,
          '#728d80',base=.74)
    i.obj('mirror-surface',cx,mirror_y,width,.025,1.55,
          '#a9c4bd',base=.85,label='큰 거울')
    i.obj('mirror-light-bar',cx,mirror_y-.04/T,width*.75,.085,.07,
          '#d3d0b0',base=2.57)
    sink_y=i.height-3.70
    if not staff:
        # Two long shared halves leave a genuine 1.10m middle approach to the
        # mirror. The camera is steep, so players can walk close enough to see
        # their reflected head rather than only the floor behind the sinks.
        for side in [-1,1]:
            xx=cx+side*1.85/T
            i.obj('student-wash-counter',xx,sink_y,2.60,.80,.81,
                  '#83998a',solid=True)
            i.obj('student-wash-worktop',xx,sink_y,2.65,.86,.055,
                  '#c0c7af',base=.81)
    offsets=[-1.35,1.35] if staff else [-2.45,-1.25,1.25,2.45]
    for n in range(count):
        x=cx+offsets[n]/T
        if staff:
            i.obj('staff-vanity-cabinet',x,sink_y,1.5,.76,.81,
                  '#8e9984',solid=True)
            i.obj('staff-vanity-front',x,sink_y-.40/T,1.36,.035,.65,
                  '#b1b79d',base=.08)
            i.obj('staff-vanity-handle',x,sink_y-.44/T,.21,.035,.035,
                  '#6b8171',base=.48)
        i.obj('washbasin-rim',x,sink_y,.98,.64,.075,CERAMIC,base=.865)
        i.obj('washbasin-bowl',x,sink_y-.05/T,.69,.43,.025,
              '#6c887d',base=.945)
        i.obj('washbasin-tap',x,sink_y+.22/T,.042,.045,.28,
              '#a5b7a8',base=.92)
        i.obj('washbasin-spout',x,sink_y+.10/T,.045,.27,.045,
              '#a5b7a8',base=1.16)
        i.obj('soap-dispenser',x+.47/T,mirror_y-.12/T,.12,.13,.24,
              '#c3c7af',base=1.10)
    _bin(i,cx+width/2/T+1.0,sink_y,staff)
    i.obj('paper-towel-dispenser',cx-width/2/T-.65,sink_y+.25,.43,.18,.36,
          '#b7bfa7',base=1.17)
    i.zone('큰 거울 앞',cx,i.height-2)
    i.zone('세면대 앞',cx,i.height-5)


def _urinals(i,staff):
    xs=[11.8,14.5] if staff else [15.0,17.25,19.5]
    y=i.height-6.2
    for n,x in enumerate(xs):
        i.obj('urinal-body',x,y,.56,.58,.82,CERAMIC,base=.18,solid=True)
        i.obj('urinal-bowl',x,y-.27/T,.37,.035,.56,'#637e72',base=.34)
        i.obj('urinal-flush-pipe',x,y+.04/T,.045,.045,.34,
              '#a4b5a3',base=1.00)
        i.obj('urinal-flush-valve',x,y-.015/T,.13,.10,.09,
              '#7f9b8c',base=1.26)
        if n<len(xs)-1:
            xx=(x+xs[n+1])/2
            i.obj('urinal-privacy-partition',xx,y-.06/T,.065,1.10,1.13,
                  '#7d9686',base=.18,solid=True)
    i.zone('소변기 앞',int(round(xs[len(xs)//2])),int(y-1.0))


def build_restroom(i):
    staff=i.kind=='staffRestroom';male=i.label.endswith('남')
    if staff:
        west_rows=[6,10]
        east_rows=[] if male else [8]
    else:
        west_rows=[6,10,14]
        east_rows=[] if male else [6,10]
    index=0
    for west,rows in [(True,west_rows),(False,east_rows)]:
        for y in rows:
            index+=1;_stall(i,3.1 if west else i.width-4.1,y,west,staff,index)
    _wash_area(i,staff)
    if male:_urinals(i,staff)
    _restroom_board(i,4.8,i.height-1.7,staff)
    if staff:
        _cabinet(i,3,3.2,1.45,.55,1.65,'staff-restroom-coat-cabinet')
        i.obj('staff-coat-rail',i.width-3.0,4.4,1.45,.065,.07,
              '#7f9786',base=1.57)
        for n in range(4):
            i.obj('staff-coat-hook',i.width-3.0+(n-1.5)*.28/T,4.32,
                  .045,.15,.09,'#b1bca3',base=1.45)
        i.obj('staff-restroom-bench',i.width-3.0,3.4,1.65,.43,.48,
              '#8f9a80',solid=True)
        i.zone('의복 보관 앞',5,3)
    else:
        _cabinet(i,3,3.2,1.35,.55,1.45,'student-cleaning-cabinet')
        i.obj('student-mop-bucket',i.width-3,3.4,.47,.46,.49,
              '#5d7f6a',solid=True)
        i.obj('student-mop-handle',i.width-3.1,3.4,.035,.035,1.35,
              '#a29465')
        i.obj('student-mop-head',i.width-3.1,3.4,.31,.12,.10,
              '#b0b69b',base=.02)
        i.zone('청소 도구 앞',i.width-5,4)
    # Scuffed dado strips and pipes add age without changing collision routes.
    for side,x in [(-1,1.1),(1,i.width-2.1)]:
        for y in [5,9,13]:
            if y>=i.height-3:continue
            i.obj('restroom-tiled-dado',x,y,.06,2.75,.73,
                  '#8eaa97' if staff else '#688c7d',base=.04)
        i.obj('restroom-pipe',x,3.4,.055,.055,1.72,
              '#8e9f8d')
    i.zone('화장실 중앙 통로',i.width//2,i.height//2)


def _rack(i,x,y):
    i.obj('broadcast-equipment-rack',x,y,1.25,.78,1.86,
          '#334d46',solid=True)
    for n in range(6):
        z=.12+n*.27
        i.obj('broadcast-rack-unit',x,y-.415/T,1.08,.045,.20,
              '#6f8271',base=z)
        i.obj('broadcast-rack-slot',x-.22/T,y-.445/T,.46,.018,.035,
              '#263d36',base=z+.13)
        for k in range(3):
            i.obj('broadcast-rack-knob',x+(.16+k*.14)/T,y-.45/T,
                  .07,.04,.07,'#b4b69a',base=z+.06,shape='cylinder')


def _microphone(i,x,y,base):
    i.obj('broadcast-microphone-base',x,y,.23,.18,.035,
          '#425950',base=base)
    i.obj('broadcast-microphone-stem',x,y,.035,.035,.29,
          '#809889',base=base+.035)
    i.obj('broadcast-microphone',x,y-.035/T,.105,.14,.18,
          '#2f4841',base=base+.29,shape='sphere')


def _mixer(i,x,y):
    i.obj('broadcast-mixer',x,y,1.03,.65,.13,'#61776b',base=.815)
    for channel in range(8):
        xx=x+(channel-3.5)*.105/T
        i.obj('broadcast-fader-track',xx,y-.07/T,.018,.26,.012,
              '#263f36',base=.951)
        i.obj('broadcast-fader-cap',xx,y+((channel%3)-1)*.06/T,
              .06,.05,.035,'#b2b79c',base=.965)
        for row in range(2):
            i.obj('broadcast-mixer-knob',xx,y+(.16+row*.11)/T,
                  .048,.048,.035,['#9aab8a','#b4a17c'][row],base=.95,shape='cylinder')


def build_broadcast(i):
    # The walkable 13x12-cell interior is 65% of the ordinary classroom's
    # 16x15-cell interior. A glass front and open side entrance form a booth.
    i.obj('broadcast-booth-partition',11.4,9.55,5.2,.10,.74,
          '#697c6c',solid=True)
    i.obj('broadcast-window-frame',11.4,9.55,5.28,.14,.09,
          '#a0a58c',base=.74)
    i.obj('broadcast-acoustic-window',11.4,9.48,4.85,.025,.72,
          '#557a78',base=.86)
    i.obj('broadcast-window-top',11.4,9.55,5.28,.14,.08,
          '#9da68d',base=1.60)
    for x in [9.35,13.45]:
        i.obj('broadcast-window-post',x,9.55,.09,.13,1.58,
              '#7c8e7a',solid=True)
    for y,d in [(10.0,1.15),(12.8,1.30)]:
        i.obj('broadcast-booth-partition',9.3,y,.10,d,1.45,
              '#758576',solid=True)
    for n in range(5):
        i.obj('broadcast-acoustic-panel',10.0+n*.68,13.3,.57,.08,.75,
              ['#777e68','#687a69'][n%2],base=.72)
    i.table(11.4,12,2.5,.85,'broadcast-announcer-desk')
    i.chair(11.4,12.9)
    _microphone(i,10.9,11.85,.815);_microphone(i,11.9,11.85,.815)
    i.obj('broadcast-script-page',11.4,11.83,.30,.38,.018,
          '#c8c5a9',base=.82)
    i.obj('broadcast-script-stand',11.4,12.20,.45,.11,.25,
          '#586e5e',base=.82)

    i.table(5.7,8.7,5.5,1.1,'broadcast-console')
    for x in [4.25,7.05]:_mixer(i,x,8.65)
    _microphone(i,5.7,8.5,.815)
    for x in [4.2,5.7,7.1]:i.chair(x,7.45)
    i.obj('broadcast-monitor-body',5.7,9.0,.70,.40,.48,
          '#8f9e88',base=.82)
    i.obj('broadcast-monitor-screen',5.7,8.83,.55,.025,.32,
          '#2c4c48',base=.91)
    for x,y in [(2.0,11.8),(4.0,12.6)]:_rack(i,x,y)
    _cabinet(i,6.9,12.6,2.2,.63,1.78,'broadcast-storage-cabinet')
    for row in range(2):
        for n in range(6):
            i.obj('broadcast-tape-box',6.9+(n-2.5)*.20/T,12.58,
                  .16,.24,.20,['#9e9e7d','#7b957e','#ac9a77'][n%3],base=1.84+row*.205)
    i.table(3.5,5.1,2.25,.85,'broadcast-club-desk');i.chair(3.5,4.15)
    i.obj('broadcast-club-notebook',3.3,5.1,.29,.38,.035,
          '#a2ab89',base=.815)
    i.obj('broadcast-club-paper',3.65,5.1,.24,.32,.025,
          '#d0c9aa',base=.815)
    _cabinet(i,2.1,8.0,1.3,.55,1.55,'broadcast-file-cabinet')
    _cabinet(i,11.6,5.2,2.6,.62,1.05,'broadcast-media-cabinet')
    for x in [10.8,12.1]:
        i.obj('broadcast-camera-case',x,5.2,.70,.42,.35,
              '#536b5c',base=1.11)
        i.obj('broadcast-case-handle',x,5.2,.21,.055,.06,
              '#9da98b',base=1.46)
    i.obj('broadcast-schedule-board',6.6,13.3,2.25,.07,.83,
          '#5b775f',base=1.14)
    for n in range(4):
        i.obj('broadcast-schedule-paper',6.6+(n-1.5)*.45/T,13.24,.34,.018,.59,
              '#cbc9a8',base=1.26)
    i.obj('broadcast-wall-TV',12.7,13.1,.73,.39,.55,
          '#344e45',base=1.30)
    i.obj('broadcast-TV-glass',12.7,12.92,.58,.025,.37,
          '#678880',base=1.39)
    i.obj('broadcast-floor-cable-case',4.8,2.8,.82,.55,.38,
          '#6f7d64',solid=True)
    i.obj('broadcast-waste-bin',2.0,3.0,.35,.35,.53,
          '#667e69',solid=True)
    i.zone('방송 콘솔 앞',6,6)
    i.zone('장비 랙 앞',3,10)
    i.zone('방송 자료장 앞',7,11)
    i.zone('방송부 작업 책상 앞',5,5)
    i.zone('방송 부스 입구',8,11)
    i.zone('안내방송 부스',11,11)
    i.zone('촬영 장비 보관 앞',11,4)
