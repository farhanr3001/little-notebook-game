using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
namespace LittleNotebook {
 public sealed partial class ExtraPuzzle {
  public static readonly string[] Keys={"crates","pour","code"};
  public readonly string Kind;public readonly int Level;public int LastFrom=-1,LastTo=-1,LastColour=-1,LastAmount,NewCompletedTube=-1,PreviousPlayer=-1;public int W,H,Selected=-1,Moves,Player,Colours,Limit,Focus,Last=-1;public bool Won,Over;public double Feedback;public string Message="";
  public int[] Cells,Goal;public List<int[]> Guesses=new List<int[]>(),Scores=new List<int[]>();public List<List<int>> Tubes=new List<List<int>>();public List<int> Solution=new List<int>();
  readonly Stack<State> undo=new Stack<State>();
  sealed class State {public int[] Cells;public int Selected,Player,Moves;public List<List<int>> Tubes;public int Guesses;}
  void Save(){undo.Push(new State{Cells=(int[])Cells.Clone(),Selected=Selected,Player=Player,Moves=Moves,Tubes=Tubes.Select(t=>t.ToList()).ToList(),Guesses=Guesses.Count});}
  public bool Undo(){if(undo.Count==0)return false;var s=undo.Pop();Cells=s.Cells;Selected=Kind=="pour"?-1:s.Selected;Player=s.Player;Moves=s.Moves;Tubes=s.Tubes;while(Guesses.Count>s.Guesses){Guesses.RemoveAt(Guesses.Count-1);Scores.RemoveAt(Scores.Count-1);}CancelCrateSearch();Deadlocked=false;Won=Over=false;LastFrom=LastTo=PreviousPlayer=NewCompletedTube=-1;Message="";Feedback=.2;return true;}
  public ExtraPuzzle(string kind,int level,int seed){Kind=kind;Level=level;var r=new Random(seed);W=H=5;Cells=new int[25];Goal=new int[25];
   if(kind=="crates")MakeCrates(r);
   if(kind=="pour")MakePour(r);
   if(kind=="code"){Colours=Math.Min(8,5+(level-1)/6);W=Math.Min(6,4+(level-1)/12);H=1;Cells=Enumerable.Repeat(-1,W).ToArray();Goal=Enumerable.Range(0,W).Select(x=>r.Next(Colours)).ToArray();Limit=Math.Max(8,12-(level-1)/10);}
  }
  public bool Input(int index,bool alternate=false){if(Won||Over||Deadlocked)return false;Feedback=.22;LastFrom=LastTo=NewCompletedTube=-1;Last=index;Message="";
   if(Kind=="code"){if(index>=100){int colour=index-100;if(colour<Colours){Save();Cells[Focus]=colour;Focus=(Focus+1)%W;return true;}return false;}if(index==99)return Guess();if(index>=0&&index<W){Focus=index;return true;}return false;}
   if(Kind=="pour")return Pour(index);
   if(index<0||index>=Cells.Length)return false;Focus=index;
   if(Kind=="crates"){int d=Enumerable.Range(0,4).FirstOrDefault(x=>MiniGrid.Next(Player,x,W,H)==index,-1);return d>=0&&Move(d);}
   return false;
  }
  public bool Move(int direction){if(Won||Over||Deadlocked)return false;
   if(Kind=="crates"){int next=MiniGrid.Next(Player,direction,W,H);if(next<0||Cells[next]==-1)return false;int beyond=MiniGrid.Next(next,direction,W,H);if(Cells[next]==1&&(beyond<0||Cells[beyond]!=0))return false;Save();bool pushed=Cells[next]==1;if(pushed){Cells[next]=0;Cells[beyond]=1;}PreviousPlayer=Player;Player=next;Last=next;Moves++;Won=Enumerable.Range(0,Cells.Length).Where(i=>Goal[i]==1).All(i=>Cells[i]==1);Feedback=.18;if(pushed&&!Won)BeginCrateSearch();return true;}return false;
  }
  void MakeCrates(Random r){int stage=Level<=3?Level:Level+4+(Level-4)/2;W=H=Math.Min(10,6+(stage-1)/5);int[] best=null;List<int> proof=null;int bestPlayer=0,bestScore=-1;for(int attempt=0;attempt<30;attempt++){Cells=new int[W*H];for(int i=0;i<Cells.Length;i++)if(i%W==0||i%W==W-1||i/W==0||i/W==H-1)Cells[i]=-1;else if(r.NextDouble()<.12)Cells[i]=-1;Goal=new int[Cells.Length];var free=Enumerable.Range(0,Cells.Length).Where(i=>Cells[i]==0).OrderBy(x=>r.Next()).ToArray();int crates=Math.Min(5,1+(stage+1)/3);foreach(int c in free.Take(crates))Goal[c]=Cells[c]=1;Player=free[crates];var actions=new List<int>();int pulls=0;for(int step=0;step<80+Math.Min(650,stage*20);step++){int d=r.Next(4),next=MiniGrid.Next(Player,d,W,H);if(next<0||Cells[next]!=0)continue;int behind=MiniGrid.Next(Player,MiniGrid.Opposite(d),W,H);if(behind>=0&&Cells[behind]==1&&r.NextDouble()<.8){Cells[behind]=0;Cells[Player]=1;pulls++;}Player=next;actions.Add(MiniGrid.Opposite(d));}int score=Enumerable.Range(0,Cells.Length).Count(i=>Cells[i]==1&&Goal[i]==0)*100+pulls;if(score>bestScore){bestScore=score;best=(int[])Cells.Clone();proof=actions.AsEnumerable().Reverse().ToList();bestPlayer=Player;crateGoals=(int[])Goal.Clone();}if(score>=crates*100+10)break;}Cells=best;Goal=crateGoals;Player=bestPlayer;Solution=proof;}
  int[] crateGoals;
  void MakePour(Random r){
   int count=Math.Min(9,3+(Level<4?Level-1:3+(Level-4)/2));W=count+2;H=4;Colours=count;
   int best=-1;List<List<int>> bestTubes=null;List<int> bestProof=null;
   for(int attempt=0;attempt<8;attempt++){
    Tubes=Enumerable.Range(0,count).Select(c=>Enumerable.Repeat(c,4).ToList()).Concat(new[]{new List<int>(),new List<int>()}).ToList();var proof=new List<int>();
    for(int i=0;i<55+Math.Min(650,Level*22);i++){
     var choices=new List<int[]>();
     for(int from=0;from<W;from++)if(Tubes[from].Count>0){int run=1;while(run<Tubes[from].Count&&Tubes[from][Tubes[from].Count-1-run]==Tubes[from].Last())run++;int available=run==Tubes[from].Count?run:run-1;
      for(int to=0;to<W;to++)if(from!=to&&Tubes[to].Count<4&&(Tubes[to].Count==0||Tubes[to].Last()!=Tubes[from].Last()))for(int amount=1;amount<=Math.Min(available,4-Tubes[to].Count);amount++)choices.Add(new[]{from,to,amount});}
     if(choices.Count==0)break;var m=choices[r.Next(choices.Count)];int colour=Tubes[m[0]].Last();Tubes[m[0]].RemoveRange(Tubes[m[0]].Count-m[2],m[2]);Tubes[m[1]].AddRange(Enumerable.Repeat(colour,m[2]));proof.Add(m[1]*100+m[0]);
    }
    int score=Tubes.Sum(t=>t.Distinct().Count())*10+Tubes.Count(t=>t.Distinct().Count()>1)*20;
    if(score>best){best=score;bestTubes=Tubes.Select(t=>t.ToList()).ToList();bestProof=proof.AsEnumerable().Reverse().ToList();}
   }Tubes=bestTubes;Solution=bestProof;Cells=new int[W];Goal=new int[W];
  }
  public bool IsTubeComplete(int index){return index>=0&&index<Tubes.Count&&Tubes[index].Count==4&&Tubes[index].All(c=>c==Tubes[index][0]);}
  bool Pour(int to){
   if(to<0||to>=Tubes.Count)return false;
   if(Selected<0){if(Tubes[to].Count==0)return false;Selected=to;Feedback=0;return true;}
   int from=Selected;if(from==to){Selected=-1;Feedback=0;return true;}
   if(Tubes[from].Count==0||Tubes[to].Count==4){Message="blocked";Feedback=.22;return false;}
   Save();int colour=Tubes[from].Last(),amount=1;while(amount<Tubes[from].Count&&Tubes[from][Tubes[from].Count-1-amount]==colour)amount++;amount=Math.Min(amount,4-Tubes[to].Count);
   LastFrom=from;LastTo=to;LastColour=colour;LastAmount=amount;Tubes[from].RemoveRange(Tubes[from].Count-amount,amount);Tubes[to].AddRange(Enumerable.Repeat(colour,amount));Selected=-1;Moves++;Feedback=.34;if(IsTubeComplete(to))NewCompletedTube=to;Won=Tubes.All(t=>t.Count==0||t.Count==4&&t.All(c=>c==t[0]));return true;
  }
  bool Guess(){if(Cells.Any(c=>c<0))return false;Save();int exact=Enumerable.Range(0,W).Count(i=>Cells[i]==Goal[i]),all=Enumerable.Range(0,Colours).Sum(c=>Math.Min(Cells.Count(x=>x==c),Goal.Count(x=>x==c)));Guesses.Add((int[])Cells.Clone());Scores.Add(new[]{exact,all-exact});Moves++;Won=exact==W;Over=!Won&&Moves>=Limit;return true;}
 }
}
