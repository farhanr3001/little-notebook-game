using System;
using System.Linq;
using System.Windows;

namespace LittleNotebook {
 public static class MouseDashChecks {
  public static void Run(Action<bool,string> check){
   foreach(int seed in Enumerable.Range(1,30))foreach(var point in new[]{new Point(12,12),new Point(450,204),new Point(888,396)}){
    var board=new MouseDashBoard(seed);check(board.Begin(point.X,point.Y)&&board.Obstacles.Count==14,"Mouse Dash did not start with fourteen dots");
    check(board.Obstacles.All(d=>Math.Pow(d.X-board.X,2)+Math.Pow(d.Y-board.Y,2)>=140*140),"Dot spawned too close to starting ball");
    board.Tick(.1);check(!board.Hit,"Mouse Dash started with an unavoidable contact");check(!board.Begin(22,22),"Second start reset a live dash");
   }
   check(MouseDashBoard.SweptHit(-100,0,100,0,12),"Swept collision missed a crossing");check(!MouseDashBoard.SweptHit(-100,13,100,13,12),"Swept collision falsely hit a near miss");check(MouseDashBoard.SweptHit(0,0,0,0,12),"Stationary overlap missed");
   var jump=new MiniSession("mousedash",1,19);jump.HoldDash(100,100);jump.Dash.Obstacles.Clear();var near=new DashObstacle{X=150,Y=100,Radius=6};var far=new DashObstacle{X=210,Y=100,Radius=6};jump.Dash.Obstacles.Add(far);jump.Dash.Obstacles.Add(near);int failures=0;jump.Lost+=()=>failures++;
   check(!jump.MoveDash(300,100)&&jump.Phase=="failed"&&jump.Dash.Collision==near&&Math.Abs(jump.Dash.X-136)<.001,"Pointer jump skipped earliest obstacle");jump.MoveDash(400,100);jump.Tick(.1);check(failures==1,"Mouse Dash failure raised more than once");
   var moving=new MouseDashBoard(12);moving.Begin(200,200);moving.Obstacles.Clear();moving.Obstacles.Add(new DashObstacle{X=180,Y=200,VX=2400,Radius=5});moving.Tick(.02);check(moving.Hit,"Fast dot passed through a stationary ball");
   var warning=new MouseDashBoard(12);warning.Begin(100,100);warning.Obstacles.Clear();warning.Obstacles.Add(new DashObstacle{X=100,Y=100,Radius=7,Warning=.95});warning.Tick(.1);check(!warning.Hit,"Warning dot caused damage before joining");for(int i=0;i<12&&!warning.Hit;i++)warning.Tick(.1);check(warning.Hit,"Active warning dot never became collidable");
   var pause=new MiniSession("mousedash",1,19);pause.Tick(.1);check(pause.Dash.Seconds==0&&pause.Phase=="waiting","Waiting screen advanced survival timer");pause.HoldDash(450,204);pause.Tick(.1);pause.ReleaseDash();double time=pause.Dash.Seconds;MiniModelChecks.Advance(pause,10);check(pause.Dash.Seconds==time&&pause.Phase=="holding","Released ball did not pause simulation");check(!pause.HoldDash(20,20),"Resume allowed ball teleport");check(pause.HoldDash(pause.Dash.X,pause.Dash.Y),"Ball could not resume from its position");pause.Tick(.1);check(pause.Dash.Seconds>time,"Resume did not advance simulation");
   var clamp=new MouseDashBoard(9);clamp.Begin(-100,9999);check(clamp.X==11&&clamp.Y==397,"Starting pointer left arena");clamp.Obstacles.Clear();clamp.Move(-500,-600);check(clamp.X==11&&clamp.Y==11,"Captured pointer left arena");check(!clamp.Move(double.NaN,40)&&!clamp.Move(double.PositiveInfinity,40),"Invalid pointer corrupted physics");
   // Compare different render cadences against the same fixed simulation, then
   // observe a long obstacle field with the test probe outside the play area.
   var fast=new MouseDashBoard(57);var slow=new MouseDashBoard(57);fast.Begin(450,204);slow.Begin(450,204);fast.X=slow.X=-1000;fast.Y=slow.Y=-1000;
   for(int second=0;second<160;second++){
    for(int i=0;i<60;i++)fast.Tick(1.0/60);for(int i=0;i<10;i++)slow.Tick(.1);
    check(Math.Abs(fast.Seconds-slow.Seconds)<.00001&&fast.Obstacles.Count==slow.Obstacles.Count,"Mouse Dash depends on render rate");
    for(int i=0;i<fast.Obstacles.Count;i++){var a=fast.Obstacles[i];var b=slow.Obstacles[i];check(Math.Abs(a.X-b.X)<.00001&&Math.Abs(a.Y-b.Y)<.00001,"Dot trajectory depends on render rate");check(a.X>=a.Radius-.001&&a.X<=900-a.Radius+.001&&a.Y>=a.Radius-.001&&a.Y<=408-a.Radius+.001,"Moving dot escaped arena");}
   }
   check(fast.Obstacles.Count==38&&fast.Wave==11&&!fast.Hit,"Survival progression was not capped or failed to grow");
   var edge=new MouseDashBoard(26);edge.Begin(450,204);edge.X=edge.Y=-1000;for(int i=0;i<51;i++)edge.Tick(.1);var incoming=edge.Obstacles.Last();check(incoming.Warning>.7&&incoming.Warning<=.95,"New edge dot had no warning window");
  }
 }
 public partial class MainWindow {
  void CheckMouseDashUi(Action<bool,string> check){
   Profile.Settings.Language="en";Profile.Settings.Theme="matcha";StartMiniGame("mousedash");MiniDashDown(new Point(450,204));check(mini.Phase=="playing","Mouse Dash hold handler did not start");MiniModelChecks.Advance(mini,.5);MiniDashUp();double seconds=mini.Dash.Seconds;MiniModelChecks.Advance(mini,1);check(mini.Dash.Seconds==seconds,"Released UI advanced time");
   MiniDashDown(new Point(20,20));check(mini.Phase=="holding","UI resumed from a different position");MiniDashDown(new Point(mini.Dash.X,mini.Dash.Y));PauseMiniGame();MiniDashMove(new Point(10,10));MiniTick(null,EventArgs.Empty);check(paused&&mini.Dash.Seconds==seconds&&mini.Phase=="holding","Modal did not freeze dash input and time");CloseModal();check(mini.Phase=="holding","Closing pause resumed without a hold");
   mini.Dash.Seconds=15.4;int coins=Profile.Coins;RememberDash(false);check(Profile.MiniGames["mousedash"].BestSeconds==15.4&&Profile.Coins==coins+2,"Dash personal best/milestone was not recorded");RememberDash(false);check(Profile.Coins==coins+2,"Dash milestone awarded duplicate stars");
   MiniDashDown(new Point(mini.Dash.X,mini.Dash.Y));mini.Dash.Obstacles.Clear();mini.Dash.Obstacles.Add(new DashObstacle{X=mini.Dash.X+60,Y=mini.Dash.Y,Radius=8});MiniDashMove(new Point(mini.Dash.X+150,mini.Dash.Y));check(mini.Phase=="failed"&&overlay.Children.Count>0&&Profile.MiniGames["mousedash"].Completed==1,"Dash collision did not save exactly one run and show retry");Capture("mini-mousedash-retry");
   ClickTestButton("Play again");check(mini.Phase=="waiting"&&mini.Dash.Seconds==0&&Profile.MiniGames["mousedash"].BestSeconds==15.4,"Dash retry lost record or retained old field");BackToMiniGames();
   var loaded=store.Load();check(loaded.MiniGames["mousedash"].BestSeconds==15.4&&loaded.MiniGames["mousedash"].Completed==1,"Dash record failed save/reload");
   foreach(string theme in new[]{"matcha","rose","sky","lavender","night"})foreach(string language in new[]{"en","ar"}){
    Profile.Settings.Theme=theme;Profile.Settings.Language=language;Profile.Settings.ReducedMotion=true;StartMiniGame("mousedash");check(miniSurface.Width==900&&miniSurface.Height==408,"Dash arena does not fit viewport");MiniDashDown(new Point(450,204));MiniModelChecks.Advance(mini,1);miniSurface.InvalidateVisual();RefreshMiniHud();Capture("mini-mousedash-"+theme+"-"+language);MiniDashUp();BackToMiniGames();check(!miniRendering&&mini==null,"Dash retained a timer after exit");
   }
   Profile.Settings.Language="en";Profile.Settings.Theme="matcha";Profile.Settings.ReducedMotion=false;
  }
 }
}
