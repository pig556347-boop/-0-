"""Authored 0.16 subject encounters. Music geometry deliberately preserved."""
def apply_subject_rooms(floors, rooms):
    def fixture(r,kind,x,y,w,d,h,color,**extra):
        o=dict(id=f'{r["id"]}-016-{len(r["fixtures"])}',kind=kind,x=x,y=y,width=w,depth=d,height=h,elevation=0,color=color,solid=True,shape='box',**extra)
        r['fixtures'].append(o);return o
    science=next(r for r in rooms if r['kind']=='science')
    w=h=25;grid=[['#' if x in [0,w-1] or y in [0,1,h-1] else '.' for x in range(w)] for y in range(h)]
    for n,y in enumerate([6,10,14,18]):
        for x in range(1,24):
            if not ((x>=21) if n%2==0 else (x<=3)):grid[y][x]='#'
        # Short dead-end branches; the alternate side always remains connected.
        bx=8 if n%2==0 else 16
        for yy in [y+1,y+2]:grid[yy][bx]='#'
    grid[23][12]='#'
    science.update(width=w,height=h,rows=[''.join(row) for row in grid],spawnX=12,spawnY=3,exitX=12,exitY=1,fixtures=[],landmarks=[],interiorZones=[],subjectEvent='science',subtitle='소등 실험 미로 · 앞문 입장 / 뒷문 퇴장')
    for label,x,y in [('앞문',12,3),('동쪽 실험 통로',22,8),('서쪽 실험 통로',2,12),('안쪽 실험 통로',22,16),('뒷문 캐비넷 앞',10,22)]:
        science['interiorZones'].append(dict(room=science['label'],label=label,x=x,y=y))
    for n,(x,y) in enumerate([(5,4),(6,8),(18,12),(6,16),(18,20)]):
        fixture(science,'science-lab-workbench',x,y,2.3,.8,.9,'#657c72',label='폐쇄된 실험대')
        for dx in [-.5,.4]:
            o=fixture(science,'sample-bottle',x+dx,y,.18,.18,.32,'#8caa93');o.update(elevation=.92,solid=False,shape='cylinder')
    fixture(science,'science-reward-cabinet',9,22,1,.65,1.7,'#647b75',label='뒷문 비상 캐비넷')
    science['portals']=[dict(id='science-rear-exit',label='과학실 뒷문 · 복도',doorX=12,doorY=23,facing='south',targetId='floor-3',arrivalX=40,arrivalY=10)]
    f=floors[2];f.setdefault('portals',[]).append(dict(id='science-rear-entry',label='과학실 뒷문 · 안쪽 잠금',doorX=40,doorY=11,facing='south',targetId=science['id'],arrivalX=12,arrivalY=22,locked=True))
    library=next(r for r in rooms if r['kind']=='library');library.update(subjectEvent='library',fixtures=[],interiorZones=[])
    # Original open shelf module keeps every book clear of boards/back panels.
    from author_interiors import Interior
    shelving=Interior.__new__(Interior)
    shelving.id=library['id'];shelving.label=library['label'];shelving.fixtures=library['fixtures']
    for n,y in enumerate([7,12,17]):
        start,end=(1,22) if n%2==0 else (6,27)
        for x in range(start,end+1,3):shelving.shelf(x+.5,y,w=3.5,d=.8,h=1.7,books=True)
    fixture(library,'loan-counter',10,4,2.2,.8,.85,'#796247',label='대출·반납 기록대')
    for code,label,x,y in [('000','총류',4,9),('300','사회과학',12,9),('400','자연과학',23,14),('600','예술',15,14),('800','문학',4,20),('900','역사',15,20)]:
        fixture(library,'library-index-cabinet',x,y,1,.65,1.6,'#726a53',label=code+' · '+label,bookCode=code)
    fixture(library,'library-reward-cabinet',24,23,1.3,.7,1.7,'#536f69',label='반납 완료 보관함')
    for label,x,y in [('대출 기록대 앞',10,3),('동쪽 우회 통로',26,9),('서쪽 우회 통로',3,14),('안쪽 열람 공간',14,23)]:library['interiorZones'].append(dict(room=library['label'],label=label,x=x,y=y))
    for x in [5,12,19]:fixture(library,'reading-table',x,24,2.5,.9,.8,'#7c6d52',label='열람 책상')
    next(r for r in rooms if r['kind']=='music')['subjectEvent']='music'
