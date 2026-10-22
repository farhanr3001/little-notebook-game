using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LittleNotebook {
 public static class LadybugChecks {
  public static MazeBoard Corridor(){
   var board=new MazeBoard{Size=20,Goal=399,Walls=Enumerable.Repeat(15,400).ToArray(),Visited=new bool[400]};board.Visited[0]=true;
   for(int cell=0;cell<19;cell++){board.Walls[cell]&=~2;board.Walls[cell+1]&=~8;}
   for(int row=0;row<5;row++){int cell=row*20+4;board.Walls[cell]&=~4;board.Walls[cell+20]&=~1;}return board;
  }
  static MiniSession Session(){return new MiniSession("maze",16,189){Maze=Corridor()};}
  public static void Run(Action<bool,string> check){
   foreach(double frame in new[]{.01,1.0/60,1.0/30,.1}){
    var run=Session();run.HoldMaze(1);double elapsed=0,last=0;
    while(elapsed<1.93){double dt=Math.Min(frame,1.93-elapsed);run.Tick(dt);elapsed+=dt;double expected=elapsed/MiniSession.MazeStepSeconds;
     check(Math.Abs(run.MazeX-expected)<1e-7&&run.MazeY==0,"Held ladybug movement pauses, jumps or depends on frame rate");check(run.MazeX>last,"Held movement stalls at a tile boundary");last=run.MazeX;}
    run.ReleaseMaze();int target=run.Maze.Player;MiniModelChecks.Advance(run,.6);check(run.Maze.Player==target&&run.MazeProgress==1,"Released movement adds another step");
   }
   var tap=Session();tap.HoldMaze(1);tap.ReleaseMaze();MiniModelChecks.Advance(tap,1);check(tap.Moves==1&&tap.MazeX==1,"A single tap moves more than one space");
   var click=Session();click.MoveMaze(1);MiniModelChecks.Advance(click,1);check(click.Moves==1&&click.MazeX==1,"A mouse step turns into held movement");
   var turn=Session();turn.HoldMaze(1);MiniModelChecks.Advance(turn,.49);turn.HoldMaze(2);MiniModelChecks.Advance(turn,.20);check(Math.Abs(turn.MazeX-4)<1e-8&&turn.MazeY>.9&&turn.MazeY<1,"Held turn cuts a corner or adds a pause");turn.ReleaseMaze();MiniModelChecks.Advance(turn,.3);check(turn.MazeX==4&&turn.MazeY==1,"Turning release overshoots a space");
   var wall=Session();wall.HoldMaze(1);MiniModelChecks.Advance(wall,4);check(wall.Maze.Player==19&&wall.Moves==19&&wall.MazeX==19,"Held movement passes through a wall or wraps a row");wall.HoldMaze(0);MiniModelChecks.Advance(wall,.3);check(wall.Maze.Player==19,"Ladybug escapes the maze boundary");
   var win=Session();win.Maze.Goal=1;int won=0;win.Won+=()=>won++;win.HoldMaze(1);win.Tick(.07);check(win.Phase=="playing"&&win.MazeX>.4&&win.MazeX<.6,"Leaf goal completes before the ladybug arrives");win.Tick(.07);check(win.Phase=="complete"&&won==1&&win.MazeHeldDirection==-1&&win.MazeX==1,"Leaf goal did not finish once at arrival");
  }
 }
 public partial class MainWindow {
  void CheckLadybugUi(Action<bool,string> check){
   Profile.MiniGames["maze"].Level=16;StartMiniGame("maze");mini.Maze=LadybugChecks.Corridor();UpdateLayout();check(MiniName("maze")=="Ladybug Trail","Maze name did not change");
   MazeKeyDown(Key.Right,false);MiniModelChecks.Advance(mini,.31);int moves=mini.Moves;MazeKeyDown(Key.Right,true);check(mini.Moves==moves&&miniHeldKeys.Count==1,"Keyboard repeat changes held movement");
   MazeKeyDown(Key.Down,false);MiniKeyUp(Key.Down);check(mini.MazeHeldDirection==1,"Releasing the most recent key loses an earlier held direction");
   MiniKeyUp(Key.Right);moves=mini.Moves;MiniModelChecks.Advance(mini,.5);check(mini.Moves==moves&&mini.MazeHeldDirection==-1,"Key-up leaves ghost movement");
   MazeKeyDown(Key.Right,false);MiniModelChecks.Advance(mini,.02);PauseMiniGame();check(miniHeldKeys.Count==0&&mini.MazeHeldDirection==-1&&mini.QueuedDirection==-1,"Pause keeps a held or queued move");CloseModal();moves=mini.Moves;MiniModelChecks.Advance(mini,.5);check(mini.Moves==moves,"Resuming moves without a new press");
   mini.Maze=LadybugChecks.Corridor();mini.MovingFrom=0;mini.MoveTime=1;ZoomMini(2.4/miniZoom);MazeKeyDown(Key.Right,false);MiniModelChecks.Advance(mini,1.8);KeepMiniMazeVisible();miniScroll.UpdateLayout();check(miniScroll.HorizontalOffset>0,"Large maze camera does not follow held movement");miniSurface.InvalidateVisual();Capture("mini-ladybug-hold");
   ClearMiniHeldInput();BackToMiniGames();check(miniHeldKeys.Count==0&&!miniRendering,"Leaving Ladybug Trail keeps movement or display callbacks");
  }
 }
}
