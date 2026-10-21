using System;
using System.Linq;
using System.Collections.Generic;
namespace LittleNotebook {
 public class WordSearchBoard {public char[] Letters;public string[] Words;public int[][] Paths;}
 public class WordSearchFactory {
  readonly Random random;readonly int[] regions=new int[4],directions=new int[8];HashSet<int> previous=new HashSet<int>();int boards;
  public WordSearchFactory(Random random){this.random=random;}
  public WordSearchBoard Create(string first,IEnumerable<string> extra,int level){
   var words=new List<string>{first.ToUpperInvariant()};if(level>0&&boards%2==0)words.AddRange(extra.Select(s=>s.ToUpperInvariant()).Where(s=>s!=words[0]&&s.Length<=8).Distinct().OrderBy(s=>random.Next()).Take(level==2?2:1));
   var letters=new char[64];var paths=new List<int[]>();int[,] dirs={{0,1},{1,0},{1,1},{-1,1},{0,-1},{-1,0},{-1,-1},{1,-1}};
   foreach(string word in words){var candidates=new List<Tuple<int[],int,double>>();for(int d=0;d<(level==0?2:level==1?4:8);d++)for(int row=0;row<8;row++)for(int col=0;col<8;col++){
    int endRow=row+dirs[d,0]*(word.Length-1),endCol=col+dirs[d,1]*(word.Length-1);if(endRow<0||endRow>7||endCol<0||endCol>7)continue;var cells=Enumerable.Range(0,word.Length).Select(i=>(row+dirs[d,0]*i)*8+col+dirs[d,1]*i).ToArray();if(cells.Where((cell,i)=>letters[cell]!='\0'&&letters[cell]!=word[i]).Any()||paths.Any(p=>p.SequenceEqual(cells)))continue;
    int region=(row>=4?2:0)+(col>=4?1:0);double weight=1.0/(1+regions[region]*.5+directions[d]*.3+cells.Count(previous.Contains)*2);if(level>0&&paths.Count==0&&boards%2==0&&dirs[d,0]!=0&&dirs[d,1]!=0)weight*=30;candidates.Add(Tuple.Create(cells,d,weight));
   }
   if(candidates.Count==0)break;double pick=random.NextDouble()*candidates.Sum(c=>c.Item3);var chosen=candidates.Last();foreach(var item in candidates){pick-=item.Item3;if(pick<=0){chosen=item;break;}}for(int i=0;i<word.Length;i++)letters[chosen.Item1[i]]=word[i];paths.Add(chosen.Item1);int start=chosen.Item1[0];regions[(start/8>=4?2:0)+(start%8>=4?1:0)]++;directions[chosen.Item2]++;
   }
   for(int i=0;i<64;i++)if(letters[i]=='\0')letters[i]=(char)('A'+random.Next(26));previous=new HashSet<int>(paths.SelectMany(p=>p));boards++;return new WordSearchBoard{Letters=letters,Words=words.Take(paths.Count).ToArray(),Paths=paths.ToArray()};
  }
 }
}
