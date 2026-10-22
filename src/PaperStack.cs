using System;
using System.Collections.Generic;

namespace LittleNotebook {
 public sealed class StackLayer {public double X,Width;public int Row;public bool Perfect;}
 public sealed class StackChip {public double X,Y,Width,VX,VY,Angle,Spin,Age;public int Row;}
 public sealed class PaperStackBoard {
  public const double Width=900,Height=408,BaseWidth=240,Floor=350,LayerHeight=22,Left=85,Right=815;
  public readonly List<StackLayer> Layers=new List<StackLayer>();public readonly List<StackChip> Chips=new List<StackChip>();
  public double MovingX,PieceWidth=BaseWidth,Camera,Elapsed,LandingAge=10,Wait,LastTap=-10;
  public int Score,Streak,Direction=1;public bool Started,Over,Perfect,Grew;public string Phase="ready";
  public StackLayer Top{get{return Layers[Layers.Count-1];}}
  public static double Speed(int score){return Math.Min(1040,170+Math.Max(0,score)*17.0+Math.Min(150,score*(double)score*.48));}
  public static double PerfectWindow(double width){return Math.Max(.8,Math.Min(4.5,width*.025));}
  public PaperStackBoard(int seed){Layers.Add(new StackLayer{X=(Width-BaseWidth)/2,Width=BaseWidth});Direction=(seed&1)==0?1:-1;MovingX=Direction==1?Left:Right-PieceWidth;}
  void Chip(double x,double width,int side){if(width<=.01)return;Chips.Add(new StackChip{X=x,Y=Floor-(Score+1)*LayerHeight,Width=width,VX=side*85,VY=-65,Spin=side*175,Row=Score+1});}
  public bool Tap(){
   if(Over||Phase=="miss"||Elapsed-LastTap<.12)return false;
   if(!Started){Started=true;Phase="moving";LastTap=Elapsed;return true;}
   if(Phase!="moving")return false;LastTap=Elapsed;double left=Math.Max(MovingX,Top.X),right=Math.Min(MovingX+PieceWidth,Top.X+Top.Width),overlap=right-left;
   if(overlap<=0){Chip(MovingX,PieceWidth,Direction);Phase="miss";Wait=.65;Streak=0;return true;}
   Perfect=Math.Abs(MovingX-Top.X)<=PerfectWindow(Top.Width);Grew=false;
   if(Perfect){left=Top.X;overlap=Top.Width;Streak++;if(Streak%3==0&&overlap<BaseWidth){double added=Math.Min(BaseWidth-overlap,Math.Max(4,overlap*.08));left-=added/2;overlap+=added;Grew=true;}}
   else{Chip(MovingX,Math.Max(0,Top.X-MovingX),-1);Chip(right,Math.Max(0,MovingX+PieceWidth-right),1);Streak=0;}
   Score++;Layers.Add(new StackLayer{X=left,Width=overlap,Row=Score,Perfect=Perfect});if(Layers.Count>64)Layers.RemoveAt(0);PieceWidth=overlap;MovingX=left;Phase="settling";Wait=.20;LandingAge=0;return true;
  }
  public void Tick(double seconds){
   if(seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds)||Over)return;double dt=Math.Min(.1,seconds);Elapsed+=dt;LandingAge+=dt;
   double target=Math.Max(0,(Score-7)*LayerHeight);Camera+=(target-Camera)*(1-Math.Exp(-dt*11));
   foreach(var chip in Chips){chip.Age+=dt;chip.X+=chip.VX*dt;chip.Y+=chip.VY*dt+340*dt*dt;chip.VY+=680*dt;chip.Angle+=chip.Spin*dt;}Chips.RemoveAll(c=>c.Age>1.6||c.Y+Camera>Height+80);
   if(Phase=="moving"){
    MovingX+=Direction*Speed(Score)*dt;while(MovingX<Left||MovingX>Right-PieceWidth){if(MovingX<Left){MovingX=2*Left-MovingX;Direction=1;}else{MovingX=2*(Right-PieceWidth)-MovingX;Direction=-1;}}
   }else if(Phase=="settling"){Wait-=dt;if(Wait<=0){Phase="moving";Direction=-Direction;MovingX=Direction==1?Left:Right-PieceWidth;}}
   else if(Phase=="miss"){Wait-=dt;if(Wait<=0){Over=true;Phase="over";}}
  }
 }
}
