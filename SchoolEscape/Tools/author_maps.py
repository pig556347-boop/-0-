"""Author only SchoolEscape data. No source or assets from sibling projects.

North is increasing grid y. Rectangles include their endpoints. Room footprints stay solid; each enterable room links to a separate larger map.
Reference: supplied DOCX Appendix A, images 1, 2 (candidate 1), and 4;
and the pinned MAP_BACKUP_2026-10-23.md in Docs/References.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
W, H = 49, 49

def room(label, x0, y0, x1, y1, dx, dy, facing="south"):
    return dict(label=label, x0=x0, y0=y0, x1=x1, y1=y1,
                doorX=dx, doorY=dy, facing=facing)

def floor(number, subtitle, walks, rooms, stairs, landmarks, barriers):
    g = [[False]*W for _ in range(H)]
    for x0,y0,x1,y1 in walks:
        for y in range(y0,y1+1):
            for x in range(x0,x1+1): g[y][x] = True
    for r in rooms:
        for y in range(r['y0'],r['y1']+1):
            for x in range(r['x0'],r['x1']+1): g[y][x] = False
    return dict(number=number, subtitle=subtitle, width=W, height=H,
                rows=[''.join('.' if c else '#' for c in row) for row in g],
                rooms=rooms, stairs=stairs, landmarks=landmarks, barriers=barriers)

def stair(id,x,y,target,arrivalX,arrivalY,label):
    return dict(id=id,x=x,y=y,target=target,arrivalX=arrivalX,arrivalY=arrivalY,label=label)

def mark(label,x,y):return dict(label=label,x=x,y=y)

f1 = floor(1,"현관 · 중앙홀 · 우회 복도",[
 (20,1,28,10),(17,7,31,16),(7,14,41,17),(7,14,10,29),
 (7,27,42,30),(39,14,42,30),(22,16,25,30),
 (29,17,32,24),(30,21,42,24),(3,19,10,23),
 (16,17,19,26),(3,14,10,17),(7,30,10,39)
],[
 room('빈교실 1',2,31,6,36,6,33,'east'),
 room('교직원 화장실 남',12,31,19,36,16,31),
 room('교직원 화장실 여',20,31,27,36,24,31),
 room('빈교실 3',28,31,47,45,32,31),
 room('빈교실 2',3,40,10,45,8,40),
 room('방송실',11,19,15,24,11,22,'west'),
 room('창고',26,19,28,25,26,22,'west'),
 room('행정실',33,25,38,26,35,25),
 room('대회의실',43,18,48,25,43,22,'west'),
 room('학생 화장실 남',3,7,6,13,5,13,'north'),
 room('학생 화장실 여',7,7,10,13,9,13,'north'),
 room('설비관리실',11,7,16,13,14,13,'north'),
 room('세미나실',33,7,38,13,35,13,'north'),
 room('보건실',39,7,46,13,41,13,'north')
],[stair('f1-up',26,29,2,23,5,'2층 ↑')],
 [mark('현관',24,3),mark('중앙홀',24,11),mark('서쪽 우회',9,22),mark('동쪽 우회',40,22),mark('북동 빈교실 앞',32,29),mark('복도 끝',4,21),mark('북서 복도',9,38)],
 [mark('외부 · 출입 금지',24,1)])

f2 = floor(2,"프리셋 후보 1 · 막힌 중앙",[
 (2,7,41,10),(7,7,10,31),(7,28,41,31),(38,7,41,31),
 (22,2,25,12),(22,28,25,36),(11,16,37,19),
 (18,9,21,29),(27,9,30,29),(2,12,10,15),(38,12,46,15),
 (22,10,25,13),(10,31,12,43),(33,31,35,43)
],[
 room('음악실',2,32,9,43,9,35,'east'),room('도서실',13,32,21,47,13,38,'west'),
 room('2학년 3반',27,32,32,39,32,35,'east'),room('2학년 4반',36,36,41,43,36,39,'west'),
 room('미술실',42,28,48,43,42,30,'west'),
 room('미술실 창고',42,23,47,26,42,24,'west'),
 room('빈교실 A',1,8,6,11,4,11,'north'),
 room('2학년 1반',1,24,6,31,6,27,'east'),
 room('2학년 2반',1,15,6,22,6,18,'east'),
 room('자습실',31,21,37,26,37,24,'east'),
 room('빈교실 B',31,11,37,15,35,15,'north'),
 room('빈교실 C',11,11,17,15,14,15,'north'),
 room('빈교실 D',11,21,17,26,17,24,'east'),
 room('학생 화장실 남',2,2,6,6,5,6,'north'),
 room('학생 화장실 여',7,2,11,6,9,6,'north'),
 room('컴퓨터실',12,2,17,6,15,6,'north'),
 room('준비실 1',18,2,21,6,20,6,'north'),
 room('자료실',27,2,33,6,30,6,'north'),
 room('창고',34,2,41,6,37,6,'north'),
 room('교직원 화장실 남',31,20,34,20,32,20,'south'),
 room('교직원 화장실 여',35,20,37,20,36,20,'south'),
 room('2학년 교무실',42,16,48,21,42,18,'west')
],[stair('f2-down',23,3,1,26,27,'1층 ↓'),stair('f2-up',24,35,3,24,5,'3층 ↑')],
 [mark('남쪽 복도',24,8),mark('서쪽 복도',8,23),mark('내부 우회',24,17),mark('북쪽 복도',24,29),mark('동쪽 복도',40,24),mark('음악실 앞',11,35),mark('대형 도서실 앞',11,38),mark('학년 분기 복도',34,40)],
 [mark('서관 · 출입 금지',2,13),mark('별관 · 출입 금지',46,13)])

f3 = floor(3,"중앙 분기 · 꺾이는 미로 통로",[
 (7,7,41,10),(7,7,10,31),(7,28,41,31),(38,7,41,31),
 (22,2,25,36),(10,18,38,21),
 (13,10,16,16),(13,13,19,16),(17,14,19,27),
 (11,24,20,27),(18,26,23,27),(27,10,30,14),(27,12,35,15),
 (32,13,35,26),(25,24,34,25),(28,24,30,29),
 (10,11,12,14),(36,23,40,26)
],[
 room('3학년 1반',4,32,14,37,9,32),
 room('학생 화장실 남',15,32,21,36,18,32),
 room('학생 화장실 여',26,32,32,36,29,32),
 room('3학년 4반',33,32,41,37,39,32),
 room('교직원 휴게실',11,22,17,23,14,23,'north'),
 room('3학년 교무실',11,11,16,12,11,12,'west'),
 room('학부모상담실',31,16,35,17,34,16),
 room('메인회의실',1,19,6,27,6,23,'east'),
 room('교장실',1,10,6,17,6,13,'east'),
 room('빈교실 1',42,26,48,32,42,29,'west'),
 room('중앙통제실',42,17,48,24,42,20,'west'),
 room('교직원 화장실 여',36,16,37,17,37,17,'north'),
 room('교직원 화장실 남',36,12,37,15,37,14,'east'),
 room('3학년 2반',4,1,13,6,9,6,'north'),
 room('빈교실 2',14,1,21,6,18,6,'north'),
 room('3학년 3반',26,1,35,6,30,6,'north'),
 room('빈교실 3',36,1,44,6,40,6,'north')
],[stair('f3-down',24,3,2,24,33,'2층 ↓')],
 [mark('남쪽 계단홀',24,8),mark('서쪽 분기',15,14),mark('중앙 교차로',24,20),mark('동쪽 굴절',34,14),mark('북서 막다른 길',12,26),mark('동쪽 우회',34,25),mark('북쪽 복도',24,30)],
 [mark('옥상 · 출입 금지',24,36)])

# Keep the candidate-1 main building intact and add real walking distance
# before the sealed wing entrances. This is a wider canvas, not scaled tiles.
def extend_second_floor_links(floors, offset=26):
    second=floors[1]
    old_width=second['width'];new_width=old_width+offset*2
    grid=[list('#'*offset+row+'#'*offset) for row in second['rows']]
    # Translate all authored main-building coordinates once. Portal return
    # coordinates are generated by populate() after this translation.
    for r in second['rooms']:
        for key in ['x0','x1','doorX']:r[key]+=offset
    for collection in ['stairs','landmarks']:
        for point in second[collection]:point['x']+=offset
    for base in floors:
        for transition in base['stairs']:
            if transition['target']==2:transition['arrivalX']+=offset
    # Four-cell corridors have a three-cell neck at their sealed far ends so
    # the existing 3.6m physical gate spans the full passable width.
    for y in range(12,16):
        for x in list(range(2,29))+list(range(72,99)):grid[y][x]='.'
    for x in list(range(2,6))+list(range(95,99)):grid[15][x]='#'
    # Extended corridor rectangles must never expose adjacent closed rooms.
    for r in second['rooms']:
        for y in range(r['y0'],r['y1']+1):
            for x in range(r['x0'],r['x1']+1):grid[y][x]='#'
    second['width']=new_width
    second['rows']=[''.join(row) for row in grid]
    second['barriers']=[mark('서관 연결 통로 끝 · 출입 금지',2,13),
                        mark('별관 연결 통로 끝 · 출입 금지',98,13)]
    second['landmarks'] += [mark('서관 연결 통로 중간',18,13),mark('서관 연결 통로 끝',3,13),
                            mark('별관 연결 통로 중간',82,13),mark('별관 연결 통로 끝',97,13)]
    second['connectionPassages']=[
        dict(id='west-wing-link',label='서관 연결 통로',entryX=33,entryY=13,middleX=18,middleY=13,endX=3,endY=13,minLengthMeters=30),
        dict(id='annex-link',label='별관 연결 통로',entryX=66,entryY=13,middleX=82,middleY=13,endX=97,endY=13,minLengthMeters=30)]

extend_second_floor_links([f1,f2,f3])

def reshape_southwest_corridor(second, offset=26):
    """Follow the user's sketch: classroom left of a broad L-shaped lane.

    The former A footprint projected into the south corridor and left only
    one cell in front of the boys' restroom. Recess that room to the west,
    put its door on the east face and keep a four-cell passage around it.
    All other room footprints, stairs and the candidate-1 core stay fixed.
    """
    grid=[list(row) for row in second['rows']]
    classroom=next(r for r in second['rooms'] if r['label']=='빈교실 A')
    # Restore the old projection to circulation before carving the new room.
    for y in range(classroom['y0'],classroom['y1']+1):
        for x in range(classroom['x0'],classroom['x1']+1):grid[y][x]='.'
    classroom.update(x0=offset-2,x1=offset+3,y0=8,y1=11,
                     doorX=offset+3,doorY=9,facing='east')
    neck=(offset+4,7,offset+7,14)
    horizontal=(offset+4,7,offset+17,10)
    for x0,y0,x1,y1 in [neck,horizontal]:
        for y in range(y0,y1+1):
            for x in range(x0,x1+1):grid[y][x]='.'
    # Keep neighboring rooms closed, including the classroom above the join.
    for r in second['rooms']:
        for y in range(r['y0'],r['y1']+1):
            for x in range(r['x0'],r['x1']+1):grid[y][x]='#'
    second['rows']=[''.join(row) for row in grid]
    second['landmarks'] += [mark('빈교실 A 앞 꺾임',offset+5,10),
                            mark('학생 화장실 앞 복도',offset+9,8)]
    second['corridorCorrections']=[dict(
        id='southwest-l-corridor',label='빈교실 A · 학생 화장실 앞 꺾인 복도',
        widthCells=4,vertical=list(neck),horizontal=list(horizontal),
        turnX=offset+5,turnY=9,
        classroomDoorX=classroom['doorX'],classroomDoorY=classroom['doorY'],
        maleApproachX=offset+5,maleApproachY=7,
        femaleApproachX=offset+9,femaleApproachY=7)]

reshape_southwest_corridor(f2)

from author_interiors import populate
room_maps=populate([f1,f2,f3])
from outdoor_map import connect_playground
playground=connect_playground([f1,f2,f3])

data=dict(title='폐교탈출',schemaVersion=4,revision='guidebook-difficulty-2026-10-26',tileSize=1.25,walkSpeed=2.6,runSpeed=4.7,
          stairEntryHalfWidth=.8,stairEntryHalfDepth=.65,stairExitRadius=3.2,stairCooldown=.75,
          playerRadius=0.27,spawnFloor=1,spawnX=24,spawnY=5,floors=[f1,f2,f3],roomMaps=room_maps,
          outdoorMaps=[playground],escapeGate=dict(spaceId='playground',x=34,y=3,
          label='운동장 교문',range=1.7))
dest=ROOT/'Assets/Resources/SchoolMap.json'
dest.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
print('Wrote',dest)
