using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public static class MiniQualityChecks {
  public static void Run(Action<bool,string> check){
   LadybugChecks.Run(check);PaperStackChecks.Run(check);
   foreach(bool arabic in new[]{false,true})foreach(double scale in new[]{.9,1.15}){
    var tile=new MiniSession("tiles",1,1);tile.Tiles=new SlidingBoard{Size=3,Tiles=new[]{0,0,0,0,8,0,0,0,0}};var panel=new MiniSurface(tile,new Palette("matcha"),arabic,false,scale);panel.Measure(new Size(panel.Width,panel.Height));panel.Arrange(new Rect(0,0,panel.Width,panel.Height));panel.UpdateLayout();var image=new RenderTargetBitmap((int)panel.Width,(int)panel.Height,96,96,PixelFormats.Pbgra32);image.Render(panel);var pixels=new byte[(int)panel.Width*(int)panel.Height*4];image.CopyPixels(pixels,(int)panel.Width*4,0);Rect box=panel.CellRect(4);int min=9999,max=-1;
    for(int y=(int)box.Y+12;y<box.Bottom-12;y++)for(int x=(int)box.X+12;x<box.Right-12;x++){int at=(y*(int)panel.Width+x)*4;if(pixels[at+3]>100&&pixels[at+2]<80&&pixels[at+1]<100&&pixels[at]<120){min=Math.Min(min,x);max=Math.Max(max,x);}}
    check(max>=min&&Math.Abs((min+max)/2.0-(box.X+box.Width/2))<4,"Rendered tile numeral is not centred in "+(arabic?"Arabic":"English"));
   }
   foreach(int level in new[]{1,2,3,12})foreach(int order in new[]{1,2}){
    var session=new MiniSession("arrows",level,31415);var heard=new List<int>();session.Note+=note=>heard.Add(note);var random=new Random(order);
    int blocked=Enumerable.Range(0,session.Arrows.Arrows.Count).FirstOrDefault(i=>!session.Arrows.CanExit(i),-1);
    if(blocked>=0){session.ArrowTap(blocked);MiniModelChecks.Advance(session,.5);check(session.MelodyStep==0&&heard.Last()==-1,"Blocked arrow advanced the song");heard.Clear();}
    while(session.Phase!="complete"){
     var options=Enumerable.Range(0,session.Arrows.Arrows.Count).Where(session.Arrows.CanExit).ToArray();session.ArrowTap(options[random.Next(options.Length)]);
     double last=0,speed=-1;for(int frame=0;frame<=120;frame++){
      session.ArrowAnimation=session.ArrowDuration*frame/120;double travel=session.ArrowTravel;check(travel+1e-8>=last,"Clear arrow reverses or pauses its trajectory");
      if(frame>30){double velocity=travel-last;if(speed>=0)check(Math.Abs(velocity-speed)<1e-7,"Arrow slows again halfway through its exit");speed=velocity;}
      var arrow=session.Arrows.Arrows[session.ActiveArrow];var points=MiniSurface.MovingArrow(arrow,session.Arrows.Size,travel);double length=0;
      for(int i=1;i<points.Count;i++){Vector line=points[i]-points[i-1];check(Math.Abs(line.X)<1e-7||Math.Abs(line.Y)<1e-7,"Moving arrow cuts diagonally across a bend");length+=line.Length;}
      check(Math.Abs(length-(arrow.Cells.Count-1)*42)<1e-6,"Arrow changes its length while unwinding");last=travel;
     }
     session.Tick(.001);
    }
    check(heard.SequenceEqual(Enumerable.Range(0,heard.Count).Select(i=>session.Melody.Notes[i%session.Melody.Notes.Length])),"Legal arrow order changed the song notes");check(session.Wait>=session.Melody.Duration,"Next board cuts off the melody replay");
   }
   foreach(int seed in Enumerable.Range(1,16))foreach(int level in new[]{1,6,15,30}){
    var tiles=SlidingBoard.Generate(level,seed);check(tiles.Distance>=Math.Min(tiles.Size*tiles.Size,4+level),"Sliding board has a trivial scramble");
   }
   var route=new MiniSession("signal",1,189);int mask=route.Signal.Masks[0];route.RotateSignal(0);route.RotateSignal(0);MiniModelChecks.Advance(route,.15);check(route.Moves==2&&route.Signal.Masks[0]==MiniGrid.Rotate(MiniGrid.Rotate(mask)),"Rapid pipe rotation was dropped");
   var sliding=new MiniSession("tiles",2,189);int first=Enumerable.Range(0,4).Select(d=>MiniGrid.Next(sliding.Tiles.Blank,d,sliding.Tiles.Size,sliding.Tiles.Size)).First(c=>c>=0);sliding.SlideTile(first);int second=Enumerable.Range(0,4).Select(d=>MiniGrid.Next(sliding.Tiles.Blank,d,sliding.Tiles.Size,sliding.Tiles.Size)).First(c=>c>=0);sliding.SlideTile(second);MiniModelChecks.Advance(sliding,.15);check(sliding.Moves==2&&sliding.Tiles.Blank==second,"Rapid sliding input was dropped");
   var maze=new MiniSession("maze",3,189);var path=maze.Maze.Route();for(int i=0;i<2;i++){int direction=Enumerable.Range(0,4).Single(d=>MiniGrid.Next(maze.Maze.Player,d,maze.Maze.Size,maze.Maze.Size)==path[i]);maze.MoveMaze(direction);}MiniModelChecks.Advance(maze,.15);check(maze.Moves==2&&maze.Maze.Player==path[1]&&maze.Maze.Visited.Count(v=>v)==3,"Maze queue or trail lost a move");
   foreach(string kind in new[]{"signal","tiles","maze"}){
    var play=new MiniSession(kind,3,1682);int won=0;play.Won+=()=>won++;
    if(kind=="signal")for(int cell=0;cell<play.Signal.Masks.Length&&play.Phase!="complete";cell++)while(play.Signal.Masks[cell]!=play.Signal.SolutionMasks[cell]&&play.Phase!="complete"){play.RotateSignal(cell);MiniModelChecks.Advance(play,.15);}
    if(kind=="tiles")foreach(int cell in play.Tiles.Scramble.AsEnumerable().Reverse()){if(play.Phase=="complete")break;play.SlideTile(cell);MiniModelChecks.Advance(play,.15);}
    if(kind=="maze")foreach(int cell in play.Maze.Route()){int direction=Enumerable.Range(0,4).Single(d=>MiniGrid.Next(play.Maze.Player,d,play.Maze.Size,play.Maze.Size)==cell);play.MoveMaze(direction);MiniModelChecks.Advance(play,.15);}
    check(play.Phase=="complete"&&won==1,"Playable "+kind+" solution did not finish once");
   }
  }
 }
 public partial class MainWindow {
  void CheckMiniQualityUi(Action<bool,string> check){
   Profile.Settings.Language="en";Profile.Settings.Theme="matcha";Profile.Settings.TextScale=.9;Profile.Settings.ButtonScale=.92;Profile.MiniGames["arrows"].Level=1;StartMiniGame("arrows");UpdateLayout();
   check(miniSurface.Cursor==Cursors.Arrow&&!miniSurface.BeginPan(new Point(20,20),-1),"Small arrow board shows an idle pan cursor");check(miniSurface.Height*miniZoom>=395&&miniSurface.Height*miniZoom<=410,"Early arrow board is poorly sized");check(mini.ExitBounds[1]>miniSurface.Width+100&&mini.ExitBounds[3]<-100&&!miniSurface.ClipToBounds&&miniScroll.ClipToBounds,"Arrow flight is clipped at the small grid instead of the paper viewport");
   BackToMiniGames();Profile.MiniGames["arrows"].Level=40;StartMiniGame("arrows");UpdateLayout();check(miniSurface.BeginPan(new Point(150,180),-1)&&miniSurface.Cursor==PanCursor.Closed,"Held pan does not use the closed hand");miniSurface.MovePan(new Point(150,80));miniScroll.UpdateLayout();check(miniScroll.VerticalOffset>0,"Held drag did not pan");miniSurface.EndPan();check(miniSurface.Cursor==Cursors.Arrow&&!miniSurface.Panning,"Released pan retained its hand cursor");BackToMiniGames();
   Profile.MiniGames["arrows"].Level=1;Profile.Settings.Sound=true;StartMiniGame("arrows");while(mini.Phase!="complete"){mini.ArrowTap(Enumerable.Range(0,mini.Arrows.Arrows.Count).First(mini.Arrows.CanExit));while(mini.Phase=="arrow")mini.Tick(.016);}
   check(mini.Wait>mini.Melody.Duration&&Convert.ToString(miniRestart.Content).StartsWith("Next level"),"Song replay is cut off or lacks next-level control");Capture("mini-song-clear");double wait=mini.Wait;PauseMiniGame();MiniTick(null,EventArgs.Empty);check(audio.MiniPaused&&mini.Wait==wait,"Pausing did not pause replay and next-level timer");CloseModal();check(!audio.MiniPaused,"Piano audio did not resume");miniRestart.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));check(mini.Level==2&&mini.Phase=="playing","Next level did not skip replay cleanly");BackToMiniGames();Profile.Settings.Sound=false;
   CheckLadybugUi(check);CheckPaperStackUi(check);
   Profile.MiniGames["pulse"].Level=1;StartMiniGame("pulse");var firstPulse=mini;double tapAt=0;
   check(mini.Phase=="ready"&&miniStatus.Text=="Press E or click the ring to start","Pulse ready state lacks its start indicator");MiniModelChecks.Advance(mini,120);check(mini.Phase=="ready"&&mini.Lives==3&&mini.Charge==.30,"Ready pulse drains before the user starts");Capture("mini-pulse-ready");
   for(int frame=0;frame<2000&&firstPulse.Phase!="complete";frame++){firstPulse.Tick(.01);if(firstPulse.Elapsed>=tapAt){firstPulse.PulseTap();tapAt=firstPulse.Elapsed+.20;}}
   check(firstPulse.Phase=="complete"&&firstPulse.Wait<=.5,"Pulse completion inserted a break");MiniModelChecks.Advance(firstPulse,.6);check(mini!=firstPulse&&mini.Level==2&&mini.Phase=="ready"&&mini.PulseKey=='E',"Pulse next round does not wait for the player");RefreshMiniHud();check(miniStatus.Text=="Press E or click the ring to start","Pulse next round lost its start indicator");BackToMiniGames();
   foreach(int level in new[]{1,12,40})foreach(string kind in MiniCatalog.Keys){
    if(kind!="stack")Profile.MiniGames[kind].Level=level;StartMiniGame(kind);UpdateLayout();if(kind=="maze"){var path=mini.Maze.Route();foreach(int cell in path.Take(4)){int d=Enumerable.Range(0,4).Single(direction=>MiniGrid.Next(mini.Maze.Player,direction,mini.Maze.Size,mini.Maze.Size)==cell);mini.MoveMaze(d);MiniModelChecks.Advance(mini,.15);}}
    if(kind=="pulse"){mini.PulseTap();mini.Charge=.67;}if(kind=="echo"){mini.Tick(.1);MiniModelChecks.Advance(mini,.7);}if(kind=="mousedash"){MiniDashDown(new Point(450,204));MiniModelChecks.Advance(mini,.7);}
    RefreshMiniHud();miniSurface.InvalidateVisual();Capture("review-"+kind+"-"+level);var transparent=new RenderTargetBitmap((int)miniSurface.Width,(int)miniSurface.Height,96,96,PixelFormats.Pbgra32);transparent.Render(miniSurface);var corner=new byte[4];transparent.CopyPixels(new Int32Rect(0,0,1,1),corner,4,0);check(corner[3]==0,"Minigame background hides the notebook paper");
    if(kind!="stack"&&kind!="mousedash"&&kind!="echo"&&kind!="pulse"){Point center=miniSurface.CellRect(kind=="arrows"?mini.Arrows.Arrows[0].Head:0).TopLeft+new Vector(miniSurface.CellSize/2,miniSurface.CellSize/2);Point mapped=miniSurface.TranslatePoint(center,miniScroll);check(miniSurface.Hit(miniScroll.TranslatePoint(mapped,miniSurface))==0,"Scaled board hit test lost alignment");}
    if(level==1&&kind!="mousedash")check(miniSurface.Height*miniZoom>=Math.Min(395,miniSurface.Height*1.85)-1,"Early board is undersized");BackToMiniGames();check(!miniRendering,"Display-frame subscription survived navigation");
   }
   var animation=new MiniSession("arrows",1,1);animation.Arrows=new ArrowBoard{Size=8,Arrows=new List<EscapeArrow>{new EscapeArrow{Direction=1,Cells=new List<int>{9,10,18,26,27,28,20,12,13,14}}}};animation.ArrowTap(0);
   var surface=new MiniSurface(animation,p,false,false,.9);surface.Measure(new Size(surface.Width,surface.Height));surface.Arrange(new Rect(0,0,surface.Width,surface.Height));string folder=Path.GetFullPath(Path.Combine(baseDir,"../test-output/arrow-motion"));Directory.CreateDirectory(folder);int frames=(int)Math.Ceiling(animation.ArrowDuration*60);
   for(int frame=0;frame<=frames;frame++){animation.ArrowAnimation=animation.ArrowDuration*frame/frames;surface.InvalidateVisual();surface.UpdateLayout();var bitmap=new RenderTargetBitmap((int)surface.Width,(int)surface.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(surface);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(folder,frame.ToString("000")+".png")))encoder.Save(file);}
   Profile.MiniGames["arrows"].Level=1;
  }
 }
}
