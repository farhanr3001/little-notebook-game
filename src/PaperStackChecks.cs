using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LittleNotebook {
 public static class PaperStackChecks {
  public static void Land(PaperStackBoard board,double offset){if(!board.Started)board.Tap();for(int i=0;i<5&&board.Phase!="moving";i++)board.Tick(.1);board.Tick(.13);board.Tick(.1);board.MovingX=board.Top.X+offset;if(!board.Tap())throw new Exception("Stack fixture landing ignored");}
  public static void Run(Action<bool,string> check){
   var ready=new MiniSession("stack",99,3);MiniModelChecks.Advance(ready,30);check(!ready.Stack.Started&&ready.Stack.Score==0&&ready.Phase=="ready","Paper Stack starts or restores progress without input");check(ready.StackTap()&&!ready.StackTap(),"Combined start/drop input accepted twice");
   foreach(int side in new[]{-1,1}){
    var board=new PaperStackBoard(1);Land(board,side*40);check(board.Score==1&&Math.Abs(board.PieceWidth-200)<1e-8&&board.Chips.Count==1&&Math.Abs(board.Chips[0].Width-40)<1e-8,"Stack trim does not match actual overlap");check(Math.Abs(board.Top.X-(side<0?330:370))<1e-8,"Stack retained the wrong side of a landing");double y=board.Chips[0].Y;for(int i=0;i<7;i++)board.Tick(.1);check(board.Chips[0].Y>y+100&&Math.Abs(board.Chips[0].Angle)>90,"Trim does not fall and rotate");
   }
   var perfect=new PaperStackBoard(2);Land(perfect,60);double width=perfect.PieceWidth;for(int i=0;i<3;i++)Land(perfect,.1);check(perfect.Score==4&&perfect.Grew&&perfect.PieceWidth>width&&perfect.PieceWidth<=PaperStackBoard.BaseWidth,"Perfect streak does not recover bounded width");
   var tower=new PaperStackBoard(2);for(int i=0;i<300;i++){Land(tower,0);check(tower.Perfect&&tower.PieceWidth==240&&tower.Top.Row==i+1,"Perfect stacking changes width or skips a layer");}for(int i=0;i<10;i++)tower.Tick(.1);check(tower.Layers.Count==64&&Math.Abs(tower.Camera-(tower.Score-7)*22)<.1,"Tall stack memory or camera is unbounded");check(PaperStackBoard.Speed(10)>PaperStackBoard.Speed(0)*2&&PaperStackBoard.Speed(30)>PaperStackBoard.Speed(10)*2&&PaperStackBoard.Speed(45)>PaperStackBoard.Speed(30)&&PaperStackBoard.Speed(100000)==1040,"Stack speed does not grow safely");
   var miss=new MiniSession("stack",1,3);miss.StackTap();miss.Tick(.1);miss.Tick(.1);miss.Stack.MovingX=PaperStackBoard.Left;int lost=0;miss.Lost+=()=>lost++;check(miss.StackTap()&&miss.Stack.Score==0&&miss.Stack.Phase=="miss","Complete miss was accepted as a layer");MiniModelChecks.Advance(miss,1);miss.Tick(.1);check(miss.Phase=="failed"&&lost==1,"Stack misses do not finish once");
   var a=new PaperStackBoard(2);var b=new PaperStackBoard(2);a.Tap();b.Tap();for(int i=0;i<600;i++)a.Tick(1.0/60);for(int i=0;i<100;i++)b.Tick(.1);check(Math.Abs(a.MovingX-b.MovingX)<1e-6&&a.Direction==b.Direction,"Stack motion depends on render cadence");
   foreach(double cadence in new[]{1.0/60,1.0/120}){var timed=new PaperStackBoard(2);timed.Tap();for(int frame=0;frame<10000&&timed.Score<12&&!timed.Over;frame++){timed.Tick(cadence);if(timed.Phase=="moving"&&Math.Abs(timed.MovingX-timed.Top.X-14)<PaperStackBoard.Speed(timed.Score)*cadence*.55)timed.Tap();}check(timed.Score==12&&timed.PieceWidth<130&&!timed.Over,"Timing real moving pieces cannot build a trimmed tower");}
   check(!MiniCatalog.Keys.Contains("lanterns")&&MiniCatalog.Keys.Contains("stack"),"Lanterns remains in the game selector");
  }
 }
 public partial class MainWindow {
  void CheckPaperStackUi(Action<bool,string> check){
   Profile.StackBest=0;int coins=Profile.Coins;StartMiniGame("stack");check(!Profile.MiniGames.ContainsKey("stack")&&mini.Stack.Score==0,"Stack saves an in-progress tower or level");check(miniSurface.Width==900&&miniSurface.Height==408&&miniSurface.Hit(new Point(450,200))==0,"Stack does not fill the arena or receive clicks");Capture("stack-ready");
   MiniPress(0);for(int i=0;i<8;i++)PaperStackChecks.Land(mini.Stack,i==2?28:0);RememberStack();check(Profile.StackBest==8&&Profile.Coins==coins,"Stack did not save only its high score");miniSurface.InvalidateVisual();RefreshMiniHud();Capture("stack-perfect");
   PaperStackChecks.Land(mini.Stack,34);RememberStack();mini.Tick(.1);miniSurface.InvalidateVisual();RefreshMiniHud();Capture("stack-cut");double moving=mini.Stack.MovingX,elapsed=mini.Stack.Elapsed;PauseMiniGame();MiniTick(null,EventArgs.Empty);check(mini.Stack.MovingX==moving&&mini.Stack.Elapsed==elapsed,"Paused tower or falling trims move");CloseModal();
   BackToMiniGames();var loaded=store.Load();check(loaded.StackBest==9&&!loaded.MiniGames.ContainsKey("stack"),"Saved stack high score or no-progress rule failed");StartMiniGame("stack");check(mini.Stack.Score==0&&!mini.Stack.Started&&Profile.StackBest==9,"New stack run resumes the old tower");MiniPress(0);mini.Tick(.1);mini.Tick(.1);mini.Stack.MovingX=PaperStackBoard.Left;MiniPress(0);MiniModelChecks.Advance(mini,1);check(mini.Phase=="failed"&&overlay.Children.Count>0,"Stack failure menu missing");Capture("stack-finished");ClickTestButton("Play again");check(mini.Phase=="ready"&&mini.Stack.Score==0&&Profile.StackBest==9,"Stack retry lost high score or retained tower");BackToMiniGames();
   Profile.Settings.SelectedMiniGame="stack";ConfirmResetMiniProgress();ClickTestButton("Cancel");check(Profile.StackBest==9,"Cancelled score reset changed record");ConfirmResetMiniProgress();ClickTestButton("Reset high score");check(Profile.StackBest==0&&Profile.Coins==coins,"Score reset changes other progress");
  }
 }
}
