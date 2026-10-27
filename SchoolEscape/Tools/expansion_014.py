"""v0.6: card-gated rooftop and 4x8 classroom investigation lockers."""
from annex_map import make

def expand_014(floors,rooms):
    f=floors[2];f['barriers']=[b for b in f['barriers'] if '옥상' not in b['label']]
    roof=make('main-rooftop','본관 옥상 · 설비 구역','rooftop',28,24,4)
    roof.map.update(building='main',noExit=True,spawnX=14,spawnY=5)
    f['stairs'].append(dict(id='f3-roof',x=24,y=35,targetId=roof.id,target=4,arrivalX=14,arrivalY=5,label='옥상 · 카드키 ↑'))
    roof.map['stairs'].append(dict(id='roof-f3',x=14,y=2,targetId=f['id'],target=3,arrivalX=24,arrivalY=32,label='본관 3층 ↓'))
    for x in [5,22]:
        roof.obj('rooftop-vent',x,17,3,3,1.5,'#516467',solid=True)
        roof.obj('rooftop-equipment-cabinet',x,10,2,.7,1.4,'#647873',solid=True)
    roof.zone('설비 점검 동선',14,17);rooms.append(roof.map)
    for r in rooms:
        if r['kind']!='classroom':continue
        banks=[o for o in r['fixtures'] if o['kind']=='locker-bank']
        for bank in banks:
            assembly=bank.get('assembly');bank['height']=1.82;bank['compartments']=32
            if not assembly:continue
            r['fixtures'][:]=[o for o in r['fixtures'] if o.get('assembly')!=assembly or o is bank]
            for row in range(8):
                for col in range(4):
                    r['fixtures'].append(dict(id=f'{assembly}-014-{row}-{col}',kind='locker-compartment',assembly=assembly,room=r['label'],x=bank['x']+(col-1.5)*.64/1.25,y=bank['y']-.32/1.25,width=.6,depth=.04,height=.19,elevation=.08+row*.215,color='#71857b',solid=False,shape='box'))
