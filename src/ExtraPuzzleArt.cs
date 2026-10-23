using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace LittleNotebook {
 public sealed partial class MiniSurface {
  static readonly Color[] PuzzleColours={Color.FromRgb(116,165,133),Color.FromRgb(224,158,156),Color.FromRgb(124,171,203),Color.FromRgb(231,190,98),Color.FromRgb(174,149,193),Color.FromRgb(212,150,106),Color.FromRgb(95,181,181),Color.FromRgb(193,167,143),Color.FromRgb(153,184,103)};
  Rect ExtraCell(int index){var b=Session.Extra;double size=Math.Min(85,350.0/Math.Max(b.W,b.H));double x=(680-b.W*size)/2,y=(408-b.H*size)/2;return new Rect(x+index%b.W*size,y+index/b.W*size,size,size);}
  Point Mid(Rect r){return new Point(r.X+r.Width/2,r.Y+r.Height/2);}
  Rect TubeRect(int index){int n=Session.Extra.Tubes.Count,columns=n<=7?n:(n+1)/2,row=index/columns;double step=Math.Min(85,610.0/columns),height=n<=7?248:135;int inRow=Math.Min(columns,n-row*columns);return new Rect((680-inRow*step)/2+(index%columns)*step+8,n<=7?90:75+row*155,step-16,height);}
  void DrawExtra(DrawingContext dc){var b=Session.Extra;
   if(b.Kind=="pour"){DrawPour(dc);return;}
   if(b.Kind=="code"){int rows=b.Limit;double step=Math.Min(25,260.0/rows);for(int row=0;row<rows;row++)for(int col=0;col<b.W;col++)dc.DrawEllipse(null,Pen(palette.Line,1),new Point(100+col*47,20+row*step),9,9);for(int row=0;row<b.Guesses.Count;row++){for(int x=0;x<b.W;x++){Point at=new Point(100+x*47,20+row*step);dc.DrawEllipse(Brush(PuzzleColours[b.Guesses[row][x]]),Pen(palette.Ink,1),at,9,9);Label(dc,((char)('A'+b.Guesses[row][x])).ToString(),at,10);}Label(dc,b.Scores[row][0]+" ●   "+b.Scores[row][1]+" ○",new Point(480,20+row*step),14);}for(int x=0;x<b.W;x++){Point at=new Point(110+x*57,308);dc.DrawEllipse(b.Cells[x]<0?Brush(palette.Paper):Brush(PuzzleColours[b.Cells[x]]),Pen(x==b.Focus?palette.Gold:palette.Ink,x==b.Focus?3:1),at,20,20);Label(dc,b.Cells[x]<0?"?":((char)('A'+b.Cells[x])).ToString(),at,17);}for(int c=0;c<b.Colours;c++){Point at=new Point(65+c*45,368);dc.DrawEllipse(Brush(PuzzleColours[c]),Pen(palette.Ink,1),at,17,17);Label(dc,((char)('A'+c)).ToString(),at,14);}dc.DrawRoundedRectangle(Brush(palette.Light),Pen(palette.Ink,1.3),new Rect(475,342,168,48),8,8);Label(dc,ar?"اختبر الرمز":"Check code",new Point(559,365),17);return;}
   for(int i=0;i<b.Cells.Length;i++){Rect r=ExtraCell(i);Point center=Mid(r);r.Inflate(-1.5,-1.5);int v=b.Cells[i];dc.DrawRoundedRectangle(v==-1?Brush(Color.FromRgb(111,138,85)):Grass(),Pen(v==-1?Color.FromRgb(88,115,64):Color.FromRgb(179,202,143),1),r,5,5);
    if(v==-1){dc.DrawLine(Pen(Color.FromArgb(70,241,244,203),1),r.TopLeft+new Vector(4,5),r.TopRight+new Vector(-4,5));continue;}
    if(b.Goal[i]==1)DrawLeaf(dc,center,Math.Min(12,r.Width*.23),palette);
    if(v==1){Rect crate=r;crate.Inflate(-5,-5);dc.DrawRoundedRectangle(Brush(Color.FromRgb(215,172,113)),Pen(Color.FromRgb(116,84,54),1.7),crate,4,4);dc.DrawLine(Pen(Color.FromRgb(151,108,61),2),crate.TopLeft+new Vector(4,4),crate.BottomRight+new Vector(-4,-4));dc.DrawLine(Pen(Color.FromRgb(151,108,61),2),crate.TopRight+new Vector(-4,4),crate.BottomLeft+new Vector(4,-4));if(b.Goal[i]==1){dc.DrawEllipse(Brush(Color.FromRgb(102,155,85)),Pen(palette.Paper,1),center,8,8);Label(dc,"✓",center,12,palette.Paper);}}

   }
   Point player=Mid(ExtraCell(b.Player));if(b.PreviousPlayer>=0&&b.Feedback>0){Point previous=Mid(ExtraCell(b.PreviousPlayer));player=previous+(player-previous)*(1-Math.Min(1,b.Feedback/.18));}DrawLadybug(dc,player,Math.Min(12,ExtraCell(b.Player).Width*.25),0,palette);
   if(b.Deadlocked){dc.DrawRoundedRectangle(Brush(palette.Paper),Pen(palette.Gold,2),new Rect(108,156,464,84),12,12);Label(dc,ar?"لا يمكن إكمال هذه الوضعية":"No winning moves remain",new Point(340,183),22);Label(dc,ar?"نعيد المستوى لتجرب من جديد…":"Resetting the garden for another try…",new Point(340,215),16);}
   if(b.Feedback>0&&b.Last>=0&&b.Last<b.Cells.Length){Rect flash=ExtraCell(b.Last);flash.Inflate(-1,-1);dc.DrawRoundedRectangle(null,Pen(b.Message=="blocked"?Color.FromRgb(195,113,105):palette.Gold,2.5),flash,5,5);}
  }
  int ExtraHit(Point point){var b=Session.Extra;if(b.Kind=="pour")return Enumerable.Range(0,b.Tubes.Count).FirstOrDefault(i=>TubeRect(i).Contains(point),-1);if(b.Kind=="code"){if(new Rect(475,342,168,48).Contains(point))return 99;for(int i=0;i<b.Colours;i++)if((point-new Point(65+i*45,368)).Length<21)return 100+i;for(int i=0;i<b.W;i++)if((point-new Point(110+i*57,308)).Length<24)return i;return -1;}return Enumerable.Range(0,b.Cells.Length).FirstOrDefault(i=>ExtraCell(i).Contains(point),-1);}
 }
}
