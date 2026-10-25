"""Original procedural surfaces; subset a system-provided OFL Korean font.
No image from the DOCX is redistributed as an in-game sprite.
"""
from pathlib import Path
from PIL import Image, ImageDraw
from fontTools.ttLib import TTCollection
from fontTools import subset
import json, math

root=Path(__file__).resolve().parents[1]
res=root/'Assets/Resources'
for kind in ['Floor','Wall','Yard']:
    im=Image.new('RGB',(256,256)); pix=im.load()
    for y in range(256):
        for x in range(256):
            h=((x*73856093)^(y*19349663))&255
            noise=(h/255-.5)*16
            if kind=='Floor':
                seam=x<3 or y<3 or x>252 or y>252
                stain=math.sin(x*.024+y*.012)*math.cos(y*.031)*9
                v=150+noise+stain-(30 if seam else 0)
                pix[x,y]=(int(v),int(v*.99),int(v*.90))
            elif kind=='Yard':
                soil=math.sin(x*.052)*math.sin(y*.037)*9
                v=112+noise+soil
                pebble=14 if h<12 else 0
                pix[x,y]=(int(v+pebble),int(v*.93+pebble),int(v*.75+pebble))
            else:
                flake=25 if h<18 and ((x//17+y//11)%4==0) else 0
                v=185+noise-flake
                pix[x,y]=(int(v),int(v*.99),int(v*.92))
    d=ImageDraw.Draw(im)
    if kind=='Floor':
        for x,y in [(36,52),(168,112),(72,184)]:
            d.line([(x,y),(x+12,y+9),(x+7,y+19),(x+19,y+29)],fill=(88,90,83),width=1)
    im.save(res/(kind+'Texture.png'))

data=json.loads((res/'SchoolMap.json').read_text())
text=json.dumps(data,ensure_ascii=False)+'''폐교탈출 탐방 베타 본관 모든 방문은 닫혀 있습니다 이동 달리기 계단 안내도 시작 외부 출입 금지 중앙 분기 남쪽 북쪽 동쪽 서쪽 층 프리셋 후보 막힌 꺾이는 미로 통로 ↑ ↓ → ← WASD Shift Tab E 0123456789F[]·– /'''
text+=''.join(path.read_text() for directory,pattern in [('Web','*.js'),('Web','*.mjs'),('Assets/Scripts','*.cs')] for path in sorted((root/directory).glob(pattern)))
text+=(root/'Web/index.html').read_text()+(root/'Assets/Resources/ItemCatalog.json').read_text()
collection=TTCollection('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc')
font=next(f for f in collection.fonts if any('Noto Sans CJK KR' in str(n.toUnicode()) for n in f['name'].names))
options=subset.Options();options.name_IDs=['*'];options.name_legacy=True;options.name_languages=['*']
sub=subset.Subsetter(options=options);sub.populate(text=text);sub.subset(font)
font.save(str(res/'SchoolFont.otf'))
(root/'Docs/FontLicense.txt').write_text(Path('/usr/share/doc/fonts-noto-cjk/copyright').read_text())
print('Original floor/wall/yard textures and OFL font subset generated')
