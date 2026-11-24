from pathlib import Path
import pypdfium2 as pdfium
from pypdf import PdfReader
from PIL import Image, ImageDraw
root=Path('Outputs/Midterm/render')
pdf=root/'report-layout.pdf'
doc=pdfium.PdfDocument(str(pdf)); reader=PdfReader(pdf)
for i,page in enumerate(doc):
    page.render(scale=1.4).to_pil().save(root/f'page-{i+1}.png')
    text=reader.pages[i].extract_text()
    print(f'Page {i+1}: {len(text)} chars; '+text[:80].replace('\n',' '))
for start in range(0,len(doc),4):
    montage=Image.new('RGB',(1200,1700),'#dddddd')
    for j in range(4):
        if start+j>=len(doc):break
        im=Image.open(root/f'page-{start+j+1}.png');im.thumbnail((590,825))
        montage.paste(im,((j%2)*600,(j//2)*850))
    montage.save(root/f'review-{start//4+1}.png')
