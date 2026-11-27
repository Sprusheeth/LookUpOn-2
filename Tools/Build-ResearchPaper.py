from pathlib import Path
import re, json, html
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Flowable, KeepTogether, PageBreak
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from pypdf import PdfReader
import pypdfium2 as pdfium
from PIL import Image, ImageOps, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
RESEARCH = ROOT / 'Research'
QA = ROOT / 'Builds/ResearchQA'
QA.mkdir(parents=True, exist_ok=True)
SOURCE = RESEARCH / 'LookUpOn-Research-Paper.md'
OUTPUT = RESEARCH / 'LookUpOn-Research-Paper.pdf'
INK = colors.HexColor('#182d37')
ACCENT = colors.HexColor('#24585e')
MUTED = colors.HexColor('#5b6970')
WIDTH = A4[0] - 100

styles = {
 'title': ParagraphStyle('PaperTitle',fontName='Helvetica-Bold',fontSize=18.5,leading=23,textColor=INK,spaceAfter=14,keepWithNext=True),
 'meta': ParagraphStyle('Meta',fontName='Helvetica',fontSize=8.7,leading=12,textColor=MUTED,spaceAfter=15,keepWithNext=True),
 'h2': ParagraphStyle('Section',fontName='Helvetica-Bold',fontSize=11.6,leading=15,textColor=ACCENT,spaceBefore=13,spaceAfter=7,keepWithNext=True),
 'h3': ParagraphStyle('Subsection',fontName='Helvetica-Bold',fontSize=10.2,leading=13,textColor=INK,spaceBefore=9,spaceAfter=5,keepWithNext=True),
 'body': ParagraphStyle('Body',fontName='Times-Roman',fontSize=10.5,leading=14.1,textColor=INK,spaceAfter=7,allowWidows=0,allowOrphans=0),
 'reference': ParagraphStyle('Reference',fontName='Times-Roman',fontSize=9.3,leading=12.6,textColor=INK,spaceAfter=8,allowWidows=0,allowOrphans=0),
 'cell': ParagraphStyle('Cell',fontName='Helvetica',fontSize=8.1,leading=11.2,textColor=INK,spaceAfter=0),
 'th': ParagraphStyle('TableHead',fontName='Helvetica-Bold',fontSize=8.1,leading=11.2,textColor=colors.white,spaceAfter=0),
 'caption': ParagraphStyle('Caption',fontName='Times-Italic',fontSize=9.1,leading=12,textColor=MUTED,spaceBefore=5,spaceAfter=9),
}

def normalize(text):
    for a,b in {'×':' x ','−':'-','–':'-','—':'-','’':"'",'“':'"','”':'"','→':' -> ','≤':'<=','≥':'>='}.items():text=text.replace(a,b)
    return text

def inline(text, reference=False):
    text=normalize(text)
    tokens=[]
    def token(markup):
        tokens.append(markup); return f'ZZTOKEN{len(tokens)-1}ZZ'
    text=re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)',lambda m:token(f'<link href="{html.escape(m[2],quote=True)}" color="#24585e"><u>{html.escape(m[1])}</u></link>'),text)
    text=re.sub(r'`([^`]+)`',lambda m:token(f'<font name="Courier" size="8.3">{html.escape(m[1])}</font>'),text)
    text=html.escape(text)
    text=re.sub(r'\*\*(.+?)\*\*',r'<b>\1</b>',text)
    if reference:
        text=re.sub(r'^\[(\d+)\]',lambda m:f'<a name="ref{m[1]}"/>[{m[1]}]',text)
    else:
        text=re.sub(r'\[(\d+)\]',lambda m:f'<link href="#ref{m[1]}" color="#24585e">[{m[1]}]</link>',text)
    for i,t in enumerate(tokens):text=text.replace(f'ZZTOKEN{i}ZZ',t)
    return text

class Architecture(Flowable):
    def __init__(self):Flowable.__init__(self);self.width=WIDTH;self.height=192
    def draw(self):
        c=self.canv
        def box(x,y,w,h,lines,fill='#eff4f3'):
            c.setFillColor(colors.HexColor(fill));c.setStrokeColor(colors.HexColor('#c8d7d7'));c.roundRect(x,y,w,h,5,fill=1,stroke=1)
            for j,line in enumerate(lines):
                c.setFont('Helvetica-Bold' if j==0 else 'Helvetica',8.1 if j==0 else 7.4);c.setFillColor(INK);c.drawCentredString(x+w/2,y+h-15-j*12,line)
        def arrow(x1,y1,x2,y2):
            c.setStrokeColor(ACCENT);c.setLineWidth(.9);c.line(x1,y1,x2,y2)
            dx,dy=x2-x1,y2-y1;length=(dx*dx+dy*dy)**.5;ux,uy=dx/length,dy/length
            c.line(x2,y2,x2-5*ux+2.6*uy,y2-5*uy-2.6*ux);c.line(x2,y2,x2-5*ux-2.6*uy,y2-5*uy+2.6*ux)
        box(0,132,134,48,['DETERMINISTIC RECIPE','Seeds and world coordinates'])
        box(181,132,134,48,['REGION CATALOG','Held manifests / availability'])
        box(361,132,134,48,['STREAMED TERRAIN','Bounded tile neighborhood'])
        arrow(134,157,181,157);arrow(315,157,361,157)
        box(0,64,134,48,['DIRECT UDP PEERS','Pose / manifests / beacons'])
        box(181,64,134,48,['LOCAL STATE + SAVE','Mutable records / snapshots'])
        box(361,64,134,48,['EXTERNAL ADMIN','Telemetry / world policy'])
        arrow(134,88,181,88);arrow(181,82,134,82);arrow(248,112,248,132);arrow(315,88,361,88);arrow(361,78,315,78)
        box(361,0,134,43,['ISOLATED SIMULATION','No command to live state'],fill='#f1f1f7');arrow(428,64,428,43)
        c.setFont('Helvetica',7.2);c.setFillColor(MUTED);c.drawString(0,22,'Data plane: UDP and local state. Operator plane: local files and HTTP.')

class PaperDoc(SimpleDocTemplate):
    def afterFlowable(self,flowable):
        if isinstance(flowable,Paragraph) and flowable.style.name in ['Section','Subsection']:
            name='section-'+str(self.seq.nextf('section'));self.canv.bookmarkPage(name);self.canv.addOutlineEntry(flowable.getPlainText(),name,0 if flowable.style.name=='Section' else 1,False)

def footer(canvas,doc):
    canvas.saveState();canvas.setFillColor(MUTED);canvas.setFont('Helvetica',7.3)
    canvas.drawString(50,27,'LOOKUPON | Prototype research report | 7 November 2026')
    canvas.drawRightString(A4[0]-50,27,str(doc.page));canvas.restoreState()

lines=SOURCE.read_text(encoding='utf8').splitlines()
story=[];i=0;in_refs=False
while i<len(lines):
    line=lines[i].strip()
    if not line:i+=1;continue
    if line.startswith('```'):
        kind=line[3:];i+=1
        while i<len(lines) and not lines[i].strip().startswith('```'):i+=1
        i+=1
        if kind=='mermaid':story.extend([Spacer(1,6),Architecture()])
        continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            cells=[x.strip() for x in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r':?-+:?',cell) for cell in cells):rows.append(cells)
            i+=1
        n=len(rows[0]);widths={2:[WIDTH*.38,WIDTH*.62],3:[WIDTH*.23,WIDTH*.385,WIDTH*.385],4:[WIDTH*.18,WIDTH*.27,WIDTH*.29,WIDTH*.26]}[n]
        table=Table([[Paragraph(inline(cell),styles['th' if r==0 else 'cell']) for cell in row] for r,row in enumerate(rows)],colWidths=widths,repeatRows=1,hAlign='LEFT')
        table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),ACCENT),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#f2f6f5'),colors.white]),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),9),('RIGHTPADDING',(0,0),(-1,-1),9),('TOPPADDING',(0,0),(-1,-1),8),('BOTTOMPADDING',(0,0),(-1,-1),8),('LINEBELOW',(0,-1),(-1,-1),.5,colors.HexColor('#c8d7d7'))]))
        story.extend([Spacer(1,5),table,Spacer(1,7)]);continue
    if line.startswith('# '):story.append(Paragraph(inline(line[2:]),styles['title']));i+=1;continue
    if line.startswith('## '):
        heading=line[3:];in_refs=heading=='References'
        if in_refs:story.append(PageBreak())
        story.append(Paragraph(inline(heading),styles['h2']));i+=1;continue
    if line.startswith('### '):story.append(Paragraph(inline(line[4:]),styles['h3']));i+=1;continue
    paragraph=[line];i+=1
    while i<len(lines) and lines[i].strip() and not lines[i].startswith(('#','|','```')):paragraph.append(lines[i].strip());i+=1
    text=' '.join(paragraph)
    key='meta' if text.startswith('Technical research report') else 'caption' if text.startswith(('Figure ','Table ')) else 'reference' if in_refs else 'body'
    story.append(Paragraph(inline(text,in_refs),styles[key]))

doc=PaperDoc(str(OUTPUT),pagesize=A4,rightMargin=50,leftMargin=50,topMargin=44,bottomMargin=46,title='LookUpOn: Procedural World Partitioning and Observable Peer-Held Backups',author='LookUpOn project',subject='Prototype design and functional evaluation',pageCompression=1)
doc.build(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(str(OUTPUT))
text='\n'.join(page.extract_text() for page in reader.pages)
for expected in ['Abstract','631','2,688','12','16','References','Appendix A.']:
    assert expected in text,expected
assert len(reader.pages)>=6
assert all(len(page.extract_text().strip())>120 for page in reader.pages),'Near-empty page'
assert len(re.findall(r'^\[\d+\]',text,re.M))==8,'Reference count'
pdf=pdfium.PdfDocument(str(OUTPUT))
previews=[]
for idx in range(len(pdf)):
    bitmap=pdf[idx].render(scale=1.55);img=bitmap.to_pil().convert('RGB');filename=QA/f'page-{idx+1:02}.png';img.save(filename)
    thumb=img.copy();thumb.thumbnail((298,421));tile=Image.new('RGB',(320,451),'#dfe3e5');tile.paste(thumb,((320-thumb.width)//2,8));ImageDraw.Draw(tile).text((12,432),f'Page {idx+1}',fill='black');previews.append(tile)
cols=3;rows=(len(previews)+cols-1)//cols
sheet=Image.new('RGB',(cols*320,rows*451),'white')
for idx,img in enumerate(previews):sheet.paste(img,((idx%cols)*320,(idx//cols)*451))
sheet.save(QA/'contact-sheet.png')
(QA/'extracted.txt').write_text(text,encoding='utf8')
summary={'pages':len(reader.pages),'wordsApprox':len(text.split()),'pdfBytes':OUTPUT.stat().st_size,'externalLinks':sum(1 for p in reader.pages for a in p.get('/Annots',[]) if a.get_object().get('/A',{}).get('/URI')),'qaPages':str(QA)}
(QA/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf8')
print(json.dumps(summary,indent=2))
