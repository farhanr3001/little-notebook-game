using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LittleNotebook {
 public partial class MainWindow {
  List<Question> StudyQuestions(){
   if(studyTopic=="mistakes"){
    return UnresolvedMistakes().Select(RestoreAttemptQuestion).ToList();
   }
   if(studyTopic=="math")return new List<Question>{
    Lesson("Addition","الجمع","7 + 5 = ?","12","Make ten: 7 + 3 = 10. There are 2 left, so 10 + 2 = 12.","أكمل العشرة: 7 + 3 = 10. يتبقى 2، إذن 10 + 2 = 12."),
    Lesson("Subtraction","الطرح","15 − 8 = ?","7","Count up from 8 to 15: 2 to reach 10, then 5 more. 2 + 5 = 7.","عدّ من 8 إلى 15: أضف 2 للوصول إلى 10، ثم 5. الفرق هو 7."),
    Lesson("Multiplication","الضرب","4 × 3 = ?","12","Four groups of three: 3 + 3 + 3 + 3 = 12.","أربع مجموعات من ثلاثة: 3 + 3 + 3 + 3 = 12."),
    Lesson("Division","القسمة","20 ÷ 4 = ?","5","Share 20 equally between 4 groups. Each group gets 5. Check: 5 × 4 = 20.","وزّع 20 بالتساوي على 4 مجموعات. لكل مجموعة 5. للتحقق: 5 × 4 = 20."),
    Lesson("Order of operations","ترتيب العمليات","3 + 4 × 2 = ?","11","Multiply first: 4 × 2 = 8. Then add 3: 3 + 8 = 11.","الضرب أولاً: 4 × 2 = 8. ثم الجمع: 3 + 8 = 11."),
    Lesson("Negative numbers","الأعداد السالبة","5 − 9 = ?","−4","Move 9 steps left from 5 on a number line. You land on −4.","تحرّك 9 خطوات إلى اليسار من 5 على خط الأعداد. تصل إلى −4.")};
   return bank.Questions.Where(q=>q.Topic==studyTopic&&(Profile.Settings.Difficulty==3||q.Difficulty==Profile.Settings.Difficulty)).ToList();
  }
  Question Lesson(string en,string ar,string stem,string answer,string ex,string exAr){return new Question{Id="lesson:"+en,Topic="math",Kind="choice",PromptEn=en,PromptAr=ar,Stem=stem,Answer=answer,ExplainEn=ex,ExplainAr=exAr};}
  void Study(){
   Header(T("STUDY","الدراسة"),T("Study mode","وضع الدراسة"),T("Review questions and explanations without a timer or lives.","راجع الأسئلة والشروح دون مؤقت أو محاولات محدودة."));
   string[] topics={"english","math","puzzles","general","mistakes"};
   for(int i=0;i<5;i++){string t=topics[i];Btn(body,t=="mistakes"?T("My mistakes","أخطائي")+" ("+UnresolvedMistakes().Count+")":Topic(t),9+i*190,135,()=>{studyTopic=t;studyIndex=0;studyRevealed=false;Build();},178,47,studyTopic==t);}
   var list=StudyQuestions();
   if(list.Count==0){Label(body,T("No mistakes to review.","صفحة جديدة!\nلا توجد أخطاء للمراجعة."),135,321,29,725).TextAlignment=TextAlignment.Center;return;}
   studyIndex=Math.Max(0,Math.Min(studyIndex,list.Count-1));var q=list[studyIndex];
   Btn(body,studyTopic=="mistakes"?T("View all mistakes","عرض كل الأخطاء"):T("View all questions","عرض كل الأسئلة"),10,199,StudyQuestionList,212,36);
   Label(body,T("Card ","بطاقة ")+(studyIndex+1)+" / "+list.Count+"  ·  "+Difficulty(Profile.Settings.Difficulty),246,207,16,405);
   var card=new Canvas();Label(card,q.Prompt(Ar),13,20,27,837).TextAlignment=TextAlignment.Center;
   DrawStudySetup(card,q);
   if(studyTopic=="mistakes")Btn(body,T("Resolve mistake","إزالة الخطأ"),724,199,()=>ResolveStudyMistake(q.Id),224,36);
   Put(body,Box(card,942,405,p.Paper,-.35),8,247);
   Btn(body,"‹",10,692,()=>{studyIndex=(studyIndex-1+list.Count)%list.Count;studyRevealed=false;Build();},66,53);
   Btn(body,studyRevealed?T("Hide answer","إخفاء الإجابة"):T("Show answer","اقلب الصفحة · أظهر الإجابة"),197,692,()=>{if(!studyRevealed){Profile.StudyCards++;Save();}studyRevealed=!studyRevealed;Build();},534,53,true);
   Btn(body,"›",884,692,()=>{studyIndex=(studyIndex+1)%list.Count;studyRevealed=false;Build();},66,53);
  }
  void Progress(){
   Header(T("PROFILE","الملف الشخصي"),T("Statistics","الإحصائيات"),T("Your performance and play time, saved on this computer.","أداؤك ووقت لعبك، محفوظان على هذا الجهاز."));
   var attempts=Profile.Attempts;double accuracy=attempts.Count==0?0:100.0*attempts.Count(a=>a.Correct)/attempts.Count;
   string[] values={attempts.Count.ToString(),Math.Round(accuracy)+"%",Math.Round(Profile.PlaySeconds/60,1)+" min",Profile.Sessions.Count(s=>s.Completed).ToString(),Profile.BestStreak.ToString(),attempts.Count==0?"—":Math.Round(attempts.Average(a=>a.Seconds),1)+" s",attempts.Count(a=>a.Hint).ToString(),Profile.StudyCards.ToString()};
   string[] captions={T("questions tried","أسئلة حاولتها"),T("accuracy","دقة الإجابات"),T("active play time","وقت اللعب الفعلي"),T("rounds finished","جولات منتهية"),T("best answer streak","أفضل سلسلة صحيحة"),T("average answer time","متوسط وقت الإجابة"),T("hints used","تلميحات مستخدمة"),T("study reveals","إجابات راجعتها")};
   for(int i=0;i<8;i++){var c=new Canvas();Label(c,values[i],1,1,30,180,true).TextAlignment=TextAlignment.Center;Label(c,captions[i],1,58,14,180).TextAlignment=TextAlignment.Center;Put(body,Box(c,221,125,i%2==0?p.Light:p.Paper),8+(i%4)*240,137+(i/4)*142);}
   Label(body,T("A closer look at your topics","نظرة أقرب إلى مواضيعك"),13,437,24,820);
   string[] topics={"english","math","puzzles","general"};for(int i=0;i<4;i++){
    var rows=attempts.Where(a=>a.Topic==topics[i]).ToList();double pct=rows.Count==0?0:rows.Count(a=>a.Correct)*100.0/rows.Count;
    Label(body,Topic(topics[i]),15,487+i*46,17,300);Put(body,new Rectangle{Width=360,Height=13,Fill=B(p.Line),RadiusX=6,RadiusY=6},320,500+i*46);Put(body,new Rectangle{Width=360*pct/100,Height=13,Fill=B(p.Accent),RadiusX=6,RadiusY=6},320,500+i*46);Label(body,Math.Round(pct)+"%  ·  "+rows.Count+T(" tried"," محاولة"),710,487+i*46,16,226);
   }
   AddReviewMistakes(10,696,275,49);
   Btn(body,T("By question type","حسب نوع السؤال"),310,696,DetailStats,290,49);
   Btn(body,T("Change character","تغيير الشخصية"),625,696,EditCharacter,324,49);
  }
  void DetailStats(){Modal(T("Statistics by question type","الإحصائيات حسب نوع السؤال"),c=>{
   int i=0;foreach(var group in Profile.Attempts.GroupBy(a=>a.Kind).OrderBy(g=>g.Key)){
    Label(c,KindName(group.Key),92,109+i*46,18,270);Label(c,group.Count(a=>a.Correct)+" / "+group.Count()+"  ·  "+Math.Round(group.Average(a=>a.Seconds),1)+" s",411,109+i*46,18,276,true);i++;
   }
   if(i==0)Label(c,T("Complete a round to see your statistics.","ابدأ جولتك الأولى لكتابة حكايتك."),95,140,22,580);
   Label(c,T("Accuracy includes timed-out answers. Pauses are excluded from play time.","تشمل الدقة الإجابات التي انتهى وقتها. لا تُحسب الاستراحات ضمن وقت اللعب."),94,489,15,595);
  },580);}
  string KindName(string s){switch(s){case "choice":return T("Choose an answer","اختيار إجابة");case "spell":return T("Write a word","كتابة كلمة");case "order":return T("Build a sentence","ترتيب جملة");case "listen":return T("Listening","استماع");case "image":return T("Picture vocabulary","مفردات مصوّرة");case "search":return T("Word search","بحث عن كلمات");case "memory":return T("Memory pairs","أزواج الذاكرة");case "pairs":return T("Word pairs","أزواج الكلمات");default:return s;}}
 }
}
