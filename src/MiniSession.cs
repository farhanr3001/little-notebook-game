using System;
using System.Linq;

namespace LittleNotebook {
 public sealed class MiniSession {
  public readonly string Kind;public readonly int Level,Seed;
  public int Lives=3,Moves,SelectedCell;
  public string Phase="playing";
  public double Wait,Elapsed,FeedbackTime;public bool WrongFeedback;
  public ArrowBoard Arrows;public SignalBoard Signal;public SlidingBoard Tiles;public MazeBoard Maze;
  public MouseDashBoard Dash;
  public PaperStackBoard Stack;public ExtraPuzzle Extra;
  public MiniMelody Melody;public int MelodyStep;
  public sealed class Flight {public int Index;public bool Blocked;public double Time,Duration,Distance;}
  public readonly System.Collections.Generic.List<Flight> Flights=new System.Collections.Generic.List<Flight>();
  public int ActiveArrow=-1,QueuedArrow=-1;public bool ArrowBlocked;public double ArrowAnimation,ArrowDuration,ArrowDistance;
  public readonly double[] ExitBounds={double.NaN,double.NaN,double.NaN,double.NaN};
  public int TurningCell=-1,TurningMask;public double TurnTime=1;
  public int MovingValue,MovingFrom,MovingTo;public double MoveTime=1;
  public const double MazeStepSeconds=.14;
  public int MazeHeldDirection=-1,MazeFacing=2;
  public double MazeProgress{get{return Math.Min(1,MoveTime/MazeStepSeconds);}}
  public double MazeX{get{return MovingFrom%Maze.Size+(Maze.Player%Maze.Size-MovingFrom%Maze.Size)*MazeProgress;}}
  public double MazeY{get{return MovingFrom/Maze.Size+(Maze.Player/Maze.Size-MovingFrom/Maze.Size)*MazeProgress;}}
  public int QueuedSignal=-1,QueuedTile=-1,QueuedDirection=-1;
  public int[] Pattern;public int PadCount,PatternIndex,ShowIndex=-1,Highlight=-1;bool showOn;
  public int Latch;public double Charge=.30,LastTap=-10;public char PulseKey;
  public event Action<int> Note;public event Action Won,Lost,Next;
  public bool HasLives{get{return Kind=="arrows"||Kind=="pulse"||Kind=="echo";}}
  public static double PulseGain(int level){return Math.Max(.15,.23-(level-1)*.012);}
  public static double PulseDrain(int level,int latch){return Math.Min(.68,.10+(level-1)*.055+latch*.035);}
  public MiniSession(string kind,int level,int seed){
   Kind=kind;Level=Math.Max(1,level);Seed=seed;var random=new Random(seed);
   switch(kind){
    case "arrows":Arrows=ArrowBoard.Generate(Level,seed);Melody=MiniMelodies.ForLevel(Level);break;
    case "signal":Signal=SignalBoard.Generate(Level,seed);SelectedCell=Signal.Source;break;
    case "tiles":Tiles=SlidingBoard.Generate(Level,seed);SelectedCell=Tiles.Blank;break;
    case "maze":Maze=MazeBoard.Generate(Level,seed);break;
    case "echo":PadCount=Level<5?4:Level<12?6:8;Pattern=Enumerable.Range(0,Math.Min(24,3+(Level-1)/2)).Select(i=>random.Next(PadCount)).ToArray();BeginEcho(.7);break;
    case "pulse":PulseKey='E';ResetPulse();break;
    case "mousedash":Dash=new MouseDashBoard(seed);Phase="waiting";break;
    case "stack":Stack=new PaperStackBoard(seed);Phase="ready";break;
    default:if(ExtraPuzzle.Keys.Contains(kind)){Extra=new ExtraPuzzle(kind,Level,seed);break;}throw new ArgumentException("Unknown minigame.");
   }
  }
  public bool ExtraInput(int index){if(Extra==null||Phase!="playing")return false;bool used=Extra.Input(index);Moves=Extra.Moves;CheckExtra();return used;}
  public void CheckExtra(){if(Extra==null)return;Moves=Extra.Moves;if(Extra.Won)Win();else if(Extra.Over&&Phase!="failed")Fail();}
  void Sound(int note){if(Note!=null)Note(note);}
  void Win(){if(Phase=="complete"||Phase=="failed")return;Phase="complete";Wait=Kind=="tiles"?4:Kind=="pulse"?.5:Melody!=null?Melody.Duration+.35:1.65;QueuedArrow=-1;if(Won!=null)Won();}
  void Fail(){Phase="failed";QueuedArrow=-1;Highlight=-1;if(Lost!=null)Lost();}
  void Wrong(){WrongFeedback=true;FeedbackTime=.42;Sound(-1);Lives--;if(Lives<=0)Fail();}
  public bool StackTap(){if(Stack==null||Phase=="failed")return false;int before=Stack.Score;if(!Stack.Tap())return false;Phase="playing";Moves++;if(Stack.Phase=="miss")Sound(-1);else if(Stack.Score>before)Sound(Stack.Perfect?Math.Min(7,1+Stack.Streak):0);return true;}
  public bool HoldDash(double x,double y){
   if(Dash==null)return false;
   if(Phase=="waiting"){if(!Dash.Begin(x,y))return false;Phase="playing";return true;}
   if(Phase=="holding"&&(x-Dash.X)*(x-Dash.X)+(y-Dash.Y)*(y-Dash.Y)<=24*24){Phase="playing";return true;}return false;
  }
  public void ReleaseDash(){if(Dash!=null&&Phase=="playing")Phase="holding";}
  public bool MoveDash(double x,double y){if(Dash==null||Phase!="playing")return false;bool moved=Dash.Move(x,y);if(Dash.Hit){Sound(-1);Fail();}return moved;}
  public bool ArrowTap(int index){
   if(Arrows==null||index<0||index>=Arrows.Arrows.Count||Arrows.Arrows[index].Removed)return false;
   Flights.RemoveAll(f=>f.Index==index&&f.Blocked);if(Flights.Any(f=>f.Index==index))return false;
   if(Phase!="playing")return false;ActiveArrow=index;int distance;ArrowBlocked=Arrows.Blocker(index,out distance)>=0;
   var arrow=Arrows.Arrows[index];double flight=distance+arrow.Cells.Count+1.2;
   if(!double.IsNaN(ExitBounds[arrow.Direction])){double head=24+((arrow.Direction%2==0?arrow.Head/Arrows.Size:arrow.Head%Arrows.Size)+.5)*42;double toEdge=(arrow.Direction==1||arrow.Direction==2?ExitBounds[arrow.Direction]-head:head-ExitBounds[arrow.Direction])/42;flight=Math.Max(.1,toEdge)+arrow.Cells.Count-.5;}
   ArrowDistance=ArrowBlocked?Math.Max(.15,distance-.12):flight;
   ArrowDuration=ArrowBlocked?.42:.055+ArrowDistance/Math.Max(21,Arrows.Size*.65);ArrowAnimation=0;Phase="playing";Moves++;Flights.Add(new Flight{Index=index,Blocked=ArrowBlocked,Duration=ArrowDuration,Distance=ArrowDistance});if(!ArrowBlocked)arrow.Removed=true;
   Sound(ArrowBlocked?-1:Melody.Notes[MelodyStep++%Melody.Notes.Length]);if(ArrowBlocked){Lives--;WrongFeedback=true;FeedbackTime=.42;if(Lives<=0)Fail();}return true;
  }
  public double ArrowTravel{get{double t=Math.Min(1,ArrowAnimation/ArrowDuration);if(ArrowBlocked){double v=t<.44?t/.44:(1-t)/.56;return ArrowDistance*Math.Sin(Math.Max(0,v)*Math.PI/2);}double ramp=Math.Min(.18,.055/ArrowDuration);return ArrowDistance*(t<ramp?t*t/(2*ramp):t-ramp/2)/(1-ramp/2);}}
  public bool RotateSignal(int cell){if(Signal==null||Phase!="playing"||cell<0||cell>=Signal.Masks.Length)return false;if(TurnTime<.14){QueuedSignal=cell;return false;}TurningCell=cell;TurningMask=Signal.Masks[cell];TurnTime=0;Signal.Rotate(cell);Moves++;if(Signal.Solved)Win();return true;}
  public bool SlideTile(int cell){
   if(Tiles==null||Phase!="playing"||cell<0||cell>=Tiles.Tiles.Length)return false;
   if(MoveTime<.14){if(Math.Abs(cell%Tiles.Size-Tiles.Blank%Tiles.Size)+Math.Abs(cell/Tiles.Size-Tiles.Blank/Tiles.Size)==1)QueuedTile=cell;return false;}
   int blank=Tiles.Blank,value=Tiles.Tiles[cell];if(!Tiles.Move(cell))return false;MovingValue=value;MovingFrom=cell;MovingTo=blank;MoveTime=0;Moves++;if(Tiles.Solved)Win();return true;
  }
  public bool MoveMaze(int direction){if(Maze==null||Phase!="playing"||direction<0||direction>3||Maze.Player==Maze.Goal)return false;if(MoveTime<MazeStepSeconds){QueuedDirection=direction;return false;}int from=Maze.Player;if(!Maze.Move(direction))return false;MovingFrom=from;MovingTo=Maze.Player;MazeFacing=direction;MoveTime=0;Moves++;return true;}
  public void HoldMaze(int direction){MazeHeldDirection=direction>=0&&direction<4?direction:-1;if(MazeHeldDirection>=0&&MoveTime>=MazeStepSeconds)MoveMaze(MazeHeldDirection);}
  public void ReleaseMaze(){MazeHeldDirection=-1;QueuedDirection=-1;}
  void BeginEcho(double delay){Phase="showing";Wait=delay;ShowIndex=-1;showOn=false;Highlight=-1;PatternIndex=0;}
  public bool ReplayEcho(){if(Kind!="echo"||Phase!="playing")return false;BeginEcho(.6);return true;}
  public bool EchoTap(int pad){
   if(Kind!="echo"||Phase!="playing"||pad<0||pad>=PadCount)return false;
   if(pad!=Pattern[PatternIndex]){Wrong();if(Phase!="failed")BeginEcho(.9);return true;}
   Highlight=pad;FeedbackTime=.20;WrongFeedback=false;Sound(pad);PatternIndex++;Moves++;if(PatternIndex==Pattern.Length)Win();return true;
  }
  void ResetPulse(){Phase="ready";Wait=0;Latch=0;Charge=.30;LastTap=-10;}
  public bool PulseTap(){
   if(Kind!="pulse"||(Phase!="playing"&&Phase!="ready")||Elapsed-LastTap<.075)return false;Phase="playing";LastTap=Elapsed;Charge=Math.Min(1,Charge+PulseGain(Level));Moves++;FeedbackTime=.13;WrongFeedback=false;
   if(Charge>=1){Latch++;if(Latch>=3)Win();else{Phase="latch";Wait=.15;Sound(2+Latch*2);}}return true;
  }
  public void Tick(double seconds){
   if(seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds)||Phase=="failed")return;double dt=Math.Min(.1,seconds);bool mazeMoving=Maze!=null&&MoveTime<MazeStepSeconds;if(Phase!="complete")Elapsed+=dt;TurnTime+=dt;MoveTime+=dt;
   if(Extra!=null){Extra.Feedback=Math.Max(0,Extra.Feedback-dt);if(Phase=="complete"){Wait-=dt;if(Wait<=0){Wait=double.MaxValue;if(Next!=null)Next();}}else CheckExtra();return;}
   if(Dash!=null){if(Phase=="playing"){Dash.Tick(dt);if(Dash.Hit){Sound(-1);Fail();}}return;}
   if(Stack!=null){Stack.Tick(dt);if(Stack.Over)Fail();return;}
   if(FeedbackTime>0){FeedbackTime=Math.Max(0,FeedbackTime-dt);if(FeedbackTime==0&&Kind=="echo"&&Phase=="playing")Highlight=-1;}
   if(Phase=="playing"){
    if(QueuedSignal>=0&&TurnTime>=.14){int cell=QueuedSignal;QueuedSignal=-1;RotateSignal(cell);}
    if(QueuedTile>=0&&MoveTime>=.14){int cell=QueuedTile;QueuedTile=-1;SlideTile(cell);}
    if(Maze!=null&&MoveTime>=MazeStepSeconds){
     if(Maze.Player==Maze.Goal){ReleaseMaze();Win();}
     else{int direction=QueuedDirection>=0?QueuedDirection:MazeHeldDirection;QueuedDirection=-1;double remainder=mazeMoving?MoveTime-MazeStepSeconds:0;if(direction>=0&&MoveMaze(direction))MoveTime=remainder;}
    }
   }
   if(Phase=="complete"){Wait-=dt;if(Wait<=0){Wait=double.MaxValue;if(Next!=null)Next();}return;}
   if(Arrows!=null&&Flights.Count>0){
    foreach(var f in Flights)f.Time+=dt;Flights.RemoveAll(f=>f.Time>=f.Duration);if(Flights.Count==0){ActiveArrow=-1;if(Arrows.Remaining==0)Win();}return;
   }
   if(Kind=="echo"&&Phase=="showing"){
    Wait-=dt;if(Wait>0)return;
    if(showOn){showOn=false;Highlight=-1;Wait=.16;}
    else{ShowIndex++;if(ShowIndex>=Pattern.Length){Phase="playing";Highlight=-1;return;}Highlight=Pattern[ShowIndex];Sound(Highlight);showOn=true;Wait=Math.Max(.24,.56-Level*.007);}return;
   }
   if(Kind=="pulse"){
    if(Phase=="latch"){Wait-=dt;if(Wait<=0){Phase="playing";Charge=.30;}return;}
    if(Phase!="playing")return;Charge=Math.Max(0,Charge-PulseDrain(Level,Latch)*dt);
    if(Charge<=0){Wrong();if(Phase!="failed")ResetPulse();}
   }
  }
 }
}
