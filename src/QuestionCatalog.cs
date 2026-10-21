using System;
using System.Collections.Generic;
using System.Linq;

namespace LittleNotebook {
 public static class QuestionCatalog {
  public static string Category(Question q){if(!string.IsNullOrEmpty(q.Category))return q.Category;if(q.Kind=="image")return "vocabulary";if(q.Kind=="spell")return "spelling";if(q.Kind=="order")return "sentences";if(q.Kind=="listen")return "listening";if(q.Kind=="pairs")return "word-pairs";if(q.Topic=="english")return q.PromptEn.StartsWith("Which spelling")?"spelling":"sentences";if(q.Kind=="search")return "word-search";if(q.Kind=="memory")return "memory";return q.Topic=="puzzles"?"patterns":"everyday";}
  public static List<string> Categories(QuestionBank bank,string topic,int level){
   if(topic=="mixed")return new List<string>{"all"};
   var keys=topic=="math"?new List<string>{"addition","subtraction","multiplication","division","word-problems"}:bank.Questions.Where(q=>q.Topic==topic&&(level==3||q.Difficulty==level)).Select(Category).Distinct().ToList();
   if(topic=="math"&&level>=2)keys.Add("mixed-operations");keys.Insert(0,"all");return keys;
  }
 }
}
