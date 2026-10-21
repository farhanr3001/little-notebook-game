using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public class Palette {
  public string Name;
  public Color Paper, Ink, Accent, AccentInk, Light, Desk, Line, Muted, Gold;
  static Color C(string s) { return (Color)ColorConverter.ConvertFromString(s); }
  public Palette(string name) {
   Name=name;
   Paper=C("#FFF8E9");Ink=C("#29384A");Accent=C("#8FAF80");Light=C("#E4ECCD");Desk=C("#D4BB97");Line=C("#CDD9D6");Muted=C("#6A7469");Gold=C("#E9BC60");
   if(name=="rose") {Accent=C("#D69A9D");Light=C("#F4DEDA");Desk=C("#DBBFA8");}
   if(name=="sky") {Accent=C("#84A9BD");Light=C("#DCEAF0");Desk=C("#C7C9BC");}
   if(name=="lavender") {Accent=C("#A79ABD");Light=C("#E9E0EF");Desk=C("#CCC0BD");}
   AccentInk=Ink;
   if(name=="night") {Ink=C("#303239");Accent=C("#34363B");AccentInk=Paper;Light=C("#E5E1D8");Desk=C("#C4BEB2");Line=C("#D7D6D2");Muted=C("#625E59");}
  }
  public Palette OnAccent(){var other=(Palette)MemberwiseClone();other.Ink=AccentInk;return other;}
  public SolidColorBrush B(Color c) { return new SolidColorBrush(c); }
 }
 public partial class Sketch : FrameworkElement {
  static BitmapSource paperTexture;
  static Dictionary<string,BitmapSource> radioArt=new Dictionary<string,BitmapSource>();
  public static BitmapSource Asset(string name){var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",name));b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();b.Freeze();return b;}
  public static Brush Textured(Color color){
   if(paperTexture==null)paperTexture=Asset("paper.png");
   var g=new DrawingGroup();g.Children.Add(new GeometryDrawing(new SolidColorBrush(color),null,new RectangleGeometry(new Rect(0,0,400,400))));
   var texture=new DrawingGroup{Opacity=color.R<60?.045:.20};texture.Children.Add(new ImageDrawing(paperTexture,new Rect(0,0,400,400)));g.Children.Add(texture);
   var b=new DrawingBrush(g){TileMode=TileMode.Tile,Viewport=new Rect(0,0,400,400),ViewportUnits=BrushMappingMode.Absolute};b.Freeze();return b;
  }
  public Palette P;
  public string Kind;
  public Sketch(string kind,Palette p,double w,double h) { Kind=kind;P=p;Width=w;Height=h;IsHitTestVisible=false; }
  SolidColorBrush B(Color c) { return new SolidColorBrush(c); }
  Pen Pen(Color c,double w) { return new Pen(B(c),w); }
  void Line(DrawingContext d,double x,double y,double x2,double y2,Color c,double w) { d.DrawLine(Pen(c,w),new Point(x,y),new Point(x2,y2)); }
  void Rect(DrawingContext d,double x,double y,double w,double h,Color c,double radius=3) { d.DrawRoundedRectangle(B(c),Pen(P.Ink,2),new Rect(x,y,w,h),radius,radius); }
  void Text(DrawingContext d,string s,double x,double y,double size,Color c) { d.DrawText(new FormattedText(s,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe Print"),size,B(c),1.0),new Point(x,y)); }
  void Star(DrawingContext d,double x,double y,double radius) {
   var g=new StreamGeometry();using(var c=g.Open()) { for(int i=0;i<10;i++) { double a=-Math.PI/2+i*Math.PI/5,r=i%2==0?radius:radius*.46;var pt=new Point(x+Math.Cos(a)*r,y+Math.Sin(a)*r);if(i==0)c.BeginFigure(pt,true,true);else c.LineTo(pt,true,false); } }d.DrawGeometry(B(P.Gold),Pen(P.Ink,1.6),g);
  }
  protected override void OnRender(DrawingContext d) {
   double w=Width,h=Height;
   if(Kind=="desk") {
    d.DrawRectangle(B(P.Desk),null,new Rect(0,0,w,h));var r=new Random(42);
    for(int i=0;i<180;i++){double x=r.NextDouble()*w,y=r.NextDouble()*h;Line(d,x,y,x+r.Next(20,190),y+r.Next(1,4),Color.FromArgb(17,73,48,26),1);}
    for(int y=0;y<h;y+=180)Line(d,0,y,w,y-14,Color.FromArgb(28,73,48,26),2);
    d.DrawEllipse(B(Color.FromArgb(35,40,30,10)),null,new Point(w-48,h-43),130,100);
    d.DrawEllipse(B(P.Paper),Pen(P.Ink,3),new Point(w-50,h-75),84,84);
    d.DrawEllipse(B(P.Accent),Pen(P.Ink,2),new Point(w-50,h-75),66,66);
    d.DrawEllipse(B(Color.FromArgb(65,255,255,230)),null,new Point(w-72,h-92),27,18);
    return;
   }
   if(Kind=="paper") {
    d.DrawRoundedRectangle(B(Color.FromArgb(35,40,35,20)),null,new Rect(8,10,w-10,h-11),8,8);
    d.DrawRoundedRectangle(Textured(P.Paper),Pen(Color.FromArgb(80,P.Ink.R,P.Ink.G,P.Ink.B),1.3),new Rect(0,0,w-10,h-12),5,5);
    if(PaperSettings==null||PaperSettings.Lines)for(double y=58;y<h-16;y+=Math.Max(20,PaperSettings==null?37:PaperSettings.Spacing))Line(d,1,y,w-12,y+1,P.Line,1);
    if(PaperSettings==null||PaperSettings.Margin){Color margin=Color.FromRgb(201,115,110);try{margin=(Color)ColorConverter.ConvertFromString(PaperSettings.MarginColor);}catch{}margin.A=90;Line(d,65,1,67,h-14,margin,1.6);}
    var r=new Random(58);for(int i=0;i<2600;i++){double x=r.NextDouble()*(w-13),y=r.NextDouble()*(h-14);Line(d,x,y,x+r.NextDouble()*4,y+1,Color.FromArgb(10,P.Ink.R,P.Ink.G,P.Ink.B),.6);}
    DrawPaperBindings(d,w,h);if(w>=1000)DrawPaperDecorations(d,w,h);
    return;
   }
   if(Kind=="radio") {
    string key=P.Name=="matcha"?"matcha":P.Name;if(!radioArt.ContainsKey(key))radioArt[key]=Asset(key=="matcha"?"radio.png":"radio-"+key+".png");d.DrawImage(radioArt[key],new Rect(0,22,w,w*992.0/1584));return;
   }
   if(Kind=="cat"||Kind=="dog"||Kind=="rabbit"||Kind=="bird"){d.DrawImage(PetArt.Get(Kind,Kind=="cat"?"ginger":""),new Rect(0,0,w,h));return;}
   if(Doodles.Paths.ContainsKey(Kind)&&Kind!="star"){Doodles.Draw(d,Kind,w,h,P);return;}
   d.PushTransform(new ScaleTransform(w/160,h/160));
   if(Kind=="book") {d.PushTransform(new RotateTransform(-12,80,80));Rect(d,39,25,86,114,Color.FromRgb(180,113,94),6);Rect(d,48,29,79,105,P.Paper,4);Rect(d,39,25,79,107,Color.FromRgb(201,139,113),4);Line(d,50,27,50,129,P.Ink,2);Rect(d,61,45,42,28,P.Light);Line(d,67,92,100,92,P.Ink,2);d.Pop();}
   else if(Kind=="chair") {Rect(d,43,23,74,46,Color.FromRgb(197,151,95),5);for(int x=50;x<=110;x+=20)Line(d,x,66,x,102,P.Ink,5);Rect(d,36,95,88,20,Color.FromRgb(197,151,95),6);Line(d,46,113,41,143,P.Ink,6);Line(d,113,114,119,143,P.Ink,6);}
   else if(Kind=="water") {Rect(d,65,16,32,22,Color.FromRgb(119,156,179),4);Rect(d,54,36,54,106,Color.FromRgb(204,229,236),17);Line(d,56,67,105,70,P.Ink,2);Line(d,57,119,105,117,P.Ink,2);Text(d,"◊",67,76,31,P.Ink);}
   else if(Kind=="apple") {d.DrawEllipse(B(Color.FromRgb(192,111,92)),Pen(P.Ink,2.5),new Point(62,94),36,44);d.DrawEllipse(B(Color.FromRgb(192,111,92)),Pen(P.Ink,2.5),new Point(94,94),36,44);Line(d,80,67,86,30,P.Ink,5);d.DrawEllipse(B(P.Accent),Pen(P.Ink,2),new Point(102,35),23,10);Line(d,49,77,44,98,P.Paper,4);}
   else if(Kind=="sun") {for(int i=0;i<10;i++){double a=i*Math.PI/5;Line(d,80+Math.Cos(a)*50,80+Math.Sin(a)*50,80+Math.Cos(a)*65,80+Math.Sin(a)*65,P.Gold,5);}d.DrawEllipse(B(P.Gold),Pen(P.Ink,2),new Point(80,80),35,35);Text(d,"ᴗ",64,74,23,P.Ink);d.DrawEllipse(B(P.Ink),null,new Point(68,73),2,3);d.DrawEllipse(B(P.Ink),null,new Point(92,73),2,3);}
   else if(Kind=="house") {Rect(d,38,69,86,72,P.Light,3);var g=Geometry.Parse("M 23,70 L 81,20 L 139,70 Z");d.DrawGeometry(B(P.Accent),Pen(P.Ink,3),g);Rect(d,72,101,23,40,Color.FromRgb(196,155,109));Rect(d,48,83,18,21,Color.FromRgb(179,209,221));}
   else if(Kind=="leaf") {Line(d,51,137,102,35,P.Ink,3);d.PushTransform(new RotateTransform(-35,76,83));d.DrawEllipse(B(P.Name=="night"?Color.FromRgb(143,175,128):P.Accent),Pen(P.Ink,2),new Point(76,83),29,54);Line(d,76,130,76,40,P.Ink,2);for(int i=0;i<4;i++)Line(d,76,60+i*17,95,47+i*17,P.Ink,1);d.Pop();}
   else if(Kind=="moon") {d.DrawEllipse(B(P.Gold),Pen(P.Ink,2),new Point(76,76),49,49);d.DrawEllipse(B(P.Paper),null,new Point(99,57),42,42);Star(d,123,115,13);}
   else if(Kind=="paw") {d.DrawEllipse(B(P.Accent),Pen(P.Ink,2),new Point(80,105),34,27);for(int i=0;i<4;i++)d.DrawEllipse(B(P.Accent),Pen(P.Ink,2),new Point(36+i*28,55+Math.Abs(i-1.5)*13),12,17);}
   else {Star(d,80,80,54);}
   d.Pop();
  }
 }
 public static class CharacterArt {
  static BitmapSource sheet,men,women;
  static Dictionary<int,BitmapSource> trimmed=new Dictionary<int,BitmapSource>();
  public static BitmapSource Get(int index) {
   if(sheet==null){sheet=Sketch.Asset("characters.png");men=Sketch.Asset("characters-men.png");women=Sketch.Asset("characters-women.png");}
   index=Math.Max(0,Math.Min(9,index));
   if(trimmed.ContainsKey(index))return trimmed[index];
   BitmapSource source=sheet;int left,right;
   if(index==3||index==5||index==6||index==7){int n=index==3?0:index==5?1:index-4;int[] edges={265,662,1083,1515,1930};source=men;left=edges[n];right=edges[n+1];}
   else if(index>=8){int[] edges={320,762,1280};source=women;left=edges[index-8];right=edges[index-7];}
   else{int[] edges={0,435,747,1080,1425,1780,2172};left=edges[index];right=edges[index+1];}
   var result=Trim(new CroppedBitmap(source,new Int32Rect(left,0,right-left,source.PixelHeight)));trimmed[index]=result;return result;
  }
  public static BitmapSource Trim(BitmapSource cell){var rgba=new FormatConvertedBitmap(cell,PixelFormats.Bgra32,null,0);int stride=rgba.PixelWidth*4;var pixels=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(pixels,stride,0);
   int minX=rgba.PixelWidth,minY=rgba.PixelHeight,maxX=0,maxY=0;
   for(int y=0;y<rgba.PixelHeight;y++)for(int x=0;x<rgba.PixelWidth;x++)if(pixels[y*stride+x*4+3]>20){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
   minX=Math.Max(0,minX-4);minY=Math.Max(0,minY-4);maxX=Math.Min(cell.PixelWidth-1,maxX+4);maxY=Math.Min(cell.PixelHeight-1,maxY+4);
   var result=new CroppedBitmap(cell,new Int32Rect(minX,minY,maxX-minX+1,maxY-minY+1));result.Freeze();return result;
  }
 }
}
