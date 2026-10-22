using System;
using System.Linq;

namespace LittleNotebook {
 public sealed class MiniSession {
  public readonly string Kind;public readonly int Level,Seed;
  public int Lives=3,Moves,SelectedCell;
  public string Phase="playing";
  public double Wait,Elapsed,FeedbackTime;public bool WrongFeedback;
  public ArrowBoard Arrows;public SignalBoard Signal;public LanternBoard Lanterns;public SlidingBoard Tiles;public MazeBoard Maze;
  public MouseDashBoard Dash;
  public int ActiveArrow=-1,QueuedArrow=-1;public bool ArrowBlocked;public double ArrowAnimation,ArrowDuration,ArrowDistance;
  public int TurningCell=-1,TurningMask;public double TurnTime=1;
  public int MovingValue,MovingFrom,MovingTo;public double MoveTime=1;
  public int[] Pattern;public int PadCount,PatternIndex,ShowIndex=-1,Highlight=-1;bool showOn;
  public int Latch;public double Charge=.30,TimeLeft,LastTap=-10;public char PulseKey;
  public event Action<int> Note;public event Action Won,Lost,Next;
  public bool HasLives{get{return Kind=="arrows"||Kind=="pulse"||Kind=="echo";}}
  public static double PulseGain(int level){return Math.Max(.14,.18-(level-1)*.0015);}
  public static double PulseDrain(int level,int latch){return Math.Min(.48,.10+(level-1)*.008+latch*.065);}
  public static double PulseLimit(int level){return Math.Max(9,13-(level-1)*.09);}
  public static int Cooldown(int level){return Math.Min(10,5+(level-1)/4);}
  public MiniSession(string kind,int level,int seed){
   Kind=kind;Level=Math.Max(1,level);Seed=seed;var random=new Random(seed);
   switch(kind){
    case "arrows":Arrows=ArrowBoard.Generate(Level,seed);break;
    case "signal":Signal=SignalBoard.Generate(Level,seed);SelectedCell=Signal.Source;break;
    case "lanterns":Lanterns=LanternBoard.Generate(Level,seed);break;
    case "tiles":Tiles=SlidingBoard.Generate(Level,seed);SelectedCell=Tiles.Blank;break;
    case "maze":Maze=MazeBoard.Generate(Level,seed);break;
    case "echo":PadCount=Level<5?4:Level<12?6:8;Pattern=Enumerable.Range(0,Math.Min(24,3+(Level-1)/2)).Select(i=>random.Next(PadCount)).ToArray();BeginEcho(.7);break;
    case "pulse":PulseKey=Level<5?'E':"EFJK"[random.Next(4)];ResetPulse(2.5);break;
    case "mousedash":Dash=new MouseDashBoard(seed);Phase="waiting";break;
    default:throw new ArgumentException("Unknown minigame.");
   }
  }
  void Sound(int note){if(Note!=null)Note(note);}
  void Win(){if(Phase=="complete"||Phase=="failed")return;Phase="complete";Wait=Kind=="pulse"?Cooldown(Level):1.45;QueuedArrow=-1;if(Won!=null)Won();}
  void Fail(){Phase="failed";QueuedArrow=-1;Highlight=-1;if(Lost!=null)Lost();}
  void Wrong(){WrongFeedback=true;FeedbackTime=.42;Sound(-1);Lives--;if(Lives<=0)Fail();}
  public bool HoldDash(double x,double y){
   if(Dash==null)return false;
   if(Phase=="waiting"){if(!Dash.Begin(x,y))return false;Phase="playing";return true;}
   if(Phase=="holding"&&(x-Dash.X)*(x-Dash.X)+(y-Dash.Y)*(y-Dash.Y)<=24*24){Phase="playing";return true;}return false;
  }
  public void ReleaseDash(){if(Dash!=null&&Phase=="playing")Phase="holding";}
  public bool MoveDash(double x,double y){if(Dash==null||Phase!="playing")return false;bool moved=Dash.Move(x,y);if(Dash.Hit){Sound(-1);Fail();}return moved;}
  public bool ArrowTap(int index){
   if(Arrows==null||index<0||index>=Arrows.Arrows.Count||Arrows.Arrows[index].Removed)return false;
   if(Phase=="arrow"){if(index!=ActiveArrow&&QueuedArrow<0)QueuedArrow=index;return false;}
   if(Phase!="playing")return false;ActiveArrow=index;int distance;ArrowBlocked=Arrows.Blocker(index,out distance)>=0;
   ArrowDistance=ArrowBlocked?Math.Max(.15,distance-.50):distance+Arrows.Arrows[index].Cells.Count+2;
   ArrowDuration=ArrowBlocked?.48:Math.Min(1.25,.32+ArrowDistance*.018);ArrowAnimation=0;Phase="arrow";Moves++;
   Sound(ArrowBlocked?0:(Moves-1)%8);if(ArrowBlocked){Lives--;WrongFeedback=true;FeedbackTime=.48;}return true;
  }
  public double ArrowTravel{get{double t=Math.Min(1,ArrowAnimation/ArrowDuration);if(ArrowBlocked){double v=t<.48?t/.48:(1-t)/.52;return ArrowDistance*(1-Math.Pow(1-Math.Max(0,v),3));}return ArrowDistance*(t*t*(3-2*t));}}
  public bool RotateSignal(int cell){if(Signal==null||Phase!="playing"||cell<0||cell>=Signal.Masks.Length||TurnTime<.14)return false;TurningCell=cell;TurningMask=Signal.Masks[cell];TurnTime=0;Signal.Rotate(cell);Moves++;if(Signal.Solved)Win();return true;}
  public bool PressLantern(int cell){if(Lanterns==null||Phase!="playing"||cell<0||cell>=Lanterns.Size*Lanterns.Size)return false;Lanterns.Press(cell);Moves++;if(Lanterns.State==0)Win();return true;}
  public bool SlideTile(int cell){
   if(Tiles==null||Phase!="playing"||MoveTime<.14||cell<0||cell>=Tiles.Tiles.Length)return false;
   int blank=Tiles.Blank,value=Tiles.Tiles[cell];if(!Tiles.Move(cell))return false;MovingValue=value;MovingFrom=cell;MovingTo=blank;MoveTime=0;Moves++;if(Tiles.Solved)Win();return true;
  }
  public bool MoveMaze(int direction){if(Maze==null||Phase!="playing"||MoveTime<.10||direction<0||direction>3)return false;int from=Maze.Player;if(!Maze.Move(direction))return false;MovingFrom=from;MovingTo=Maze.Player;MoveTime=0;Moves++;if(Maze.Player==Maze.Goal)Win();return true;}
  void BeginEcho(double delay){Phase="showing";Wait=delay;ShowIndex=-1;showOn=false;Highlight=-1;PatternIndex=0;}
  public bool ReplayEcho(){if(Kind!="echo"||Phase!="playing")return false;BeginEcho(.6);return true;}
  public bool EchoTap(int pad){
   if(Kind!="echo"||Phase!="playing"||pad<0||pad>=PadCount)return false;
   if(pad!=Pattern[PatternIndex]){Wrong();if(Phase!="failed")BeginEcho(.9);return true;}
   Highlight=pad;FeedbackTime=.20;WrongFeedback=false;Sound(pad);PatternIndex++;Moves++;if(PatternIndex==Pattern.Length)Win();return true;
  }
  void ResetPulse(double delay){Phase="ready";Wait=delay;Latch=0;Charge=.30;TimeLeft=PulseLimit(Level);LastTap=-10;}
  public bool PulseTap(){
   if(Kind!="pulse"||Phase!="playing"||Elapsed-LastTap<.075)return false;LastTap=Elapsed;Charge=Math.Min(1,Charge+PulseGain(Level));Moves++;FeedbackTime=.13;WrongFeedback=false;
   if(Charge>=1){Latch++;if(Latch>=3)Win();else{Phase="latch";Wait=.38;Sound(2+Latch*2);}}return true;
  }
  public void Tick(double seconds){
   if(seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds)||Phase=="failed")return;double dt=Math.Min(.1,seconds);Elapsed+=dt;TurnTime+=dt;MoveTime+=dt;
   if(Dash!=null){if(Phase=="playing"){Dash.Tick(dt);if(Dash.Hit){Sound(-1);Fail();}}return;}
   if(FeedbackTime>0){FeedbackTime=Math.Max(0,FeedbackTime-dt);if(FeedbackTime==0&&Kind=="echo"&&Phase=="playing")Highlight=-1;}
   if(Phase=="complete"){Wait-=dt;if(Wait<=0){Wait=double.MaxValue;if(Next!=null)Next();}return;}
   if(Phase=="arrow"){
    ArrowAnimation+=dt;if(ArrowAnimation<ArrowDuration)return;int finished=ActiveArrow;ActiveArrow=-1;
    if(ArrowBlocked){if(Lives<=0){Fail();return;}}else Arrows.Arrows[finished].Removed=true;
    Phase="playing";if(Arrows.Remaining==0){Win();return;}int queued=QueuedArrow;QueuedArrow=-1;if(queued>=0)ArrowTap(queued);return;
   }
   if(Kind=="echo"&&Phase=="showing"){
    Wait-=dt;if(Wait>0)return;
    if(showOn){showOn=false;Highlight=-1;Wait=.16;}
    else{ShowIndex++;if(ShowIndex>=Pattern.Length){Phase="playing";Highlight=-1;return;}Highlight=Pattern[ShowIndex];Sound(Highlight);showOn=true;Wait=Math.Max(.24,.56-Level*.007);}return;
   }
   if(Kind=="pulse"){
    if(Phase=="ready"||Phase=="latch"){Wait-=dt;if(Wait<=0){Phase="playing";Charge=.30;TimeLeft=PulseLimit(Level);}return;}
    if(Phase!="playing")return;Charge=Math.Max(0,Charge-PulseDrain(Level,Latch)*dt);TimeLeft=Math.Max(0,TimeLeft-dt);
    if(Charge<=0||TimeLeft<=0){Wrong();if(Phase!="failed")ResetPulse(Cooldown(Level));}
   }
  }
 }
}
