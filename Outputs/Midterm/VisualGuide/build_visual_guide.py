from pathlib import Path
import json, re, math, hashlib
from collections import deque
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path.cwd(); OUT=ROOT/'Outputs/Midterm/VisualGuide'; FIG=OUT/'figures'; FIG.mkdir(exist_ok=True)
ASSET=ROOT/'Assets/Game/Images/Tiny Swords'; NN=Image.Resampling.NEAREST
F=lambda size=25,bold=False:ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf' if bold else 'C:/Windows/Fonts/arial.ttf',size)
DATA={s:json.loads((OUT/f'data/seed-{s}.json').read_text()) for s in [0,1,1543]}
M=DATA[0]; W=M['width'];H=M['height']; ASSETS=[]

def sprite_rects(path):
    text=Path(str(path)+'.meta').read_text(encoding='utf8')
    result={}
    for block in re.split(r'    - serializedVersion: 2\s*\n',text)[1:]:
        name=re.search(r'      name: (.+)',block); iid=re.search(r'      internalID: (-?\d+)',block)
        rect=re.search(r'      rect:\s*\n        serializedVersion: \d+\s*\n        x: ([\d.]+)\s*\n        y: ([\d.]+)\s*\n        width: ([\d.]+)\s*\n        height: ([\d.]+)',block)
        if name and iid and rect:result[iid[1]]=(name[1],tuple(int(float(v)) for v in rect.groups()))
    return result

def load(path,frame=True):
    p=ASSET/path;im=Image.open(p).convert('RGBA');ASSETS.append(p)
    rects=sprite_rects(p)
    if frame and rects:
        _,(x,y,w,h)=next(iter(rects.values()));im=im.crop((x,im.height-y-h,x+w,im.height-y))
    return im

atlaspath=ASSET/'Terrain/Tileset/Tilemap_color3.png';atlas=Image.open(atlaspath).convert('RGBA');ASSETS.append(atlaspath)
TILES={}
for iid,(name,(x,y,w,h)) in sprite_rects(atlaspath).items():TILES[iid]=atlas.crop((x,atlas.height-y-h,x+w,atlas.height-y))
rules_text=(ROOT/'Assets/Game/Tilemaps/RuleTile.asset').read_text()
RULES=[]
for block in re.split(r'  - m_Id: \d+\s*\n',rules_text)[1:]:
    sid=re.search(r'm_Sprites:\s*\n    - \{fileID: (-?\d+)',block)
    nb=re.search(r'm_Neighbors: ([0-9a-fA-F]+)',block)
    poses=re.search(r'm_NeighborPositions:(.*?)m_RuleTransform:',block,re.S)
    if not(sid and nb and poses):continue
    values=[int.from_bytes(bytes.fromhex(nb[1])[i:i+4],'little') for i in range(0,len(nb[1])//2,4)]
    xy=[tuple(map(int,p)) for p in re.findall(r'\{x: (-?\d+), y: (-?\d+), z: -?\d+\}',poses[1])]
    RULES.append((sid[1],list(zip(values,xy))))
water=load('Terrain/Tileset/Water Background color.png',False)
tree=load('Terrain/Resources/Wood/Trees/Tree1.png');sheep=load('Terrain/Resources/Meat/Sheep/Sheep_Idle.png')
golds=[load(f'Terrain/Resources/Gold/Gold Stones/Gold Stone {i}.png') for i in range(1,7)]
castle=load('Buildings/Blue Buildings/Castle.png')
PALETTE=[(231,190,97),(101,192,151),(200,137,221)]

def text(im,xy,t,size=25,color='#173447',bold=False,anchor=None):ImageDraw.Draw(im).text(xy,t,font=F(size,bold),fill=color,anchor=anchor)
def land(a,x,y):return 0<=x<W and 0<=y<H and a[y*W+x]==1
def pick(a,x,y):
    for sid,conditions in RULES:
        if sid not in TILES:continue
        if all(n==0 or (land(a,x+dx,y+dy) if n==1 else not land(a,x+dx,y+dy)) for n,(dx,dy) in conditions):return TILES[sid]
    return TILES['-956396442']

def overlay_cell(im,x,y,crop,tile,color,outline=None):
    x0,y0,x1,y1=crop;X=(x-x0)*tile;Y=(y1-1-y)*tile
    lay=Image.new('RGBA',(tile,tile),color)
    if outline:ImageDraw.Draw(lay).rectangle((0,0,tile-1,tile-1),outline=outline)
    im.alpha_composite(lay,(X,Y))

def board(m=M,stage='terrain',crop=None,tile=16,objects=False,region=False,reserved=False,route=False,grid=False):
    a=m[stage];crop=crop or (0,0,W,H);x0,y0,x1,y1=crop
    im=Image.new('RGBA',((x1-x0)*tile,(y1-y0)*tile),'#48a6a7')
    for y in range(y0,y1):
        for x in range(x0,x1):
            t=pick(a,x,y) if land(a,x,y) else water
            im.alpha_composite(t.resize((tile,tile),NN),((x-x0)*tile,(y1-y-1)*tile))
            if region and land(a,x,y):overlay_cell(im,x,y,crop,tile,(*PALETTE[m['regionIds'][y*W+x]],80))
            if reserved and m['reserved'][y*W+x]:overlay_cell(im,x,y,crop,tile,(72,226,244,110))
            if route and m['routes'][y*W+x]:overlay_cell(im,x,y,crop,tile,(255,244,170,145))
    if objects:
        entries=[(r['X'],r['Y'],[tree,golds[r['VariantId']%6],sheep][r['Kind']]) for r in m['resources']]
        entries.append((W//2,H//2,castle))
        for x,y,spr in sorted(entries,key=lambda r:-r[1]):
            if not(x0-4<=x<x1+4 and y0-4<=y<y1+4):continue
            # Use original frame at the same pixels-per-unit as the 64 px tiles.
            s=spr.resize((max(1,round(spr.width*tile/64)),max(1,round(spr.height*tile/64))),NN)
            X=round((x-x0+.5)*tile-s.width/2);Y=round((y1-y)*tile-s.height*.75)
            im.alpha_composite(s,(X,Y))
    if grid:
        d=ImageDraw.Draw(im)
        for x in range(0,im.width,tile):d.line((x,0,x,im.height),fill=(255,255,255,70),width=1)
        for y in range(0,im.height,tile):d.line((0,y,im.width,y),fill=(255,255,255,70),width=1)
    return im

def panel(images,labels,file,cols=2,cell=(650,620),captions=None):
    rows=math.ceil(len(images)/cols);cw,ch=cell
    out=Image.new('RGB',(cw*cols,ch*rows),'#ffffff');d=ImageDraw.Draw(out)
    for i,(im,label) in enumerate(zip(images,labels)):
        x=(i%cols)*cw;y=(i//cols)*ch
        text(out,(x+22,y+16),label,25,bold=True)
        ratio=min((cw-40)/im.width,(ch-100)/im.height)
        p=im.resize((max(1,round(im.width*ratio)),max(1,round(im.height*ratio))),NN)
        out.paste(p,(x+(cw-p.width)//2,y+60),p if p.mode=='RGBA' else None)
        if captions:text(out,(x+cw/2,y+ch-27),captions[i],21,anchor='mm')
    out.save(FIG/file);return out

def annotate(im,crop,tile,label,x,y,col='#fff5b5'):
    X=(x-crop[0]+.5)*tile;Y=(crop[3]-1-y+.5)*tile
    d=ImageDraw.Draw(im);d.ellipse((X-9,Y-9,X+9,Y+9),fill=col,outline='#182e45',width=2)
    box=d.textbbox((0,0),label,font=F(25,True));tw=box[2];th=box[3]
    tx=min(max(5,X+13),im.width-tw-8);ty=min(max(5,Y-35),im.height-th-8)
    d.rectangle((tx-4,ty-3,tx+tw+5,ty+th+5),fill='#f7fafc');d.text((tx,ty),label,font=F(25,True),fill='#162e40')

# 1 Asset catalogue enlarged with nearest-neighbor pixel scaling.
assetim=Image.new('RGB',(1400,690),'#f4f7f8')
items=[(pick([1]*(W*H),64,64),'Cỏ ở giữa'),(pick([1 if x<64 else 0 for y in range(H) for x in range(W)],63,50),'Cỏ sát bờ'),(water,'Nước'),(tree,'Cây Tree1'),(golds[3],'Vàng Variant 3'),(sheep,'Cừu Idle')]
for i,(spr,label) in enumerate(items):
    x=(i%3)*465;y=(i//3)*340
    p=spr.crop(spr.getbbox());p.thumbnail((220,235),NN)
    if p.width<120 and p.height<120:p=p.resize((p.width*2,p.height*2),NN)
    assetim.paste(p,(x+(465-p.width)//2,y+40),p)
    text(assetim,(x+232,y+300),label,27,bold=True,anchor='mm')
assetim.save(FIG/'01-assets.png')

# 2 Actual noise arrays of accepted attempt 0.
def gray(values):
    im=Image.new('RGB',(W,H));im.putdata([(max(0,min(255,round((v+1)*127.5))),)*3 for v in values]);return im.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
panel([gray(M['layers'][0]),gray(M['layers'][1]),gray(M['values']),board(stage='raw',tile=5)],['Octave 1  mảng lớn','Octave 2  chi tiết nhỏ hơn','Trộn 4 octave có trọng số','Qua ngưỡng −0,22 → cỏ hoặc nước'],'02-noise.png',cell=(640,560))

# 3 Real layout before/after and protected cells.
im=board(tile=8,region=True,route=True);crop=(0,0,W,H)
for r in M['regions']:annotate(im,crop,8,['Start','Economy','Gold'][r['Role']],r['X'],r['Y'])
for i,r in enumerate(M['gateways']):annotate(im,crop,8,f'G{i+1}',r['X'],r['Y'],'#ff7a7a')
panel([board(stage='raw',tile=8),im],['Trước quy hoạch','Sau carve và BFS  có lớp chú giải'],'03-layout.png',cell=(650,720))
panel([board(crop=(50,50,79,79),tile=24,reserved=True,objects=True),board(crop=(3,51,32,80),tile=24,route=True,objects=True)],['Cyan  Reserved quanh TownHall','Vàng nhạt  route trong vùng Economy'],'04-reserved.png',cell=(650,720))

# 4 Isolated-land cleanup from actual arrays, not invented.
removed=[i for i,(a,b) in enumerate(zip(M['carved'],M['terrain'])) if a==1 and b==0]
before=board(stage='carved',tile=8);after=board(tile=8)
for i in removed:overlay_cell(before,i%W,i//W,(0,0,W,H),8,(242,89,94,210))
panel([before,after],[f'Đỏ  {len(removed)} ô đất cô lập bị loại','Sau BFS  đất giữ lại nối tới Start'],'05-connectivity.png',cell=(650,720))

# 5 Actual resource placements, plus readable starting-area crop.
panel([board(tile=8,objects=True),board(crop=(44,44,85,85),tile=22,objects=True)],['Đủ 120 cây  12 vàng  18 cừu','Phóng vùng gần căn cứ  cùng seed 0'],'06-resources.png',cell=(650,720))

# 6 Didactic fixture uses actual sprites and explicit cell positions.
def fixture(kind='gold',blocked=False,narrow=False):
    a=[1]*(W*H);crop=(0,0,13,11);t=47
    if narrow:
        for y in range(11):
            for x in range(13):a[y*W+x]=int(x in ([5] if blocked else [4,5,6]))
    im=board({'terrain':a},crop=crop,tile=t,grid=True)
    d=ImageDraw.Draw(im)
    if not narrow:
        for y in range(4,7):
            for x in range(5,8):overlay_cell(im,x,y,crop,t,(239,184,77,125))
        for y in range(3,8):
            for x in range(4,9):
                if not(5<=x<=7 and 4<=y<=6):overlay_cell(im,x,y,crop,t,(76,217,241,130))
        s=golds[3].crop(golds[3].getbbox());s=s.resize((t*2,t*2),NN);im.alpha_composite(s,(int(5.5*t),int(4*t)))
        if blocked:
            for y in range(11):
                s=tree.crop(tree.getbbox());s=s.resize((int(t*1.15),t*2),NN);im.alpha_composite(s,(3*t,int((10-y)*t)-t))
            d=ImageDraw.Draw(im);d.line((2*t,5*t,4*t,5*t),fill='#ee686b',width=8)
            d.line((int(3.6*t),int(4.6*t),int(4.4*t),int(5.4*t)),fill='red',width=7);d.line((int(4.4*t),int(4.6*t),int(3.6*t),int(5.4*t)),fill='red',width=7)
        else:
            d.line([(1*t,8*t),(3*t,8*t),(3*t,6*t),(4*t,6*t)],fill='#fff3a3',width=9)
            d.polygon([(4*t,6*t),(int(3.7*t),int(5.8*t)),(int(3.7*t),int(6.2*t))],fill='#fff3a3')
    else:
        color='#ef6b70' if blocked else '#56e2f2'
        d.rectangle((4*t,4*t,7*t,7*t),outline=color,width=7)
        text(im,(im.width//2,im.height//2),'3 × 3',28,color=color,bold=True,anchor='mm')
    return im
panel([fixture(),fixture(blocked=True)],['Ô vàng  footprint mỏ 3 × 3','Ví dụ thêm cây  đường bị chặn'],'07-footprint.png',cell=(650,630))
panel([fixture(narrow=True,blocked=True),fixture(narrow=True)],['Đường 1 ô  BFS điểm có thể đi','Đường 3 ô  tâm có clearance'],'08-clearance.png',cell=(650,630))

# 7 Real BFS through resource blockers from start to interaction ring of a gold node.
def bfs(m):
    blocked=set()
    for r in m['resources']:
        rad=1 if r['Kind']==1 else 0
        if r['Kind']==2:continue
        for dy in range(-rad,rad+1):
            for dx in range(-rad,rad+1):blocked.add((r['X']+dx,r['Y']+dy))
    start=(64,64);q=deque([start]);prev={start:None}
    while q:
        x,y=q.popleft()
        for p in [(x+1,y),(x-1,y),(x,y+1),(x,y-1)]:
            if p not in prev and p not in blocked and land(m['terrain'],*p):prev[p]=(x,y);q.append(p)
    return prev
prev=bfs(M)
gold=next(r for r in M['resources'] if r['Kind']==1 and r['IsStartingResource'])
targets=[(gold['X']+dx,gold['Y']+dy) for dx in range(-2,3) for dy in range(-2,3) if max(abs(dx),abs(dy))==2 and (gold['X']+dx,gold['Y']+dy) in prev]
def path_to(p):
    path=[]
    while p is not None:path.append(p);p=prev[p]
    return path[::-1]
path=min((path_to(p) for p in targets),key=len)
xx=[p[0] for p in path]+[gold['X']];yy=[p[1] for p in path]+[gold['Y']]
crop=(max(0,min(xx)-4),max(0,min(yy)-4),min(W,max(xx)+5),min(H,max(yy)+5))
im=board(crop=crop,tile=40,objects=True,grid=True)
pts=[((x-crop[0]+.5)*40,(crop[3]-y-.5)*40) for x,y in path]
ImageDraw.Draw(im).line(pts,fill='#fff2a5',width=8)
annotate(im,crop,40,'Start',64,64);annotate(im,crop,40,'Ô tương tác',*path[-1]);im.save(FIG/'09-bfs-path.png')

# 8 Three genuine seeds at the same visual scale.
panel([board(DATA[s],tile=8,objects=True) for s in [0,1,1543]],['Seed 0','Seed 1','Seed 1543'],'10-seeds.png',cols=3,cell=(440,540),captions=['attempt 0  150 placement']*3)

# 9 Integration screenshot is existing evidence, explicitly dated.
shot=ROOT/'Docs/Modules/ProceduralGeneration/Evidence/S7-final-after-seed1-day.png'
shotim=Image.open(shot);shotim.save(FIG/'11-gameplay-evidence.png')

# 10 Compact visual preview for chat, containing actual tiles.
panel([board(crop=(44,44,85,85),tile=24,objects=True),im],['Asset trong game  bố trí gần căn cứ','BFS bốn hướng  đi tới mỏ vàng'],'preview.png',cell=(680,670))

# Document inherits verified typographic styles without executing the earlier builder.
overview=Image.new('RGB',(1440,1660),'white'); od=ImageDraw.Draw(overview)
def overview_box(bounds,lines,fill='#edf4f7'):
    od.rounded_rectangle(bounds,radius=16,fill=fill,outline='#36576b',width=3)
    x0,y0,x1,y1=bounds
    for j,line in enumerate(lines):text(overview,((x0+x1)/2,(y0+y1)/2+(j-(len(lines)-1)/2)*38),line,29,bold=j==0,anchor='mm')
def overview_arrow(points,label=None,labelxy=None,color='#36576b'):
    od.line(points,fill=color,width=4)
    x,y=points[-1];px,py=points[-2];angle=math.atan2(y-py,x-px)
    od.polygon([(x,y),(x-17*math.cos(angle)+8*math.sin(angle),y-17*math.sin(angle)-8*math.cos(angle)),(x-17*math.cos(angle)-8*math.sin(angle),y-17*math.sin(angle)+8*math.cos(angle))],fill=color)
    if label:text(overview,labelxy,label,27,color=color,bold=True,anchor='mm')
overview_box((40,15,920,155),['1  Seed và cấu hình hợp lệ','128 × 128 ô   120 cây   12 vàng   18 cừu','Các giá trị trên thuộc preset hiện tại'])
overview_box((40,195,920,315),['2  Simplex noise 4 octave','Trộn giá trị rồi phân loại cỏ hoặc nước'])
overview_box((40,355,920,525),['3  Start và hai vùng chiến thuật','Economy và GoldFrontier  nối tới hai gateway','Carve đường rộng 5 ô  đánh dấu Reserved','Chưa đặt tài nguyên'])
overview_box((40,565,920,705),['4  BFS bốn hướng từ Start','Đất cô lập → nước','Tính distance field cho các ô cỏ tới được'])
overview_box((40,745,920,895),['5  Đặt tài nguyên','Quota khởi đầu trước  rồi quota theo vùng','Tránh nước  Reserved và chồng footprint'])
od.polygon([(480,940),(920,1045),(480,1150),(40,1045)],fill='#fff4d8',outline='#96743b',width=3)
for j,line in enumerate(['6  Validation cuối có đạt không?','Quota  ô tương tác  footprint','Đường đủ rộng  pad và gateway']):text(overview,(480,1005+j*37),line,29,bold=j==0,anchor='mm')
overview_box((40,1210,920,1305),['7  Chấp nhận MapData'],fill='#e8f4e9')
overview_box((40,1345,920,1480),['8  Áp dữ liệu vào Unity','Tilemap  prefab  Physics2D  navigation','Root và bridge kết nối gameplay'])
overview_box((40,1520,920,1615),['Bản đồ sẵn sàng cho trận đấu'],fill='#e8f4e9')
for y0,y1 in [(155,195),(315,355),(525,565),(705,745),(895,940),(1305,1345),(1480,1520)]:overview_arrow([(480,y0),(480,y1)])
overview_arrow([(480,1150),(480,1210)],'Đạt',(535,1178))
od.polygon([(1210,955),(1400,1045),(1210,1135),(1020,1045)],fill='#fff4d8',outline='#96743b',width=3)
text(overview,(1210,1027),'Còn lượt thử?',28,bold=True,anchor='mm');text(overview,(1210,1064),'MaxAttempts = 8',25,anchor='mm')
overview_arrow([(920,1045),(1020,1045)],'Không đạt',(975,1007),color='#a65b44')
overview_box((1030,325,1400,480),['Tăng attempt','Dùng seed con mới','Sinh lại từ bước 2'])
overview_arrow([(1210,955),(1210,480)],'Còn',(1250,720))
overview_arrow([(1030,402),(970,402),(970,255),(920,255)])
overview_box((1030,1240,1400,1420),['Dừng và báo lỗi','Đã hết lượt thử','Không tự giảm quota'],fill='#fbecec')
overview_arrow([(1210,1135),(1210,1240)],'Hết',(1250,1180),color='#a65b44')
overview.save(FIG/'00-overview.png')

doc=Document(ROOT/'Outputs/Midterm/Giai_thich_Procedural_Generation_RTSgame2026.docx')
body=doc._element.body
for child in list(body):
    if child.tag!=qn('w:sectPr'):body.remove(child)
doc.core_properties.title='Cách sinh bản đồ procedural qua hình minh họa bằng tile trong game'
doc.styles['Normal'].font.size=Pt(12.5)
doc.styles['Normal'].paragraph_format.space_after=Pt(6)
MD=['# Cách sinh bản đồ procedural qua hình minh họa bằng tile trong game\n','Ngày cập nhật 24/11/2026. Core strategic v3.\n']
def h(t):doc.add_heading(t,level=1);MD.append('\n## '+t+'\n')
def p(t,bold=False):
    q=doc.add_paragraph();q.add_run(t).bold=bold;MD.append(t+'\n');return q
def b(t):
    q=doc.add_paragraph(t,style='List Bullet');MD.append('- '+t);return q
def fig(file,caption,width=16):
    q=doc.add_paragraph();q.alignment=WD_ALIGN_PARAGRAPH.CENTER;q.add_run().add_picture(str(FIG/file),width=Cm(width))
    q=doc.add_paragraph(caption,style='Caption');q.alignment=WD_ALIGN_PARAGRAPH.CENTER
    MD.append(f'\n![{caption}](../../../Outputs/Midterm/VisualGuide/figures/{file})\n')
def page(title):doc.add_page_break();h(title)
def link(label,url):
    q=doc.add_paragraph();hyper=OxmlElement('w:hyperlink');hyper.set(qn('r:id'),doc.part.relate_to(url,'http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink',is_external=True))
    r=OxmlElement('w:r');t=OxmlElement('w:t');t.text=label;r.append(t);hyper.append(r);q._p.append(hyper);MD.append(f'[{label}]({url})\n')

doc.add_paragraph('Cách sinh bản đồ procedural qua hình minh họa bằng tile trong game',style='Title')
p('Tài liệu dành cho người muốn hiểu game tạo bản đồ ra sao. Các hình dùng tile và sprite Tiny Swords đang có trong RTSgame2026, kèm lớp màu chú giải. Dữ liệu seed 0, 1, 1543 được xuất bằng chính Core generator strategic v3 của project.')
fig('00-overview.png','Sơ đồ tổng quát pipeline sinh bản đồ và nhánh thử lại',16)
p('Đọc từ trên xuống. Kiểm tra lúc đặt từng tài nguyên chỉ xét vị trí hợp lệ; validation cuối kiểm khả năng đi lại của toàn bản đồ sau placement. Cấu hình sai bị từ chối trước khi sinh; chỉ MapData hợp lệ mới được áp vào scene.')
doc.add_page_break()
h('1  Những vật liệu dùng để tạo bản đồ')
fig('01-assets.png','Hình 1  Tile và sprite được trích từ asset của dự án',16)
b('Cỏ là ô đi được; nước là ô không đi được. Hình bờ cỏ đổi theo các ô lân cận bằng RuleTile hiện có. Tile không tự tạo ra luật sinh địa hình.')
b('Cây, mỏ vàng và cừu là đối tượng được đặt sau khi có terrain. Sprite là hình ảnh; footprint là diện tích logic dùng kiểm chồng lấn và đường đi.')
p('Các hình tái dựng chỉ thể hiện các lớp cần giải thích, không có fog, hoạt ảnh foam, ánh sáng hoặc sorting đầy đủ như khi chạy Unity. Hình có nhãn “ví dụ” là fixture minh họa; hình có seed dùng dữ liệu Core thật. Báo cáo giữa kỳ đã được giữ nguyên.')
p('Luồng tổng quát: seed và cấu hình → noise → vùng và tuyến → BFS → tài nguyên → validation → Tilemap/prefab/navigation → trận sẵn sàng.',True)

page('2  Từ noise đến tile cỏ và nước')
p('Seed là số đầu vào cho bộ sinh giả ngẫu nhiên. Với cùng cấu hình và phiên bản thuật toán, nó cho phép tái lập MapData. Trong ví dụ seed 0, attempt được chấp nhận là 0; lưới có 128 × 128 ô.')
fig('02-noise.png','Hình 2  Trường noise thực của seed 0 và terrain trước carve',15.5)
p('Ô sáng/tối ở ba hình đầu biểu diễn giá trị noise, chưa phải độ cao gameplay. Hai hình trên cho thấy hai lớp đầu; hình dưới trái là hỗn hợp đủ bốn octave. Lớp sau có tần số cao hơn và ảnh hưởng nhỏ hơn.')
b('Core dùng noise.snoise của Unity.Mathematics, tức Simplex noise. Cấu hình đang minh họa: 4 octave, NoiseScale 34, Persistence 0,5 và Lacunarity 2.')
b('Giá trị trung bình có trọng số từ −0,22 trở lên được chọn thành cỏ; thấp hơn thành nước. Vì các giá trị lân cận liên tục, bản đồ tạo thành mảng thay vì các ô ngẫu nhiên rời rạc.')
p('Noise chỉ tạo hình địa hình nền. Nếu dừng ở bước này, căn cứ có thể nằm trong nước hoặc đất bị chia cắt. Các bước tiếp theo sửa và kiểm tra để tạo điều kiện chơi được.')

page('3  Dành chỗ cho căn cứ vùng mở rộng và đường chính')
fig('03-layout.png','Hình 3  Cùng seed 0 trước và sau quy hoạch chiến thuật',15)
p('StrategicLayout chọn một trong bốn hướng xoay, đặt hai vùng ngoài theo hai hướng vuông góc và thêm độ lệch nhỏ. Mỗi ô nhận nhãn của tâm vùng gần nhất. Màu trên hình là chú giải vai trò; terrain gameplay vẫn chỉ là cỏ hoặc nước.')
b('Start: TownHall ở trung tâm, có vùng giữ trống. Economy: ưu tiên cây và cừu. GoldFrontier: vùng nhiều vàng và có landmark.')
b('Hai tuyến chữ L nối Start tới vùng ngoài rồi ra hai gateway G1/G2 ở biên. Half-width bằng 2 tạo đường rộng 5 ô. Bản đồ có thể còn nhánh nối tự nhiên bên trong.')
b('Strategic tạo dải nước biên dày 2 ô trước khi mở gateway. Noise và quy hoạch được phối hợp; không đơn thuần rải tài nguyên lên một hình noise có sẵn.')
fig('04-reserved.png','Hình 4  Phóng chi tiết Reserved và tuyến đi  màu được phủ để giải thích',14)
p('Reserved ngăn tài nguyên chiếm pad của căn cứ, pad landmark và đường được bảo vệ. Hình TownHall chỉ minh họa vị trí căn cứ; Reserved không phải bán kính sprite của tòa nhà.')

page('4  Loại đất cô lập bằng BFS bốn hướng')
fig('05-connectivity.png',f'Hình 5  Seed 0 có {len(removed)} ô đất bị loại ở bước làm sạch kết nối',16)
p('Hãy hình dung đổ mực từ Start: mực chỉ lan sang trên, dưới, trái, phải trên ô cỏ. Các ô đất không nhận mực là đất cô lập. Core đổi chúng thành nước để tránh giữ lại đảo không thể đi tới.')
b('Ảnh trái lấy terrain sau carve, trước RemoveUnreachableLand; màu đỏ đánh dấu chính các ô đất biến mất. Ảnh phải lấy terrain sau hàm này từ MapData cuối.')
b('BFS đồng thời tạo distance field: mỗi ô có số bước từ Start. Khoảng này phù hợp chọn tài nguyên đầu game hơn khoảng cách thẳng xuyên qua hồ.')
b('Hai ô cỏ chỉ chạm góc không được coi là liền nhau. Quy tắc bốn hướng khớp với phép kiểm lưới của module, giúp tránh map trông thông nhưng đường thực tế bị đứt.')
p('Đây mới là kiểm kết nối trên terrain. Sau khi đặt cây và mỏ vàng, hệ thống phải kiểm lại với footprint tài nguyên; một bãi cỏ thông trước placement vẫn có thể bị tài nguyên chặn sau đó.')

page('5  Đặt tài nguyên gần trước rồi phân bổ theo vùng')
fig('06-resources.png','Hình 6  Placement thật seed 0 và vùng gần căn cứ được phóng lớn',16)
p('Core đặt quota khởi đầu trước: 30 cây, 3 mỏ vàng, 6 cừu. Chúng được chọn theo khoảng BFS quanh căn cứ; không buộc mọi node khởi đầu phải mang nhãn vùng Start. Sau đó mới đặt phần còn lại theo vai trò vùng.')
b('Tổng map: 120 cây, 12 mỏ vàng, 18 cừu. Ngoài quota khởi đầu, Economy nhận 68 cây và 9 cừu; GoldFrontier nhận 22 cây, 9 vàng và 3 cừu.')
b('Ba mỏ vàng quanh landmark nằm trong tổng 12 mỏ. Các VariantId chọn hình Gold Stone 1–6; tất cả cùng loại tài nguyên Gold.')
b('Cây/vàng xa được lấy mẫu quanh các tâm cụm; cừu lấy mẫu trên đất mở. Occupancy và Reserved loại vị trí chồng lấn hoặc chắn chỗ đã dành trước.')
p('Con số quota nói về số đối tượng được đặt, không tự chứng minh nguồn tài nguyên sẽ cạn hoặc game đã cân bằng. Mức lợi ích của mở rộng còn phụ thuộc quãng vận chuyển, depot và thời điểm wave.')

page('6  Sprite và footprint là hai thứ khác nhau')
fig('07-footprint.png','Hình 7  Fixture minh họa footprint vàng và một lối bị cây chắn',16)
p('Các ô vàng biểu diễn footprint 3 × 3 của mỏ, không phải hình dạng chính xác của sprite. Vòng cyan là các ô tương tác ứng viên phía ngoài. Dân cần đứng được ở một ô hợp lệ trên vòng này để khai thác.')
b('Cây chặn 1 ô trong validator Core. Vàng chặn 3 × 3 ô. Cừu không chặn đường trong phép kiểm topology tĩnh nhưng vẫn được kiểm vị trí và overlap khi placement.')
b('Sprite có phần trang trí và khoảng trong suốt; hình trông nhỏ hoặc lớn không tự thay đổi footprint lưới. Collider và offset thật được đối chiếu ở Integration.')
b('Hình bên phải cố tình thêm hàng cây chạy hết chiều cao khu vực minh họa: lối đi từ phía trái tới mỏ bị chia cắt. Ví dụ xét khu vực đóng trong hình; nếu có đường vòng ngoài khung thì phải kiểm cả đường đó. Đây là fixture để đọc luật, không phải kết quả hợp lệ của generator.')
p('Sau placement, hệ thống kiểm tất cả tài nguyên: nằm trên đất, không đè Reserved, không trùng footprint và có ít nhất một ô tương tác tới được. Chỉ kiểm “tâm mỏ nằm trên cỏ” là chưa đủ.')

page('7  Đường phải vừa nối được vừa đủ rộng')
fig('09-bfs-path.png',f'Hình 8  BFS trên map seed 0 tới mỏ khởi đầu  đường có {len(path)-1} bước',12.5)
p('Đường vàng được dựng bằng BFS bốn hướng trên terrain thật cộng vật cản cây/vàng. Đường kết thúc ở ô tương tác ngoài mỏ, không chạy xuyên qua tâm mỏ. Đây là phép minh họa validator, không phải lệnh di chuyển cho một Unit đang chạy.')
fig('08-clearance.png','Hình 9  Fixture so sánh hành lang hẹp và hành lang đủ clearance',15.5)
p('Đường 1 ô có thể nối được nếu xem nhân vật như một điểm. Phép kiểm clearance dùng footprint 3 × 3, tương đương cần 1 ô trống xung quanh tâm; lối hẹp bên trái không đạt. Hệ thống co vùng đi được rồi kiểm tới hai gateway và landmark. Ô biên được xử lý theo quy tắc gateway.')

page('8  Vì sao cần seed attempt và validation')
fig('10-seeds.png','Hình 10  Ba bản đồ xuất từ Core hiện tại  cùng preset khác seed',16)
p('Ba seed 0, 1, 1543 ở đợt xuất hình này đều được chấp nhận tại attempt 0 và đủ 150 placement. Đây là kiểm tra phục vụ minh họa, không phải một đợt benchmark hoặc chứng nhận mọi seed đều hợp lệ.')
b('Validation cuối kiểm quota, ô tương tác, footprint, pad, clearance và gateway. Mọi cửa biên đi tới được phải thuộc vùng gateway được khai báo.')
b('Nếu một attempt không đạt, Core thử attempt tiếp theo. Mặc định tối đa 8 attempt; từng lời gọi PlaceQuota có giới hạn phép thử. Hết giới hạn thì trả lỗi, không tự giảm quota để báo thành công.')
b('Terrain, layout và resource có stream seed riêng được suy ra từ seed gốc, mã phase và attempt. Một lần UI gọi random không được làm xê dịch bố trí map.')
p('Cùng seed chỉ đủ để tái lập khi cấu hình và phiên bản generator cũng phù hợp. Nếu đổi thuật toán hoặc quota, kết quả có thể đổi. Khi debug cần giữ seed, settings, version, attempt và nguyên nhân validator.')
p('Các bài kiểm lịch sử: S5 quét 1.000 strategic và 1.000 legacy, có sinh lại để so dữ liệu; SL4 có 18/18 procedural tests đạt. Không dùng các kết quả đó để kết luận thời gian scene đến Ready hay cân bằng kinh tế đã đạt.')

page('9  Từ bản đồ dữ liệu tới màn hình chơi')
fig('11-gameplay-evidence.png','Hình 11  Ảnh gameplay đã lưu ở S7 ngày 16 tháng 11 năm 2026',16)
p('Ảnh trên là evidence Unity có sẵn, không phải ảnh chụp mới của candidate E10. Nó cho thấy RuleTile, bờ nước, tài nguyên và lớp presentation được áp vào scene. Hình tái dựng ở các trang trước chủ động lược bỏ các lớp này để thấy từng bước logic.')
b('Core tạo MapData. ProceduralMapAuthoring ghi Tilemap theo batch, tạo prefab đúng placement, tắt spawner cũ để tránh sinh trùng, rồi đồng bộ Physics2D và dựng lại pathfinding.')
b('Root và bridge kết nối dependency, đăng ký thực thể/tài nguyên vào ECS. Đồng hồ và wave chờ quá trình chuẩn bị; map chưa hợp lệ không được coi là sinh thành công.')
b('Gateway là dữ liệu tuyến tiếp cận. Điểm spawn thực tế được chọn trên tuyến ở bán kính khởi đầu phù hợp; không mặc định quái luôn sinh đúng mép map.')
p('New Game chạy generator. Load Game SL4 áp snapshot terrain/topology và khôi phục đối tượng bằng ID, không sinh map và tài nguyên lần nữa. Vì vậy tài nguyên đã mất không mọc lại chỉ nhờ còn seed. SL4 vẫn chỉ nhận lát cắt đầu ngày không có công việc đang chạy.')
p('Cách xem thử: RTS → Procedural Generation → Preview Regions → StrategicRegions → nhập seed → Sinh preview. Muốn chơi dùng ProceduralGame. Đọc asset được gán thực tế ở authoring; không suy preset gameplay từ asset DefaultProceduralMapSettings riêng.')

page('10  Tham khảo từ game nào và học điều gì')
p('Có hai loại nguồn: tài liệu/công trình giải thích thuật toán và game cho thấy cách áp dụng luật sinh vào trải nghiệm. Không có bằng chứng rằng RTSgame2026 sao chép nguyên bộ sinh map của một game thương mại.')
h('0 A D là tham khảo đã có trong nghiên cứu ban đầu')
p('Research.md ngày 26/10/2026 đã dẫn mã nguồn 0 A.D. File rmgen-common/player.js tách các bước đặt căn cứ, cây, mỏ và động vật khởi đầu, đồng thời dùng constraints và khoảng cách. Điểm học cho dự án là tách tài nguyên quanh căn cứ khỏi phần ngẫu nhiên xa hơn. Không lấy quota hay thông số cân bằng của 0 A.D. làm thông số của game này.')
link('Nguồn 1  0 A D  rmgen common player js','https://github.com/0ad/0ad/blob/master/binaries/data/mods/public/maps/random/rmgen-common/player.js')
h('Factorio là nguồn bổ sung ở lần cập nhật này')
p('Friday Facts 258 giải thích việc làm tài nguyên đầu game dễ dự đoán hơn, tránh chồng lấn và nước che tài nguyên. Friday Facts 390 minh họa cách kết hợp trường noise với biểu thức hình học để quyết định tile và vị trí đối tượng. Liên hệ với dự án là “nền tự nhiên + vùng bắt buộc + tài nguyên có luật”; đây là đối chiếu thiết kế, không khẳng định hai game dùng cùng thuật toán hoặc mã nguồn.')
link('Nguồn 2  Factorio Friday Facts 258  New autoplace','https://www.factorio.com/blog/post/fff-258')
link('Nguồn 3  Factorio Friday Facts 390  Noise expressions 2 0','https://www.factorio.com/blog/post/fff-390')
h('Các nguồn khác và giới hạn diễn giải')
p('Crown Siege được dùng tham khảo giao diện tech tree nền tối, node và điều hướng; không có bằng chứng về thuật toán sinh map của Crown Siege trong nghiên cứu hiện có. They Are Billions và Age of Darkness xuất hiện trong đề cương như bối cảnh RTS sinh tồn, không phải nguồn chứng minh cách generator hiện tại được cài đặt.')
p('Nền tảng noise tham khảo Red Blob Games và API Unity.Mathematics. WFC, Cellular Automata, Poisson disk và multiobjective generation có được khảo sát; Core hiện tại không triển khai đầy đủ các kỹ thuật đó. Tài nguyên theo cụm không được gọi là Bridson Poisson disk hoàn chỉnh.')
link('Nguồn 4  Red Blob Games  Making maps with noise functions','https://www.redblobgames.com/maps/terrain-from-noise/')
p('Asset: Tiny Swords đã có trong project. Hình tái dựng bằng tile gốc, dữ liệu Core xuất riêng và lớp chú giải; không thay đổi asset, scene hay gameplay. Source đối chiếu: ProceduralMapGenerator.cs, StrategicLayout.cs, StrategicResources.cs, DeterministicRandom.cs, ProceduralMapAuthoring.cs; các báo cáo S4, S5, S6 và SaveLoad SL4.')

target=ROOT/'Outputs/Midterm/Giai_thich_Procedural_Generation_Minh_hoa_tile.docx';doc.save(target)
(ROOT/'Docs/Modules/ProceduralGeneration/IllustratedGuide.md').write_text('\n'.join(MD)+'\n',encoding='utf8')
manifest={'generatorVersion':3,'seeds':[{'seed':s,'attempt':DATA[s]['attempt'],'placements':len(DATA[s]['resources']),'valid':DATA[s]['validation']['IsValid']} for s in DATA],
          'sourceFiles':{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Assets/Game/ProceduralGeneration/Core').glob('*.cs')},
          'assets':[str(p.relative_to(ROOT)) for p in ASSETS], 'removedIsolatedCellsSeed0':len(removed),'bfsPathSteps':len(path)-1,
          'notes':'Figures are deterministic offline reconstructions with project tiles, plus explicitly labelled fixtures. No performance benchmark. Gameplay screenshot is historical S7 evidence.'}
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
print(target);print('Figures',len(list(FIG.glob('*.png'))),'removed cells',len(removed),'BFS steps',len(path)-1)
