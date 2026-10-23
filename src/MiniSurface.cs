using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public sealed partial class MiniSurface:FrameworkElement {
  public readonly MiniSession Session;readonly Palette palette;readonly bool ar,reduced;readonly double textScale;
  public bool ShowPicture;System.Windows.Media.Imaging.BitmapSource picture;BitmapSource[] pictureTiles;
  public ScrollViewer Viewport;public Action<int> Press;public int Hover=-1;
  public Action<Point> DashDown,DashMove;public Action DashUp;
  Point down;double panX,panY;bool dragging,moved;int downCell=-1;DrawingGroup dots;
  public MiniSurface(MiniSession session,Palette colors,bool arabic,bool reducedMotion,double scale){
   Session=session;palette=colors;ar=arabic;reduced=reducedMotion;textScale=scale/.9;Focusable=true;FocusVisualStyle=null;
   Size size=BoardSize();Width=size.Width;Height=size.Height;Cursor=Cursors.Arrow;ClipToBounds=session.Kind!="arrows";
  }
  public double CellSize{get{return Session.Kind=="arrows"?42:Session.Kind=="signal"?64:Session.Kind=="tiles"?78:32;}}
  public double MarginSize{get{return Session.Kind=="signal"?50:Session.Kind=="maze"?28:24;}}
  Size BoardSize(){switch(Session.Kind){case "stack":return new Size(900,408);case "mousedash":return new Size(MouseDashBoard.Width,MouseDashBoard.Height);case "arrows":return new Size(Session.Arrows.Size*42+48,Session.Arrows.Size*42+48);case "signal":return new Size(Session.Signal.Width*64+100,Session.Signal.Height*64+100);case "tiles":return new Size(Session.Tiles.Size*78+48,Session.Tiles.Size*78+48);case "maze":return new Size(Session.Maze.Size*32+56,Session.Maze.Size*32+56);case "echo":return new Size((Session.PadCount/2)*116+48,304);default:return new Size(540,380);}}
  Brush Ink{get{return new SolidColorBrush(palette.Ink);}}
  Brush Brush(Color color){return new SolidColorBrush(color);}
  Color Blend(Color color,double alpha){return Color.FromRgb((byte)(color.R*alpha+palette.Paper.R*(1-alpha)),(byte)(color.G*alpha+palette.Paper.G*(1-alpha)),(byte)(color.B*alpha+palette.Paper.B*(1-alpha)));}
  Color Mix(Color a,Color b,double t){t=Math.Max(0,Math.Min(1,t));return Color.FromRgb((byte)(a.R+(b.R-a.R)*t),(byte)(a.G+(b.G-a.G)*t),(byte)(a.B+(b.B-a.B)*t));}
  Pen Pen(Color color,double width){return new Pen(Brush(color),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};}
  void Label(DrawingContext dc,string value,Point center,double size,Color? color=null){var text=new FormattedText(value,CultureInfo.InvariantCulture,ar?FlowDirection.RightToLeft:FlowDirection.LeftToRight,new Typeface("Segoe Print"),size*textScale,Brush(color??palette.Ink),1.0){TextAlignment=TextAlignment.Center};dc.DrawText(text,new Point(center.X,center.Y-text.Height/2));}
  Point Center(int cell,int width,double size,double margin){return new Point(margin+(cell%width+.5)*size,margin+(cell/width+.5)*size);}
  public Rect CellRect(int cell){int columns=Session.Kind=="signal"?Session.Signal.Width:Session.Kind=="arrows"?Session.Arrows.Size:Session.Kind=="maze"?Session.Maze.Size:Session.Kind=="tiles"?Session.Tiles.Size:Session.Maze.Size;double size=CellSize;return new Rect(MarginSize+cell%columns*size,MarginSize+cell/columns*size,size,size);}
  protected override void OnRender(DrawingContext dc){
   dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,Width,Height));
   switch(Session.Kind){case "stack":DrawStack(dc);break;case "mousedash":DrawDash(dc);break;case "arrows":DrawArrows(dc);break;case "signal":DrawSignal(dc);break;case "tiles":DrawTiles(dc);break;case "maze":DrawMaze(dc);break;case "echo":DrawEcho(dc);break;case "pulse":DrawPulse(dc);break;}
   if(Session.Phase=="complete"&&Session.Kind=="arrows"){Label(dc,ar?"أحسنت!":"Beautifully cleared!",new Point(Width/2,Height/2-15),23);Label(dc,ar?"انقر للمتابعة · أو استمع إلى اللحن":"Click to continue · or enjoy the tune",new Point(Width/2,Height/2+22),16);}
  }
  void DrawArrows(DrawingContext dc){
   var board=Session.Arrows;if(dots==null){dots=new DrawingGroup();using(var context=dots.Open())for(int y=0;y<board.Size;y++)for(int x=0;x<board.Size;x++)context.DrawEllipse(Brush(Blend(palette.Accent,.45)),null,Center(y*board.Size+x,board.Size,42,24),1.35,1.35);dots.Freeze();}dc.DrawDrawing(dots);
   for(int i=0;i<board.Arrows.Count;i++){
    var arrow=board.Arrows[i];var flight=Session.Flights.FirstOrDefault(f=>f.Index==i);if(arrow.Removed&&flight==null)continue;bool active=flight!=null;double travel=0;if(active&&!reduced){double t=Math.Min(1,flight.Time/flight.Duration);travel=flight.Distance*(flight.Blocked?Math.Sin((t<.44?t/.44:(1-t)/.56)*Math.PI/2):t);}
    var points=MovingArrow(arrow,board.Size,travel);Color color=active&&flight.Blocked?Color.FromRgb(192,88,84):palette.Ink;
    if(i==Hover||(IsKeyboardFocusWithin&&i==Session.SelectedCell)||active)DrawPath(dc,points,Pen(Blend(active&&flight.Blocked?Color.FromRgb(211,125,111):palette.Gold,.48),8));
    DrawPath(dc,points,Pen(color,3.2));Point head=points.Last();var direction=new Vector(MiniGrid.Dx[arrow.Direction],MiniGrid.Dy[arrow.Direction]);var normal=new Vector(-direction.Y,direction.X);
    DrawPath(dc,new[]{head-direction*9+normal*5,head,head-direction*9-normal*5},Pen(color,3.2));
   }
  }
  public static List<Point> MovingArrow(EscapeArrow arrow,int size,double travel){
   // The tail follows each bend; the head extends along its own exit lane.
   double length=arrow.Cells.Count-1;Func<double,Point> sample=distance=>{if(distance>=length){int head=arrow.Head;return new Point(head%size+.5+MiniGrid.Dx[arrow.Direction]*(distance-length),head/size+.5+MiniGrid.Dy[arrow.Direction]*(distance-length));}int index=Math.Min(arrow.Cells.Count-2,(int)Math.Floor(Math.Max(0,distance)));double t=distance-index;int a=arrow.Cells[index],b=arrow.Cells[index+1];return new Point(a%size+.5+(b%size-a%size)*t,a/size+.5+(b/size-a/size)*t);};
   var path=new List<Point>{sample(travel)};for(int i=(int)Math.Floor(travel)+1;i<length;i++)path.Add(sample(i));path.Add(sample(length+travel));return path.Select(point=>new Point(24+point.X*42,24+point.Y*42)).ToList();
  }
  static void DrawPath(DrawingContext dc,IEnumerable<Point> values,Pen pen){var points=values.ToArray();if(points.Length<2)return;var shape=new StreamGeometry();using(var c=shape.Open()){c.BeginFigure(points[0],false,false);c.PolyLineTo(points.Skip(1).ToArray(),true,false);}dc.DrawGeometry(null,pen,shape);}
  void DrawSignal(DrawingContext dc){
   var board=Session.Signal;bool[] lit=board.Powered();double size=64;
   for(int cell=0;cell<board.Masks.Length;cell++)foreach(int d in new[]{1,2}){int next=MiniGrid.Next(cell,d,board.Width,board.Height);if(next<0||(board.Masks[cell]&(1<<d))==0||(board.Masks[next]&(1<<MiniGrid.Opposite(d)))==0||Session.TurnTime<.14&&(cell==Session.TurningCell||next==Session.TurningCell)&&!reduced)continue;Point center=Center(cell,board.Width,size,50);Vector direction=new Vector(MiniGrid.Dx[d],MiniGrid.Dy[d]);dc.DrawLine(Pen(lit[cell]?palette.Accent:Blend(palette.Ink,.35),4),center+direction*29,center+direction*35);}
   for(int cell=0;cell<board.Masks.Length;cell++){
    Rect r=CellRect(cell);r.Inflate(-3,-3);dc.DrawRoundedRectangle(Brush(lit[cell]?palette.Light:Blend(palette.Line,.19)),Pen(cell==Session.SelectedCell?palette.Gold:palette.Line,cell==Session.SelectedCell?2.5:1),r,7,7);
    Point center=Center(cell,board.Width,size,50);int mask=board.Masks[cell];dc.PushClip(new RectangleGeometry(r));bool turning=cell==Session.TurningCell&&Session.TurnTime<.14&&!reduced;if(turning){mask=Session.TurningMask;dc.PushTransform(new RotateTransform(Math.Min(1,Session.TurnTime/.14)*90,center.X,center.Y));}
    Color color=lit[cell]?palette.Accent:Blend(palette.Ink,.48);if(palette.Name=="night"&&lit[cell])color=palette.Ink;
    for(int d=0;d<4;d++)if((mask&(1<<d))!=0)dc.DrawLine(Pen(color,5.8),center,center+new Vector(MiniGrid.Dx[d]*29,MiniGrid.Dy[d]*29));dc.DrawEllipse(Brush(color),null,center,3,3);if(turning)dc.Pop();dc.Pop();
   }
   double inputY=Center(board.Source,board.Width,size,50).Y,outputY=Center(board.Destination,board.Width,size,50).Y;
   dc.DrawLine(Pen(palette.Accent,4),new Point(12,inputY),new Point(48,inputY));dc.DrawLine(Pen(lit[board.Destination]&&(board.Masks[board.Destination]&2)!=0?palette.Gold:palette.Ink,4),new Point(50+board.Width*size,outputY),new Point(86+board.Width*size,outputY));
   Label(dc,ar?"من":"IN",new Point(25,inputY-23),12);Label(dc,ar?"إلى":"OUT",new Point(75+board.Width*size,outputY-23),12);
  }
  void DrawTile(DrawingContext dc,int value,Rect r){
   if(picture==null){var files=System.IO.Directory.GetFiles(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets/pictures"),"*.jpg").OrderBy(f=>f).ToArray();picture=Sketch.Asset("pictures/"+System.IO.Path.GetFileName(files[(Session.Level-1)%files.Length]));}
   r.Inflate(-1,-1);int n=Session.Tiles.Size,index=value-1;double side=Math.Min(picture.PixelWidth,picture.PixelHeight);if(pictureTiles==null)pictureTiles=new BitmapSource[n*n];if(pictureTiles[index]==null){pictureTiles[index]=new CroppedBitmap(picture,new Int32Rect((int)((picture.PixelWidth-side)/2+index%n*side/n),(int)((picture.PixelHeight-side)/2+index/n*side/n),(int)(side/n),(int)(side/n)));pictureTiles[index].Freeze();}dc.DrawImage(pictureTiles[index],r);dc.DrawRectangle(null,Pen(palette.Ink,.8),r);
  }
  void DrawTiles(DrawingContext dc){
   var board=Session.Tiles;if(ShowPicture||Session.Phase=="complete"){for(int cell=0;cell<board.Tiles.Length;cell++)DrawTile(dc,cell+1,CellRect(cell));return;}bool moving=Session.MoveTime<.14&&!reduced;
   for(int cell=0;cell<board.Tiles.Length;cell++){int value=board.Tiles[cell];if(value==0){Rect r=CellRect(cell);r.Inflate(-5,-5);dc.DrawRoundedRectangle(null,Pen(palette.Line,1.2),r,9,9);}else if(!moving||cell!=Session.MovingTo)DrawTile(dc,value,CellRect(cell));}
   if(moving){Rect from=CellRect(Session.MovingFrom),to=CellRect(Session.MovingTo);double t=1-Math.Pow(1-Math.Min(1,Session.MoveTime/.14),3);DrawTile(dc,Session.MovingValue,new Rect(from.X+(to.X-from.X)*t,from.Y+(to.Y-from.Y)*t,from.Width,from.Height));}
  }
  void Star(DrawingContext dc,Point center,double radius){var points=Enumerable.Range(0,10).Select(i=>{double angle=-Math.PI/2+i*Math.PI/5,r=i%2==0?radius:radius*.45;return new Point(center.X+Math.Cos(angle)*r,center.Y+Math.Sin(angle)*r);}).ToArray();var shape=new StreamGeometry();using(var c=shape.Open()){c.BeginFigure(points[0],true,true);c.PolyLineTo(points.Skip(1).ToArray(),true,false);}dc.DrawGeometry(Brush(palette.Gold),Pen(palette.Ink,1.1),shape);}
  void DrawMaze(DrawingContext dc){
   var board=Session.Maze;double size=32,margin=28;
   if(board.Visited!=null)for(int cell=0;cell<board.Visited.Length;cell++)if(board.Visited[cell]&&cell!=board.Player&&cell!=board.Goal)dc.DrawEllipse(Brush(Blend(palette.Accent,.50)),null,Center(cell,board.Size,size,margin),2.6,2.6);
   for(int cell=0;cell<board.Walls.Length;cell++){Rect r=CellRect(cell);int mask=board.Walls[cell];if((mask&1)!=0)dc.DrawLine(Pen(palette.Ink,2),r.TopLeft,r.TopRight);if((mask&8)!=0)dc.DrawLine(Pen(palette.Ink,2),r.TopLeft,r.BottomLeft);if(cell/board.Size==board.Size-1&&(mask&4)!=0)dc.DrawLine(Pen(palette.Ink,2),r.BottomLeft,r.BottomRight);if(cell%board.Size==board.Size-1&&(mask&2)!=0)dc.DrawLine(Pen(palette.Ink,2),r.TopRight,r.BottomRight);}
   DrawLeaf(dc,Center(board.Goal,board.Size,size,margin),11,palette);
   Point player=new Point(margin+(Session.MazeX+.5)*size,margin+(Session.MazeY+.5)*size);
   DrawLadybug(dc,player,10,Session.MazeFacing*90,palette,Session.MoveTime<MiniSession.MazeStepSeconds?Math.Sin(Session.Elapsed*40)*.7:0);
  }
  internal static void DrawLeaf(DrawingContext dc,Point center,double radius,Palette colors){
   dc.PushTransform(new TranslateTransform(center.X,center.Y));dc.PushTransform(new ScaleTransform(radius/10,radius/10));
   var outline=new Pen(new SolidColorBrush(colors.Ink),1.15){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
   dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(139,172,106)),outline,Geometry.Parse("M-7,8 C-15,-4 -3,-10 10,-10 C11,1 5,13 -7,8 Z"));
   dc.DrawGeometry(null,outline,Geometry.Parse("M-10,12 L7,-7 M-4,5 L-7,-1 M0,1 L5,3 M3,-2 L1,-6"));dc.Pop();dc.Pop();
  }
  internal static void DrawLadybug(DrawingContext dc,Point center,double radius,double angle,Palette colors,double stride=0){
   dc.PushTransform(new TranslateTransform(center.X,center.Y));dc.PushTransform(new RotateTransform(angle));dc.PushTransform(new ScaleTransform(radius/10,radius/10));
   var dark=new SolidColorBrush(Color.FromRgb(57,51,51));var line=new Pen(dark,1.15){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
   for(int side=-1;side<=1;side+=2)for(int leg=0;leg<3;leg++){double y=-3+leg*4,swing=stride*(leg==1?-1:1);dc.DrawLine(line,new Point(side*5.5,y),new Point(side*9.5,y+(leg-1)*2+swing));}
   dc.DrawLine(line,new Point(-2,-7),new Point(-4,-11));dc.DrawLine(line,new Point(2,-7),new Point(4,-11));dc.DrawEllipse(dark,null,new Point(0,-6),4.3,4);
   dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(203,91,78)),line,new Point(0,1),7.1,8.1);dc.DrawLine(line,new Point(0,-5.5),new Point(0,8.7));
   foreach(int side in new[]{-1,1})foreach(double y in new[]{-2.0,3.8})dc.DrawEllipse(dark,null,new Point(side*(y<0?3.3:3.5),y),1.6,1.65);
   dc.DrawEllipse(Brushes.Ivory,null,new Point(-1.8,-8),.75,.75);dc.DrawEllipse(Brushes.Ivory,null,new Point(1.8,-8),.75,.75);dc.Pop();dc.Pop();dc.Pop();
  }
  public static readonly string[] EchoKeys={"A","S","D","F","J","K","L",";"};
  Rect PadRect(int pad){int columns=Session.PadCount/2;return new Rect(24+pad%columns*116,30+pad/columns*130,100,108);}
  void DrawEcho(DrawingContext dc){Color[] colors={palette.Accent,palette.Gold,Color.FromRgb(211,144,145),Color.FromRgb(126,166,192),Color.FromRgb(178,151,196),Color.FromRgb(126,185,178),Color.FromRgb(220,170,127),Color.FromRgb(170,187,128)};for(int i=0;i<Session.PadCount;i++){Rect r=PadRect(i);bool lit=Session.Highlight==i;dc.DrawRoundedRectangle(Brush(Blend(colors[i],lit?1:.32)),Pen(Session.WrongFeedback&&Session.FeedbackTime>0?Color.FromRgb(188,91,83):lit?palette.Gold:palette.Ink,lit?4:1.5),r,15,15);Star(dc,new Point(r.X+50,r.Y+37),lit?16:12);Label(dc,EchoKeys[i],new Point(r.X+50,r.Y+82),21);}}
  void DrawPulse(DrawingContext dc){
   Point center=new Point(270,172);double radius=130;dc.DrawEllipse(null,Pen(palette.Light,22),center,radius,radius);double amount=Session.Phase=="complete"||Session.Phase=="latch"?1:Session.Charge;
   if(amount>=.999)dc.DrawEllipse(null,Pen(palette.Accent,22),center,radius,radius);
   else if(amount>0){double angle=(-90+amount*360)*Math.PI/180;var geometry=new StreamGeometry();using(var c=geometry.Open()){c.BeginFigure(new Point(center.X,center.Y-radius),false,false);c.ArcTo(new Point(center.X+Math.Cos(angle)*radius,center.Y+Math.Sin(angle)*radius),new Size(radius,radius),0,amount>.5,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,Pen(Session.WrongFeedback&&Session.FeedbackTime>0?Color.FromRgb(191,98,89):palette.Accent,22),geometry);}
   dc.DrawEllipse(Brush(Session.FeedbackTime>0?palette.Light:palette.Paper),Pen(palette.Ink,1.6),center,61,61);
   if(Session.Phase=="complete")Label(dc,"✓",center,49);
   else if(Session.Phase=="ready"){Label(dc,"E",center+new Vector(0,-17),41);Label(dc,ar?"جاهز؟":"Ready?",center+new Vector(0,29),16);}
   else Label(dc,"E",center,49);
   for(int i=0;i<3;i++)dc.DrawEllipse(Brush(i<Session.Latch?palette.Gold:palette.Light),Pen(palette.Ink,1),new Point(242+i*28,335),6,6);
  }
  internal static void DrawPest(DrawingContext dc,Point center,double r){var dark=new SolidColorBrush(Color.FromRgb(101,81,58));var line=new Pen(dark,1);dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160,173,202,208)),line,center+new Vector(-r*.45,-r*.35),r*.7,r*.45);dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160,173,202,208)),line,center+new Vector(r*.45,-r*.35),r*.7,r*.45);dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(222,174,67)),line,center,r*.58,r*.75);dc.DrawLine(line,center+new Vector(-r*.5,0),center+new Vector(r*.5,0));dc.DrawEllipse(dark,null,center+new Vector(0,-r*.6),r*.3,r*.3);}
  void DrawDash(DrawingContext dc){
   var board=Session.Dash;dc.DrawRoundedRectangle(null,Pen(Blend(palette.Ink,.35),1.2),new Rect(1,1,Width-2,Height-2),12,12);
   for(int y=25;y<Height;y+=28)for(int x=24;x<Width;x+=28)dc.DrawEllipse(Brush(Blend(palette.Line,.42)),null,new Point(x,y),.8,.8);
   foreach(var dot in board.Obstacles){
    Point center=new Point(dot.X,dot.Y);Color color=Blend(palette.Ink,.56);
    if(dot.Warning>0){var outline=Pen(palette.Gold,1.8);outline.DashStyle=DashStyles.Dash;dc.DrawEllipse(Brush(Blend(palette.Gold,.12)),outline,center,dot.Radius+3,dot.Radius+3);double length=Math.Sqrt(dot.VX*dot.VX+dot.VY*dot.VY);dc.DrawLine(Pen(palette.Gold,1.4),center,center+new Vector(dot.VX/length*14,dot.VY/length*14));}
    else{if(board.Collision==dot)color=Color.FromRgb(188,91,83);DrawPest(dc,center,dot.Radius);}
   }
   Point ball=new Point(board.X,board.Y);if(!board.Started)ball=new Point(Width/2,Height/2+53);
   Color ballColor=board.Hit?Color.FromRgb(188,91,83):palette.Accent;
   DrawLadybug(dc,ball,MouseDashBoard.PlayerRadius,0,palette);
   if(Session.Phase=="holding"||Session.Phase=="waiting")dc.DrawEllipse(null,Pen(palette.Gold,1.8),ball,16,16);
   if(board.Hit)dc.DrawEllipse(null,Pen(Color.FromRgb(188,91,83),2),ball,21,21);
   if(!board.Started){Label(dc,ar?"أمسك أي موضع للبدء":"Hold anywhere to begin",new Point(Width/2,Height/2-27),24);Label(dc,ar?"حرّك الكرة وتجنّب النقاط · اتركها للإيقاف":"Guide your ladybug past the pests · let go to pause",new Point(Width/2,Height/2+9),14);}
   else if(Session.Phase=="holding"){
    dc.DrawRoundedRectangle(Brush(palette.Paper),Pen(palette.Line,1),new Rect(Width/2-145,13,290,36),9,9);Label(dc,ar?"أمسك الكرة للمتابعة":"Hold your ladybug to continue",new Point(Width/2,31),14);
   }
  }
  static double Distance(Point point,Point a,Point b){Vector line=b-a;double t=line.LengthSquared==0?0:Math.Max(0,Math.Min(1,Vector.Multiply(point-a,line)/line.LengthSquared));return (point-(a+line*t)).Length;}
  public int Hit(Point point){
   if(Session.Stack!=null)return new Rect(0,0,Width,Height).Contains(point)?0:-1;
   if(Session.Dash!=null)return -1;
   if(Session.Kind=="arrows"){int best=-1;double nearest=16;for(int i=0;i<Session.Arrows.Arrows.Count;i++){var arrow=Session.Arrows.Arrows[i];if(arrow.Removed||Session.Flights.Any(f=>f.Index==i))continue;var path=MovingArrow(arrow,Session.Arrows.Size,0);for(int j=1;j<path.Count;j++){double d=Distance(point,path[j-1],path[j]);if(d<nearest){nearest=d;best=i;}}}return best;}
   if(Session.Kind=="pulse")return (point-new Point(270,172)).Length<155?0:-1;
   if(Session.Kind=="echo"){for(int i=0;i<Session.PadCount;i++)if(PadRect(i).Contains(point))return i;return -1;}
   int width=Session.Kind=="signal"?Session.Signal.Width:Session.Kind=="tiles"?Session.Tiles.Size:Session.Maze.Size,height=Session.Kind=="signal"?Session.Signal.Height:width;
   int x=(int)Math.Floor((point.X-MarginSize)/CellSize),y=(int)Math.Floor((point.Y-MarginSize)/CellSize);return x<0||x>=width||y<0||y>=height?-1:y*width+x;
  }
  public bool CanPan{get{return Viewport!=null&&(Viewport.ScrollableWidth>1||Viewport.ScrollableHeight>1);}}
  public bool Panning{get{return dragging;}}
  public bool BeginPan(Point position,int cell){if(!CanPan)return false;down=position;panX=Viewport.HorizontalOffset;panY=Viewport.VerticalOffset;downCell=cell;dragging=true;moved=false;Cursor=PanCursor.Closed;return true;}
  public void MovePan(Point position){if(!dragging||Viewport==null)return;Vector delta=position-down;if(delta.Length>5)moved=true;if(moved){Viewport.ScrollToHorizontalOffset(panX-delta.X);Viewport.ScrollToVerticalOffset(panY-delta.Y);}}
  public void EndPan(){dragging=false;Cursor=Cursors.Arrow;}
  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){
   if(Session.Dash!=null){Focus();if(DashDown!=null)DashDown(e.GetPosition(this));if(Session.Phase=="playing"){CaptureMouse();Cursor=Cursors.None;}e.Handled=true;return;}
   if(Session.Kind=="arrows"&&Session.Phase=="complete"){if(Press!=null)Press(0);e.Handled=true;return;}
   Focus();int hit=Hit(e.GetPosition(this));bool pan=hit<0||Session.Kind=="maze";
   if(pan&&BeginPan(e.GetPosition(Viewport??(IInputElement)this),hit))CaptureMouse();
   else if(hit>=0&&Press!=null)Press(hit);e.Handled=true;
  }
  protected override void OnMouseDown(MouseButtonEventArgs e){if(e.ChangedButton==MouseButton.Middle&&Session.Dash==null&&BeginPan(e.GetPosition(Viewport??(IInputElement)this),-1)){CaptureMouse();e.Handled=true;return;}base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseButtonEventArgs e){if(e.ChangedButton==MouseButton.Middle&&dragging){EndPan();ReleaseMouseCapture();e.Handled=true;return;}base.OnMouseUp(e);}
  protected override void OnMouseMove(MouseEventArgs e){
   if(Session.Dash!=null){if(IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed&&DashMove!=null)DashMove(e.GetPosition(this));if(Session.Phase!="playing"&&IsMouseCaptured)CancelDashPointer();e.Handled=true;return;}
   if(dragging&&Viewport!=null){MovePan(e.GetPosition(Viewport));e.Handled=true;return;}
   if(Session.Kind=="arrows"){int next=Hit(e.GetPosition(this));Cursor=Cursors.Arrow;if(next!=Hover){Hover=next;InvalidateVisual();}}
  }
  public void CancelDashPointer(){if(Session.Dash==null)return;Cursor=Cursors.Arrow;if(IsMouseCaptured)ReleaseMouseCapture();}
  protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(Session.Dash!=null){CancelDashPointer();if(DashUp!=null)DashUp();e.Handled=true;return;}if(dragging){EndPan();ReleaseMouseCapture();if(!moved&&Session.Kind=="maze"&&downCell>=0&&Press!=null)Press(downCell);e.Handled=true;}}
  protected override void OnLostMouseCapture(MouseEventArgs e){EndPan();if(Session.Dash!=null){if(DashUp!=null)DashUp();}base.OnLostMouseCapture(e);}
  protected override void OnMouseLeave(MouseEventArgs e){if(!dragging&&Hover!=-1){Hover=-1;InvalidateVisual();}base.OnMouseLeave(e);}
 }
 public sealed class MiniGameBadge:FrameworkElement {
  readonly string kind;readonly Palette palette;
  public MiniGameBadge(string key,Palette colors){kind=key;palette=colors;Width=38;Height=38;IsHitTestVisible=false;}
  protected override void OnRender(DrawingContext dc){
   if(kind=="mousedash"){MiniSurface.DrawLadybug(dc,new Point(15,23),11,15,palette);MiniSurface.DrawPest(dc,new Point(30,9),7);return;}
   if(kind=="maze"){MiniSurface.DrawLadybug(dc,new Point(17,22),13,30,palette);MiniSurface.DrawLeaf(dc,new Point(30,8),7,palette);return;}
   var paths=new Dictionary<string,string>{
    {"arrows","M15,78 L40,78 L40,51 L73,51 L73,15 M61,27 L73,15 L85,27 M16,18 L38,18 L38,36 M28,26 L38,36 L48,26"},
    {"signal","M12,27 L40,27 L40,72 L68,72 L68,27 L88,27 M12,59 L24,59 M83,58 L83,85 L55,85"},
    {"pulse","M79,48 A30,30 0 1 1 50,18 M50,8 L50,26 M72,29 L84,17 M38,47 L62,47 L62,72 L38,72 Z M42,47 L42,39 Q50,27 58,39 L58,47"},
    {"stack","M12,75 L88,75 L88,91 L12,91 Z M22,50 L79,50 L79,67 L22,67 Z M35,25 L80,25 L80,42 L35,42 Z M17,8 L56,8 L56,18 L17,18 Z M68,9 L88,9 M83,3 L90,9 L83,15"},
    {"tiles","M8,8 L44,8 L44,44 L8,44 Z M56,8 L92,8 L92,44 L56,44 Z M8,56 L44,56 L44,92 L8,92 Z M64,65 L84,84 M84,65 L64,84"},
    {"echo","M8,19 L92,19 L92,87 L8,87 Z M29,19 L29,87 M50,19 L50,87 M71,19 L71,87 M22,19 L36,19 L36,54 L22,54 Z M64,19 L78,19 L78,54 L64,54 Z"},
    {"maze","M9,9 L90,9 L90,90 L9,90 L9,9 M34,9 L34,36 L63,36 L63,63 L90,63 M9,63 L34,63 L34,90 M63,9 L63,23 M63,78 L63,90 M77,77 L82,82 M82,77 L77,82"},
    {"mousedash","M62,51 A12,12 0 1 1 38,51 A12,12 0 1 1 62,51 M50,28 L50,18 M73,51 L83,51 M27,51 L17,51 M50,74 L50,84 M17,15 A5,5 0 1 1 7,15 A5,5 0 1 1 17,15 M89,19 A5,5 0 1 1 79,19 A5,5 0 1 1 89,19 M86,84 A5,5 0 1 1 76,84 A5,5 0 1 1 86,84 M23,83 A5,5 0 1 1 13,83 A5,5 0 1 1 23,83"}
   };dc.PushTransform(new ScaleTransform(Width/100,Height/100));dc.DrawGeometry(new SolidColorBrush(kind=="stack"?Color.FromRgb(235,193,112):kind=="tiles"?Color.FromRgb(138,187,201):Color.FromRgb(137,174,117)),new Pen(new SolidColorBrush(palette.Ink),4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round},Geometry.Parse(paths[kind]));dc.Pop();
  }
 }
}
