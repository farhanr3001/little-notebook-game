using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace LittleNotebook {
 public class QuestionStream {
  class Slot {public string Topic,Category;public int Level;public int[] Levels;}
  readonly QuestionBank bank;readonly string[] topics;readonly int difficulty,length;readonly Dictionary<string,string> filters;readonly List<Attempt> history;
  readonly HashSet<string> used=new HashSet<string>();readonly Queue<Slot> plan=new Queue<Slot>();string previous="";
  public QuestionStream(QuestionBank bank,string topic,int difficulty,IEnumerable<string> recent,string category="all"):this(bank,topic=="mixed"?new[]{"english","math","puzzles","general"}:topic.Split(','),difficulty,recent.Select(id=>new Attempt{Id=id,Correct=true}).ToList(),new Dictionary<string,string>{{topic,category}},60){}
  public QuestionStream(QuestionBank bank,IEnumerable<string> topics,int difficulty,IEnumerable<Attempt> history,Dictionary<string,string> filters,int length){this.bank=bank;this.topics=topics.Distinct().ToArray();this.difficulty=difficulty;this.history=history.ToList();this.filters=filters;this.length=length==0?60:Math.Max(1,length);}
  List<T> Balanced<T>(IEnumerable<T> source,int count){var keys=source.ToArray();var result=new List<T>();while(result.Count<count)result.AddRange(keys.OrderBy(x=>bank.Random.Next()).Take(count-result.Count));return result;}
  void Plan(){
   var slots=new List<Slot>();foreach(var group in Balanced(topics,length).GroupBy(x=>x)){
    string filter;filters.TryGetValue(group.Key,out filter);var available=QuestionCatalog.Categories(bank,group.Key,difficulty).Where(x=>x!="all").ToArray();var categories=string.IsNullOrEmpty(filter)||filter=="all"?available:available.Where(x=>filter.Split(',').Contains(x)).ToArray();if(categories.Length==0)categories=available;
    foreach(string category in Balanced(categories,group.Count())){var levels=Enumerable.Range(0,3).Where(l=>QuestionCatalog.Categories(bank,group.Key,l).Contains(category)).ToArray();slots.Add(new Slot{Topic=group.Key,Category=category,Levels=levels,Level=difficulty==3?0:difficulty});}
   }
   if(difficulty==3){
    var tokens=Balanced(new[]{0,1,2},slots.Count).ToArray();var owners=Enumerable.Repeat(-1,tokens.Length).ToArray();
    // Bipartite matching preserves exact difficulty quotas whenever the chosen filters permit them.
    Func<int,HashSet<int>,bool> assign=null;assign=(index,visited)=>{foreach(int token in Enumerable.Range(0,tokens.Length).Where(t=>slots[index].Levels.Contains(tokens[t])).OrderBy(t=>bank.Random.Next())){if(!visited.Add(token))continue;if(owners[token]<0||assign(owners[token],visited)){owners[token]=index;slots[index].Level=tokens[token];return true;}}return false;};
    foreach(int index in Enumerable.Range(0,slots.Count).OrderBy(i=>slots[i].Levels.Length))if(!assign(index,new HashSet<int>()))slots[index].Level=slots[index].Levels.OrderBy(l=>slots.Count(s=>s.Level==l)).First();
   }
   foreach(var slot in slots.OrderBy(x=>bank.Random.Next()))plan.Enqueue(slot);
  }
  public static double HistoryWeight(IEnumerable<Attempt> attempts,string id){var records=attempts.Where(a=>a.Id==id).ToList();if(records.Count==0)return 1;int correct=records.Count(a=>a.Correct);double weight=1.0/(1+correct*.7);var last=records.Last();if(!last.Correct&&!last.Resolved)weight*=3.5;else if(last.Correct)weight*=.55;return Math.Max(.08,weight);}
  public Question Next(){if(plan.Count==0)Plan();var slot=plan.Dequeue();List<Question> candidates;
   if(slot.Topic=="math")candidates=Enumerable.Range(0,24).Select(i=>bank.MathQuestion(slot.Level,slot.Category)).ToList();else candidates=bank.Questions.Where(q=>q.Topic==slot.Topic&&q.Difficulty==slot.Level&&QuestionCatalog.Category(q)==slot.Category).ToList();
   if(candidates.Count==0)throw new InvalidDataException("No questions for selected filter");var fresh=candidates.Where(q=>!used.Contains(q.Id)).ToList();if(fresh.Count==0){foreach(var q in candidates)used.Remove(q.Id);fresh=candidates;}var pool=fresh.Where(q=>q.Id!=previous).ToList();if(pool.Count==0)pool=fresh;
   double total=pool.Sum(q=>HistoryWeight(history,q.Id)),pick=bank.Random.NextDouble()*total;Question chosen=pool.Last();foreach(var q in pool){pick-=HistoryWeight(history,q.Id);if(pick<=0){chosen=q;break;}}used.Add(chosen.Id);previous=chosen.Id;
   return new Question{Id=chosen.Id,Topic=chosen.Topic,Kind=chosen.Kind,Category=chosen.Category,PromptEn=chosen.PromptEn,PromptAr=chosen.PromptAr,Stem=chosen.Stem,Answer=chosen.Answer,HintEn=chosen.HintEn,HintAr=chosen.HintAr,ExplainEn=chosen.ExplainEn,ExplainAr=chosen.ExplainAr,Image=chosen.Image,Speech=chosen.Speech,Translation=chosen.Translation,PairArabic=chosen.PairArabic==null?null:chosen.PairArabic.ToArray(),PairMeanings=chosen.PairMeanings==null?null:chosen.PairMeanings.ToArray(),Pairs=chosen.Pairs==null?null:chosen.Pairs.ToArray(),Difficulty=chosen.Difficulty,Options=chosen.Options==null?null:chosen.Options.OrderBy(x=>bank.Random.Next()).ToArray()};
  }
 }
}
