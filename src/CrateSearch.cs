using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LittleNotebook {
 public sealed partial class ExtraPuzzle {
  CancellationTokenSource crateSearchCancellation;Task<bool?> crateSearch;
  public bool Deadlocked;double restartWait;
  public void CancelCrateSearch(){if(crateSearchCancellation!=null){crateSearchCancellation.Cancel();crateSearchCancellation.Dispose();crateSearchCancellation=null;}crateSearch=null;}
  void BeginCrateSearch(){CancelCrateSearch();crateSearchCancellation=new CancellationTokenSource();var token=crateSearchCancellation.Token;int[] cells=(int[])Cells.Clone(),goals=(int[])Goal.Clone();int player=Player,w=W,h=H;crateSearch=Task.Run(()=>CrateCanWin(cells,goals,player,w,h,token));}
  public bool TickCrateSearch(double dt){
   if(Kind!="crates")return false;
   if(!Deadlocked&&crateSearch!=null&&crateSearch.IsCompleted){if(crateSearch.Status==TaskStatus.RanToCompletion&&crateSearch.Result==false){Deadlocked=true;restartWait=1.65;Message="deadlock";}CancelCrateSearch();}
   if(!Deadlocked)return false;restartWait-=dt;return restartWait<=0;
  }
  // Search push states, collapsing every walkable player position into one region.
  // A resource limit returns unknown, never a false deadlock or an unfair restart.
  internal static bool? CrateCanWin(int[] cells,int[] goals,int player,int w,int h,CancellationToken token){
   var targets=Enumerable.Range(0,cells.Length).Where(i=>goals[i]==1).ToArray();var reach=new List<HashSet<int>>();
   foreach(int goal in targets){var found=new HashSet<int>{goal};var q=new Queue<int>();q.Enqueue(goal);while(q.Count>0){int at=q.Dequeue();for(int d=0;d<4;d++){int box=MiniGrid.Next(at,d,w,h),support=box<0?-1:MiniGrid.Next(box,d,w,h);if(support>=0&&cells[box]!=-1&&cells[support]!=-1&&found.Add(box))q.Enqueue(box);}}reach.Add(found);}
   Func<int[],bool> assign=boxes=>{var used=new bool[targets.Length];Func<int,bool> match=null;match=n=>{if(n==boxes.Length)return true;for(int g=0;g<targets.Length;g++)if(!used[g]&&reach[g].Contains(boxes[n])){used[g]=true;if(match(n+1))return true;used[g]=false;}return false;};return match(0);};
   int[] initial=Enumerable.Range(0,cells.Length).Where(i=>cells[i]==1).ToArray();if(!assign(initial))return false;
   var queue=new Queue<Tuple<int[],int>>();queue.Enqueue(Tuple.Create(initial,player));var seen=new HashSet<string>();int work=0;
   while(queue.Count>0){if(token.IsCancellationRequested)return null;if(++work>180000)return null;var state=queue.Dequeue();var boxes=new HashSet<int>(state.Item1);if(boxes.All(i=>goals[i]==1))return true;
    var floor=new HashSet<int>{state.Item2};var walking=new Queue<int>();walking.Enqueue(state.Item2);int canonical=state.Item2;
    while(walking.Count>0){int at=walking.Dequeue();canonical=Math.Min(canonical,at);for(int d=0;d<4;d++){int next=MiniGrid.Next(at,d,w,h);if(next>=0&&cells[next]!=-1&&!boxes.Contains(next)&&floor.Add(next))walking.Enqueue(next);}}
    string key=string.Join(",",state.Item1)+":"+canonical;if(!seen.Add(key))continue;
    foreach(int box in state.Item1)for(int d=0;d<4;d++){int next=MiniGrid.Next(box,d,w,h),support=MiniGrid.Next(box,MiniGrid.Opposite(d),w,h);if(next<0||!floor.Contains(support)||cells[next]==-1||boxes.Contains(next))continue;var moved=state.Item1.Select(i=>i==box?next:i).OrderBy(i=>i).ToArray();if(assign(moved))queue.Enqueue(Tuple.Create(moved,box));}
   }return false;
  }
 }
}
