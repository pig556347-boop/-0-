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
        svg.text(x+28,y-32,'현관 E ↔ 운동장 북쪽 입구',14,DOOR)
    base = top+map_height+35
    for x, color, label in [(34,WALK,'이동 가능한 복도'),(240,ROOM,'방 위치 / 번호'),(470,DOOR,'닫힌 E 문'),(650,GOLD,'자동 계단'),(810,BLOCK,'진입 봉쇄')]:
        svg.rect(x,base-12,16,16,color)
        svg.text(x+25,base+1,label,14,MUTED)
    svg.text(34, base+38, '방 외곽은 복도에서의 위치입니다. 문을 열면 별도로 확대한 실내 맵으로 이동합니다.', 15, MUTED)
    svg.text(34, base+67, '문은 기본 닫힘 · E로 출입 · 계단에 걸어 들어가면 자동 층 이동 · 탈출은 운동장 남쪽 교문', 15, MUTED)
    return svg.save(output/f"floor-{floor['number']}.svg")


def overview(data, output):
    svg = SVG(1200, 1195, '폐교탈출 전체 맵 전개도')
    svg.text(46,49,'폐교탈출 · 전체 맵 전개도',32,bold=True)
    svg.text(46,82,'본관 53개 독립 실내 + 운동장 · 현재 실행 지도 연결 구조',17,MUTED)
    descriptions = {
        3:['3학년 교실·빈교실 / 교무실·휴게실·상담실', '메인회의실·교장실·중앙통제실 / 화장실'],
        2:['교실·음악실·도서실·미술실·미술실 창고', '컴퓨터실·준비실·자료실·창고·교무실 / 화장실'],
        1:['현관·중앙홀 / 빈교실·행정실·보건실·방송실', '창고·대회의실·설비관리실·세미나실 / 화장실']}
    for index, floor in enumerate(reversed(data['floors'])):
        y = 125+index*225
        svg.rect(255,y,690,165,ROOM,'#89a8a5',10)
        svg.text(287,y+40,f"본관 {floor['number']}층 · {len(floor['rooms'])}개 방",25,bold=True)
        for row, text in enumerate(descriptions[floor['number']]):
            svg.text(287,y+78+row*30,text,17)
        if index < 2:
            svg.line([(600,y+173),(600,y+211)],GOLD)
            svg.text(629,y+198,'↕ 계단으로 왕복 · 자동 이동',16,GOLD)
    for x,label in [(30,'서관'),(980,'별관')]:
        svg.rect(x,396,190,89,'#2c3035',BLOCK,8)
        svg.text(x+95,430,label,22,FG,'middle',True)
        svg.text(x+95,459,'연결 통로 끝 봉쇄',14,BLOCK,'middle')
        svg.line([(x+190 if x<255 else 945,440),(255 if x<255 else x,440)],BLOCK,3,'7 5')
    svg.rect(980,155,190,83,'#2c3035',BLOCK,8)
    svg.text(1075,187,'옥상',21,FG,'middle',True)
    svg.text(1075,215,'연결부 봉쇄',14,BLOCK,'middle')
    svg.line([(945,197),(980,197)],BLOCK,3,'7 5')
    svg.line([(600,750),(600,802)],DOOR)
    svg.text(629,780,'↕ 현관 E 출입 · 양방향 연결',16,DOOR)
    yard = data['outdoorMaps'][0]
    svg.rect(255,815,690,185,'#343f34','#94b18d',10)
    svg.text(287,856,'운동장 · 독립된 실제 야외 공간',25,GOLD,bold=True)
    svg.text(287,892,f"약 {yard['approximateWidthMeters']:g} × {yard['approximateDepthMeters']:g}m · 중앙 길 + 좌우 우회로",17)
    svg.text(287,924,'북쪽: 본관 현관 왕복 · 남쪽: 유일한 탈출 교문',17)
    svg.text(287,956,'A·B·C 열쇠는 본관과 운동장에 나누어 배치',17)
    svg.line([(600,1007),(600,1035)],GOLD)
    svg.rect(380,1045,440,58,'#514631',GOLD,8)
    svg.text(600,1081,'남쪽 교문 · A + B + C로 탈출',21,GOLD,'middle',True)
    svg.text(46,1140,'실선: 현재 연결 · 점선: 봉쇄 연결 · 모든 방과 현관은 기본 닫힌 E 문',16,MUTED)
    svg.text(46,1171,'운동장 출입은 탈출이 아닙니다. 세 열쇠를 모아 남쪽 교문에서 E를 누르세요.',16,MUTED)
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


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--map',type=Path,default=ROOT/'Assets/Resources/SchoolMap.json')
    parser.add_argument('--output',type=Path,default=ROOT/'Docs/MapAtlas')
    args=parser.parse_args()
    raw=args.map.read_bytes();data=json.loads(raw);output=args.output
    output.mkdir(parents=True,exist_ok=True)
    yard=next(f for f in data['outdoorMaps'] if f['id']=='playground')
    rooms=[(f,r) for f in data['floors'] for r in f['rooms']]
    diagrams=[overview(data,output)]+[floor_diagram(data,f,output) for f in data['floors']]+[yard_diagram(data,yard,output)]
    entries=[{'file':'overview.svg','label':'전체 맵 전개도'}]
    entries += [{'file':f"floor-{f['number']}.svg",'label':f"본관 {f['number']}층 · {len(f['rooms'])}개 방"} for f in data['floors']]
    entries += [{'file':'playground.svg','label':'운동장 · 실제 시설과 탐색 동선'}]
    entries += [{'file':f"../RoomPlans/{r['interiorId']}.svg",'label':f"{f['number']}층 / {r['label']} · 실내 배치"} for f,r in rooms]
    atlas={'gameRevision':data['revision'],'gameVersion':'0.11','mapSha256':hashlib.sha256(raw).hexdigest(),'roomCount':len(rooms),'playgroundImplemented':True,'escapeGate':data['escapeGate'],'floors':[{'floor':f['number'],'rooms':len(f['rooms'])} for f in data['floors']],'diagrams':diagrams,'entries':entries}
    (output/'atlas.json').write_text(json.dumps(atlas,ensure_ascii=False,indent=2)+'\n')
    viewer='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>폐교탈출 맵 전개도</title><style>body{margin:0;background:#101e27;color:#e5e9dd;font-family:system-ui,sans-serif}header{padding:24px;max-width:1200px;margin:auto}h1{margin:0 0 12px}p{color:#b2c1b9;line-height:1.65}select,a{font:inherit}select{max-width:100%;padding:12px;background:#304b55;color:#fff;border:1px solid #89a8a5;border-radius:6px}a{color:#e4c572;margin-left:15px}main{padding:0 14px 24px;text-align:center}img{display:block;max-width:100%;height:auto;margin:auto;background:#101e27;border:1px solid #42575e}</style><header><h1>폐교탈출 맵 전개도 · 0.11</h1><p>본관 1~3층의 기본 배치 예시와 실제 운동장입니다. 교실·교무실 위치는 매판 같은 층 안에서 섞이며 플레이 중 안내도와 가이드북이 현재 배치를 반영합니다. 방 외곽은 복도에서의 위치이며, 실제 방은 독립된 확대 공간입니다.<br>1층 현관에서 E로 운동장을 왕복합니다. 본관과 운동장에 나뉜 A·B·C 열쇠를 모아 운동장 남쪽 교문에서 탈출하세요.</p><select id="plan" aria-label="보고 싶은 지도"></select><a id="original" target="_blank" rel="noopener">벡터 원본 열기</a></header><main><img id="map" alt="현재 선택한 맵 전개도"></main><script>const plans=ENTRIES,s=document.getElementById('plan'),m=document.getElementById('map'),a=document.getElementById('original');plans.forEach((p,i)=>s.add(new Option(p.label,i)));function show(){const p=plans[+s.value];m.src=p.file;m.alt=p.label;a.href=p.file;document.title=p.label+' | 폐교탈출'}s.onchange=show;show();</script></html>'''
    (output/'index.html').write_text(viewer.replace('ENTRIES',json.dumps(entries,ensure_ascii=False).replace('<','\\u003c'))+'\n')
    (output/'README.md').write_text(f'''# 폐교탈출 맵 전개도

현재 0.11 `{data['revision']}` 실행 지도에서 생성한 전개도입니다.

- `overview.svg`: 본관 3개 층, 운동장 E 연결, 남쪽 탈출 교문과 봉쇄부.
- `floor-1.svg` ~ `floor-3.svg`: 실제 복도 격자·방 위치·닫힌 문·자동 계단과 방 목록.
- `playground.svg`: 실제 운동장 시설물, 북쪽 본관 출입구, 남쪽 탈출 교문, 주요 지점과 3개 탐색 동선.
- `index.html`: 전체도·층별도·운동장·53개 독립 실내 배치를 선택하는 58개 보기.

본관 1층 현관과 운동장 북쪽 입구는 E로 왕복합니다. A·B·C 열쇠는 본관과 운동장에 나뉘어 배치되며, 탈출은 운동장 남쪽 교문에서만 가능합니다. 서관·별관·옥상은 봉쇄 상태입니다.

방 외곽은 복도에서의 위치이며 실제 실내 크기와 다릅니다. 53개 실내 링크는 기존 `Docs/RoomPlans`를 참조합니다. 뷰어는 최신 SVG 원본을 직접 엽니다. PNG는 별도 렌더링한 공유용 사본입니다.

재생성: `python SchoolEscape/Tools/export_map_atlas.py`. `--map`과 `--output`으로 입력 지도와 출력 폴더를 지정할 수 있습니다. 지도 변경 후 다시 생성하면 시설물·출입구·동선·방 목록도 함께 갱신됩니다. `atlas.json`에 원본 지도 SHA-256과 각 SVG 크기를 기록합니다.

`review.json`은 과거 Chromium 로드 기록입니다. 이 생성기는 브라우저 검증이나 공개 링크 갱신을 수행하지 않습니다.
''',encoding='utf-8')
    print(json.dumps({'output':str(output),'mapRevision':data['revision'],'views':len(entries),'rooms':len(rooms),'diagrams':diagrams},ensure_ascii=False,indent=2))


if __name__=='__main__':
    main()
