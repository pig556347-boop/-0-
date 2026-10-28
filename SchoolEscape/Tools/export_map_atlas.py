#!/usr/bin/env python3
"""Export SchoolEscape's authored runtime map as standalone SVG diagrams.

No browser, image generation, publication, or runtime mutation is performed.
PNG companions can be rendered from the SVGs in a separate review step.
"""
import argparse
import hashlib
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BG, FG, MUTED = '#101e27', '#e5e9dd', '#b2c1b9'
ROOM, WALK, DOOR, GOLD, BLOCK = '#304b55', '#718879', '#91c8c3', '#e4c572', '#d68c7e'
ROUTE_COLORS = ['#f1cd76', '#80cccd', '#d5a5db']


def esc(value):
    return html.escape(str(value), quote=True)


class SVG:
    def __init__(self, width, height, title):
        self.width, self.height = width, height
        self.parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img"><title>{esc(title)}</title>',
                      '<style>text{font-family:"Noto Sans CJK KR","Noto Sans KR",sans-serif}</style>']
        self.rect(0, 0, width, height, BG)

    def rect(self, x, y, w, h, fill, stroke='none', radius=0, extra=''):
        self.parts.append(f'<rect x="{x:g}" y="{y:g}" width="{w:g}" height="{h:g}" fill="{fill}" stroke="{stroke}" rx="{radius:g}" {extra}/>')

    def text(self, x, y, value, size=16, color=FG, anchor='start', bold=False):
        self.parts.append(f'<text x="{x:g}" y="{y:g}" font-size="{size}" fill="{color}" text-anchor="{anchor}" font-weight="{"bold" if bold else "normal"}">{esc(value)}</text>')

    def line(self, points, color, width=3, dash=''):
        points = ' '.join(f'{x:g},{y:g}' for x, y in points)
        self.parts.append(f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round" stroke-dasharray="{dash}"/>')

    def marker(self, x, y, label, fill=GOLD, size=11):
        self.parts.append(f'<circle cx="{x:g}" cy="{y:g}" r="{size}" fill="{fill}" stroke="{BG}" stroke-width="1.5"/>')
        self.text(x, y+4, label, 11, BG, 'middle', True)

    def save(self, path):
        path.write_text('\n'.join(self.parts + ['</svg>'])+'\n', encoding='utf-8')
        return {'file': path.name, 'width': self.width, 'height': self.height}


def map_base(svg, space, left, top, scale, fill=WALK):
    svg.rect(left, top, space['width']*scale, space['height']*scale, '#14232d', '#42575e')
    for y, row in enumerate(space['rows']):
        start = None
        for x, cell in enumerate(row+'#'):
            if cell == '.' and start is None:
                start = x
            elif cell != '.' and start is not None:
                svg.rect(left+start*scale, top+(space['height']-1-y)*scale, (x-start)*scale, scale, fill)
                start = None
    return lambda x, y: (left+(x+.5)*scale, top+(space['height']-.5-y)*scale)


def floor_diagram(data, floor, output):
    scale, left, top = 16, 34, 126
    map_width, map_height = floor['width']*scale, floor['height']*scale
    width, height = map_width+420, max(map_height+264, len(floor['rooms'])*31+200)
    svg = SVG(width, height, f"폐교탈출 본관 {floor['number']}층")
    svg.text(34, 44, f"폐교탈출 · 본관 {floor['number']}층 평면도", 29, bold=True)
    svg.text(34, 75, f"기본 배치 예시 · 방 {len(floor['rooms'])}개 · 북쪽 ↑", 17, MUTED)
    svg.text(34, 103, '방 번호는 오른쪽 목록과 대응 · 실내는 문으로 연결된 독립 공간', 14, MUTED)
    point = map_base(svg, floor, left, top, scale)
    for index, room in enumerate(floor['rooms'], 1):
        x = left+room['x0']*scale
        y = top+(floor['height']-1-room['y1'])*scale
        w, h = (room['x1']-room['x0']+1)*scale, (room['y1']-room['y0']+1)*scale
        svg.parts.append(f'<a href="../RoomPlans/{esc(room["interiorId"])}.svg"><title>{esc(room["label"])}</title>')
        svg.rect(x, y, w, h, ROOM, '#7a979a')
        svg.text(x+w/2, y+h/2+5, index, 18, GOLD, 'middle', True)
        if w >= 125 and h >= 55:
            svg.text(x+w/2, y+h/2+27, room['label'], 13, FG, 'middle')
        dx, dy = point(room['doorX'], room['doorY'])
        svg.rect(dx-6, dy-6, 12, 12, DOOR)
        sx, sy = map_width+70, top+index*31-9
        svg.rect(sx, sy-20, 29, 25, ROOM, '#6b9192', 5)
        svg.text(sx+14.5, sy-2, index, 14, GOLD, 'middle', True)
        svg.text(sx+42, sy-2, room['label'], 17)
        svg.parts.append('</a>')
    for stair in floor['stairs']:
        x, y = point(stair['x'], stair['y'])
        svg.rect(x-13, y-10, 26, 20, GOLD, '#f8e9b3', 4)
        svg.text(x, y+4, f"{stair['target']}F", 12, BG, 'middle', True)
    for barrier in floor['barriers']:
        x, y = point(barrier['x'], barrier['y'])
        svg.line([(x-8,y-8),(x+8,y+8)], BLOCK, 4)
        svg.line([(x+8,y-8),(x-8,y+8)], BLOCK, 4)
    for portal in floor.get('portals', []):
        x, y = point(portal['doorX'], portal['doorY'])
        svg.rect(x-10,y-5,20,10,DOOR,BG)
        svg.line([(x,y-9),(x,y-37),(x+23,y-37)],DOOR,2)
        svg.text(x+28,y-32,portal['label']+' · E',14,DOOR)
    base = top+map_height+35
    for x, color, label in [(34,WALK,'이동 가능한 복도'),(240,ROOM,'방 위치 / 번호'),(470,DOOR,'닫힌 E 문'),(650,GOLD,'자동 계단'),(810,BLOCK,'진입 봉쇄')]:
        svg.rect(x,base-12,16,16,color)
        svg.text(x+25,base+1,label,14,MUTED)
    svg.text(34, base+38, '방 외곽은 복도에서의 위치입니다. 문을 열면 별도로 확대한 실내 맵으로 이동합니다.', 15, MUTED)
    svg.text(34, base+67, '문은 기본 닫힘 · E로 출입 · 계단에 걸어 들어가면 자동 층 이동 · 탈출은 운동장 남쪽 교문', 15, MUTED)
    return svg.save(output/f"floor-{floor['number']}.svg")


def overview(data, output):
    svg=SVG(1200,1460,'폐교탈출 4층 확장 전개도')
    svg.text(45,50,'폐교탈출 · 본관 4층 확장',32,bold=True)
    svg.text(45,86,'본관 67개 방 + 옥상 / 서관 14 / 별관 18 / 운동장·산책로·교문 경비실',18,MUTED)
    desc={4:'3학년 1~6반 · 3학년 교무실 · 컴퓨터실 · 미술실',3:'2학년 1~6반 · 2학년 교무실 · 과학실·음악실·도서실',2:'1학년 1~6반 · 교무실 · 학생회실 · 기술가정실 · 화장실',1:'현관 · 행정실 · 보건실 · 방송실 · 상담실 · 빈교실 2개'}
    for n in range(4,0,-1):
        y=230+(4-n)*215
        svg.rect(250,y,700,145,ROOM,DOOR,8)
        svg.text(280,y+42,f'본관 {n}층',27,bold=True)
        svg.text(280,y+85,desc[n],17)
        if n>1:svg.line([(600,y+145),(600,y+215)],GOLD,4)
    svg.rect(420,115,360,65,ROOM,GOLD,8);svg.text(600,156,'옥상 · 4층 계단 + 카드키',21,GOLD,'middle')
    svg.line([(600,180),(600,230)],GOLD,4)
    for x,label in [(25,'서관'),(980,'별관')]:
        svg.rect(x,680,195,90,ROOM,DOOR,8);svg.text(x+98,718,label,25,FG,'middle');svg.text(x+98,746,'2층 연결 통로 유지',14,DOOR,'middle')
        svg.line([(220 if x<250 else 950,725),(250 if x<250 else 980,725)],DOOR,4)
    svg.line([(600,1020),(600,1080)],DOOR,4)
    svg.rect(250,1080,700,145,ROOM,DOOR,8);svg.text(600,1120,'운동장 · 기존 크기 유지',26,FG,'middle')
    svg.text(600,1156,'동쪽: 산책로 순환 동선 / 남서쪽: 교문 경비실',18,FG,'middle')
    svg.text(600,1190,'북쪽 현관 E 왕복 · 남쪽 교문 A+B+C 탈출',18,GOLD,'middle')
    svg.text(45,1300,'실내는 E 문으로 개별 전환 / 계단은 걸어 들어가 자동 이동',20,MUTED)
    svg.text(45,1340,'층 카드키 순서: 1층 → 2층 → 3층 → 4층 → 옥상',20,MUTED)
    svg.text(45,1380,'각 학년 6반 · 교무실 10석(담임 6 + 과목교사 4) · 가구 크기는 유지',20,MUTED)
    return svg.save(output/'overview.svg')


def yard_diagram(data, yard, output):
    scale, left, top = 16, 34, 140
    width, height = yard['width']*scale+450, yard['height']*scale+300
    svg = SVG(width,height,'폐교탈출 실제 운동장 평면도')
    svg.text(34,44,'폐교탈출 · 운동장 평면도',29,bold=True)
    svg.text(34,76,f"실행 지도 기준 · 약 {yard['approximateWidthMeters']:g} × {yard['approximateDepthMeters']:g}m · 북쪽 ↑",17,MUTED)
    svg.text(34,106,'북쪽 현관은 본관과 E로 왕복 · 남쪽 교문에서 A·B·C 열쇠를 사용해 탈출',16,DOOR)
    point = map_base(svg,yard,left,top,scale,'#66775b')
    for fixture in sorted(yard['fixtures'],key=lambda f:f.get('elevation',0)):
        x,y = point(fixture['x'],fixture['y'])
        w,h = fixture['width']/data['tileSize']*scale,fixture['depth']/data['tileSize']*scale
        solid = fixture.get('solid',False)
        svg.parts.append(f'<g><title>{esc(fixture.get("label") or fixture["kind"])} · {"충돌" if solid else "장식"}</title>')
        svg.rect(x-w/2,y-h/2,w,h,fixture.get('color','#74806c'),'#263e35' if solid else 'none',extra=f'opacity="{.95 if solid else .40}"')
        svg.parts.append('</g>')
    for index,route in enumerate(yard['explorationRoutes']):
        points=[point(p['x'],p['y']) for p in route['points']]
        svg.line(points,BG,7)
        svg.line(points,ROUTE_COLORS[index],3.5,'8 4')
    sidebar=left+yard['width']*scale+28
    svg.text(sidebar,top+10,'주요 지점',21,bold=True)
    for index,landmark in enumerate(yard['landmarks'],1):
        x,y=point(landmark['x'],landmark['y'])
        svg.marker(x,y,index,FG,9)
        svg.marker(sidebar+10,top+31+index*26,index,FG,9)
        svg.text(sidebar+28,top+36+index*26,landmark['label'],14)
    ylegend=top+31+(len(yard['landmarks'])+1)*26+18
    svg.text(sidebar,ylegend,'탐색 동선',21,bold=True)
    for index,route in enumerate(yard['explorationRoutes']):
        y=ylegend+34+index*31
        svg.line([(sidebar,y-5),(sidebar+25,y-5)],ROUTE_COLORS[index],4)
        svg.text(sidebar+35,y,route['label'],14)
    for portal in yard.get('portals',[]):
        x,y=point(portal['doorX'],portal['doorY'])
        svg.rect(x-14,y-5,28,10,DOOR,BG)
        svg.text(x,y-18,'본관 1층 ↔ E 출입',16,DOOR,'middle',True)
    gate=data['escapeGate'];x,y=point(gate['x'],gate['y'])
    svg.marker(x,y,'E',GOLD,14)
    svg.rect(x-150,y+22,300,31,BG,GOLD,4)
    svg.text(x,y+43,'남쪽 교문 · A + B + C 탈출',16,GOLD,'middle',True)
    base=top+yard['height']*scale+43
    svg.text(34,base,'실제 시설물 위치와 크기 표시 · 번호: 주요 지점 · 색 점선: 중앙 / 서쪽 / 동쪽 탐색 동선',16,MUTED)
    svg.text(34,base+32,'진한 시설물은 충돌 대상입니다. 가구를 조사하고 바닥을 살펴 무작위 배치된 열쇠를 찾으세요.',16,MUTED)
    svg.text(34,base+64,'운동장도 당직자의 시야·소음 감지와 추격이 이어집니다. 화단과 우회로를 이용해 시야를 끊을 수 있습니다.',16,MUTED)
    return svg.save(output/'playground.svg')


def annex_diagram(data,output):
    svg=SVG(1400,1000,'별관 · 급식실과 통층 체육관 전개도')
    svg.text(40,48,'별관 · 급식실 / 2~3층 통층 체육관',30,bold=True)
    svg.text(40,83,'사용자 스케치 기반 · 각 방은 E로 이동하는 독립 공간 · 계단은 자동 왕복',17,MUTED)
    svg.rect(360,122,650,220,ROOM,DOOR,8)
    svg.text(685,175,'체육관 · 2~3층 높이를 통으로 사용',25,FG,'middle',True)
    svg.text(685,215,'코트 / 농구대 / 무대 / 관람 벤치 / 높은 창문',18,FG,'middle')
    svg.text(685,255,'중앙은 통층 · 가장자리 3층 관람통로 / 조사실 3개',18,GOLD,'middle')
    svg.line([(685,342),(685,385)],DOOR)
    svg.rect(310,385,800,75,WALK,DOOR,8)
    svg.text(710,431,'별관 2층 공용 복도',23,BG,'middle',True)
    svg.rect(25,385,245,75,ROOM,DOOR,8)
    svg.text(147,417,'본관 2층 동쪽',18,FG,'middle')
    svg.text(147,443,'긴 연결통로 → E',17,DOOR,'middle')
    svg.line([(270,423),(310,423)],DOOR)
    labels=['학생 화장실 남','학생 화장실 여','체력단련실','체육용품 창고','작은 제어실','학생휴게실','체육 이론 교실','체조·무용 실습실']
    for i,label in enumerate(labels):
        x=30+(i%4)*345;y=510+(i//4)*110
        svg.rect(x,y,310,78,ROOM,DOOR,6);svg.text(x+155,y+46,label,20,FG,'middle',True)
    svg.text(700,735,'3층: 체육 기록실 / 경기 영상분석실 / 스포츠 동아리실 · 무용실 → 탈의실',18,MUTED,'middle')
    svg.line([(1160,425),(1320,425),(1320,812)],GOLD,4)
    svg.text(1215,780,'↕ 자동 계단',18,GOLD,'middle')
    svg.rect(340,812,670,125,ROOM,GOLD,8)
    svg.text(675,859,'별관 1층 · 급식실',24,FG,'middle',True)
    svg.text(675,898,'식탁 12개 / 배식대 / 우회 통로',18,MUTED,'middle')
    svg.rect(1040,840,300,90,ROOM,DOOR,8);svg.text(1190,892,'급식실 창고 · E',21,FG,'middle')
    svg.line([(1010,885),(1040,885)],DOOR)
    return svg.save(output/'annex.svg')


def west_diagram(data,output):
    svg=SVG(1200,1000,'서관 전개도')
    svg.text(40,50,'서관 · 저장된 기획 기반 확장',30,bold=True)
    svg.text(40,88,'본관2층 서쪽 연결통로에서 E 입장 · 각 층 자동 계단 왕복',18,MUTED)
    for i,(floor,description) in enumerate([(3,'통제구역 기록실 / 감시실 / 서버실'),(2,'경비원 기숙사실 / 탕비실 / 화장실 / 통제실'),(1,'직원급식실 / 준비조리실 / 냉동창고 / 매점')]):
        y=145+i*255;svg.rect(185,y,850,185,ROOM,DOOR,8)
        svg.text(220,y+48,f'서관 {floor}층',28,FG,bold=True);svg.text(220,y+100,description,22,FG)
        if i<2:svg.line([(610,y+185),(610,y+250)],GOLD,4)
    svg.text(45,970,'외부 출입문은 잠김 · 조리실 추격 장면은 별관 급식실 옆 조리실에 배치',19,GOLD)
    return svg.save(output/'west.svg')

def kitchen_diagram(data,output):
    f=next(r for r in data['roomMaps'] if r.get('kitchenSequence'));svg=SVG(1200,1090,'조리실 추격 동선')
    svg.text(35,48,'조리실 · 탐색 → 붕괴 → 추격 → 파괴 흔적 유지',27,bold=True)
    svg.text(35,83,'노란 선: 붕괴 후 한 칸 폭 동선 · 괴물이 지나간 주변 잔해만 파괴됩니다.',17,MUTED)
    point=map_base(svg,f,60,120,20,fill='#344b4a')
    for o in f['fixtures']:
        if o.get('solid'):
            x,y=point(o['x'],o['y']);w=o['width']/data['tileSize']*20;h=o['depth']/data['tileSize']*20;svg.rect(x-w/2,y-h/2,w,h,'#94a294')
    bx,by=point(3,22);svg.marker(bx,by,'箱',GOLD,11);svg.text(bx+20,by-18,'먼저 조사할 보급 상자',16,GOLD)
    route=[point(x,y) for x,y in f['kitchenSequence']['path']];svg.line(route,GOLD,5)
    for p in f['portals']:
        x,y=point(p['doorX'],p['doorY']);svg.marker(x,y,'E',DOOR,11);svg.text(x+20,y-12,'북쪽: 상자 조사 후 귀환 시 붕괴' if p['id']=='kitchen-entry' else '서쪽: 비상 탈출',16,DOOR)
    svg.text(40,945,'길 끝 상자를 조사하고 북쪽 출입문으로 돌아올 때 붕괴합니다. 추격 중 스태미나 무제한.',18,FG)
    svg.text(40,984,'배식장: 넓은 체형 / 솥뚜껑 같은 얼굴 / 앞치마 / 거대한 팔과 배식 도구',18,FG)
    svg.text(40,1023,'같은 판에서 재방문: 실제로 부순 길만 유지 · 새 판: 원래 탐색 상태로 복구',18,GOLD)
    return svg.save(output/'kitchen-route.svg')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--map',type=Path,default=ROOT/'Assets/Resources/SchoolMap.json')
    parser.add_argument('--output',type=Path,default=ROOT/'Docs/MapAtlas')
    args=parser.parse_args()
    raw=args.map.read_bytes();data=json.loads(raw);output=args.output
    output.mkdir(parents=True,exist_ok=True)
    yard=next(f for f in data['outdoorMaps'] if f['id']=='playground')
    rooms=[(f,r) for f in data['floors'] for r in f['rooms']]
    diagrams=[overview(data,output)]+[floor_diagram(data,f,output) for f in data['floors']]+[yard_diagram(data,yard,output),annex_diagram(data,output),west_diagram(data,output),kitchen_diagram(data,output)]
    entries=[{'file':'overview.svg','label':'전체 맵 전개도'}]
    entries += [{'file':f"floor-{f['number']}.svg",'label':f"본관 {f['number']}층 · {len(f['rooms'])}개 방"} for f in data['floors']]
    entries += [{'file':'playground.svg','label':'운동장 · 실제 시설과 탐색 동선'}]
    entries += [{'file':f"../RoomPlans/{r['interiorId']}.svg",'label':f"{f['number']}층 / {r['label']} · 실내 배치"} for f,r in rooms]
    entries += [{'file':'annex.svg','label':'별관 전체 전개도 · 1층 급식실 / 통층 체육관'}]
    entries += [{'file':f"../RoomPlans/{r['id']}.svg",'label':f"별관 {r['sourceFloor']}층 / {r['label']}"} for r in data['roomMaps'] if r.get('building')=='annex']
    entries += [{'file':'west.svg','label':'서관 전체 전개도'},{'file':'kitchen-route.svg','label':'조리실 상자 조사·귀환 추격 동선'}]
    entries += [{'file':f"../RoomPlans/{r['id']}.svg",'label':f"서관 {r['sourceFloor']}층 / {r['label']}"} for r in data['roomMaps'] if r.get('building')=='west']
    entries += [{'file':'../RoomPlans/main-rooftop.svg','label':'본관 옥상 · 카드키 설비 구역'}]
    entries += [{'file':'../RoomPlans/gatehouse.svg','label':'교문 경비실'},{'file':'school-walkway.svg','label':'학교 뒤 산책로'}]
    (output/'school-walkway.svg').write_text((ROOT/'Docs/school-walkway.svg').read_text())
    atlas={'gameRevision':data['revision'],'gameVersion':'0.15','mapSha256':hashlib.sha256(raw).hexdigest(),'roomCount':len(data['roomMaps']),'annexSpaceCount':18,'westSpaceCount':14,'playgroundImplemented':True,'escapeGate':data['escapeGate'],'floors':[{'floor':f['number'],'rooms':len(f['rooms'])} for f in data['floors']],'diagrams':diagrams,'entries':entries}
    (output/'atlas.json').write_text(json.dumps(atlas,ensure_ascii=False,indent=2)+'\n')
    viewer='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>폐교탈출 맵 전개도</title><style>body{margin:0;background:#101e27;color:#e5e9dd;font-family:system-ui,sans-serif}header{padding:24px;max-width:1200px;margin:auto}h1{margin:0 0 12px}p{color:#b2c1b9;line-height:1.65}select,a{font:inherit}select{max-width:100%;padding:12px;background:#304b55;color:#fff;border:1px solid #89a8a5;border-radius:6px}a{color:#e4c572;margin-left:15px}main{padding:0 14px 24px;text-align:center}img{display:block;max-width:100%;height:auto;margin:auto;background:#101e27;border:1px solid #42575e}</style><header><h1>폐교탈출 맵 전개도 · 0.15</h1><p>본관 1~4층·별관·서관·운동장의 전개도입니다. 별관은 2층 동쪽 연결통로에서 E로 출입합니다. 교실·교무실 위치는 매판 같은 층 안에서 섞이며 플레이 중 안내도와 가이드북이 현재 배치를 반영합니다. 방 외곽은 복도에서의 위치이며, 실제 방은 독립된 확대 공간입니다.<br>1층 현관에서 E로 운동장을 왕복합니다. 본관과 운동장에 나뉜 A·B·C 열쇠를 모아 운동장 남쪽 교문에서 탈출하세요.</p><select id="plan" aria-label="보고 싶은 지도"></select><a id="original" target="_blank" rel="noopener">벡터 원본 열기</a></header><main><img id="map" alt="현재 선택한 맵 전개도"></main><script>const plans=ENTRIES,s=document.getElementById('plan'),m=document.getElementById('map'),a=document.getElementById('original');plans.forEach((p,i)=>s.add(new Option(p.label,i)));function show(){const p=plans[+s.value];m.src=p.file;m.alt=p.label;a.href=p.file;document.title=p.label+' | 폐교탈출'}s.onchange=show;show();</script></html>'''
    (output/'index.html').write_text(viewer.replace('ENTRIES',json.dumps(entries,ensure_ascii=False).replace('<','\\u003c'))+'\n')
    (output/'README.md').write_text(f'''# 폐교탈출 맵 전개도

현재 0.14 `{data['revision']}` 실행 지도에서 생성한 전개도입니다.

- `overview.svg`: 본관 3개 층, 운동장 E 연결, 남쪽 탈출 교문과 봉쇄부.
- `floor-1.svg` ~ `floor-4.svg`: 실제 복도 격자·방 위치·닫힌 문·자동 계단과 방 목록.
- `playground.svg`: 실제 운동장 시설물, 북쪽 본관 출입구, 남쪽 탈출 교문, 주요 지점과 3개 탐색 동선.
- `index.html`: 전체도·층별도·운동장·본관53개·별관18개·서관13개·옥상1개 공간의 배치를 선택하는 93개 보기.

본관 1층 현관과 운동장 북쪽 입구는 E로 왕복합니다. A·B·C 열쇠는 본관과 운동장에 나뉘어 배치되며, 탈출은 운동장 남쪽 교문에서만 가능합니다. 별관은 본관2층 동쪽 연결통로로 입장합니다. 별관1층은 급식실,2~3층은 통층 체육관과 주변 시설입니다. 서관은 본관2층 서쪽 통로로 연결됩니다. 서관·별관 외부문은 봉쇄하며 옥상은 본관4층 계단과 카드키로 연결됩니다.

방 외곽은 복도에서의 위치이며 실제 실내 크기와 다릅니다. 53개 실내 링크는 기존 `Docs/RoomPlans`를 참조합니다. 뷰어는 최신 SVG 원본을 직접 엽니다. PNG는 별도 렌더링한 공유용 사본입니다.

재생성: `python SchoolEscape/Tools/export_map_atlas.py`. `--map`과 `--output`으로 입력 지도와 출력 폴더를 지정할 수 있습니다. 지도 변경 후 다시 생성하면 시설물·출입구·동선·방 목록도 함께 갱신됩니다. `atlas.json`에 원본 지도 SHA-256과 각 SVG 크기를 기록합니다.

`review.json`은 과거 Chromium 로드 기록입니다. 이 생성기는 브라우저 검증이나 공개 링크 갱신을 수행하지 않습니다.
''',encoding='utf-8')
    print(json.dumps({'output':str(output),'mapRevision':data['revision'],'views':len(entries),'rooms':len(rooms),'diagrams':diagrams},ensure_ascii=False,indent=2))


if __name__=='__main__':
    main()
