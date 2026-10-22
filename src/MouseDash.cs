using System;
using System.Collections.Generic;

namespace LittleNotebook {
 public sealed class DashObstacle {
  public double X,Y,VX,VY,Radius,Warning;
 }
 // Fixed simulation steps and swept circles keep fast pointer moves and fast dots honest.
 public sealed class MouseDashBoard {
  public const double Width=900,Height=408,PlayerRadius=8;
  public readonly List<DashObstacle> Obstacles=new List<DashObstacle>();
  readonly Random random;double accumulator,spawnAt=5;
  public double X=Width/2,Y=Height/2,Seconds;public bool Started,Hit;
  public DashObstacle Collision;
  public int Wave{get{return 1+(int)(Seconds/15);}}
  public MouseDashBoard(int seed){random=new Random(seed);}
  public static double Clamp(double value,double min,double max){return Math.Max(min,Math.Min(max,value));}
  public static bool SweptHit(double ax,double ay,double bx,double by,double radius){
   double dx=bx-ax,dy=by-ay,length=dx*dx+dy*dy,t=length==0?0:Clamp(-(ax*dx+ay*dy)/length,0,1);
   double x=ax+dx*t,y=ay+dy*t;return x*x+y*y<=radius*radius;
  }
  public bool Begin(double x,double y){
   if(Started||Hit||double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y))return false;
   X=Clamp(x,PlayerRadius+3,Width-PlayerRadius-3);Y=Clamp(y,PlayerRadius+3,Height-PlayerRadius-3);Started=true;
   for(int i=0;i<14;i++){
    var dot=new DashObstacle{Radius=5+random.NextDouble()*6};
    do{dot.X=dot.Radius+random.NextDouble()*(Width-2*dot.Radius);dot.Y=dot.Radius+random.NextDouble()*(Height-2*dot.Radius);}while((dot.X-X)*(dot.X-X)+(dot.Y-Y)*(dot.Y-Y)<140*140);
    double angle=random.NextDouble()*Math.PI*2,speed=48+random.NextDouble()*34;dot.VX=Math.Cos(angle)*speed;dot.VY=Math.Sin(angle)*speed;Obstacles.Add(dot);
   }return true;
  }
  public bool Move(double x,double y){
   if(!Started||Hit||double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y))return false;
   x=Clamp(x,PlayerRadius+3,Width-PlayerRadius-3);y=Clamp(y,PlayerRadius+3,Height-PlayerRadius-3);
   double first=double.MaxValue;DashObstacle contact=null;
   foreach(var dot in Obstacles)if(dot.Warning<=0&&SweptHit(X-dot.X,Y-dot.Y,x-dot.X,y-dot.Y,PlayerRadius+dot.Radius)){
    // Stop at the first contact on this motion, rather than teleporting past a dot.
    double dx=x-X,dy=y-Y,a=dx*dx+dy*dy,b=2*((X-dot.X)*dx+(Y-dot.Y)*dy),c=(X-dot.X)*(X-dot.X)+(Y-dot.Y)*(Y-dot.Y)-Math.Pow(PlayerRadius+dot.Radius,2);
    double t=a>0?Clamp((-b-Math.Sqrt(Math.Max(0,b*b-4*a*c)))/(2*a),0,1):0;if(t<first){first=t;contact=dot;}
   }
   if(contact!=null){X+=(x-X)*first;Y+=(y-Y)*first;Crash(contact);return false;}
   X=x;Y=y;return true;
  }
  void Crash(DashObstacle dot){Hit=true;Collision=dot;}
  void Spawn(){
   var dot=new DashObstacle{Radius=6+random.NextDouble()*6,Warning=.95};int edge=random.Next(4);double inset=dot.Radius+4;
   dot.X=edge==1?Width-inset:edge==3?inset:inset+random.NextDouble()*(Width-2*inset);
   dot.Y=edge==0?inset:edge==2?Height-inset:inset+random.NextDouble()*(Height-2*inset);
   double tx=90+random.NextDouble()*(Width-180),ty=50+random.NextDouble()*(Height-100),dx=tx-dot.X,dy=ty-dot.Y,length=Math.Sqrt(dx*dx+dy*dy),speed=48+random.NextDouble()*34;
   dot.VX=dx/length*speed;dot.VY=dy/length*speed;Obstacles.Add(dot);
  }
  public void Tick(double seconds){
   if(!Started||Hit||seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds))return;
   accumulator+=Math.Min(.1,seconds);const double step=1.0/120;
   while(accumulator+1e-9>=step&&!Hit){accumulator-=step;Seconds+=step;double speed=1+Math.Min(1.65,Seconds/65);
    foreach(var dot in Obstacles){
     if(dot.Warning>0){dot.Warning=Math.Max(0,dot.Warning-step);continue;}
     double oldX=dot.X,oldY=dot.Y;dot.X+=dot.VX*speed*step;dot.Y+=dot.VY*speed*step;
     if(dot.X<dot.Radius){dot.X=2*dot.Radius-dot.X;dot.VX=Math.Abs(dot.VX);}else if(dot.X>Width-dot.Radius){dot.X=2*(Width-dot.Radius)-dot.X;dot.VX=-Math.Abs(dot.VX);}
     if(dot.Y<dot.Radius){dot.Y=2*dot.Radius-dot.Y;dot.VY=Math.Abs(dot.VY);}else if(dot.Y>Height-dot.Radius){dot.Y=2*(Height-dot.Radius)-dot.Y;dot.VY=-Math.Abs(dot.VY);}
     if(SweptHit(oldX-X,oldY-Y,dot.X-X,dot.Y-Y,PlayerRadius+dot.Radius)){Crash(dot);break;}
    }
    if(!Hit&&Seconds>=spawnAt&&Obstacles.Count<38){Spawn();spawnAt+=Math.Max(1.6,4-Seconds/55);}
   }
  }
 }
}
