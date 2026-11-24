from pathlib import Path
from shutil import copy2
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path.cwd(); OUT=ROOT/'Outputs/Midterm'; FIG=OUT/'guide_figures'; FIG.mkdir(exist_ok=True)
GUIDE=ROOT/'Docs/Modules/ProceduralGeneration/HowItWorks.md'
FONT='C:/Windows/Fonts/arial.ttf'; BOLD='C:/Windows/Fonts/arialbd.ttf'

def diagram(file, labels, cols=3):
    rows=(len(labels)+cols-1)//cols; w=1500; h=rows*180+30
    im=Image.new('RGB',(w,h),'white'); d=ImageDraw.Draw(im); font=ImageFont.truetype(FONT,29)
    for i,t in enumerate(labels):
        x=25+(i%cols)*500; y=20+(i//cols)*180
        d.rounded_rectangle((x,y,x+440,y+125),12,fill='#eef4f6',outline='#36576b',width=3)
        ls=t.split('\n')
        for j,l in enumerate(ls): d.text((x+220,y+62+(j-(len(ls)-1)/2)*37),l,font=font,anchor='mm',fill='#182f40')
        if i%cols<cols-1 and i+1<len(labels):
            d.line((x+445,y+63,x+485,y+63),fill='#36576b',width=4); d.polygon([(x+485,y+63),(x+472,y+55),(x+472,y+71)],fill='#36576b')
        elif i+1<len(labels):
            d.line([(x+220,y+125),(x+220,y+150),(245,y+150),(245,y+180)],fill='#36576b',width=3)
            d.polygon([(245,y+180),(237,y+168),(253,y+168)],fill='#36576b')
    im.save(FIG/file)

diagram('pipeline.png',['1  Seed và cấu hình','2  Noise tạo đất nước','3  Ba vùng và tuyến đi','4  BFS bỏ đất cô lập','5  Đặt đủ tài nguyên','6  Kiểm tra bản đồ cuối','7  MapData hợp lệ','8  Tilemap và prefab','9  Navigation rồi Ready'])
diagram('runtime.png',['MapData\ndữ liệu lưới và vị trí','Integration\nTilemap và prefab','ECS gameplay\nkhai thác và chiến đấu'],3)

im=Image.new('RGB',(1450,670),'white');d=ImageDraw.Draw(im)
f=ImageFont.truetype(FONT,28); fb=ImageFont.truetype(BOLD,30)
for a,b in [((350,330),(1050,330)),((350,330),(350,110))]:d.line([a,b],fill='#abbbc3',width=40)
for xy,col,title,sub in [((350,330),'#d9b768','START','TownHall và vùng an toàn'),((1050,330),'#78ac87','ECONOMY','Ưu tiên cây và cừu'),((350,110),'#c5a0d4','GOLD FRONTIER','Vàng và landmark')]:
 x,y=xy;d.rounded_rectangle((x-190,y-62,x+190,y+62),12,fill=col,outline='#344c59',width=3);d.text((x,y-16),title,font=fb,anchor='mm',fill='#172b35');d.text((x,y+27),sub,font=f,anchor='mm',fill='#172b35')
d.line((350,45,350,15),fill='#c55454',width=15);d.line((1245,330,1415,330),fill='#c55454',width=15)
d.text((1000,150),'Gateway là cửa ở biên bản đồ',font=f,anchor='mm',fill='#9e3434')
d.text((720,500),'Bố cục có thể xoay theo seed và lệch nhẹ vị trí vùng',font=f,anchor='mm',fill='#172b35')
d.text((720,550),'Hình minh họa quan hệ vùng   không phải kết quả seed cụ thể',font=f,anchor='mm',fill='#172b35')
im.save(FIG/'regions.png')

im=Image.new('RGB',(1400,550),'white');d=ImageDraw.Draw(im)
for offset,label,gold in [(40,'Một mỏ vàng và các ô tiếp cận',True),(760,'Đường bị cây chắn phải bị loại',False)]:
 for y in range(5):
  for x in range(5):
   c='#c5dce8' if x in (0,4) or y in (0,4) else '#e7c568'
   if not gold:c='#bccea9' if x==2 else '#eaf0f3'
   X=offset+x*87;Y=65+y*75;d.rectangle((X,Y,X+84,Y+72),fill=c,outline='white',width=2)
 d.text((offset+220,485),label,font=ImageFont.truetype(FONT,25),anchor='mm',fill='#203e50')
d.text((260,28),'Vàng chiếm 3 × 3 ô',font=f,anchor='mm',fill='#203e50');d.text((980,28),'Cần kiểm sau placement',font=f,anchor='mm',fill='#203e50')
im.save(FIG/'footprints.png')

def document(title, size=12.5):
 doc=Document();s=doc.sections[0];s.page_width=Cm(21);s.page_height=Cm(29.7)
 s.top_margin=Cm(1.7);s.bottom_margin=Cm(1.7);s.left_margin=Cm(2.25);s.right_margin=Cm(1.85)
 for name in ['Normal','Title','Heading 1','Heading 2','Caption','List Bullet']:
  st=doc.styles[name];st.font.name='Times New Roman';st.font.size=Pt(size);st.font.color.rgb=RGBColor(0,0,0)
  st.paragraph_format.space_after=Pt(6);st.paragraph_format.line_spacing=1.06
  fonts=st.element.get_or_add_rPr().get_or_add_rFonts()
  for k in list(fonts.attrib):
   if 'Theme' in k:del fonts.attrib[k]
  for k in ['ascii','hAnsi','eastAsia','cs']:fonts.set(qn('w:'+k),'Times New Roman')
 for st in doc.styles:
  if st.element.pPr is not None:
   for border in list(st.element.pPr.findall(qn('w:pBdr'))):st.element.pPr.remove(border)
 doc.styles['Title'].font.size=Pt(15);doc.styles['Title'].font.bold=True
 for name in ['Heading 1','Heading 2']:doc.styles[name].font.size=Pt(13);doc.styles[name].font.bold=True
 doc.styles['Caption'].font.size=Pt(10.5);doc.styles['Caption'].font.italic=True
 footer=s.footer.paragraphs[0];footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
 field=OxmlElement('w:fldSimple');field.set(qn('w:instr'),'PAGE');footer._p.append(field)
 doc.core_properties.author='Trần Ngọc Bin';doc.core_properties.title=title
 return doc

def para(doc,text,bold=False,style=None):
 p=doc.add_paragraph(style=style);p.add_run(text).bold=bold;return p
def bullet(doc,text):return para(doc,text,style='List Bullet')
def heading(doc,text):doc.add_heading(text,level=1)
def figure(doc,path,caption,width=16):
 p=doc.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.add_run().add_picture(str(path),width=Cm(width))
 p=doc.add_paragraph(caption,style='Caption');p.alignment=WD_ALIGN_PARAGRAPH.CENTER
def table(doc,heads,rows,widths):
 t=doc.add_table(rows=1,cols=len(heads));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.autofit=False
 for c,w in zip(t.columns,widths):c.width=Cm(w)
 for c,text in zip(t.rows[0].cells,heads):c.text=text
 repeat=OxmlElement('w:tblHeader');t.rows[0]._tr.get_or_add_trPr().append(repeat)
 for row in rows:
  for c,text in zip(t.add_row().cells,row):c.text=str(text)
 for i,row in enumerate(t.rows):
  for j,c in enumerate(row.cells):
   c.width=Cm(widths[j]);c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
   pr=c._tc.get_or_add_tcPr();borders=OxmlElement('w:tcBorders')
   for side in ['top','left','bottom','right']:
    b=OxmlElement('w:'+side);b.set(qn('w:val'),'single');b.set(qn('w:sz'),'4');b.set(qn('w:color'),'D9D9D9');borders.append(b)
   pr.append(borders);shade=OxmlElement('w:shd');shade.set(qn('w:fill'),'E9EFF3' if i==0 else ('F7F9FA' if i%2==0 else 'FFFFFF'));pr.append(shade)
   for p in c.paragraphs:
    p.paragraph_format.space_before=Pt(4);p.paragraph_format.space_after=Pt(4)
    for r in p.runs:r.font.size=Pt(11.5);r.bold=i==0
 return t

# Báo cáo rút gọn theo hai mẫu được cung cấp
report=document('Báo cáo giữa kỳ khóa luận tốt nghiệp')
p=report.add_paragraph();p.paragraph_format.space_after=Pt(3);p.paragraph_format.tab_stops.add_tab_stop(Cm(8.2))
p.add_run('TRƯỜNG ĐH CÔNG NGHỆ THÔNG TIN\tCỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM').bold=True
for r in p.runs:r.font.size=Pt(9)
p=report.add_paragraph();p.paragraph_format.tab_stops.add_tab_stop(Cm(9.2));p.add_run('KHOA CÔNG NGHỆ PHẦN MỀM\tĐộc lập – Tự do – Hạnh phúc').font.size=Pt(10)
p=para(report,'Ngày 24 tháng 11 năm 2026');p.alignment=WD_ALIGN_PARAGRAPH.RIGHT
p=report.add_paragraph('BÁO CÁO GIỮA KỲ KHÓA LUẬN TỐT NGHIỆP',style='Title');p.alignment=WD_ALIGN_PARAGRAPH.CENTER
para(report,'Tên đề tài: Nghiên cứu và Phát triển Game Chiến thuật Thời gian thực (RTS) kết hợp yếu tố Sinh tồn 2D trên nền tảng Unity.',True)
para(report,'Họ tên – Mã số sinh viên thực hiện: Trần Ngọc Bin – 23520150')
para(report,'Hệ đào tạo: ........................................................')
para(report,'Giảng viên hướng dẫn: ThS. Đinh Nguyễn Anh Dũng')
heading(report,'I  NHỮNG VIỆC ĐÃ HOÀN THÀNH')
para(report,'Tự đánh giá tiến độ toàn khóa luận khoảng 70%, còn khoảng 30%. Đây là ước lượng khối lượng công việc so với đề cương, không phải tỷ lệ kiểm thử thành công. Các hạng mục đang làm dở được ghi rõ bên dưới.')
completed=[
'Nghiên cứu Unity DOTS, ECS, C# Job System, Burst Compiler và kiến trúc kết hợp ECS với GameObject; xây dựng tài liệu kỹ thuật theo từng module.',
'Chuyển logic quái, đạn và các nghiệp vụ dân, quân, cừu, công trình, khai thác, xây dựng, huấn luyện sang ECS; tích hợp tìm đường, va chạm và phần hiển thị hiện có.',
'Triển khai sinh bản đồ procedural bằng Simplex noise nhiều lớp, ba vùng chiến thuật, hai gateway, phân bổ cây/vàng/cừu và kiểm tra kết nối sau placement. Quét 1.000 seed strategic và 1.000 seed legacy, không ghi nhận lỗi trong đợt kiểm Core đã lưu.',
'Triển khai sáu công nghệ thuộc ba nhánh Kinh tế, Quân sự và Mở rộng; có điều kiện tiên quyết, chi phí, thời gian nghiên cứu và hiệu ứng gameplay. Giao diện toàn màn hình hỗ trợ kéo, thu phóng và xem chi tiết node.',
'Có vòng chơi ngày – hoàng hôn – đêm, năm đêm phòng thủ, điều kiện thắng/thua, lịch wave chiến thuật và báo cáo bình minh. Bổ sung ánh sáng đêm, cảnh báo hoàng hôn, trang trí vùng và phản hồi hành động.',
'Save/Load đạt SL4: có JSON kiểm tra phiên bản, ID bền vững, ghi file kèm backup, capture trạng thái và tải lại map/thực thể bằng hai lượt nối tham chiếu. Đã kiểm tải trong process Windows mới với seed 0/1/1543; hiện chỉ hỗ trợ lát cắt đầu ngày, không có công việc đang chạy.',
'Có bản build Windows, kiểm thử tự động và dữ liệu profiling. Mốc SL4 ghi nhận 148/148 EditMode tests đạt; các số đo Release của E10 thuộc cấu hình và kịch bản đã kiểm, chưa thay thế đánh giá toàn bộ tải lớn trong đề cương.'
]
for x in completed:bullet(report,x)
report.add_page_break()
heading(report,'II  NHỮNG VIỆC CÒN CẦN THỰC HIỆN')
para(report,'Khối lượng còn lại ước tính khoảng 30%. Đối chiếu với các yêu cầu và mốc trong đề cương:')
remaining=[
'Mốc Lưu/Tải và Đo lường 16/11–22/11: hoàn thiện SL5–SL9 để tải được cargo, công việc của dân, xây dựng, hàng đợi huấn luyện, nghiên cứu, đạn và trạng thái giữa đêm; bổ sung rollback, giao diện Continue/Save/Load và tự lưu. SL4 chưa đáp ứng lưu/tải toàn trận bất kỳ lúc nào.',
'Mốc mở rộng gameplay và tech tree: mở rộng chiều sâu nội dung so với sáu tech hiện tại; các yêu cầu lính cấp cao như Pháp sư, Máy bắn đá và nâng cấp Nhà chính cần tiếp tục đối chiếu, triển khai hoặc thống nhất điều chỉnh phạm vi với giảng viên.',
'Mốc hoàn thiện vòng chơi: bổ sung Boss/Tinh anh, Blood Moon và cơ chế tấn công đặc biệt theo đề cương; hoàn thiện tutorial và cân bằng giữa mở rộng kinh tế với phòng thủ. Lịch wave hiện tại chưa thay thế hệ thống Boss Fight.',
'Mốc Đồ họa, Âm thanh và UI/UX 23/11–06/12: tiếp tục nghiệm thu hình ảnh, âm thanh, độ rõ giao diện và trải nghiệm người chơi; xử lý các vấn đề qua chơi thử thực tế.',
'Mốc Kiểm thử và Cân bằng cuối 07/12–20/12: kiểm thử dài, tải lớn, bộ nhớ và nhiều cấu hình máy; bổ sung so sánh OOP–DOTS có cùng điều kiện. Chưa kết luận toàn game đạt 60 FPS với hàng nghìn quái chỉ từ các bài smoke hoặc số đo tải nhỏ.',
'Mốc Phát hành và Báo cáo 21/12–26/12: đóng gói bản PC cuối, hoàn thiện báo cáo khóa luận, số liệu, trích dẫn, slide và video demo; bản phát hành công khai chưa hoàn thành.'
]
for x in remaining:bullet(report,x)
heading(report,'III  THUẬN LỢI VÀ KHÓ KHĂN')
bullet(report,'Thuận lợi: Có bộ khung RTS từ giai đoạn trước; mã nguồn được chia module, có kiểm thử và dữ liệu kiểm chứng để tái hiện các tình huống lỗi.')
bullet(report,'Khó khăn: Cần đồng bộ logic ECS với hiển thị GameObject; map ngẫu nhiên phải vừa đủ tài nguyên vừa có đường đi. Save/Load phải giữ đúng liên kết và tiến độ; cân bằng gameplay, kiểm thử tải lớn và đo trên nhiều máy còn cần thời gian.')
para(report,'Xác nhận của giảng viên hướng dẫn',True)
para(report,'☐ Đồng ý cho sinh viên thực hiện tiếp KLTN\n☐ Không đồng ý cho sinh viên thực hiện tiếp KLTN')
p=para(report,'GIẢNG VIÊN HƯỚNG DẪN KÝ TÊN                    SINH VIÊN KÝ TÊN',True)
p.alignment=WD_ALIGN_PARAGRAPH.CENTER
para(report,'\nThS. Đinh Nguyễn Anh Dũng                                      Trần Ngọc Bin')
report_path=OUT/'Bao_cao_giua_ky_KLTN_Tran_Ngoc_Bin_23520150_Rut_gon.docx';report.save(report_path)

# Tài liệu giải thích riêng và bản Markdown dùng trong repo
guide=document('Cách sinh bản đồ procedural trong RTSgame2026',12.5)
md=['# Cách sinh bản đồ procedural trong RTSgame2026\n','Cập nhật 24/11/2026. Đối chiếu Core strategic v3, Integration hiện tại và Save/Load SL4.\n']
def gh(t):heading(guide,t);md.append('\n## '+t+'\n')
def gp(t):para(guide,t);md.append(t+'\n')
def gb(t):bullet(guide,t);md.append('- '+t)
def gf(name,caption,width=16):
 figure(guide,FIG/name,caption,width);md.append('\n!['+caption+'](../../../..'+'/Outputs/Midterm/guide_figures/'+name+')\n')
def newpage():guide.add_page_break()
p=guide.add_paragraph('Cách sinh bản đồ procedural trong RTSgame2026',style='Title')
gp('Tài liệu giải thích cách game tạo một bản đồ mới có đất, nước, vùng chiến thuật, tài nguyên và đường đi hợp lệ. Người đọc có thể theo dõi từ ý tưởng đến mã nguồn mà không cần biết trước về Unity DOTS. Phạm vi là preset StrategicRegions v3; LegacyMainland là nhánh cũ để đối chiếu.')
gh('1  Một bản đồ được tạo như thế nào')
gp('Có thể hình dung hệ thống như người lập quy hoạch: trước hết tạo địa hình tự nhiên, sau đó dành chỗ cho căn cứ và đường chính, đặt tài nguyên rồi kiểm tra người chơi có thật sự đi tới được hay không. Ngẫu nhiên được kiểm soát bởi luật chơi.')
gf('pipeline.png','Hình 1  Pipeline New Game đọc từ trái sang phải từng hàng')
gp('Ba hàng trong hình là ba giai đoạn nối tiếp: tạo dữ liệu thô, hoàn thiện và kiểm tra dữ liệu, rồi đưa dữ liệu vào Unity. Nếu bước 6 không đạt, hệ thống quay về bước 2 với attempt kế tiếp; chưa đưa map lỗi vào scene.')
gb('Seed là con số đầu vào cho bộ sinh số giả ngẫu nhiên. Cùng seed, cấu hình và phiên bản thuật toán cho cùng MapData trong phạm vi môi trường đã kiểm.')
gb('Preset tham chiếu có lưới 128 × 128 cell. Một cell là một ô lưới. Terrain cho biết ô là cỏ hay nước; Reserved là mặt nạ giữ chỗ; placement là bản ghi vị trí tài nguyên.')
gb('MapData là bản thiết kế logic. Tilemap và prefab là cách Unity biểu diễn bản thiết kế đó; không tạo một Entity cho từng ô đất.')
newpage();gh('2  Tạo đất nước và bố trí ba vùng')
gp('Bước đầu dùng noise.snoise của Unity.Mathematics, tức Simplex noise. Hàm này tạo giá trị biến thiên liên tục giữa các ô, nhờ đó đất và nước kết thành mảng thay vì rải ô ngẫu nhiên rời rạc. Đề cương nêu Perlin Noise hoặc Cellular Automata; thuật toán thực tế được triển khai là Simplex noise nhiều octave.')
gp('Mỗi octave là một lớp chi tiết. Lớp sau có tần số cao hơn và biên độ nhỏ hơn. Với preset tham chiếu: 4 lớp, NoiseScale 34, Persistence 0,5, Lacunarity 2. Sau khi lấy trung bình có trọng số, ô có giá trị từ −0,22 trở lên thành cỏ; thấp hơn thành nước. Tăng ngưỡng nước thường làm giảm diện tích đất trước bước sửa map.')
gp('Noise chỉ tạo hình dạng nền. StrategicLayout tiếp tục chọn một trong bốn hướng xoay, đặt hai vùng ngoài theo hai hướng vuông góc và lệch nhẹ vị trí. Mỗi ô nhận nhãn của tâm vùng gần nhất; đây là cách phân vùng đơn giản, chưa phải một bộ sinh Voronoi đầy đủ.')
gf('regions.png','Hình 2  Quan hệ giữa ba vùng và hai gateway',16)
gb('Start chứa TownHall và khoảng trống đầu trận. Economy ưu tiên gỗ, thịt; GoldFrontier ưu tiên vàng và landmark Old Watchpost ở phần hiển thị.')
gb('Hai nhánh nối từ Start ra vùng ngoài rồi tới gateway ở biên. Đường được vẽ theo chữ L, rộng 5 ô khi half-width bằng 2; các đoạn đường và pad được giữ chỗ để tài nguyên không mọc đè lên.')
gb('Biên strategic có dải nước dày 2 ô trước khi mở gateway. Noise vẫn có thể tạo đường phụ bên trong; hai gateway không có nghĩa toàn bản đồ chỉ tồn tại đúng hai đường đi độc quyền.')
newpage();gh('3  Đặt tài nguyên theo nhu cầu chơi')
gp('BFS là phép duyệt lan từ căn cứ sang bốn ô kề. Nó đánh dấu đất đi tới được và cho biết số bước đường đi tới từng ô. Đất cô lập bị đổi thành nước. Khoảng BFS giúp ưu tiên tài nguyên khởi đầu theo đường đi thay vì chỉ đo đường thẳng xuyên qua hồ.')
gp('Hệ thống đặt tài nguyên gần căn cứ trước, sau đó mới phân bổ phần còn lại theo vai trò vùng. Quota là số đối tượng cần đặt, không phải số đơn vị gỗ/vàng/thịt người chơi sẽ nhận. Bảng dưới là preset 120 cây, 12 mỏ vàng và 18 cừu.')
rows=[['Khởi đầu','30','3','6'],['Economy ngoài khởi đầu','68','0','9'],['GoldFrontier ngoài khởi đầu','22','9','3'],['Tổng','120','12','18']]
table(guide,['Nhóm placement','Cây','Mỏ vàng','Cừu'],rows,[9,2.3,2.3,2.3])
md.append('\n|Nhóm|Cây|Mỏ vàng|Cừu|\n|---|---:|---:|---:|\n'+'\n'.join('|'+ '|'.join(r)+'|' for r in rows)+'\n')
gp('75% cây và cừu ngoài khởi đầu vào Economy, làm tròn lên; toàn bộ vàng ngoài khởi đầu vào GoldFrontier. Quota khởi đầu được chọn theo khoảng BFS, không buộc mọi node gần căn cứ phải mang nhãn Start. Ba mỏ quanh landmark nằm trong tổng 12 mỏ, không phải ba mỏ cộng thêm.')
gf('footprints.png','Hình 3  Kiểm footprint và đường tiếp cận sau khi đặt tài nguyên',16)
gb('Cây chặn 1 ô; mỏ vàng chiếm 3 × 3 ô và cần ô tương tác trên vòng ngoài. Cừu không chặn đường trong phép kiểm topology nhưng vẫn kiểm vị trí và chồng lấn placement.')
gb('Cây và vàng xa căn cứ được lấy mẫu quanh tâm cụm; cừu lấy mẫu trên vùng mở. Sáu VariantId vàng chỉ chọn sáu hình ảnh của cùng loại Gold, không tạo sáu tài nguyên khác nhau.')
newpage();gh('4  Kiểm tra bản đồ trước khi cho chơi')
gp('Một map trông có vẻ thông thoáng vẫn có thể không chơi được: mỏ vàng chắn kín hành lang, lối chỉ nối chéo góc hoặc tài nguyên nằm trên đất nhưng không có ô nào cho dân đứng khai thác. Vì vậy hệ thống kiểm lại trên terrain cộng footprint tài nguyên sau cùng.')
for t in [
'Kiểm đủ quota và footprint nằm trên đất, không trùng tài nguyên khác hoặc Reserved.',
'Chạy BFS bốn hướng với vật cản cuối để bảo đảm có đường tới ô tương tác của từng tài nguyên. Hai ô chạm góc không được coi là có lối nối trực tiếp.',
'Kiểm khoảng trống cho tác nhân: co vùng đi được bằng footprint 3 × 3, tương đương cần thêm 1 ô trống quanh tâm, rồi duyệt tới gateway và landmark.',
'Kiểm pad đầu trận và pad landmark còn trống; thử footprint công trình 5 × 5 và đường thoát ở vòng bán kính 3. Footprint Core là quy ước lưới, cần được đối chiếu collider thật ở Integration.',
'Kiểm cả hai gateway tới được và không xuất hiện cửa biên đi tới được ngoài vùng gateway đã đăng ký.'
]:gb(t)
gp('Ví dụ dễ hình dung: BFS giống vết mực lan từ TownHall. Ô nước và ô bị cây/vàng chặn không nhận mực. Nếu mực không tới được vòng ngoài của một mỏ, map bị từ chối dù mỏ đó nằm rất gần căn cứ trên màn hình.')
gh('Vì sao cần attempt và seed riêng')
gp('Không phải bố trí ngẫu nhiên nào cũng đủ chỗ. Khi validation thất bại, generator thử attempt tiếp theo; mặc định tối đa 8 lần. Mỗi nhóm terrain, layout và resource dùng seed con được suy ra từ seed gốc, mã phase và attempt. Vì thế có thể tái hiện đúng lần thử lỗi mà không phụ thuộc số lần gọi random của UI.')
gp('Mỗi lần đặt tài nguyên cũng có giới hạn số phép thử, mặc định 12.000 cho một lời gọi PlaceQuota. Hệ thống không lặp vô hạn và không tự giảm quota để báo thành công. Hết attempt thì trả lỗi; authoring ghi lỗi và xử lý nhánh thất bại theo scene, không xác nhận procedural Ready thành công.')
gp('Khoảng cách tài nguyên khởi đầu được chọn trên terrain trước placement. Validator cuối bảo đảm vẫn tiếp cận được, nhưng không cam kết mọi khoảng cách sau khi có vật cản giữ nguyên giới hạn ban đầu. Cân bằng nhịp kinh tế vẫn cần chơi thử.')
newpage();gh('5  Đưa MapData vào Unity và gameplay')
gf('runtime.png','Hình 4  Dữ liệu sinh map được chuyển sang phần hiển thị và gameplay')
gp('ProceduralMapAuthoring kiểm cấu hình, chạy generator rồi ghi terrain theo batch vào Tilemap: cỏ vào lớp đi được, nước vào lớp nước. Foam được đặt quanh vùng bờ. Authoring tạo các prefab cây, vàng, cừu theo placement và vô hiệu spawner cũ để tránh sinh trùng.')
gp('Sau đó Physics2D.SyncTransforms cập nhật collider; TilemapManager.ReinitializePathfinding dựng lại dữ liệu tìm đường. GameSceneCompositionRoot nối dependency, đăng ký thực thể/tài nguyên qua bridge và chỉ hoàn tất composition khi các thành phần sẵn sàng. Đồng hồ và wave được chặn trong lúc sinh, tránh quái xuất hiện khi map chưa chuẩn bị xong.')
gp('Core chỉ tính dữ liệu, không điều khiển nông dân hoặc chiến đấu. Integration chuyển cell sang tọa độ world và tạo đối tượng phù hợp. ECS giữ trạng thái gameplay; các GameObject có thể chỉ làm phần hiển thị. Renderer trang trí không được thay thế dữ liệu terrain hay collider logic.')
gp('EnemyWaveManager dùng gateway và tuyến tiếp cận, nhưng điểm spawn thực tế được chọn trên tuyến ở bán kính khởi đầu đã định để giữ thời gian tiếp cận. Không diễn giải gateway ở mép map thành việc mọi quái đều sinh ngay tại mép.')
gh('New Game và Load Game khác nhau ở đâu')
gp('New Game chạy seed → generator → MapData → scene. Trong nhánh Load Game của SL4, authoring đọc terrain, Reserved, topology, origin và cell size đã lưu; không gọi generator và không SpawnResources lần nữa. Bridge tạo lại đối tượng theo ID rồi nối tham chiếu ở lượt hai. Tài nguyên đã mất không mọc lại chỉ vì seed vẫn giữ nguyên.')
gp('Seed mô tả bản đồ ban đầu, không mô tả đầy đủ trận đang chơi. Sau khai thác, xây dựng hoặc thay đổi terrain, phải lưu trạng thái thực tế. SL4 đã kiểm tải map và thực thể trong process mới ở lát cắt đầu ngày; cargo/công việc/research/đạn đang chạy và giữa đêm vẫn bị từ chối đến các mốc sau.')
gh('Lớp hình ảnh được tách khỏi luật chơi')
gp('Màu vùng, cỏ/hoa, landmark, ánh sáng đêm và sắp xếp chiều sâu đọc map để hiển thị. Presentation có stream seed riêng và không sửa quota, đường đi hoặc Reserved. Một nguồn sáng trang trí không tự mở fog hoặc tăng tầm nhìn gameplay.')
newpage();gh('6  Cách xem và kiểm chứng trong dự án')
preview=ROOT/'Docs/Modules/Progression/S5-preview-seed0.png'
figure(guide,preview,'Hình 5  Preview Core strategic v3 seed 0 đã lưu ở mốc S5',7)
md.append('\n![Preview strategic v3 seed 0](../Progression/S5-preview-seed0.png)\n')
gp('Trong preview: xanh dương là nước; vùng Start/Economy/GoldFrontier có màu phân biệt; đỏ là gateway, tím là landmark, đường sáng là route và các chấm biểu diễn tài nguyên. Đây là ảnh dữ liệu Core ở mốc S5, không phải ảnh gameplay hoặc hình ảnh thẩm mỹ mới nhất.')
gb('Mở RTS → Procedural Generation → Preview Regions, chọn StrategicRegions, nhập seed 0, 1 hoặc 1543 rồi sinh preview. Cửa sổ dùng dữ liệu sao chép; không cần sinh lại scene đang chỉnh để xem thử.')
gb('Muốn chơi, mở ProceduralGame hoặc vào từ MainMenu. Kiểm asset thật được gán ở ProceduralMapAuthoring; scene generator dùng StrategicProceduralMapSettings.asset. DefaultProceduralMapSettings.asset là asset riêng, không được dùng để kết luận preset của scene hiện tại.')
gb('Khi báo lỗi, ghi seed, attempt, preset, generator version, cấu hình, log và ảnh. Cùng seed nhưng khác settings hoặc thuật toán có thể sinh map khác. Việc seed mới có được chọn tự động hay không thuộc luồng khởi tạo trận, không phải cam kết của generator.')
gp('Bằng chứng hiện có: S5 quét 1.000 strategic + 1.000 legacy, mỗi seed sinh lại và so dữ liệu, không ghi nhận lỗi; S6 kiểm tích hợp navigation/wave/kinh tế; bộ kiểm SL4 có 18/18 procedural tests đạt. Đây là kết quả đã lưu, không phải phép chạy mới trong phiên soạn tài liệu.')
gp('Chưa kết luận mọi seed/cấu hình đều hợp lệ hoặc sinh map không gây khựng. Đo hiệu năng cần tách thời gian Core, ghi Tilemap, tạo prefab, đồng bộ physics/navigation và scene đến Ready. Số đo Core .NET không thay cho thời gian tải bản đồ trong Player. Chưa có streaming/vô hạn, nhiều biome phức tạp hoặc bảo đảm cân bằng chỉ từ validation.')
gh('Mã nguồn và tài liệu để đọc tiếp')
for t in ['Assets/Game/ProceduralGeneration/Core: ProceduralMapGenerator, DeterministicRandom, StrategicLayout, StrategicResources.', 'Assets/Game/ProceduralGeneration/Integration: ProceduralMapAuthoring và ProceduralMapSettings.', 'Docs/Modules/ProceduralGeneration: S4, S5, S6, S7; Docs/Modules/SaveLoad/SL4-Report.md.']:gb(t)
guide_path=OUT/'Giai_thich_Procedural_Generation_RTSgame2026.docx';guide.save(guide_path)
GUIDE.write_text('\n'.join(md)+'\n',encoding='utf8')

notes=OUT/'Doi_chieu_bao_cao_rut_gon.md'
notes.write_text('''# Căn cứ biên soạn báo cáo giữa kỳ rút gọn

Ngày 24/11/2026. Bản cũ được giữ nguyên; bản mới có hậu tố Rut_gon. Tài liệu sinh bởi build_concise_and_guide.py.

- Mẫu hình thức: hai BCGK do người dùng cung cấp, mỗi mẫu 2 trang, dùng mục I việc đã làm, II việc còn lại, III thuận lợi và khó khăn, xác nhận và chữ ký. Không sao chép nội dung nghiên cứu, danh tính hoặc tỷ lệ của người khác.
- Đề cương: DC KLTN0127 23520150_DinhNguyenAnhDung.pdf, nhất là nội dung trang 2 và tiến độ trang 5–6; giữ đúng tên đề tài, sinh viên, giảng viên. Hệ đào tạo không được xác minh nên để trống.
- Tiến độ SaveLoad: Docs/Modules/SaveLoad/Progress.md, SL4-Report.md và Validation.md. SL4 là lát cắt đầu ngày/idle, không phải Save/Load toàn trận; không suy 5/10 mốc thành đúng 50% công sức.
- Procedural: source Core và Integration; S4, S5, S6 final. Thuật toán thực tế là Simplex noise, không đổi tên thành Perlin/Cellular Automata để khớp đề cương.
- Tech tree: ResearchTreeUI.md và Progression Architecture. Sáu tech, ba chuỗi hai cấp; không ghi đã có cây lớn, Pháp sư, Máy bắn đá hoặc nâng cấp Hall khi chưa có bằng chứng.
- Gameplay và đo lường: TechnicalOverview.md, E8–E10 và DOTS/Validation.md. Boss/Blood Moon/tutorial chưa có bằng chứng hoàn thành; wave chiến thuật không đồng nghĩa Boss Fight. Smoke swarm offscreen lịch sử không là chứng nhận FPS toàn game hiện tại.
- 70% là ước lượng biên tập về khối lượng toàn khóa luận dựa trên phần nền đã triển khai, còn Save/Load sâu, nội dung đề cương, đánh giá và sản phẩm cuối. Không là số đo, không là xác nhận của giảng viên, không tính bằng tỷ lệ test PASS. Cần điều chỉnh nếu giảng viên dùng trọng số khác.
- Kết quả 148/148 tests và 3 seed cold-process lấy từ evidence SL4 đã có, không chạy Unity hoặc đo hiệu năng lại trong phiên viết tài liệu.
''',encoding='utf8')
print(report_path);print(guide_path);print(GUIDE)
