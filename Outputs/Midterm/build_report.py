from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path.cwd(); OUT=ROOT/'Outputs/Midterm'; FIG=OUT/'figures'; FIG.mkdir(exist_ok=True)
FONT='C:/Windows/Fonts/arial.ttf'; BOLD='C:/Windows/Fonts/arialbd.ttf'
def diagram(name, nodes, edges, size=(1500,650)):
    im=Image.new('RGB',size,'white'); d=ImageDraw.Draw(im)
    for a,b in edges:
        x,y,w,h,_=nodes[a]; X,Y,W,H,_=nodes[b]
        dx=X+W/2-(x+w/2); dy=Y+H/2-(y+h/2)
        if abs(dx)>abs(dy):
            start=(x+w if dx>0 else x,y+h/2); end=(X if dx>0 else X+W,Y+H/2)
        else:
            start=(x+w/2,y+h if dy>0 else y); end=(X+W/2,Y if dy>0 else Y+H)
        d.line([start,end], fill='#637582',width=5)
        ex,ey=end; angle=math.atan2(ey-start[1],ex-start[0]); ux,uy=math.cos(angle),math.sin(angle)
        d.polygon([(ex,ey),(ex-17*ux+8*uy,ey-17*uy-8*ux),(ex-17*ux-8*uy,ey-17*uy+8*ux)],fill='#637582')
    for x,y,w,h,t in nodes:
        d.rounded_rectangle((x,y,x+w,y+h),radius=12,fill='#eef3f6',outline='#294e65',width=3)
        lines=t.split('\n'); font=ImageFont.truetype(FONT,28)
        for k,line in enumerate(lines): d.text((x+w/2,y+h/2+(k-(len(lines)-1)/2)*37),line,font=font,fill='#172f40',anchor='mm')
    im.save(FIG/name)
diagram('architecture.png',[(30,40,420,130,'UI và GameObject\nhiển thị và nhận thao tác'),(540,40,420,130,'Integration và DI\ncommand và snapshot'),(1050,40,420,130,'ECS simulation\nwallet, actor, research'),(540,330,420,130,'Procedural Core\nseed và MapData'),(1050,330,420,130,'Tilemap và navigation\náp dữ liệu vào scene')],[(0,1),(1,2),(3,4)],(1500,520))
diagram('pipeline.png',[(40,40,420,100,'Seed và settings'),(540,40,420,100,'Noise nhiều octave'),(1040,40,420,100,'Carve vùng và tuyến'),(1040,250,420,100,'BFS và đặt tài nguyên'),(540,250,420,100,'Validate topology cuối'),(40,250,420,100,'MapData hợp lệ'),(540,470,420,100,'Không hợp lệ: attempt mới\nkhông giảm quota')],[(0,1),(1,2),(2,3),(3,4),(4,5),(4,6)],(1500,620))
diagram('tree.png',[(565,20,370,90,'TownHall / node gốc'),(40,180,420,90,'KINH TẾ'),(540,180,420,90,'QUÂN SỰ'),(1040,180,420,90,'MỞ RỘNG'),(40,330,420,90,'Tools: gỗ mỗi nhịp ×1,25'),(540,330,420,90,'Archery: mở cung thủ'),(1040,330,420,90,'Outposts: mở tháp'),(40,490,420,90,'Cargo: sức chứa ×1,5'),(540,490,420,90,'Training: thời gian ×0,8'),(1040,490,420,90,'Fortify: damage ×1,25')],[(0,1),(0,2),(0,3),(1,4),(2,5),(3,6),(4,7),(5,8),(6,9)],(1500,620))
diagram('research.png',[(40,30,420,100,'Người chơi chọn công nghệ'),(540,30,420,100,'StartResearch\nkiểm Hall, slot, prerequisite'),(1040,30,420,100,'Kiểm số dư\ntrước debit'),(1040,250,420,100,'Trừ ví một lần\nActive và Elapsed = 0'),(540,250,420,100,'Tick thời gian simulation\npause: không tăng tiến độ'),(40,250,420,100,'Hoàn tất: bật UnlockMask\nđọc modifier hiệu dụng'),(540,460,420,100,'Hall mất: hủy nghiên cứu\nkhông hoàn tiền')],[(0,1),(1,2),(2,3),(3,4),(4,5),(4,6)],(1500,610))

doc=Document(); sec=doc.sections[0]
sec.page_width=Cm(21); sec.page_height=Cm(29.7)
sec.top_margin=Cm(1.8); sec.bottom_margin=Cm(1.8); sec.left_margin=Cm(2.5); sec.right_margin=Cm(2)
sec.header_distance=Cm(.7); sec.footer_distance=Cm(.8)
for n in ['Normal','Title','Heading 1','Heading 2','Heading 3','Caption']:
    st=doc.styles[n]; st.font.name='Times New Roman'; st.font.color.rgb=RGBColor(0,0,0)
    for key in ['ascii','hAnsi','eastAsia','cs']:
        st.element.get_or_add_rPr().get_or_add_rFonts().set(qn('w:'+key),'Times New Roman')
    st.font.size=Pt(13 if n=='Normal' else 14)
    st.paragraph_format.space_after=Pt(7); st.paragraph_format.line_spacing=1.15
doc.styles['Normal'].paragraph_format.widow_control=True
doc.styles['Title'].font.size=Pt(15); doc.styles['Title'].font.bold=True
doc.styles['Heading 1'].font.size=Pt(14); doc.styles['Heading 2'].font.size=Pt(13)
doc.styles['Caption'].font.size=Pt(11); doc.styles['Caption'].font.italic=True
doc.styles['Caption'].font.bold=False
footer=sec.footer.paragraphs[0]; footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
field=OxmlElement('w:fldSimple'); field.set(qn('w:instr'),'PAGE'); footer._p.append(field)
doc.core_properties.author='Trần Ngọc Bin'; doc.core_properties.title='Báo cáo giữa kỳ khóa luận tốt nghiệp RTSgame2026'
def p(t,bold=False):
    a=doc.add_paragraph(); a.alignment=WD_ALIGN_PARAGRAPH.JUSTIFY
    a.add_run(t).bold=bold; return a
def h(t): doc.add_heading(t,level=2)
def page(t): doc.add_page_break(); h(t)
def fig(path,caption,width=16):
    a=doc.add_paragraph(); a.alignment=WD_ALIGN_PARAGRAPH.CENTER
    a.add_run().add_picture(str(path),width=Cm(width))
    c=doc.add_paragraph(caption,style='Caption'); c.alignment=WD_ALIGN_PARAGRAPH.CENTER
def table(headers,rows,widths=None):
    t=doc.add_table(rows=1,cols=len(headers)); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.style='Table Grid'
    for c,v in zip(t.rows[0].cells,headers): c.text=v
    repeat=OxmlElement('w:tblHeader'); t.rows[0]._tr.get_or_add_trPr().append(repeat)
    for row in rows:
        for c,v in zip(t.add_row().cells,row): c.text=str(v)
    for ri,row in enumerate(t.rows):
        pr=row._tr.get_or_add_trPr(); pr.append(OxmlElement('w:cantSplit'))
        for ci,c in enumerate(row.cells):
            c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if widths: c.width=Cm(widths[ci])
            for a in c.paragraphs:
                a.paragraph_format.space_after=Pt(5); a.paragraph_format.space_before=Pt(5); a.paragraph_format.line_spacing=1.05
                for r in a.runs: r.font.size=Pt(11); r.bold=(ri==0)
    doc.add_paragraph().paragraph_format.space_after=Pt(0)

t=doc.add_table(rows=1,cols=2); t.autofit=False
texts=['TRƯỜNG ĐH CÔNG NGHỆ THÔNG TIN\nKHOA CÔNG NGHỆ PHẦN MỀM','CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nĐộc lập - Tự do - Hạnh phúc']
for c,txt in zip(t.rows[0].cells,texts):
    c.text=txt
    for a in c.paragraphs:
        a.alignment=WD_ALIGN_PARAGRAPH.CENTER
        for r in a.runs:r.font.size=Pt(10);r.bold=True
a=doc.add_paragraph('TP. Hồ Chí Minh, ngày 24 tháng 11 năm 2026');a.alignment=WD_ALIGN_PARAGRAPH.RIGHT
a=doc.add_paragraph('BÁO CÁO GIỮA KỲ\nKHÓA LUẬN TỐT NGHIỆP',style='Title');a.alignment=WD_ALIGN_PARAGRAPH.CENTER
p('Tên đề tài: Nghiên cứu và Phát triển Game Chiến thuật Thời gian thực (RTS) kết hợp yếu tố Sinh tồn 2D trên nền tảng Unity.',True)
p('Họ tên - Mã số sinh viên thực hiện: Trần Ngọc Bin - 23520150')
p('Hệ đào tạo: ............................................................')
p('Giảng viên hướng dẫn: ThS. Đinh Nguyễn Anh Dũng')
p('Thời gian thực hiện theo đề cương: 07/09/2026 đến 26/12/2026.\nPhạm vi báo cáo: kết quả đã có bằng chứng đến ngày 24/11/2026.')
doc.add_heading('I. NHỮNG VIỆC ĐÃ HOÀN THÀNH',level=1)
p('Tỷ lệ hoàn thành: ..........% (chờ sinh viên và giảng viên hướng dẫn xác nhận).')
h('1. Tổng quan kết quả giữa kỳ')
p('Khóa luận kế thừa bộ khung RTS 2D từ Đồ án 2 và tập trung mở rộng khả năng mô phỏng, sinh nội dung và phát triển chiến thuật. Đến thời điểm báo cáo, sản phẩm đã có bản chơi được trên Windows với vòng khai thác tài nguyên, xây dựng, huấn luyện và phòng thủ qua năm đêm. Hai nội dung trọng tâm được trình bày là sinh bản đồ thủ tục có ràng buộc và hệ thống cây công nghệ quản lý trạng thái nghiên cứu trong ECS.')
p('Báo cáo đối chiếu tiến độ với đề cương đã đăng ký [1]. Mốc “Hệ thống Lưu/Tải và Đo lường” dự kiến từ 16 đến 22/11/2026 được tách thành hai nhiệm vụ độc lập: đo lường đã có kết quả thực nghiệm, còn lưu và khôi phục toàn bộ trận đấu chưa có bằng chứng triển khai. Các nội dung hình ảnh hoàn thành sớm hơn lịch cuối kỳ được ghi nhận bổ sung, không dùng để thay thế nhiệm vụ lưu/tải.')

page('2. Kiến trúc hệ thống và nền tảng mô phỏng')
p('Sản phẩm sử dụng Unity 6000.3.11f1, URP 2D và các gói Entities, Jobs, Burst, Collections, Mathematics theo cấu hình của dự án. Kiến trúc kết hợp ECS với GameObject presentation cho phép giữ prefab, sprite, Animator và giao diện đang có, đồng thời chuyển logic gameplay sang cấu trúc dữ liệu và system chuyên trách [2].')
fig(FIG/'architecture.png','Hình 1. Ranh giới kiến trúc và đường trao đổi dữ liệu chính. Sơ đồ dựng từ implementation của dự án.')
p('ECS là nguồn trạng thái chính đối với vị trí, máu, mục tiêu, công việc, hàng mang, ví tài nguyên, xây dựng, hàng đợi huấn luyện và nghiên cứu. GameObject phản chiếu kết quả mô phỏng và phát lệnh thông qua Integration. Do đó, sự hiện diện của một Unit trong Hierarchy không đồng nghĩa logic của Unit đó vẫn chạy độc lập trên MonoBehaviour.')
p('Đối với quái, flow field cung cấp hướng di chuyển dùng chung; spatial hash giới hạn tập ứng viên lân cận và va chạm. Với dân, quân người chơi và cừu, hệ thống sử dụng đường đi A* riêng phù hợp nhiệm vụ. Job System và Burst được áp dụng trong các phần tính toán tương ứng. Renderer quái và đạn sử dụng instancing riêng; chưa thể đồng nhất implementation này với Entities Graphics cho toàn dự án.')
p('Composition root cấp dependency cho các consumer qua khởi tạo tường minh. Runtime assembly RTS.DOTS độc lập với các lớp gameplay MonoBehaviour; cầu nối ở Integration. Cách tổ chức này giúp giới hạn trường hợp hai bên cùng cập nhật máu, tài nguyên hoặc tiến độ nghiên cứu. Đây cũng là cơ sở kiểm thử command bằng trạng thái ECS thay vì chỉ quan sát hình ảnh.')
h('2.1. Luồng khởi động')
p('Map được sinh và áp dụng trước khi các hệ thống phụ thuộc bắt đầu. Sau khi Tilemap, collider và navigation đã đồng bộ, bridge bind thực thể và tài nguyên; composition root mới mở trạng thái Ready. Thứ tự này tránh việc wave, người chơi hoặc worker nhận lệnh trên một bản đồ chưa hoàn chỉnh.')

page('3. Sinh bản đồ thủ tục có ràng buộc')
h('3.1. Bài toán và biểu diễn dữ liệu')
p('Sinh bản đồ trong RTS cần đồng thời thỏa hai mục tiêu: tạo sự khác biệt giữa các lần chơi và bảo đảm các tương tác cơ bản vẫn thực hiện được. Một địa hình có hình thức tự nhiên nhưng cô lập TownHall, chặn mỏ vàng hoặc bịt gateway sẽ không đáp ứng yêu cầu gameplay. Vì vậy, module kết hợp noise với cấu trúc chiến thuật, bố trí có điều kiện và bước kiểm tra kết quả [3].')
p('Preset chiến thuật hiện tại dùng lưới 128 × 128 ô. Đầu vào gồm seed, kích thước, tham số noise, bán kính vùng khởi đầu, độ rộng hành lang và quota tài nguyên. MapData lưu Terrain, Reserved, ResourcePlacement, vị trí khởi đầu, layout vùng và báo cáo validation. Core chỉ phụ thuộc dữ liệu và Unity.Mathematics, không cần prefab hoặc scene.')
fig(FIG/'pipeline.png','Hình 2. Pipeline sinh map. Nhánh không hợp lệ tạo attempt kế tiếp trong giới hạn cấu hình.')
h('3.2. Tính xác định và seed theo giai đoạn')
p('Bộ sinh số giả ngẫu nhiên sử dụng trạng thái uint và phép biến đổi xorshift; seed con được suy ra từ seed gốc, định danh phase và chỉ số attempt. Terrain, layout và tài nguyên có luồng ngẫu nhiên riêng. Với cùng seed, settings và phiên bản generator, kết quả MapData có thể được sinh lại để kiểm lỗi. Tính xác định này áp dụng cho dữ liệu map, không đồng nghĩa toàn bộ combat và PlayerLoop là một deterministic replay.')
p('Khi validation thất bại, generator không tự giảm quota để chấp nhận map thiếu tài nguyên. Thay vào đó, attempt mới được sinh theo seed con khác, tối đa tám attempt trong cấu hình hiện hành. Nếu vẫn không đạt, hệ thống trả lỗi kèm thông tin seed và nguyên nhân, thay vì vòng lặp không giới hạn.')

page('3.3. Địa hình noise và cấu trúc vùng chiến thuật')
p('Implementation dùng Simplex noise thông qua noise.snoise, kết hợp bốn octave. Biên độ giảm theo persistence 0,5 và tần số tăng theo lacunarity 2. Tổng đóng góp được chuẩn hóa theo tổng biên độ, sau đó so với ngưỡng nước −0,22 để phân loại cỏ và nước. NoiseScale hiện tại là 34. Các giá trị này là cấu hình thiết kế, chưa phải nghiệm tối ưu cho mọi seed [3, 7].')
p('Đề cương nêu Perlin Noise hoặc Cellular Automata như các hướng nghiên cứu. Lựa chọn Simplex noise trong hiện thực vẫn giải quyết mục tiêu tạo địa hình dựa trên hàm nhiễu, đồng thời tận dụng thư viện Mathematics đã có. Báo cáo ghi rõ sự điều chỉnh thuật toán; không mô tả implementation như đang sử dụng Cellular Automata. Việc tạo hình bằng noise và việc bảo đảm bản đồ chơi được được xử lý ở những bước riêng [8].')
fig(ROOT/'Docs/Modules/Progression/S5-preview-seed0.png','Hình 3. Preview strategic version 3, seed 0. Xanh lục: Start; vàng: Economy; cam: GoldFrontier. Dải xám là tuyến giữ chỗ, cửa biên màu đỏ. Nguồn [3].',10)
p('Layout gồm ba vùng Start, Economy và GoldFrontier. Start chứa căn cứ; Economy ưu tiên cây và cừu; GoldFrontier tập trung vàng và landmark. Tâm vùng có thể xoay theo seed và thay đổi trong khoảng jitter hữu hạn. RegionIds được gán theo tâm gần nhất, còn tuyến chữ L được carve để nối Start với hai vùng ngoài và gateway tương ứng.')
p('Bước carve tạo cấu trúc chiến thuật trên nền noise và giữ các ô quan trọng trong mask Reserved. Map có hai gateway được đăng ký ở biên; tuy nhiên, noise vẫn có thể tạo thêm nhánh kết nối bên trong. Do đó, không khẳng định toàn map chỉ có đúng hai hành lang độc quyền.')

page('3.4. Phân bố tài nguyên và bảo toàn đường tiếp cận')
p('Tài nguyên khởi đầu được đặt trước theo distance field từ căn cứ. Khoảng cách được tính bằng BFS bốn hướng trên lưới, phản ánh đường đi trên terrain thay vì chỉ dùng khoảng cách thẳng. Cây và vàng ngoài vùng đầu được lấy mẫu có thiên hướng quanh các tâm cụm; cừu lấy mẫu trên vùng đất mở. Đây không phải implementation đầy đủ của Bridson Poisson disk.')
table(['Phần phân bổ','Cây','Vàng','Cừu'],[['Quota khởi đầu theo BFS',30,3,6],['Economy ngoài khởi đầu',68,0,9],['GoldFrontier ngoài khởi đầu',22,9,3],['Tổng',120,12,18]],[8,2,2,2])
p('Bảng 1. Quota placement của preset hiện tại [3]. Quota khởi đầu được chọn theo khoảng BFS, không bảo đảm mọi node này có region label Start. Số placement cũng không đồng nghĩa trữ lượng tài nguyên hữu hạn.',False)
p('Quy tắc chung đưa 75% cây và cừu ngoài khởi đầu vào Economy, làm tròn lên; toàn bộ vàng ngoài khởi đầu vào GoldFrontier. Landmark có pad 5 × 5 được giữ trống và ba mỏ vàng footprint 3 × 3 đặt quanh pad. Ba mỏ này nằm trong tổng quota, không phải tài nguyên cộng thêm.')
h('3.5. Kiểm tra topology sau placement')
p('BFS ban đầu loại bỏ những ô đất không thể tới từ căn cứ. Sau khi tài nguyên đã đặt, validator kiểm lại terrain cùng footprint cuối: cây chặn một ô, vàng chặn vùng 3 × 3, còn cừu không được dùng như vật cản tĩnh. Từng resource phải có ô tương tác hợp lệ và không đè Reserved hoặc resource khác.')
p('Để kiểm độ rộng tuyến, hệ thống co miền walkable theo footprint 3 × 3 rồi chạy BFS tới hai gateway và landmark. Đồng thời kiểm pad xây dựng và vòng thoát quanh pad. Cách kiểm trên grid sau placement phát hiện được trường hợp resource vô tình bịt đường mặc dù metadata Routes vẫn tồn tại. Clearance trong Core là quy ước lưới; khi áp scene vẫn phải đối chiếu với collider và navigation thực.')
p('Mỗi lượt BFS có chi phí tuyến tính theo số ô và quan hệ kề được duyệt. Toàn pipeline còn có nhiều lần kiểm, đặt tài nguyên có giới hạn thử và retry; vì vậy không suy chi phí của một BFS thành thời gian tổng của generator hoặc thời gian tải toàn scene.')

page('3.6. Tích hợp scene và kết quả kiểm chứng generator')
p('MapData hợp lệ được Integration chuyển thành Tilemap và các prefab tài nguyên. Terrain được ghi theo batch bằng SetTilesBlock; tiếp theo là spawn resource, Physics2D.SyncTransforms và tái khởi tạo pathfinding. Module không tạo Entity cho từng tile. Fog, minimap, camera và wave nhận dữ liệu map thông qua composition root thay vì tự đọc các cấu trúc riêng không đồng bộ.')
table(['Phép kiểm','Kết quả đã ghi','Phạm vi kết luận'],[['Seed sweep ngày 12/11','1.000 strategic + 1.000 legacy; 0 lỗi','Seed 0–999 của từng preset; có sinh lại đối chiếu'],['Procedural EditMode tại S5','17/17 PASS','Quota, footprint, kết nối, cấu hình lỗi và retry hữu hạn'],['Accepted attempt strategic','822 / 146 / 27 / 4 / 1','Số seed chấp nhận tại attempt 0 / 1 / 2 / 3 / 4'],['Scene integration S6','Seed 0 / 1 / 1543 đã kiểm','Navigation, kinh tế, xây, gateway và lifecycle trong phạm vi báo cáo S6']],[4.1,5.2,6.2])
p('Bảng 2. Bằng chứng kiểm chứng map; số liệu lấy từ báo cáo và artifact đã lưu [3]. Không có đợt quét seed mới trong quá trình biên soạn báo cáo.')
h('3.7. Nhận xét và giới hạn')
p('Kết quả trên hỗ trợ nhận định rằng generator đáp ứng các invariant đã định nghĩa trên tập seed kiểm tra. Nó chưa chứng minh mọi seed trong không gian đầu vào đều hợp lệ hoặc các bản đồ cân bằng như nhau. Retry cho thấy một số cấu hình ngẫu nhiên ban đầu bị từ chối, là hành vi có chủ đích của pipeline thay vì lỗi bị che giấu.')
p('Thời gian Core trên công cụ .NET không bao gồm Tilemap, prefab, Physics2D, bridge và rendering. Báo cáo vì thế không dùng số thời gian Core để đại diện thời gian tải scene. Chỉ số MaxResourcePathDistance là khoảng cách ô tới ô tương tác, chưa phải thời gian vận chuyển thực tế sau khi người chơi xây depot mới.')
p('Đóng góp kỹ thuật của module nằm ở sự phối hợp giữa tính tái lập theo seed, cấu trúc vùng có vai trò gameplay và kiểm tra ràng buộc sau placement. Giá trị chơi lại là mục tiêu thiết kế được hỗ trợ bởi biến thiên map; mức độ hấp dẫn và cân bằng cần được đánh giá thêm bằng playtest người dùng.')

page('4. Hệ thống cây công nghệ')
h('4.1. Mô hình phụ thuộc và phạm vi hiện thực')
p('Cây công nghệ tổ chức các quyết định sử dụng gỗ, vàng và thịt thành ba lĩnh vực: kinh tế, quân sự và mở rộng. Mỗi công nghệ có ID ổn định, prerequisite, chi phí, thời gian và hiệu ứng. Sáu node hiện tại tạo ba chuỗi hai cấp; root và hub lĩnh vực trong giao diện là thành phần tổ chức trực quan, không có chi phí hoặc nâng cấp gameplay riêng [4, 5].')
fig(FIG/'tree.png','Hình 4. Cấu trúc cây công nghệ hiện có. Liên kết giữa hai tech trong mỗi nhánh biểu diễn prerequisite; root/hub biểu diễn phân nhóm.')
table(['Công nghệ','Điều kiện','Gỗ/Vàng/Thịt','Giây','Hiệu ứng'],[['Tools','Không','40/15/0',20,'Gỗ mỗi nhịp ×1,25'],['Cargo','Tools','80/30/0',30,'Sức chứa dân ×1,5'],['Archery','Không','50/30/10',25,'Mở ArcheryRange và Archer'],['Training','Archery','80/60/20',35,'Lượt train mới ×0,8 thời gian'],['Outposts','Không','60/25/0',25,'Mở xây Tower'],['Fortify','Outposts','100/60/0',40,'Damage đạn tháp mới ×1,25']],[2.5,2.3,3.2,1.2,6.3])
p('Bảng 3. Catalog nghiên cứu từ EconomyTools.asset [4]. Đây là thông số cấu hình thử; không gọi là cân bằng cuối. Phạm vi sáu node hiện tại chưa bao phủ toàn bộ ý tưởng lính cấp cao hoặc nâng cấp TownHall trong đề cương.')

page('4.2. Quản lý nghiên cứu và giao dịch tài nguyên')
p('Cấu hình trong ScriptableObject được validate và sao chép vào simulation trước Ready. Validator từ chối ID không hỗ trợ, ID trùng, prerequisite thiếu, chu trình phụ thuộc và các giá trị chi phí/thời gian/modifier không hợp lệ. Catalog không thay đổi trong một trận, giúp chỉ số bit trong UnlockMask giữ ý nghĩa nhất quán.')
fig(FIG/'research.png','Hình 5. Luồng command nghiên cứu và chuyển trạng thái. Lệnh không hợp lệ bị từ chối trước bước trừ tài nguyên.')
p('PlayerResearch và PlayerWallet cùng được lưu trên WalletEntity. StartResearch kiểm trạng thái trận, pause, ID, TownHall còn sống và xây xong, slot đang bận, trạng thái đã mở, prerequisite và số dư. Chỉ sau khi toàn bộ điều kiện thỏa mãn, command mới trừ ví và ghi Active, Technology, Elapsed. UI không thực hiện một giao dịch trừ tiền riêng trước khi gọi ECS.')
p('Mỗi thời điểm chỉ có một nghiên cứu hoạt động. Tiến độ tăng theo thời gian simulation; pause không làm timer tiếp tục. Khi đủ Duration, system bật bit tương ứng trong UnlockMask và kết thúc trạng thái Active. Mất TownHall đang nghiên cứu sẽ hủy tiến trình, không hoàn phí. Bắt đầu trận mới xóa trạng thái nghiên cứu theo lifecycle hiện có.')
p('Modifier được tính từ dữ liệu base thay vì nhân lặp vào prefab. Kiểm chứng S3 ghi nhận capacity 4 thành 6 sau Cargo trong khi profile gốc vẫn 4; lượt train đã enqueue giữ 10 giây, lượt mới còn 8 giây; damage base 10 tạo đạn mới damage 13 sau Fortify theo quy tắc làm tròn. Đạn đã bắn không được sửa ngược damage [4].')

page('4.3. Giao diện graph và kiểm tra quyền sử dụng')
p('Giao diện mới mở bằng nút Công nghệ hoặc phím T, phủ màn hình và có node gốc ở giữa. Ba nhánh dùng màu riêng, khung node và đường nối phát sáng trên nền tối. Khi chọn node, bảng bên phải trình bày hiệu ứng, prerequisite, chi phí và tiến độ. Giao diện cho phép kéo, cuộn zoom, dùng nút cộng/trừ và đưa cây về trung tâm [5].')
fig(ROOT/'Docs/Modules/GameExperience/Evidence/E0/tree-1920x1080.png','Hình 6. Giao diện cây công nghệ trong Game View 1920 × 1080, artifact E0 ngày 23/11/2026 [5].')
p('Zoom điều chỉnh pan theo vị trí con trỏ để giữ điểm graph dưới chuột. Pan và scale hiển thị được nội suy theo unscaledDeltaTime. Drag vượt ngưỡng sẽ hủy khả năng click của thao tác, tránh việc kéo trên node đồng thời gửi lệnh nghiên cứu. Input gameplay và camera bị chặn khi cây mở; đóng cây khôi phục trạng thái camera.')
p('IResearchService được inject từ composition root. UI chỉ đọc snapshot và gửi command, không giữ wallet hoặc timer authoritative thứ hai. Trận đấu tiếp tục diễn ra khi mở cây; người chơi vẫn chịu áp lực thời gian. TownHall hợp lệ có thể được chọn từ registry khi người chơi chưa chọn nhà chính, nhưng command ECS vẫn kiểm tra lại điều kiện cuối cùng.')
p('Quyền xây ArcheryRange, train Archer và xây Tower được kiểm ở các đường thao tác và command tương ứng, không chỉ qua trạng thái nút. Kiểm thử đã ghi nhận lệnh trực tiếp bị từ chối trước unlock và được chấp nhận sau unlock. E0 có fixture cho prerequisite, debit, init lặp, camera, drag và zoom; người dùng đã xác nhận nghiệm thu chung ngày 23/11 [4, 5].')

page('5. Tích hợp vòng chơi và trình bày thế giới')
p('Vòng chơi hiện tại kết nối map sinh thủ tục với kinh tế và tiến trình công nghệ: người chơi khai thác, lựa chọn nghiên cứu hoặc sản xuất, mở kho/tháp và chống wave. Mục tiêu là bảo vệ ít nhất một TownHall qua năm đêm, thắng khi bắt đầu ngày 6. Cấu hình hiện hành có Day 60 giây, Dusk 30 giây và Night 45 giây, tổng danh nghĩa 675 giây cho năm chu kỳ.')
p('Các mốc E bổ sung palette ba pha, đèn công trình, dấu hiệu hoàng hôn, decor vùng, sorting theo chân, che khuất phần trên và phản hồi hành động. Presentation nhận phase, fog và snapshot từ hệ thống hiện có. Đèn và decor không tự thay tầm nhìn gameplay, không tạo collider cản đường hoặc can thiệp vào lượng tài nguyên.')
paths=[ROOT/f'Docs/Modules/GameExperience/Evidence/E1/candidate-{phase}-fog-off.png' for phase in ['Day','Dusk','Night']]
ims=[Image.open(x).convert('RGB') for x in paths]
montage=Image.new('RGB',(1500,340),'white'); dr=ImageDraw.Draw(montage)
for i,im in enumerate(ims):
    im.thumbnail((490,285)); montage.paste(im,(i*500,35));dr.text((i*500+245,10),['BAN NGÀY','HOÀNG HÔN','BAN ĐÊM'][i],font=ImageFont.truetype(FONT,21),anchor='mt',fill='black')
montage.save(FIG/'phases.png')
fig(FIG/'phases.png','Hình 7. Ba palette tại cùng góc căn cứ trong artifact E1. Fog được tắt bằng fixture để đối chiếu màu; ảnh không chứng minh cơ chế khám phá.')
p('Đợt E9 bổ sung candidate lịch đêm có vai trò và tổ hợp khác nhau, dùng wave seed riêng để tái lập thí nghiệm. Các policy control, kinh tế, quân sự và mở rộng được dùng để đánh giá phản ứng của vòng chơi. Trên tập seed huấn luyện 0, 1, 1543, control thua và các policy chủ động có lượt thắng; trên holdout 137 và 901, policy Economy vẫn thua. Kết quả bất lợi được giữ lại để thể hiện giới hạn cân bằng, không loại khỏi báo cáo.')
p('Tổng kết đêm và trận phân biệt tài nguyên ban đầu, lượng nộp, chi tiêu, mất quân, damage TownHall và nghiên cứu hoàn thành. Chức năng này hỗ trợ phân tích quyết định của người chơi; dữ liệu tổng kết và log đo lường không phải file save có thể khôi phục một trận đang diễn ra.')

page('6. Kiểm thử và đánh giá thực nghiệm')
h('6.1. Phương pháp và môi trường')
p('Báo cáo sử dụng các artifact đã lưu của candidate E10 ngày 24/11/2026 [6], không chạy lại benchmark trong phiên biên soạn. Bản Windows x64 Release dùng Unity 6000.3.11f1, Mono, D3D11, quality Ultra, 1920 × 1080, fullscreen borderless, vSync 1 và target FPS không giới hạn riêng. Máy đo là Core i5-11300H, GTX 1650 và khoảng 16 GB RAM; màn hình khoảng 144 Hz.')
p('Mỗi lượt đo warmup 30 giây, ghi 90 giây, kiểm focus, kích thước cửa sổ và camera render onscreen trong capture. Ba lượt dùng seed 1 và một lượt military dùng seed 1543. Percentile được tính bằng nearest-rank từ CSV; p95 là ngưỡng thời gian mà ít nhất 95% mẫu không vượt quá, không có nghĩa mọi frame đều đạt ngưỡng đó.')
table(['Lượt Release','Số frame','P95 ms','Max ms'],[['Seed 1 - lượt 1','12.154','13,8884','27,7858'],['Seed 1 - lượt 2','12.169','13,8882','27,7798'],['Seed 1 - lượt 3','12.148','13,8884','34,7176'],['Seed 1543 - military','12.175','13,8881','27,7911'],['Stress đêm tổng hợp','11.186','13,8952','27,7907']],[5,3,3,3])
p('Bảng 4. Thời gian frame trong batch E10 [6]. Bốn lượt chuẩn đạt mục tiêu p95 ≤16,67 ms. Stress là phép thử riêng, không gộp vào tỷ lệ bốn lượt chuẩn.')
h('6.2. Kiểm chức năng và giới hạn diễn giải')
p('EditMode đạt 89/89, smoke SampleScene đạt 18/18 và kiểm vòng đời procedural đạt 15/15. Năm mẫu scene request đến Ready có median 771,890 ms; không bao gồm khởi chạy process và không kiểm soát cache hệ điều hành. GC counter trong Release không khả dụng, vì vậy không kết luận game không có cấp phát hoặc không có GC.')
p('Stress đo 94 quái sống, 8 đèn và 32 pulse; peak projectile bằng 0. Nó không chứng minh tải hàng nghìn quái hoặc tải đạn đánh xa. Max frame toàn batch còn khoảng 34,718 ms. Các benchmark swarm lịch sử thuộc workload khác và phải được trình bày riêng; hiện chưa có phép so OOP–DOTS đồng điều kiện đủ để công bố hệ số tăng tốc toàn game.')

page('II. NHỮNG VIỆC CHƯA THỰC HIỆN')
p('Tỷ lệ công việc còn lại: ..........% (chờ xác nhận trên cùng cơ sở đánh giá với mục I). Phần này đối chiếu toàn bộ mục tiêu đề cương, không suy từ việc hoàn tất một build plan nội bộ thành hoàn thành toàn khóa luận.')
table(['Nội dung đề cương','Hiện trạng đến 24/11','Công việc tiếp theo'],[['Lưu/Tải toàn trạng thái bằng JSON/Binary','Chưa tìm thấy module và bằng chứng round-trip trận đấu','Thiết kế snapshot, ID bền vững, khôi phục quan hệ và kiểm dữ liệu lỗi'],['Cây công nghệ phức tạp, nâng TownHall, lính cấp cao','Đã có sáu tech và UI graph; chưa có toàn bộ nội dung mở rộng','Thống nhất phạm vi còn lại, bổ sung có chọn lọc và kiểm balance'],['Boss/Blood Moon có hành vi riêng','Chưa có bằng chứng hoàn thành trong tài liệu đã kiểm','Thiết kế cơ chế, tích hợp ECS và kiểm command/lifecycle'],['Tutorial và khảo sát người chơi','Có kiểm UI/playtest kỹ thuật; chưa có khảo sát UX có mẫu và thang đo','Hoàn thiện hướng dẫn; tổ chức khảo sát và phân tích kết quả'],['So sánh OOP và DOTS','Có đo riêng các candidate/workload; chưa đủ đối chứng đồng điều kiện','Chốt workload, phần cứng và tiêu chí để đo đối chứng'],['Phát hành công khai và báo cáo cuối','Có build Windows và bộ bằng chứng kỹ thuật','Hoàn tất nội dung, thẩm mỹ/audio, đóng gói, video và hồ sơ phát hành']],[4,5,6])
h('1. Hướng hoàn thiện hệ thống lưu và tải')
p('Save/Load cần lưu dữ liệu thuần có version, gồm seed/settings hoặc terrain cuối đã thay đổi, thực thể với ID bền vững, ví, unlock, nghiên cứu đang chạy, ngày/pha, wave, hàng đợi và fog. Entity handle và GetInstanceID chỉ có ý nghĩa runtime, không được dùng trực tiếp như định danh xuyên phiên.')
p('Khôi phục dự kiến theo hai lượt: tạo đối tượng và bảng ánh xạ ID trước, sau đó phục hồi tham chiếu như target, depot, worker và producer. Cần kiểm save→load tương đương, file hỏng/truncated, version không hỗ trợ, tham chiếu thiếu, ghi nguyên tử và rollback khi đọc lỗi. Đây là thiết kế hướng tới, chưa phải chức năng đã hiện thực.')
p('Tiến độ tiếp theo sẽ được điều chỉnh cùng giảng viên theo kết quả thực tế. Báo cáo không gán ngày hoàn thành giả cho nhiệm vụ còn thiếu, không coi seed tái tạo map hoặc file log JSON là khả năng tiếp tục một trận đấu bất kỳ.')

page('III. THUẬN LỢI VÀ KHÓ KHĂN TRONG QUÁ TRÌNH THỰC HIỆN KLTN')
h('1. Thuận lợi')
p('Bộ khung Đồ án 2 cung cấp nền tảng tương tác, tài nguyên, công trình và asset, giúp tập trung vào các vấn đề nâng cao. Việc giữ lớp presentation khi chuyển logic sang ECS giảm nhu cầu thay toàn bộ nội dung cùng lúc. Core generator độc lập scene tạo điều kiện quét seed và kiểm invariant nhanh hơn thao tác bằng tay.')
p('Tài liệu module, harness và artifact giúp truy xuất cấu hình đã đo, phân biệt lỗi chức năng với lỗi fixture và kiểm tra hồi quy sau mỗi thay đổi. Cấu hình research tách khỏi trạng thái trận làm thuận tiện việc kiểm prerequisite, debit và các hiệu ứng mà không phải thao tác toàn bộ chu kỳ bằng UI trong mọi test.')
h('2. Khó khăn và cách xử lý')
p('Thứ nhất, noise chỉ tạo hình dạng mà không bảo đảm topology. Nhóm ràng buộc pad, hành lang, resource access và gateway được bổ sung, kiểm lại sau placement bằng BFS và clearance. Tuy vậy, tính hợp lệ hình học không chứng minh cân bằng tài nguyên hoặc độ hấp dẫn; hai nội dung vẫn cần đánh giá riêng.')
p('Thứ hai, kiến trúc lai có nguy cơ cập nhật trùng máu, tài nguyên hoặc trạng thái từ ECS và MonoBehaviour. Giải pháp là xác định ECS làm nguồn trạng thái chính, đưa command qua bridge và dùng DI tại composition root. Pool, scene reload và init lặp được kiểm để tránh giữ dependency của trận cũ.')
p('Thứ ba, mở khóa công nghệ phải có hiệu lực nhất quán cho đơn vị cũ và mới, đồng thời không làm thay đổi ngược hàng đợi hoặc projectile đã tồn tại. Modifier hiệu dụng và dữ liệu snapshot được áp dụng tại đúng thời điểm tạo hành động; command kiểm lại capability để tránh chỉ khóa trên UI.')
p('Thứ tư, phép đo chịu ảnh hưởng workload, vSync, focus và loại build. Do đó, báo cáo giữ dữ liệu thô, percentile, max frame, cấu hình máy và nhãn fixture. Kết quả trên một máy không được suy thành hiệu năng phổ quát. Balance trên holdout và nghiệm thu người chơi vẫn còn mở, dù các gate kỹ thuật của candidate đã đạt.')
h('3. Đánh giá giai đoạn giữa kỳ')
p('Kết quả hiện tại tạo nền kỹ thuật cho bản RTS sinh tồn có map sinh tự động và tiến trình nghiên cứu được kiểm soát. Phần còn lại cần ưu tiên khớp các cam kết đề cương, đặc biệt Save/Load, nội dung chiến đấu mở rộng và đánh giá người dùng. Tỷ lệ hoàn thành sẽ được xác định sau khi thống nhất trọng số từng yêu cầu với giảng viên hướng dẫn.')

page('TÀI LIỆU THAM KHẢO VÀ XÁC NHẬN')
refs=[
'[1] Trần Ngọc Bin. Đề cương chi tiết KLTN: Nghiên cứu và Phát triển Game Chiến thuật Thời gian thực (RTS) kết hợp yếu tố Sinh tồn 2D trên nền tảng Unity. GVHD: ThS. Đinh Nguyễn Anh Dũng. Thời gian đăng ký 07/09–26/12/2026.',
'[2] Hồ sơ dự án RTSgame2026. Docs/TechnicalOverview.md; Docs/Modules/DOTS/Architecture.md; Docs/Modules/DOTSGameplay/Architecture.md; Docs/Modules/DependencyInjection/Architecture.md. Đối chiếu ngày 24/11/2026.',
'[3] Hồ sơ Procedural Generation. Architecture.md, S5-ResourcesTopology.md, S5-SeedCheck-2026-11-12.txt và S6-FinalValidation.md tại Docs/Modules/ProceduralGeneration/. Bằng chứng Core/seed sweep và scene integration.',
'[4] Hồ sơ Progression. Docs/Modules/Progression/S3-Evidence-2026-11-11.md; Assets/Game/DOTS/EconomyTools.asset; Runtime/GameplayResearch.cs. Dữ liệu cấu hình và kết quả research.',
'[5] Hồ sơ UI cây công nghệ. Docs/Modules/Progression/ResearchTreeUI.md; Docs/Modules/GameExperience/Evidence/E0/Acceptance.md; tree-1920x1080.png. Nghiệm thu ngày 23/11/2026.',
'[6] Hồ sơ E7–E10. Docs/Modules/GameExperience/E7-DawnReports.md, E9-TacticalNights.md, E10-ReleaseHandoff.md; Evidence/E10/Release/summary.json và CSV/JSON từng lượt. Kết quả ngày 24/11/2026.',
'[7] Unity Technologies. Unity.Mathematics noise.snoise API. https://docs.unity.cn/Packages/com.unity.mathematics@1.3/api/Unity.Mathematics.noise.snoise.html (truy cập 24/11/2026).',
'[8] Amit Patel. Making maps with noise functions. Red Blob Games. https://www.redblobgames.com/maps/terrain-from-noise/ (truy cập 24/11/2026).'
]
for ref in refs:
    a=p(ref)
    a.alignment=WD_ALIGN_PARAGRAPH.LEFT
    for r in a.runs:r.font.size=Pt(11)
p('Ảnh trong báo cáo lấy từ artifact của dự án; các sơ đồ được dựng lại theo implementation. Ảnh so màu ba pha có ghi rõ fixture fog-off. Không dùng ảnh minh họa để thay thế bằng chứng kiểm thử chức năng.',False)
h('Xác nhận của giảng viên hướng dẫn')
a=p('☐ Đồng ý cho sinh viên thực hiện tiếp KLTN\n☐ Không đồng ý cho sinh viên thực hiện tiếp KLTN'); a.alignment=WD_ALIGN_PARAGRAPH.LEFT
t=doc.add_table(rows=1,cols=2)
for c,txt in zip(t.rows[0].cells,['GIẢNG VIÊN HƯỚNG DẪN KÝ TÊN\n\n\n\nThS. Đinh Nguyễn Anh Dũng','SINH VIÊN KÝ TÊN\n\n\n\nTrần Ngọc Bin']):
    c.text=txt
    for a in c.paragraphs:a.alignment=WD_ALIGN_PARAGRAPH.CENTER
for node in list(doc.styles.element.iter(qn('w:pBdr'))): node.getparent().remove(node)
for node in list(doc.element.iter(qn('w:pBdr'))): node.getparent().remove(node)
path=OUT/'Bao_cao_giua_ky_KLTN_Tran_Ngoc_Bin_23520150.docx';doc.save(path)
print(path)
