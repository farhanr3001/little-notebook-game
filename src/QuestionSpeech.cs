using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LittleNotebook {
 public class SpeechSegment{public string Text,Language;public SpeechSegment(string text,string language){Text=text;Language=language;}}
 public static class QuestionSpeech {
  public static List<SpeechSegment> Make(Question question,bool arabic){
   var result=new List<SpeechSegment>();string language=arabic?"ar":"en";result.Add(new SpeechSegment(question.Prompt(arabic),language));
   // Listening answers and word-pair solutions must never be read by question narration.
   if(question.Kind!="listen"&&!string.IsNullOrWhiteSpace(question.Stem)){
    string stem=question.Stem;string stemLanguage=question.Topic=="math"?language:Regex.IsMatch(stem,"[\\u0600-\\u06FF]")?"ar":"en";
    if(question.Topic=="math")stem=stem.Replace(" = ?",arabic?" يساوي كم؟":" equals what?").Replace(" + ",arabic?" زائد ":" plus ").Replace(" − ",arabic?" ناقص ":" minus ").Replace(" × ",arabic?" ضرب ":" times ").Replace(" ÷ ",arabic?" مقسوماً على ":" divided by ");
    stem=stem.Replace("___",stemLanguage=="ar"?" فراغ ":" blank ");if(question.Kind=="search")stem=stem.ToLowerInvariant();result.Add(new SpeechSegment(stem,stemLanguage));
   }
   return result.Where(x=>!string.IsNullOrWhiteSpace(x.Text)).ToList();
  }
 }
}
