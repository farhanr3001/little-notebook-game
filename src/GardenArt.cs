using System;
using System.Windows;
using System.Windows.Media;
namespace LittleNotebook {
 public sealed partial class MiniSurface {
  static Brush grass;
  static Brush Grass(){if(grass!=null)return grass;var tile=new DrawingGroup();using(var d=tile.Open()){
   d.DrawRectangle(new SolidColorBrush(Color.FromRgb(206,225,170)),null,new Rect(0,0,80,80));var random=new Random(51);
   for(int i=0;i<18;i++){double x=random.Next(80),y=random.Next(80);var pen=new Pen(new SolidColorBrush(Color.FromArgb(62,94,139,67)),.85);d.DrawLine(pen,new Point(x,y),new Point(x-2,y-4));d.DrawLine(pen,new Point(x,y),new Point(x+2,y-5));}
  }grass=new DrawingBrush(tile){TileMode=TileMode.Tile,Viewbox=new Rect(0,0,80,80),ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill,Viewport=new Rect(0,0,80,80),ViewportUnits=BrushMappingMode.Absolute};grass.Freeze();return grass;}
  internal static void DrawGardenPest(DrawingContext dc,Point at,double r,int species,double angle){
   dc.PushTransform(new RotateTransform(angle*180/Math.PI+90,at.X,at.Y));var ink=new SolidColorBrush(Color.FromRgb(66,61,57));var pen=new Pen(ink,1.1);
   if(species==0)DrawPest(dc,at,r);
   else if(species==1){for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++){Point joint=at+new Vector(side*r*.8,(i-1.5)*r*.5);dc.DrawLine(pen,at+new Vector(0,(i-1.5)*r*.25),joint);dc.DrawLine(pen,joint,joint+new Vector(side*r*.35,(i<2?-1:1)*r*.35));}dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(112,91,132)),pen,at,r*.57,r*.67);dc.DrawEllipse(ink,null,at+new Vector(0,-r*.6),r*.37,r*.32);}
   else if(species==2){for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)dc.DrawLine(pen,at+new Vector(side*r*.4,(i-1)*r*.55),at+new Vector(side*r*.9,(i-1)*r*.7));dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(150,104,72)),pen,at,r*.6,r*.85);dc.DrawLine(pen,at+new Vector(0,-r*.7),at+new Vector(0,r*.8));dc.DrawEllipse(ink,null,at+new Vector(0,-r*.7),r*.4,r*.28);}
   else {var wing=new SolidColorBrush(Color.FromArgb(190,225,242,239));for(int side=-1;side<=1;side+=2){dc.DrawEllipse(wing,pen,at+new Vector(side*r*.55,-r*.2),r*.65,r*.3);dc.DrawEllipse(wing,pen,at+new Vector(side*r*.5,r*.35),r*.55,r*.25);}dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(83,136,149)),pen,at,r*.23,r);dc.DrawEllipse(ink,null,at+new Vector(0,-r*.8),r*.32,r*.25);}
   dc.DrawEllipse(Brushes.White,null,at+new Vector(-r*.15,-r*.66),r*.09,r*.09);dc.DrawEllipse(Brushes.White,null,at+new Vector(r*.15,-r*.66),r*.09,r*.09);dc.Pop();
  }
  void DrawPour(DrawingContext dc){var b=Session.Extra;
   for(int i=0;i<b.Tubes.Count;i++){
    Rect r=TubeRect(i);if(i==b.Selected)r.Y-=10;
    dc.DrawEllipse(Brush(Color.FromArgb(22,51,66,52)),null,new Point(r.X+r.Width/2,r.Bottom+5),r.Width*.5,4);
    var inner=new Rect(r.X+5,r.Y+4,r.Width-10,r.Height-10);dc.PushClip(new RectangleGeometry(inner,10,10));
    dc.DrawRectangle(Brush(Color.FromArgb(100,224,236,231)),null,inner);
    double unit=(r.Height-20)/4;
    for(int j=0;j<b.Tubes[i].Count;j++){
     if(b.Feedback>0&&i==b.LastTo&&j>=b.Tubes[i].Count-b.LastAmount)continue;
     var band=new Rect(inner.X,r.Bottom-10-(j+1)*unit,inner.Width,unit+.7);dc.DrawRectangle(Brush(PuzzleColours[b.Tubes[i][j]]),null,band);
     if(j==b.Tubes[i].Count-1){dc.DrawEllipse(Brush(Color.FromArgb(70,255,255,255)),null,new Point(band.X+band.Width/2,band.Top+2),band.Width/2,3);}
    }dc.Pop();
    dc.DrawRoundedRectangle(null,Pen(i==b.Selected?palette.Gold:Blend(palette.Ink,.75),i==b.Selected?3:1.7),r,12,12);
    dc.DrawLine(Pen(Color.FromArgb(180,255,255,255),2),new Point(r.X+8,r.Y+12),new Point(r.X+8,r.Bottom-20));
    dc.DrawRoundedRectangle(Brush(Blend(palette.Paper,.9)),Pen(palette.Ink,1),new Rect(r.X-2,r.Y-3,r.Width+4,7),3,3);
    if(b.IsTubeComplete(i)){Point done=new Point(r.X+r.Width/2,r.Y+r.Height/2);dc.DrawEllipse(Brush(palette.Paper),Pen(Color.FromRgb(53,132,69),1.8),done,14,14);Label(dc,"✓",done,22,Color.FromRgb(42,137,64));}
   }
   if(b.Feedback>0&&b.LastFrom>=0){var a=TubeRect(b.LastFrom);var z=TubeRect(b.LastTo);double t=1-Math.Min(1,b.Feedback/.34);Point start=new Point(a.X+a.Width/2,a.Y),end=new Point(z.X+z.Width/2,z.Bottom-10-(b.Tubes[b.LastTo].Count-b.LastAmount)*((z.Height-20)/4));
    Point drop=start+(end-start)*t;drop.Y-=Math.Sin(t*Math.PI)*42;var stream=Pen(PuzzleColours[b.LastColour],7);stream.StartLineCap=stream.EndLineCap=PenLineCap.Round;dc.DrawLine(stream,drop,drop+new Vector(0,12));dc.DrawEllipse(Brush(PuzzleColours[b.LastColour]),null,drop+new Vector(0,13),4,6);
   }
   Label(dc,ar?"اختر أنبوباً ثم صب في أي أنبوب به مساحة":"Select a glass · pour into any glass with space",new Point(340,385),14);
   if(Session.Phase=="complete"){
    dc.DrawRoundedRectangle(Brush(palette.Light),Pen(palette.Accent,1.5),new Rect(162,8,356,49),12,12);Label(dc,ar?"✓ اكتمل الترتيب! المستوى التالي…":"✓ Beautifully sorted! Next level…",new Point(340,32),19);
   }else if(b.Moves==0){Label(dc,(ar?"المستوى ":"Level ")+Session.Level+"  ·  "+b.Colours+(ar?" ألوان":" colours"),new Point(340,29),19);}
  }
 }
}
