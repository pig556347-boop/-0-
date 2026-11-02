"""0.18: ordinary library, furnished rooftop, faster exploration. Reproducible authoring."""
def apply_018(data):
    data.update(revision='library-rooftop-roaming-018-2026-10-30',walkSpeed=3.1,runSpeed=5.5)
    rooms=data['roomMaps'];library=next(r for r in rooms if r['kind']=='library');roof=next(r for r in rooms if r['id']=='main-rooftop')
    def add(room,kind,x,y,w,d,h,color,elevation=0,solid=True,**extra):
        f=dict(id=f'{room["id"]}-018-{len(room["fixtures"])}',kind=kind,x=x,y=y,width=w,depth=d,height=h,elevation=elevation,color=color,solid=solid,shape='box',**extra);room['fixtures'].append(f);return f
    for f in library['fixtures']:
        f.pop('bookCode',None)
        if f['kind']=='loan-counter':f['label']='사서 선생님 책상'
        if f['kind']=='library-reward-cabinet':f['label']='도서실 비상 물품 보관함'
    library['subtitle']='도서실 · 서가와 사서 자리를 자유롭게 조사'
    add(library,'librarian-chair',10,5,.55,.55,.48,'#53665e')
    add(library,'chair-back',10,5.2,.55,.1,.45,'#53665e',.48)
    add(library,'desk-monitor',9.65,4,.52,.13,.4,'#283b3c',.85,False)
    add(library,'desk-records',10.4,3.9,.36,.25,.07,'#b1ac8d',.86,False)
    add(library,'desk-lamp',10.7,4.1,.15,.15,.4,'#9e976e',.85,False)
    add(library,'librarian-files-cabinet',8,4,.8,.6,1.1,'#606d60',label='사서 자료 보관함')
    roof.update(isOutdoor=True,subtitle='옥상 · 급수탑과 휴게 공간 / 교문 열쇠 탐색')
    add(roof,'roof-water-tank',12,20,2.8,2.8,2.5,'#7e9493',shapeOverride='cylinder')
    roof['fixtures'][-1]['shape']='cylinder';roof['fixtures'][-1].pop('shapeOverride')
    add(roof,'water-tank-cap',12,20,3,3,.15,'#5c7272',2.5,False)
    for x,y in [(4,4),(21,4),(24,14)]:
        add(roof,'roof-planter',x,y,2,.8,.5,'#716454')
        for dx in [-.5,0,.5]:add(roof,'dry-shrub',x+dx,y,.32,.35,.55,'#5a6850',.5,False)
    for x,y in [(6,21),(22,5)]:
        add(roof,'roof-bench',x,y,2,.65,.48,'#77705b')
        add(roof,'bench-back',x,y+.22,2,.12,.45,'#68634e',.48)
    add(roof,'roof-maintenance-crate',23,21,1.2,.9,.7,'#81765c',label='옥상 점검 상자')
    add(roof,'roof-hose-reel',3,13,.7,.7,.8,'#5c776f')
    for x in [4,8,12,16,20,24]:
        add(roof,'parapet-rail',x,22.7,4.9,.08,.12,'#657b7d',1.1,False)
        add(roof,'parapet-post',x,22.7,.1,.1,1.2,'#657b7d',0,False)
    for x,y in [(10,10),(15,16),(20,20)]:add(roof,'roof-drain',x,y,.6,.6,.02,'#354647',.015,False)
    roof['landmarks'] += [dict(label='급수탑',x=12,y=18),dict(label='옥상 휴게 자리',x=6,y=20),dict(label='설비 점검 상자',x=23,y=20)]
