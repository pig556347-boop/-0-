"""Original authored schoolyard, using SchoolEscape's metre-sized fixtures.

North is increasing grid y.  The schoolyard is a separate E-door space;
the north main-building door and south escape gate never expose another map.
Only this project's existing plans and furniture vocabulary are used here.
"""
from math import cos, sin, tau

T = 1.25
W, H = 69, 55
WOOD = '#84765a'
METAL = '#536e68'
PALE = '#b3b69b'


class Schoolyard:
    def __init__(self):
        self.fixtures = []
        self.landmarks = []
        self.zones = []
        grid = [['#'] * W for _ in range(H)]
        for y in range(1, H - 1):
            for x in range(1, W - 1):
                grid[y][x] = '.'
        grid[53][34] = '#'
        self.map = dict(
            id='playground', label='운동장', kind='playground',
            isOutdoor=True, isRoom=False, number=0, sourceFloor=0,
            subtitle='본관 앞마당 · 중앙 운동장 · 교문', width=W, height=H,
            spawnX=34, spawnY=50, rows=[''.join(row) for row in grid],
            rooms=[], stairs=[], barriers=[], fixtures=self.fixtures,
            landmarks=self.landmarks, interiorZones=self.zones,
            portals=[dict(id='playground-to-main', label='본관 1층으로 들어가기',
                          doorX=34, doorY=53, facing='south',
                          targetId='floor-1', arrivalX=24, arrivalY=3)],
            approximateWidthMeters=83.75, approximateDepthMeters=66.25,
        )

    def obj(self, kind, x, y, w, d, h, color=WOOD, base=0,
            solid=False, label='', shape='box', assembly=''):
        item = dict(id=f'playground-{len(self.fixtures):04d}', room='운동장',
                    kind=kind, label=label, x=round(x, 3), y=round(y, 3),
                    width=round(w, 3), depth=round(d, 3), height=round(h, 3),
                    elevation=round(base, 3), color=color, solid=solid,
                    shape=shape)
        if assembly:
            item['assembly'] = assembly
        self.fixtures.append(item)
        return item

    def mark(self, label, x, y):
        self.landmarks.append(dict(label=label, x=x, y=y))
        self.zones.append(dict(room='운동장', label=label, x=x, y=y))

    def cabinet(self, x, y, label='야외 쓰레기통', w=2.0):
        self.obj('yard-trash-bin',x,y,.85,.85,1.05,'#53645a',solid=True,label=label,shape='cylinder')
        self.obj('yard-bin-rim',x,y,.93,.93,.10,'#829181',base=1.05,shape='cylinder')
        self.obj('yard-bin-opening',x,y,.64,.64,.025,'#172b25',base=1.155,shape='cylinder')

    def bench(self, x, y, label='야외 벤치'):
        self.obj('yard-bench-base', x, y, 3.0, .65, .47,
                 '#526c60', solid=True, label=label)
        self.obj('yard-bench-seat', x, y, 3.10, .72, .09,
                 '#9b8a67', base=.47)
        self.obj('yard-bench-back', x, y + .27 / T, 3.12, .075, .48,
                 '#8a805f', base=.56)
        for xx in [-.70, .70]:
            self.obj('bench-storage-handle', x + xx / T,
                     y - .36 / T, .18, .04, .045, '#b9b799', base=.24)

    def crate(self, x, y):
        self.obj('yard-parcel-box', x, y, .88, .76, .68,
                 '#837455', solid=True, label='낡은 택배 상자')
        self.obj('yard-crate-lid', x, y, .94, .82, .055,
                 '#a5916c', base=.68)
        for xx in [-.25, .25]:
            self.obj('yard-parcel-tape', x + xx / T, y, .05, .84, .025,
                     '#b9a77a', base=.735)

    def cover(self, x, y, label):
        # Eight metres of opaque cover, with both ends accessible.  The body
        # and inset notice panels have separate planes to avoid depth fighting.
        self.obj('yard-cover-planter', x, y, 8.0, 3.0, 1.15,
                 '#677b56', solid=True, label=label)
        self.obj('planter-stone-rim', x, y, 8.10, 3.10, .09,
                 '#a2a98a', base=1.15)
        self.obj('planter-soil', x, y, 7.70, 2.70, .04,
                 '#4a5140', base=1.245)
        for n in range(7):
            self.obj('low-planter-shrub', x + (n - 3) * .94 / T, y,
                     1.12, 2.05, .42, '#69794f', base=1.29,
                     shape='sphere')
        self.obj('yard-opaque-notice-panel', x, y + .32, 6.40, .15, 1.16,
                 '#536e63', base=.55, solid=True)
        for n in range(4):
            self.obj('yard-notice-paper', x + (n - 1.5) * 1.13 / T,
                     y + .32 - .10 / T, .78, .025, .57,
                     '#b8b499', base=.89)

    def tree(self, x, y):
        self.obj('yard-tree-trunk', x, y, .36, .36, 1.64,
                 '#615b43', solid=True, shape='cylinder')
        # Trimmed, low crowns stay at the outer edge, away from route centres.
        self.obj('yard-trimmed-tree-crown', x, y, 2.9, 2.6, 1.12,
                 '#40594b', base=1.22, shape='sphere')

    def goal(self, x, y, back):
        for dy in [-1.7, 1.7]:
            self.obj('yard-goal-post', x, y + dy, .10, .10, 2.25,
                     '#a8ad99', solid=True, shape='cylinder')
            self.obj('yard-goal-back-foot', x + back * .9,
                     y + dy, .08, .08, 1.15, METAL,
                     solid=True, shape='cylinder')
            self.obj('yard-goal-ground-rail', x + back * .45,
                     y + dy, 1.2, .07, .06, '#778b78')
        self.obj('yard-goal-crossbar', x, y, .10, 4.35, .10,
                 '#b6bba3', base=2.20)
        # Sparse visible net threads are decorations, not an invisible wall.
        for n in range(7):
            self.obj('yard-goal-net-thread', x + back * .9,
                     y + (n - 3) * .5, .025, .025, 1.10,
                     '#687f6a', base=.04)

    def boundary(self):
        for x in [.5, 67.5]:
            for y in range(2, 54, 4):
                self.obj('yard-fence-post', x, y, .12, .12, 1.65,
                         METAL, solid=True)
            for z in [.30, 1.40]:
                self.obj('yard-fence-rail', x, 27, .065, 64.0, .065,
                         '#758d7d', base=z)
        for y in [.65, 53.65]:
            for x in range(2, 68, 4):
                if y < 1 and 32 <= x <= 36:
                    continue
                self.obj('yard-fence-post', x, y, .12, .12, 1.65,
                         METAL, solid=True)
            # Close the fence onto the gate pillars.  The former 35m rails
            # left apparent 2.3m exits beside a gate that cannot be bypassed.
            rails = [(16, 38.5), (52, 38.5)] if y < 1 else [(15.5, 35.0), (52.5, 35.0)]
            for xx, width in rails:
                for z in [.30, 1.40]:
                    self.obj('yard-fence-rail', xx, y, width, .065, .065,
                             '#758d7d', base=z)
        # The closed south school gate stands at the true map edge.  E use
        # happens at (34,3), never at the former indoor exit or north portal.
        self.obj('escape-gate-panel', 34, 1, 5.6, .18, 1.85,
                 '#526e61', solid=True, label='교문 · 열쇠 A·B·C')
        for n in range(13):
            self.obj('escape-gate-bar', 34 + (n - 6) * .40 / T, .89,
                     .055, .07, 1.60, '#a4aa8e', base=.10)
        for dx in [-2.40, 2.40]:
            self.obj('escape-gate-pillar', 34 + dx, 1, .70, .70, 2.20,
                     '#7d8d78', solid=True)
            self.obj('escape-gate-lamp', 34 + dx, 1, .40, .40, .18,
                     '#e0d6a4', base=2.20)
        for n, color in enumerate(['#d8b56c', '#a8c5bf', '#c0a8c7']):
            self.obj('escape-gate-lock', 34 + (n - 1) * .42 / T,
                     1.11, .23, .16, .30, color, base=.74,
                     label='ABC'[n])

    def build(self):
        self.boundary()
        self.cover(31, 31, '차폐 화단 A')
        self.cover(37, 21, '차폐 화단 B')
        # The forecourt has no obstacle within six metres of the arrival or
        # north door approach.  The stage and noticeboard sit to either side.
        self.obj('yard-stage', 20, 45, 9.0, 4.0, .42,
                 '#7e7860', solid=True, label='구령대')
        self.obj('yard-stage-front', 20, 43.35, 9.1, .10, .42,
                 '#a69b75')
        self.obj('yard-stage-lectern', 20, 45.2, .85, .65, 1.10,
                 WOOD, base=.42)
        self.obj('yard-stage-speaker', 16.9, 45.3, .60, .55, .94,
                 '#364e49', base=.42)
        self.obj('yard-stage-speaker', 23.1, 45.3, .60, .55, .94,
                 '#364e49', base=.42)
        self.obj('yard-forecourt-noticeboard', 46, 46.2, 4.0, .28, 1.7,
                 '#677c6b', solid=True, label='학교 안내판')
        for x in [44.9, 46, 47.1]:
            self.obj('yard-forecourt-notice', x, 46.04, .75, .025, .60,
                     '#b9b69a', base=.74)
        self.obj('yard-entrance-landing', 34, 50.5, 7.5, 4.0, .025,
                 '#919c86')
        for y in [50.9, 51.3, 51.7]:
            self.obj('yard-entrance-tread-mark', 34, y, 6.0, .045, .015,
                     '#bac2a5', base=.03)
        # Exterior silhouettes live outside the closed playable boundary.
        for name,cx,cy,w,d,floors in [('main',34,56.5,48,6,4),('west',6,58,17,11,3),('annex',62,58,15,10,2)]:
            self.obj('school-'+name+'-facade',cx,cy,w,d,floors*2.55,'#475b59')
            self.obj('school-'+name+'-roof',cx,cy,w+.35,d+.35,.22,'#253c40',base=floors*2.55)
            for floor in range(floors):
                self.obj('school-floor-band',cx,cy-d/2/T-.03,w,.08,.13,'#7a8a7e',base=floor*2.55+.12)
                for n in range(int(w/3.2)):
                    xx=cx+(n-(int(w/3.2)-1)/2)*3.2/T
                    self.obj('school-window-frame',xx,cy-d/2/T-.08,1.82,.11,1.32,'#81928a',base=floor*2.55+.88)
                    self.obj('school-window-glass',xx,cy-d/2/T-.14,1.61,.035,1.12,'#1c343b',base=floor*2.55+.98)
                    self.obj('school-window-mullion',xx,cy-d/2/T-.17,.055,.03,1.12,'#607b77',base=floor*2.55+.98)
        self.obj('yard-main-sign',34,53.65,5,.09,.62,'#a5b19b',base=2.45,label='본관')
        # Perimeter furnishings deliberately leave the x10/x58 side lanes,
        # y13/y39 cross-connections and the entire gate approach clear.
        for x in [4.5, 63.5]:
            for y in [16, 28, 40]:
                self.bench(x, y)
                self.crate(x+1.8 if x<10 else x-1.8,y)
            for y in [7, 21, 34, 47]:
                self.tree(3 if x < 10 else 65, y)
        for x, y in [(4.5, 35.5), (64, 35.5), (5, 10), (63, 11)]:
            self.cabinet(x, y)
        for x, y in [(7, 44), (60.5, 42), (6.5, 24), (61.5, 17)]:
            self.crate(x, y)
        self.goal(16, 27, -1)
        self.goal(52, 27, 1)
        self.obj('yard-water-station', 64, 22.5, 2.8, .80, .84,
                 '#8c9f8b', solid=True, label='급수대')
        for x in [63.4, 64, 64.6]:
            self.obj('yard-water-basin', x, 22.5, .56, .58, .06,
                     '#c2c6ad', base=.84)
            self.obj('yard-water-tap', x, 22.77, .045, .045, .26,
                     '#839d90', base=.90)
        for x in [61.5, 63, 64.5]:
            self.obj('yard-cycle-stand', x, 25, .12, .65, .65,
                     '#738f7b', solid=True)
        self.obj('yard-guard-hut', 61, 5.5, 6.0, 4.5, 2.35,
                 '#526d61', solid=True, label='경비초소 · 닫힘')
        self.obj('guard-hut-roof', 61, 5.5, 6.3, 4.8, .18,
                 '#7e8c73', base=2.35)
        self.obj('guard-hut-door', 60, 3.67, 1.05, .045, 1.7,
                 '#334c44', base=.07)
        self.obj('guard-hut-window', 62, 3.66, 1.55, .04, .78,
                 '#233f42', base=1.1)
        self.cabinet(65,6)
        # Light-coloured ground marks stand above the floor without matching
        # planes.  Dashed loops have no collision and are not narrow rails.
        for n in range(76):
            angle = tau * n / 76
            self.obj('yard-track-mark', 34 + 25 * cos(angle),
                     27 + 18 * sin(angle), .26, .26, .012,
                     '#b0ab87', base=.025)
        for y in [15, 39]:
            self.obj('yard-field-line', 34, y, 40.0, .075, .012,
                     '#b5b393', base=.025)
        for x in [18, 50]:
            self.obj('yard-field-line', x, 27, .075, 30.0, .012,
                     '#b5b393', base=.025)
        self.obj('yard-field-centre-line', 34, 27, .075, 30.0, .012,
                 '#b5b393', base=.025)
        for n in range(28):
            angle = tau * n / 28
            self.obj('yard-centre-circle-mark', 34 + 3.5 * cos(angle),
                     27 + 3.5 * sin(angle), .18, .18, .012,
                     '#b5b393', base=.025)
        for label, x, y in [
            ('본관 앞마당', 34, 48), ('본관 현관 앞', 34, 52),
            ('북쪽 가로길', 34, 43), ('구령대 앞', 20, 41),
            ('서쪽 우회로 북쪽', 10, 42), ('서쪽 우회로 중간', 10, 28),
            ('서쪽 우회로 남쪽', 10, 10), ('동쪽 우회로 북쪽', 58, 42),
            ('동쪽 우회로 중간', 58, 28), ('동쪽 우회로 남쪽', 58, 10),
            ('중앙 북쪽 분기', 34, 38), ('차폐 화단 A 동쪽', 38, 31),
            ('중앙 가로 연결', 34, 26), ('차폐 화단 B 서쪽', 30, 21),
            ('남쪽 가로길', 34, 11), ('교문 앞', 34, 5),
            ('서쪽 휴식 구역', 8, 20), ('급수대 앞', 61, 21),
            ('경비초소 앞', 55, 5), ('분리수거장 앞', 8, 35),
        ]:
            self.mark(label, x, y)
        self.map['explorationRoutes'] = [
            dict(id='yard-central', label='중앙 차폐 우회 동선', points=[
                dict(x=x, y=y) for x, y in [(34, 50), (34, 38),
                    (38, 34), (38, 28), (30, 26), (30, 18),
                    (34, 11), (34, 5)]]),
            dict(id='yard-west', label='서쪽 큰 우회 동선', points=[
                dict(x=x, y=y) for x, y in [(34, 50), (34, 43),
                    (10, 43), (10, 28), (10, 10), (34, 10), (34, 5)]]),
            dict(id='yard-east', label='동쪽 큰 우회 동선', points=[
                dict(x=x, y=y) for x, y in [(34, 50), (34, 43),
                    (58, 43), (58, 28), (58, 10), (34, 10), (34, 5)]]),
        ]
        return self.map


def connect_playground(floors):
    """Open only the planned main-building connection, preserving all rooms."""
    first = floors[0]
    first['barriers'] = [b for b in first['barriers']
                         if not (b['x'] == 24 and b['y'] == 1
                                 and b['label'] == '외부 · 출입 금지')]
    grid = [list(row) for row in first['rows']]
    grid[1][24] = '#'
    first['rows'] = [''.join(row) for row in grid]
    first['portals'] = [dict(id='main-to-playground', label='운동장으로 나가기',
                             doorX=24, doorY=1, facing='north',
                             targetId='playground', arrivalX=34, arrivalY=50)]
    return Schoolyard().build()
