from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math

OUT=Path(r'E:\VLAB\4 LABS\Ngua Man VLAB\Assets\Labs\Upgrade\LabIdentity')
OUT.mkdir(parents=True,exist_ok=True)
FONT=r'C:\Windows\Fonts\segoeui.ttf'
BOLD=r'C:\Windows\Fonts\segoeuib.ttf'
INK='#243441'; PAPER='#F3F2EC'
def text(d,xy,s,size=32,fill=INK,bold=False):
    d.text(xy,s,font=ImageFont.truetype(BOLD if bold else FONT,size),fill=fill)
def base(accent,num):
    im=Image.new('RGB',(2048,1024),PAPER);d=ImageDraw.Draw(im)
    d.rectangle((0,0,2048,24),fill=accent)
    text(d,(70,52),'V L A B',36,bold=True);text(d,(1850,44),num,48,accent,True)
    d.line((70,123,1978,123),fill='#C7CBC7',width=2)
    return im,d
def line(d,pts,c=INK,w=6):d.line(pts,fill=c,width=w,joint='curve')
def circle(d,x,y,r,c=INK,w=5):d.ellipse((x-r,y-r,x+r,y+r),outline=c,width=w)

im,d=base('#3B729D','01')
# A small-angle pendulum and a sinusoidal waveform; diagrams, not measured data.
line(d,[(180,220),(780,220)],w=14);line(d,[(480,220),(480,750)],'#AFBABF',3)
theta=.32;L=510;end=(480+L*math.sin(theta),220+L*math.cos(theta))
line(d,[(480,220),end],w=10);d.ellipse((end[0]-38,end[1]-38,end[0]+38,end[1]+38),fill='#3B729D')
d.arc((350,90,610,350),70,90,fill='#3B729D',width=5);text(d,(504,342),'θ',40)
text(d,(611,429),'L',42);text(d,(220,820),'T = 2π √(L / g)',54,bold=True)
text(d,(247,906),'θ₀ < 0.2 rad',31,fill='#637580')
line(d,[(1010,480),(1910,480)],'#7F9099',3);line(d,[(1040,220),(1040,750)],'#7F9099',3)
points=[(1040+i,480-180*math.sin(i/820*math.pi*4)) for i in range(840)]
line(d,points,'#3B729D',8);text(d,(1880,510),'t',36);text(d,(985,197),'x',36)
line(d,[(1142,740),(1553,740)],w=3)
for x in [1142,1553]:line(d,[(x,720),(x,760)],w=4)
text(d,(1325,760),'T',40);text(d,(1200,858),'f = 1 / T',54,bold=True)
im.save(OUT/'Physics.png')

im,d=base('#348B88','02')
symbols='H He Li Be B C N O F Ne Na Mg Al Si P S Cl Ar K Ca Sc Ti V Cr Mn Fe Co Ni Cu Zn Ga Ge As Se Br Kr Rb Sr Y Zr Nb Mo Tc Ru Rh Pd Ag Cd In Sn Sb Te I Xe Cs Ba La Ce Pr Nd Pm Sm Eu Gd Tb Dy Ho Er Tm Yb Lu Hf Ta W Re Os Ir Pt Au Hg Tl Pb Bi Po At Rn Fr Ra Ac Th Pa U Np Pu Am Cm Bk Cf Es Fm Md No Lr Rf Db Sg Bh Hs Mt Ds Rg Cn Nh Fl Mc Lv Ts Og'.split()
assert len(symbols)==118
rows=[[1]+[None]*16+[2],[3,4]+[None]*10+list(range(5,11)),[11,12]+[None]*10+list(range(13,19)),list(range(19,37)),list(range(37,55)),[55,56,57]+list(range(72,87)),[87,88,89]+list(range(104,119))]
for iy,row in enumerate(rows):
    assert len(row)==18
    for ix,n in enumerate(row):
        if n is None:continue
        x=74+ix*104;y=180+iy*91
        color='#DCE8E4' if ix<2 else '#E7DCC9' if ix>11 else '#DDE3EA'
        d.rounded_rectangle((x,y,x+96,y+83),radius=5,fill=color)
        text(d,(x+8,y+3),str(n),19,fill='#56686C');text(d,(x+17,y+25),symbols[n-1],35,bold=True)
for iy,start in enumerate([58,90]):
    for ix,n in enumerate(range(start,start+14)):
        x=282+ix*104;y=846+iy*72;d.rectangle((x,y,x+96,y+65),fill='#E1E6D8')
        text(d,(x+8,y+2),str(n),16,fill='#56686C');text(d,(x+30,y+21),symbols[n-1],27,bold=True)
im.save(OUT/'Chemistry.png')

im,d=base('#557A52','03')
# Stylised cell cross section, paired DNA bases. No fabricated microscope photo.
d.ellipse((100,220,1040,850),fill='#E4EACF',outline='#557A52',width=14)
d.ellipse((140,252,1000,817),outline='#96AD79',width=5)
d.ellipse((405,366,690,649),fill='#D9C9AC',outline='#7F735F',width=7)
d.ellipse((495,460,595,562),fill='#8E7D68')
for x,y,a in [(260,400,.5),(770,390,-.4),(270,680,-.2),(810,675,.4)]:
    d.ellipse((x-67,y-31,x+67,y+31),fill='#BCD19B',outline='#557A52',width=5)
    for k in [-34,-17,0,17,34]:line(d,[(x+k,y-20),(x+k+6,y+20)],'#557A52',3)
for k in range(5):
    pts=[(720+i,480+k*22+14*math.sin(i/35)) for i in range(210)]
    line(d,pts,'#A69A7D',5)
for x,y in [(350,340),(320,530),(690,720),(830,530),(460,725)]:circle(d,x,y,16,'#A9B883',4)
left=[];right=[]
for y in range(215,880):
    phase=(y-215)/665*math.pi*4;x=1560+150*math.sin(phase)
    left.append((x,y));right.append((3120-x,y))
for k,y in enumerate(range(230,870,37)):
    x=1560+150*math.sin((y-215)/665*math.pi*4)
    line(d,[(x,y),(1560,y)],'#B88D58',7);line(d,[(1560,y),(3120-x,y)],'#658F98',7)
line(d,left,'#557A52',12);line(d,right,'#7F9F72',12)
text(d,(1468,913),'A–T   G–C',42,bold=True)
im.save(OUT/'Biology.png')

im,d=base('#B07C38','04')
# Two external gears: ideal angular-speed ratio equals inverse tooth ratio.
def gear(x,y,r,teeth,phase):
    pts=[]
    for k in range(teeth*4):
        a=k/(teeth*4)*2*math.pi+phase;rr=r+(12 if k%4 in [1,2] else -12)
        pts.append((x+rr*math.cos(a),y+rr*math.sin(a)))
    d.polygon(pts,fill='#E6DAC5');line(d,pts+[pts[0]],'#806033',6)
    circle(d,x,y,r*.62,'#B07C38',5);circle(d,x,y,35,'#806033',6)
    line(d,[(x-r-40,y),(x+r+40,y)],'#A6ADB1',2);line(d,[(x,y-r-40),(x,y+r+40)],'#A6ADB1',2)
gear(470,480,190,20,0);gear(945,480,285,30,math.pi/30)
text(d,(350,810),'z₁ = 20',45,bold=True);text(d,(825,850),'z₂ = 30',45,bold=True)
text(d,(1330,300),'|ω₁| / |ω₂|',66,bold=True);text(d,(1438,410),'=',58,bold=True);text(d,(1390,510),'z₂ / z₁',66,bold=True)
line(d,[(1340,660),(1850,660)],'#B07C38',5);text(d,(1390,720),'i = 1.5',65,bold=True)
for x,reverse in [(1450,False),(1710,True)]:
    d.arc((x-40,850,x+40,930),35,320,fill='#806033',width=6)
    px,py=(x+34,863) if reverse else (x+34,916)
    d.polygon([(px,py),(px-22,py-5 if reverse else py+5),(px+3,py+20 if reverse else py-20)],fill='#806033')
im.save(OUT/'Engineering.png')
for f in OUT.glob('*.png'):
    with Image.open(f) as check:assert check.size==(2048,1024)
print('4 original scientific diagram textures written')
