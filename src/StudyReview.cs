using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Web.Script.Serialization;

namespace LittleNotebook {
 public class QuestionLayout {
  public string[] Tokens,MemoryIcons,SearchWords,FoundWords;public int[][] SearchPaths;public string Search;public int[] Memory,Right,Ordered,Matched,Open;public int[] PairsMatched;
 }
 public partial class MainWindow {
  TextBox activeAnswerInput;DispatcherOperation answerFocus;
  void FocusAnswerInput(){if(answerFocus!=null&&answerFocus.Status==DispatcherOperationStatus.Pending)answerFocus.Abort();answerFocus=Dispatcher.BeginInvoke(new Action(()=>{if(page=="game"&&game!=null&&!game.Answered&&!paused&&IsActive&&activeAnswerInput!=null&&activeAnswerInput.IsVisible&&!activeAnswerInput.IsReadOnly){activeAnswerInput.Focus();activeAnswerInput.CaretIndex=activeAnswerInput.Text.Length;}}),DispatcherPriority.ContextIdle);}
  void CaptureAttemptSetup(Attempt attempt){
   attempt.QuestionJson=new JavaScriptSerializer().Serialize(game.Current);attempt.LayoutJson=new JavaScriptSerializer().Serialize(new QuestionLayout{SearchWords=searchBoard==null?null:searchBoard.Words,SearchPaths=searchBoard==null?null:searchBoard.Paths,FoundWords=searchFound.ToArray(),Tokens=orderTokens.ToArray(),Search=searchLetters==null?"":new string(searchLetters),Memory=memoryCards,MemoryIcons=memoryIcons,Right=wordPairRight,Ordered=ordered.ToArray(),Matched=memoryMatched.ToArray(),Open=memoryOpen.ToArray(),PairsMatched=wordPairsMatched.ToArray()});
  }
  List<Attempt> UnresolvedMistakes(){return Profile.Attempts.GroupBy(a=>a.Id).Select(g=>g.Last()).Where(a=>!a.Correct&&!a.Resolved).Reverse().ToList();}
  Question RestoreAttemptQuestion(Attempt attempt){if(!string.IsNullOrEmpty(attempt.QuestionJson))try{return new JavaScriptSerializer().Deserialize<Question>(attempt.QuestionJson);}catch{}var q=bank.Questions.FirstOrDefault(x=>x.Id==attempt.Id);return q??new Question{Id=attempt.Id,Topic=attempt.Topic,Kind=attempt.Kind??"choice",Stem=attempt.Prompt,PromptEn="Review this question",PromptAr="راجع هذا السؤال",Answer=attempt.Answer,ExplainEn=attempt.Explanation,ExplainAr=attempt.ExplanationAr??attempt.Explanation};}
  QuestionLayout StudyLayout(Question question){var attempt=studyTopic=="mistakes"?UnresolvedMistakes().FirstOrDefault(x=>x.Id==question.Id):null;if(attempt!=null&&!string.IsNullOrEmpty(attempt.LayoutJson))try{return new JavaScriptSerializer().Deserialize<QuestionLayout>(attempt.LayoutJson);}catch{}return new QuestionLayout();}
  void ResolveStudyMistake(string id){var attempt=UnresolvedMistakes().FirstOrDefault(x=>x.Id==id);if(attempt==null)return;attempt.Resolved=true;Save();studyRevealed=false;Build();}
  string StudyCaption(Question q){string detail=q.Stem;if(q.Kind=="order")detail=string.Join(" · ",q.Options??q.Answer.Split(' '));if(q.Kind=="pairs")detail=string.Join(" · ",(q.Pairs??new string[0]).Select(x=>x.Split('|')[0]));if(q.Kind=="listen")detail=T("Listening practice","تدريب الاستماع");if(q.Kind=="memory")detail=T("Memory cards","بطاقات الذاكرة");return q.Prompt(Ar)+(string.IsNullOrWhiteSpace(detail)?"":" — "+detail);}
  void StudyQuestionList(){var questions=StudyQuestions();Modal(studyTopic=="mistakes"?T("All mistakes","كل الأخطاء"):T("All study questions","كل أسئلة الدراسة"),c=>{
   var search=new TextBox{Width=595,Height=42,FontFamily=new FontFamily(Ar?"Segoe UI":"Segoe Print"),FontSize=18,Padding=new Thickness(10,3,10,3),Background=B(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink)};search.ToolTip=T("Search questions","ابحث عن سؤال");Put(c,search,96,104);
   var list=new StackPanel{Width=589};var scroll=new ScrollViewer{Width=615,Height=423,Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(c,scroll,93,164);
   Action refresh=()=>{list.Children.Clear();for(int i=0;i<questions.Count;i++){int index=i;var q=questions[i];string caption=StudyCaption(q);if(search.Text.Length>0&&(caption+" "+q.Answer+" "+q.Id).IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)<0)continue;var button=Button("",()=>{studyIndex=index;studyRevealed=false;CloseModal();Build();},580,76,index==studyIndex);var text=Text((index+1)+". "+caption,16,545);text.MaxHeight=64;button.Content=text;button.Margin=new Thickness(0,0,0,9);list.Children.Add(button);}if(list.Children.Count==0)list.Children.Add(Text(T("No matching questions.","لا توجد أسئلة مطابقة."),18,550));};search.TextChanged+=(s,e)=>refresh();refresh();Btn(c,T("Done","تم"),253,610,CloseModal,290,45,true);
  },692);}
  void DrawStudySetup(Canvas card,Question q){
   var layout=StudyLayout(q);var content=new StackPanel{Width=793};
   if(q.Kind=="image"){content.Children.Add(new Sketch(q.Image,p,103,103){HorizontalAlignment=HorizontalAlignment.Center});}
   else if(q.Kind=="listen"){content.Children.Add(Button(T("♫ Listen","♫ استمع"),()=>audio.Speak(q.Speech),300,47,true));}
   else if(q.Kind=="order"){
    var words=new WrapPanel{Width=780,HorizontalAlignment=HorizontalAlignment.Center};foreach(string token in layout.Tokens??q.Options??q.Answer.Split(' ')){var label=Text(token,20,double.NaN,true);var frame=new Border{Child=label,Padding=new Thickness(12,5,12,7),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Margin=new Thickness(3),Background=B(p.Light)};words.Children.Add(frame);}content.Children.Add(words);
   }else if(q.Kind=="pairs"){
    var pairs=q.Pairs??new string[0];var right=layout.Right??Enumerable.Range(0,pairs.Length).Reverse().ToArray();for(int i=0;i<pairs.Length;i++){var row=new Grid{Width=730,Margin=new Thickness(0,3,0,3)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition());var left=Text(pairs[i].Split('|')[0],19,320,true);var other=Text(q.Category=="dictionary"&&Ar?q.PairArabic[right[i]]:pairs[right[i]].Split('|')[1],19,320);Grid.SetColumn(other,1);row.Children.Add(left);row.Children.Add(other);content.Children.Add(row);}
   }else if(q.Kind=="search"){
    content.Children.Add(Text(T("Find: ","ابحث عن: ")+string.Join(" · ",layout.SearchWords??new[]{q.Answer}),20,780,true));string grid=layout.Search;if(string.IsNullOrEmpty(grid)){var chars=Enumerable.Repeat('·',64).ToArray();for(int i=0;i<q.Answer.Length&&i<8;i++)chars[8+i]=char.ToUpperInvariant(q.Answer[i]);grid=new string(chars);}var text=Text(string.Join("\n",Enumerable.Range(0,8).Select(row=>string.Join("  ",grid.Skip(row*8).Take(8)))),16,780,true);text.TextAlignment=TextAlignment.Center;content.Children.Add(text);
   }else if(q.Kind=="memory"){
    var values=layout.Memory??Enumerable.Range(0,4).Concat(Enumerable.Range(0,4)).ToArray();var icons=layout.MemoryIcons??new[]{"book","chair","water","cup","leaf","sun","star","moon"};var wrap=new WrapPanel{Width=720};for(int i=0;i<values.Length;i++){var art=new Sketch(icons[values[i]%icons.Length],p,52,52);wrap.Children.Add(new Border{Child=art,Width=77,Height=65,Margin=new Thickness(4),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Background=B(p.Light)});}content.Children.Add(wrap);
   }else{
    if(!string.IsNullOrEmpty(q.Stem)){var stem=Text(q.Stem,q.Topic=="math"?29:24,775,true);stem.TextAlignment=TextAlignment.Center;content.Children.Add(stem);}if(q.Options!=null){var options=new WrapPanel{Width=775};foreach(string option in q.Options){var text=Text(option,18,340,q.Topic=="english");var frame=new Border{Child=text,Padding=new Thickness(8),Margin=new Thickness(4),BorderBrush=B(p.Line),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(6)};options.Children.Add(frame);}content.Children.Add(options);}
   }
   if(studyTopic=="mistakes"){var attempt=UnresolvedMistakes().FirstOrDefault(x=>x.Id==q.Id);if(attempt!=null){var given=Text(T("Your answer: ","إجابتك: ")+(attempt.TimedOut?T("Time ran out","انتهى الوقت"):attempt.Given),15,780);given.Margin=new Thickness(0,12,0,0);content.Children.Add(given);}}
   if(studyRevealed){var answer=Text(q.Kind=="pairs"?string.Join("\n",(q.Pairs??new string[0]).Select(x=>x.Replace("|"," = "))):q.Answer=="matched"?T("Find all matching pairs.","اعثر على كل الأزواج المتطابقة."):q.Answer,24,775,q.Topic=="english");answer.Margin=new Thickness(0,16,0,5);answer.TextAlignment=TextAlignment.Center;content.Children.Add(answer);var explanation=Text(q.Explain(Ar),18,775);explanation.TextAlignment=TextAlignment.Center;content.Children.Add(explanation);}
   var scroll=new ScrollViewer{Width=820,Height=275,Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(card,scroll,29,77);
  }
 }
}
