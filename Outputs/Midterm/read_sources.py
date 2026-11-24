from pathlib import Path
from pypdf import PdfReader
import pypdfium2 as pdfium

out = Path('Outputs/Midterm/source_review')
out.mkdir(parents=True, exist_ok=True)
files = [('proposal', Path('E:/DC KLTN0127 23520150_DinhNguyenAnhDung.pdf')), ('template', Path('C:/Users/ACER/Downloads/Mau bao cao giua ky KLTN_2024.doc.pdf'))]
for name, path in files:
    doc = PdfReader(path)
    render = pdfium.PdfDocument(str(path))
    texts = []
    for i, page in enumerate(doc.pages):
        texts.append(f'\n--- PAGE {i+1} ---\n' + page.extract_text())
        render[i].render(scale=1.1).to_pil().save(str(out / f'{name}-{i+1}.png'))
    (out / f'{name}.txt').write_text(''.join(texts), encoding='utf-8')
    print(f'{name}: {len(doc.pages)} pages')
