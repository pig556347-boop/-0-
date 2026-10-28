"""Compact west-wing ground-floor shop, original procedural fixtures."""
from annex_map import make, attach
from expansion_013 import wrap, seal

def add_school_shop(rooms):
    hub=wrap(next(r for r in rooms if r['id']=='west-floor-1'))
    shop=make('west-school-shop','매점','schoolShop',16,14,1)
    shop.map.update(building='west',subtitle='서관 1층 · 매점')
    shop.table(4,3.8,3,.8,'shop-sales-desk')
    next(o for o in shop.fixtures if o['kind']=='shop-sales-desk')['label']='매점 계산대'
    shop.obj('shop-register',4,3.8,.5,.4,.28,'#9ba89a',base=.81)
    shop.obj('shop-register-screen',4,3.65,.38,.035,.19,'#273e3f',base=.88)
    shop.obj('shop-price-board',7.5,12.7,3,.08,.9,'#a3aa8d',base=.9,label='과자 · 음료')
    for x in [4,9]:
        for y in [7,11]:
            start=len(shop.fixtures);shop.shelf(x,y,2.2,.65,1.65)
            for o in shop.fixtures[start:]:
                if o['kind']=='shelf-body':o['label']='과자 진열대'
                if o['kind']=='supply-box':o['kind']='shop-snack-packet';o['color']=['#9d7657','#9b9d65','#66898a'][int(o['x']*7)%3]
    for y in [7,11]:
        shop.obj('shop-drink-cabinet',13,y,1.5,.8,1.9,'#677e78',solid=True,label='음료 냉장고')
        shop.obj('shop-fridge-glass',13,y-.34,1.3,.025,1.58,'#334f51',base=.16)
        shop.obj('shop-fridge-handle',13.5,y-.38,.035,.06,.65,'#b1bdad',base=.65)
        for row in range(3):
            for col in range(4):
                shop.obj('shop-drink-bottle',13+(col-1.5)*.22,y-.37,.12,.08,.28,['#93af8e','#c09c6f'][col%2],base=.22+row*.47,shape='cylinder')
    shop.obj('shop-stock-cabinet',12,3.5,2,.65,1.2,'#7a8068',solid=True,label='매점 보관함')
    shop.obj('shop-delivery-crate',2,10,1,.7,.55,'#867252',solid=True,label='입고 상자')
    shop.zone('계산대 앞',4,2.8);shop.zone('중앙 진열 통로',7,7);shop.zone('음료 냉장고 앞',12,7);shop.zone('안쪽 진열대',7,11)
    attach(hub,shop,34,12,'west');seal(hub);seal(shop)
    rooms.append(shop.map)
