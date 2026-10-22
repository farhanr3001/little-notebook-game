using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace LittleNotebook {
 public static class MiniModelChecks {
  public static void Advance(MiniSession session,double seconds){for(double t=0;t<seconds;t+=.05)session.Tick(Math.Min(.05,seconds-t));}
  static bool IndependentExit(ArrowBoard board,int index,int[] owner){var arrow=board.Arrows[index];int x=arrow.Head%board.Size,y=arrow.Head/board.Size;while(true){x+=MiniGrid.Dx[arrow.Direction];y+=MiniGrid.Dy[arrow.Direction];if(x<0||y<0||x>=board.Size||y>=board.Size)return true;int other=owner[y*board.Size+x];if(other>=0&&other!=index)return false;}}
  public static void Run(Action<bool,string> check){
   foreach(int level in new[]{1,2,5,10,20,40,80,160})for(int seed=1;seed<=12;seed++){
    var board=ArrowBoard.Generate(level,seed*9127+level);var occupied=Enumerable.Repeat(-1,board.Size*board.Size).ToArray();
    check(board.Arrows.Count>=Math.Min(4+Math.Min(level,148)*2,board.Size*board.Size/7)*.75,"Arrow board underfilled");
    for(int i=0;i<board.Arrows.Count;i++){
     var arrow=board.Arrows[i];check(arrow.Cells.Count>=3,"Short arrow path");for(int j=0;j<arrow.Cells.Count;j++){int cell=arrow.Cells[j];check(cell>=0&&cell<occupied.Length&&occupied[cell]<0,"Arrow paths overlap or leave grid");occupied[cell]=i;if(j>0)check(Math.Abs(cell%board.Size-arrow.Cells[j-1]%board.Size)+Math.Abs(cell/board.Size-arrow.Cells[j-1]/board.Size)==1,"Disconnected arrow");}
     int before=arrow.Cells[arrow.Cells.Count-2];check(arrow.Head%board.Size-before%board.Size==MiniGrid.Dx[arrow.Direction]&&arrow.Head/board.Size-before/board.Size==MiniGrid.Dy[arrow.Direction],"Arrowhead points against its path");
    }
    check(board.Solution()!=null,"Forward arrow solver found a deadlock");
    for(int count=0;count<board.Arrows.Count;count++){
     int clear=-1;for(int i=0;i<board.Arrows.Count;i++)if(!board.Arrows[i].Removed){bool expected=IndependentExit(board,i,occupied);check(board.CanExit(i)==expected,"Runtime collision differs from independent occupancy check");if(expected&&clear<0)clear=i;}
     check(clear>=0,"Independent solver reached a dead end");board.Arrows[clear].Removed=true;foreach(int cell in board.Arrows[clear].Cells)occupied[cell]=-1;
    }
   }
   var deadlock=new ArrowBoard{Size=4,Arrows=new List<EscapeArrow>{new EscapeArrow{Cells=new List<int>{4,5},Direction=1},new EscapeArrow{Cells=new List<int>{7,6},Direction=3}}};check(deadlock.Solution()==null,"Solver accepted a mutually blocked board");
   for(int seed=1;seed<=50;seed++)foreach(int level in new[]{1,7,20,60}){
    var signal=SignalBoard.Generate(level,seed*197);check(!signal.Solved,"Signal begins solved");for(int i=0;i<signal.Masks.Length;i++){int turns=0;while(signal.Masks[i]!=signal.SolutionMasks[i]&&turns++<4)signal.Rotate(i);check(signal.Masks[i]==signal.SolutionMasks[i],"Signal tile cannot reach solution orientation");}check(signal.Solved,"Signal solution does not connect endpoints");
    var lights=LanternBoard.Generate(level,seed*197);ulong? solution=lights.Solve();check(solution.HasValue&&lights.State!=0,"Lantern board unsolvable or already complete");for(int i=0;i<lights.Size*lights.Size;i++)if((solution.Value&(1UL<<i))!=0)lights.Press(i);check(lights.State==0,"Lantern solver does not clear board");
    var tiles=SlidingBoard.Generate(level,seed*197);check(!tiles.Solved,"Sliding puzzle starts solved");foreach(int cell in tiles.Scramble.AsEnumerable().Reverse())check(tiles.Move(cell),"Sliding scramble cannot be legally reversed");check(tiles.Solved,"Reversed sliding scramble is not solved");
    var maze=MazeBoard.Generate(level,seed*197);var route=maze.Route();check(route!=null&&route.Count>=maze.Size,"Maze exit unreachable or too close");foreach(int cell in route){int direction=Enumerable.Range(0,4).Single(d=>MiniGrid.Next(maze.Player,d,maze.Size,maze.Size)==cell);check(maze.Move(direction),"Maze solution crosses a wall");}check(maze.Player==maze.Goal,"Maze route missed goal");
   }
   var arrowRun=new MiniSession("arrows",8,184);int lost=0;arrowRun.Lost+=()=>lost++;int blocked=Enumerable.Range(0,arrowRun.Arrows.Arrows.Count).First(i=>!arrowRun.Arrows.CanExit(i));
   for(int i=0;i<3;i++){check(arrowRun.ArrowTap(blocked),"Blocked arrow ignored");check(!arrowRun.ArrowTap(blocked),"Repeated moving arrow accepted twice");Advance(arrowRun,.6);check(arrowRun.Arrows.Arrows.All(a=>!a.Removed),"Collision removed an arrow");}
   check(arrowRun.Phase=="failed"&&arrowRun.Lives==0&&lost==1,"Three collisions did not fail exactly once");
   var retry=new MiniSession("arrows",8,184);check(retry.Lives==3&&retry.Arrows.Arrows.Select(a=>string.Join(",",a.Cells)).SequenceEqual(arrowRun.Arrows.Arrows.Select(a=>string.Join(",",a.Cells))),"Arrow restart changed level layout");
   int clearIndex=Enumerable.Range(0,retry.Arrows.Arrows.Count).First(retry.Arrows.CanExit);retry.ArrowTap(clearIndex);int queued=(clearIndex+1)%retry.Arrows.Arrows.Count;retry.ArrowTap(queued);retry.ArrowTap((queued+1)%retry.Arrows.Arrows.Count);check(retry.QueuedArrow==queued,"Arrow input queue accepted more than one move");Advance(retry,3);check(retry.Moves<=2,"Arrow queue caused duplicate moves");
   foreach(int level in new[]{1,10,30,100,1000}){
    var pulse=new MiniSession("pulse",level,81);int won=0;pulse.Won+=()=>won++;double nextTap=0;
    for(int frame=0;frame<5000&&pulse.Phase!="complete"&&pulse.Phase!="failed";frame++){pulse.Tick(.02);if(pulse.Elapsed>=nextTap){pulse.PulseTap();nextTap=pulse.Elapsed+.18;}}
    check(won==1&&pulse.Latch==3&&pulse.Lives==3,"Pulse tuning cannot be beaten at a reasonable tap rate");check(pulse.Wait>=5&&pulse.Wait<=10,"Pulse cooldown outside requested range");
   }
   var spam=new MiniSession("pulse",1,81);Advance(spam,2.6);int accepted=0;for(int i=0;i<100;i++)if(spam.PulseTap())accepted++;check(accepted==1,"Same-frame combined input bypasses pulse rate guard");Advance(spam,60);check(spam.Phase=="failed"&&spam.Lives==0,"Unattended lock did not stop after three attempts");
   var echo=new MiniSession("echo",20,193);Advance(echo,20);check(echo.Phase=="playing"&&echo.Highlight==-1,"Echo sequence did not yield input control");check(echo.ReplayEcho(),"Echo replay unavailable");Advance(echo,20);foreach(int pad in echo.Pattern)echo.EchoTap(pad);check(echo.Phase=="complete"&&echo.Lives==3,"Correct echo sequence failed");
   var wrongEcho=new MiniSession("echo",1,193);for(int i=0;i<3;i++){Advance(wrongEcho,10);wrongEcho.EchoTap((wrongEcho.Pattern[0]+1)%wrongEcho.PadCount);}check(wrongEcho.Phase=="failed","Echo wrong attempts did not stop the round");
  }
 }
 public partial class MainWindow {
  public async void RunMiniGameTests(){
   string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/minigames.txt"));int checks=0;
   Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
   try{
    timer.Stop();audio.Dispose();Profile=new Profile{Name="Maya"};Profile.Settings.Sound=false;Profile.Settings.Music=false;Profile.Settings.Volume=0;audio=new AudioSystem(Profile.Settings);MiniCatalog.Normalize(Profile);
    MiniModelChecks.Run(check);File.WriteAllText(report,"Procedural solvability and input-state checks passed. Rendering native screens next.\n");await Task.Delay(1);
    page="home";Build();check(Descendants<Button>(body).Any(b=>Convert.ToString(b.Content)=="Minigames"),"Play entry to minigames missing");Capture("mini-learning-entry");
    SwitchPlaySection(true);UpdateLayout();check(page=="minigames"&&Descendants<MiniGameBadge>(body).Count()==7,"Seven minigame selections missing");check(!Descendants<Button>(body).Any(b=>new[]{"Easy","Medium","Hard","Mixed"}.Contains(Convert.ToString(b.Content))),"Learning difficulty leaked into minigame selector");Capture("mini-selector");
    foreach(string key in MiniCatalog.Keys){Profile.MiniGames[key].Level=1;Profile.MiniGames[key].Seed=10203;StartMiniGame(key);UpdateLayout();check(mini.Kind==key&&miniScroll.Content==miniSurface,"Wrong minigame rendered");check(miniTimer.IsEnabled,"Minigame timer not running");if(key=="pulse"){MiniModelChecks.Advance(mini,2.6);mini.Charge=.67;RefreshMiniHud();}if(key=="echo"){mini.Tick(.8);mini.Tick(.1);RefreshMiniHud();}miniSurface.InvalidateVisual();Capture("mini-"+key);BackToMiniGames();check(!miniTimer.IsEnabled&&mini==null,"Leaving minigame retained its timer/session");}
    Profile.MiniGames["arrows"].Level=8;StartMiniGame("arrows");int savedLevel=mini.Level;int blocked=Enumerable.Range(0,mini.Arrows.Arrows.Count).First(i=>!mini.Arrows.CanExit(i));
    for(int i=0;i<3;i++){mini.ArrowTap(blocked);MiniModelChecks.Advance(mini,.6);}check(overlay.Children.Count>0&&mini.Phase=="failed"&&Profile.MiniGames["arrows"].Level==savedLevel,"Arrow death lost checkpoint or missed its menu");Capture("mini-retry-menu");ClickTestButton("Restart level");check(mini.Level==savedLevel&&mini.Lives==3&&mini.Arrows.Arrows.All(a=>!a.Removed),"Full-board restart did not reset lives/arrows");
    int coins=Profile.Coins;while(mini.Phase!="complete"){int next=Enumerable.Range(0,mini.Arrows.Arrows.Count).First(mini.Arrows.CanExit);mini.ArrowTap(next);while(mini.Phase=="arrow")mini.Tick(.05);}
    check(Profile.MiniGames["arrows"].Level==savedLevel+1&&Profile.Coins==coins+2,"Level win did not save checkpoint/reward");Save();var loaded=store.Load();check(loaded.MiniGames["arrows"].Level==savedLevel+1,"Minigame progress failed save/reload");check(loaded.Attempts.Count==0&&loaded.Sessions.Count==0,"Minigames polluted learning records");
    BackToMiniGames();Profile.Settings.SelectedMiniGame="arrows";ConfirmResetMiniProgress();ClickTestButton("Reset to level 1");check(Profile.MiniGames["arrows"].Level==1&&Profile.MiniGames["arrows"].BestCompleted==savedLevel,"Reset changed best record or did not reset selected game");
    StartMiniGame("arrows");int clear=Enumerable.Range(0,mini.Arrows.Arrows.Count).First(mini.Arrows.CanExit);mini.ArrowTap(clear);double animation=mini.ArrowAnimation;PauseMiniGame();MiniTick(null,EventArgs.Empty);check(paused&&mini.ArrowAnimation==animation,"Paused board advanced animation");CloseModal();check(!paused&&mini.Phase=="arrow","Resume discarded active animation");BackToMiniGames();
    Profile.MiniGames["arrows"].Level=40;StartMiniGame("arrows");UpdateLayout();miniScroll.ScrollToVerticalOffset(260);miniScroll.UpdateLayout();check(miniScroll.VerticalOffset>0&&miniScroll.ExtentHeight>miniScroll.ViewportHeight,"Large arrow board is not pannable");check(miniSurface.Hit(new Point(3,3))<0,"Empty paper incorrectly hits an arrow");ZoomMini(0);check(miniSurface.Width*miniZoom<=921&&miniSurface.Height*miniZoom<=425,"Fit does not contain large board");Capture("mini-large-board");BackToMiniGames();
    foreach(string theme in new[]{"matcha","rose","sky","lavender","night"})foreach(string language in new[]{"en","ar"}){
     Profile.Settings.Theme=theme;Profile.Settings.Language=language;Profile.Settings.TextScale=1.15;Profile.Settings.ButtonScale=1;page="minigames";Build();UpdateLayout();miniSelectionScroll.ScrollToEnd();miniSelectionScroll.UpdateLayout();double offset=miniSelectionScroll.VerticalOffset;Profile.Settings.SelectedMiniGame="maze";Save();Build();check(Math.Abs(miniSelectionScroll.VerticalOffset-offset)<.1,"Minigame selection scroll jumped");
     check(Descendants<MiniGameBadge>(body).Count()==7,"Theme/language lost minigame choices");foreach(string kind in MiniCatalog.Keys){StartMiniGame(kind);UpdateLayout();check(miniSurface.Width>0&&miniScroll.Width<960,"Minigame layout overflowed its viewport");BackToMiniGames();}if(language=="ar"){StartMiniGame("signal");Capture("mini-arabic-"+theme);BackToMiniGames();}await Task.Delay(1);
    }
    Profile.Settings.Language="en";Profile.Settings.Theme="matcha";Profile.Settings.TextScale=.9;Profile.Settings.ButtonScale=.92;page="minigames";Build();Save();
    string legacy=Path.GetFullPath(Path.Combine(baseDir,"../test-output/mini-legacy.json"));File.WriteAllText(legacy,"{\"Version\":1,\"Name\":\"Earlier save\",\"Settings\":{},\"Attempts\":[],\"Sessions\":[],\"Owned\":[\"leaf\"]}");var migrated=new SaveStore(legacy).Load();check(migrated.Name=="Earlier save"&&migrated.MiniGames.Count==7&&migrated.Settings.PlaySection=="learning","Earlier save migration failed");
    check(Diagnostics.ErrorCount==0,"Asynchronous minigame error logged");
    File.WriteAllText(report,"PASS: "+checks+" focused minigame checks. 96 arrow boards independently cleared across early/large levels; collision geometry, three lives, queue guard and identical retries; 200 each of signal, lantern, sliding and maze boards verified solvable; pulse progression/input guard/cooldowns and echo replay/failure; seven native games in all five themes and English/Arabic; save migration/checkpoints/rewards/reset, pause/resume, large-board pan/fit, selection-scroll retention and timer cleanup. Learning/question stress tests were not run.\n");
   }catch(Exception error){Diagnostics.Log("minigames test",error);File.WriteAllText(report,"FAIL after "+checks+" checks: "+error);Environment.ExitCode=1;}
   finally{StopMiniGame();game=null;closing=true;Close();}
  }
 }
}
