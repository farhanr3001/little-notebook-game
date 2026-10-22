using System;
using System.Collections.Generic;
using System.Linq;

namespace LittleNotebook {
 public sealed class MiniProgress {
  public int Level=1,Seed,BestCompleted,Completed,Failures;
 }
 public static class MiniCatalog {
  public static readonly string[] Keys={"arrows","signal","pulse","lanterns","tiles","echo","maze"};
  public static void Normalize(Profile profile){
   if(profile.MiniGames==null)profile.MiniGames=new Dictionary<string,MiniProgress>();
   foreach(string key in Keys){MiniProgress value;if(!profile.MiniGames.TryGetValue(key,out value)||value==null)profile.MiniGames[key]=value=new MiniProgress();
    value.Level=Math.Max(1,Math.Min(100000,value.Level));value.BestCompleted=Math.Max(0,value.BestCompleted);
    if(value.Seed==0)value.Seed=Guid.NewGuid().GetHashCode()|1;
   }
   if(!Keys.Contains(profile.Settings.SelectedMiniGame))profile.Settings.SelectedMiniGame="arrows";
   if(profile.Settings.PlaySection!="minigames")profile.Settings.PlaySection="learning";
  }
  public static int LevelSeed(MiniProgress progress){return unchecked(progress.Seed+progress.Level*7919);}
 }
 public static class MiniGrid {
  public static readonly int[] Dx={0,1,0,-1},Dy={-1,0,1,0};
  public static int Next(int cell,int direction,int width,int height){int x=cell%width+Dx[direction],y=cell/width+Dy[direction];return x<0||x>=width||y<0||y>=height?-1:y*width+x;}
  public static int Opposite(int direction){return (direction+2)%4;}
  public static int Rotate(int mask){return ((mask<<1)&15)|((mask>>3)&1);}
  public static void Shuffle<T>(IList<T> values,Random random){for(int i=values.Count-1;i>0;i--){int j=random.Next(i+1);T value=values[i];values[i]=values[j];values[j]=value;}}
 }
 public sealed class EscapeArrow {
  public int Direction;public List<int> Cells=new List<int>();public bool Removed;
  public int Head{get{return Cells[Cells.Count-1];}}
 }
 public sealed class ArrowBoard {
  public int Size;public List<EscapeArrow> Arrows=new List<EscapeArrow>();
  public int Remaining{get{return Arrows.Count(a=>!a.Removed);}}
  public int Blocker(int index,out int distance){
   var arrow=Arrows[index];int at=arrow.Head;distance=0;
   while((at=MiniGrid.Next(at,arrow.Direction,Size,Size))>=0){distance++;for(int i=0;i<Arrows.Count;i++)if(i!=index&&!Arrows[i].Removed&&Arrows[i].Cells.Contains(at))return i;}
   return -1;
  }
  public bool CanExit(int index){int distance;return !Arrows[index].Removed&&Blocker(index,out distance)<0;}
  public List<int> Solution(){
   // Independent forward check of the reverse-construction invariant.
   var removed=Arrows.Select(a=>a.Removed).ToArray();var answer=new List<int>();
   try{while(Remaining>0){int next=Enumerable.Range(0,Arrows.Count).FirstOrDefault(i=>CanExit(i),-1);if(next<0)return null;Arrows[next].Removed=true;answer.Add(next);}return answer;}
   finally{for(int i=0;i<Arrows.Count;i++)Arrows[i].Removed=removed[i];}
  }
  public static ArrowBoard Generate(int level,int seed){
   level=Math.Max(1,level);int n=6+Math.Min(58,level),target=Math.Min(4+Math.Min(level,148)*2,n*n/7);ArrowBoard best=null;double bestScore=-1;
   for(int pass=0;pass<4;pass++){
    var random=new Random(unchecked(seed+pass*104729));var board=new ArrowBoard{Size=n};var occupied=new bool[n*n];int misses=0;
    while(board.Arrows.Count<target&&misses<Math.Max(700,target*50)){
     int head=random.Next(n*n),direction=random.Next(4),back=MiniGrid.Opposite(direction),first=MiniGrid.Next(head,back,n,n);
     if(occupied[head]||first<0||occupied[first]){misses++;continue;}
     var ray=new HashSet<int>();int at=head;bool clear=true;while((at=MiniGrid.Next(at,direction,n,n))>=0){if(occupied[at]){clear=false;break;}ray.Add(at);}if(!clear){misses++;continue;}
     int maximum=Math.Max(5,Math.Min(12,(int)(n*n*.80/target)-pass));
     var path=new List<int>{head,first};var used=new HashSet<int>(path);int previous=back,wanted=random.Next(3,maximum+1);
     while(path.Count<wanted){
      var choices=Enumerable.Range(0,4).Where(d=>d!=MiniGrid.Opposite(previous)).ToList();MiniGrid.Shuffle(choices,random);
      if(random.NextDouble()<.54&&choices.Remove(previous))choices.Insert(0,previous);
      int next=-1,chosen=-1;foreach(int d in choices){int cell=MiniGrid.Next(path.Last(),d,n,n);if(cell>=0&&!occupied[cell]&&!used.Contains(cell)&&!ray.Contains(cell)){next=cell;chosen=d;break;}}
      if(next<0)break;path.Add(next);used.Add(next);previous=chosen;
     }
     if(path.Count<3){misses++;continue;}path.Reverse();board.Arrows.Add(new EscapeArrow{Direction=direction,Cells=path});foreach(int cell in path)occupied[cell]=true;misses=0;
    }
    int blocked=Enumerable.Range(0,board.Arrows.Count).Count(i=>!board.CanExit(i));double score=board.Arrows.Count*1000+blocked;
    if(score>bestScore){best=board;bestScore=score;}
    if(board.Arrows.Count>=target&&blocked>=target*.25)break;
   }
   if(best==null||best.Arrows.Count==0||best.Solution()==null)throw new InvalidOperationException("Arrow board failed solvability verification.");
   return best;
  }
 }
 public sealed class SignalBoard {
  public int Width,Height,Source,Destination,Moves;public int[] Masks,SolutionMasks;
  public bool[] Powered(){
   var found=new bool[Masks.Length];if((Masks[Source]&8)==0)return found;var queue=new Queue<int>();queue.Enqueue(Source);found[Source]=true;
   while(queue.Count>0){int at=queue.Dequeue();for(int d=0;d<4;d++){int next=MiniGrid.Next(at,d,Width,Height);if(next>=0&&!found[next]&&(Masks[at]&(1<<d))!=0&&(Masks[next]&(1<<MiniGrid.Opposite(d)))!=0){found[next]=true;queue.Enqueue(next);}}}return found;
  }
  public bool Solved{get{return Powered()[Destination]&&(Masks[Destination]&2)!=0;}}
  public void Rotate(int cell){Masks[cell]=MiniGrid.Rotate(Masks[cell]);Moves++;}
  public static SignalBoard Generate(int level,int seed){
   int w=Math.Min(12,4+(level-1)/4),h=Math.Min(11,3+(level-1)/5);SignalBoard best=null;int bestLength=-1;
   for(int pass=0;pass<12;pass++){
    var random=new Random(unchecked(seed+pass*1009));var board=new SignalBoard{Width=w,Height=h,Source=random.Next(h)*w,Destination=random.Next(h)*w+w-1,Masks=new int[w*h]};
    var visited=new bool[w*h];var stack=new Stack<int>();stack.Push(board.Source);visited[board.Source]=true;
    while(stack.Count>0){int at=stack.Peek();var options=Enumerable.Range(0,4).Where(d=>{int next=MiniGrid.Next(at,d,w,h);return next>=0&&!visited[next];}).ToList();if(options.Count==0){stack.Pop();continue;}int direction=options[random.Next(options.Count)],cell=MiniGrid.Next(at,direction,w,h);board.Masks[at]|=1<<direction;board.Masks[cell]|=1<<MiniGrid.Opposite(direction);visited[cell]=true;stack.Push(cell);}
    board.Masks[board.Source]|=8;board.Masks[board.Destination]|=2;board.SolutionMasks=(int[])board.Masks.Clone();
    var distances=Enumerable.Repeat(-1,w*h).ToArray();var queue=new Queue<int>();distances[board.Source]=0;queue.Enqueue(board.Source);while(queue.Count>0){int at=queue.Dequeue();for(int d=0;d<4;d++)if((board.Masks[at]&(1<<d))!=0){int next=MiniGrid.Next(at,d,w,h);if(next>=0&&distances[next]<0){distances[next]=distances[at]+1;queue.Enqueue(next);}}}
    int length=distances[board.Destination];if(length>bestLength){best=board;bestLength=length;}if(length>=w*h*.48)break;
   }
   var scramble=new Random(unchecked(seed^0x45ab19));for(int attempt=0;attempt<200;attempt++){best.Masks=(int[])best.SolutionMasks.Clone();for(int i=0;i<best.Masks.Length;i++)for(int r=scramble.Next(4);r>0;r--)best.Masks[i]=MiniGrid.Rotate(best.Masks[i]);if(!best.Solved)return best;}
   throw new InvalidOperationException("Could not scramble the route.");
  }
 }
 public sealed class LanternBoard {
  public int Size,Moves;public ulong State;
  public ulong Effect(int cell){ulong mask=1UL<<cell;for(int d=0;d<4;d++){int next=MiniGrid.Next(cell,d,Size,Size);if(next>=0)mask|=1UL<<next;}return mask;}
  public void Press(int cell){State^=Effect(cell);Moves++;}
  public ulong? Solve(){
   int n=Size*Size;var rows=new ulong[n];var rhs=new bool[n];for(int r=0;r<n;r++){rows[r]=Effect(r);rhs[r]=(State&(1UL<<r))!=0;}int rank=0;var pivots=new List<int>();
   for(int column=0;column<n;column++){int row=rank;while(row<n&&(rows[row]&(1UL<<column))==0)row++;if(row==n)continue;ulong mask=rows[rank];rows[rank]=rows[row];rows[row]=mask;bool bit=rhs[rank];rhs[rank]=rhs[row];rhs[row]=bit;
    for(int other=0;other<n;other++)if(other!=rank&&(rows[other]&(1UL<<column))!=0){rows[other]^=rows[rank];rhs[other]^=rhs[rank];}pivots.Add(column);rank++;
   }
   for(int r=rank;r<n;r++)if(rows[r]==0&&rhs[r])return null;ulong solution=0;for(int r=0;r<rank;r++)if(rhs[r])solution|=1UL<<pivots[r];return solution;
  }
  public static LanternBoard Generate(int level,int seed){var board=new LanternBoard{Size=Math.Min(7,3+(level-1)/6)};var random=new Random(seed);do{board.State=0;for(int i=0;i<Math.Min(board.Size*board.Size,3+level*2);i++)board.State^=board.Effect(random.Next(board.Size*board.Size));}while(board.State==0);return board;}
 }
 public sealed class SlidingBoard {
  public int Size,Blank,Moves;public int[] Tiles;public List<int> Scramble=new List<int>();
  public bool Solved{get{return Tiles.Take(Tiles.Length-1).Select((v,i)=>v==i+1).All(x=>x)&&Tiles.Last()==0;}}
  public bool Move(int cell){if(cell<0||cell>=Tiles.Length||Math.Abs(cell%Size-Blank%Size)+Math.Abs(cell/Size-Blank/Size)!=1)return false;Tiles[Blank]=Tiles[cell];Tiles[cell]=0;Blank=cell;Moves++;return true;}
  public static SlidingBoard Generate(int level,int seed){
   int n=level<8?3:level<25?4:5;var board=new SlidingBoard{Size=n,Tiles=Enumerable.Range(1,n*n).Select(i=>i==n*n?0:i).ToArray(),Blank=n*n-1};var random=new Random(seed);int previous=-1;
   for(int i=0;i<Math.Min(240,10+level*5);i++){var options=Enumerable.Range(0,4).Select(d=>MiniGrid.Next(board.Blank,d,n,n)).Where(c=>c>=0&&c!=previous).ToList();int next=options[random.Next(options.Count)];board.Scramble.Add(board.Blank);previous=board.Blank;board.Move(next);}if(board.Solved){board.Scramble.Add(board.Blank);board.Move(MiniGrid.Next(board.Blank,0,n,n));}board.Moves=0;return board;
  }
 }
 public sealed class MazeBoard {
  public int Size,Player,Goal,Moves;public int[] Walls;
  public bool Move(int direction){if((Walls[Player]&(1<<direction))!=0)return false;int next=MiniGrid.Next(Player,direction,Size,Size);if(next<0)return false;Player=next;Moves++;return true;}
  public List<int> Route(){var prior=Enumerable.Repeat(-1,Walls.Length).ToArray();var queue=new Queue<int>();queue.Enqueue(Player);prior[Player]=Player;while(queue.Count>0){int at=queue.Dequeue();if(at==Goal)break;for(int d=0;d<4;d++)if((Walls[at]&(1<<d))==0){int next=MiniGrid.Next(at,d,Size,Size);if(next>=0&&prior[next]<0){prior[next]=at;queue.Enqueue(next);}}}if(prior[Goal]<0)return null;var result=new List<int>();for(int at=Goal;at!=Player;at=prior[at])result.Add(at);result.Reverse();return result;}
  public static MazeBoard Generate(int level,int seed){
   int n=Math.Min(25,5+(level-1));var board=new MazeBoard{Size=n,Walls=Enumerable.Repeat(15,n*n).ToArray()};var random=new Random(seed);var used=new bool[n*n];var stack=new Stack<int>();stack.Push(0);used[0]=true;
   while(stack.Count>0){int at=stack.Peek();var options=Enumerable.Range(0,4).Where(d=>{int next=MiniGrid.Next(at,d,n,n);return next>=0&&!used[next];}).ToList();if(options.Count==0){stack.Pop();continue;}int direction=options[random.Next(options.Count)],nextCell=MiniGrid.Next(at,direction,n,n);board.Walls[at]&=~(1<<direction);board.Walls[nextCell]&=~(1<<MiniGrid.Opposite(direction));used[nextCell]=true;stack.Push(nextCell);}
   var distance=Enumerable.Repeat(-1,n*n).ToArray();var queue=new Queue<int>();distance[0]=0;queue.Enqueue(0);while(queue.Count>0){int at=queue.Dequeue();for(int d=0;d<4;d++)if((board.Walls[at]&(1<<d))==0){int next=MiniGrid.Next(at,d,n,n);if(next>=0&&distance[next]<0){distance[next]=distance[at]+1;queue.Enqueue(next);}}}board.Goal=Enumerable.Range(1,n*n-1).OrderByDescending(i=>distance[i]).First();return board;
  }
 }
 internal static class MiniEnumerable {
  // .NET Framework does not have the newer FirstOrDefault(defaultValue) overload.
  public static int FirstOrDefault(this IEnumerable<int> source,Func<int,bool> predicate,int fallback){foreach(int value in source)if(predicate(value))return value;return fallback;}
 }
}
