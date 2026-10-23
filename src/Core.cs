using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace LittleNotebook {
 public class Settings {
  public string PlaySection="learning",SelectedMiniGame="arrows";
  public string Language = "en", Theme = "matcha", Topics="mixed";
  public bool Timer = true, Sound = true, Music = true, ReducedMotion = false;
  public double Volume = 0.3;
  public double TextScale = 0.9, ButtonScale = 0.92;
  public bool SidebarHidden = false, SingleLife = false;
  public bool AutoReadQuestions=false;
  public bool BorderlessFullscreen=false;
  public bool? HardSingleLife=null;
  public bool OneLife(){return Difficulty==2?(HardSingleLife??true):SingleLife;}
  public List<string> DisabledMusic=new List<string>(),RecentColors=new List<string>();
  public PaperDesign Paper=new PaperDesign();
  public List<string> CustomMusic = new List<string>();
  public Dictionary<string,string> MusicTitles=new Dictionary<string,string>();
  public Dictionary<string,string> QuestionCategories=new Dictionary<string,string>();
  public int Track = 0, Difficulty = 0, RoundLength = 10;
 }
 public class Attempt {
  public string Id, Topic, Kind, Prompt, Answer, Given, Explanation, ExplanationAr, Date;
  public int Difficulty;public string Category;
  public bool Correct, Hint, TimedOut;
  public double Seconds;
  public string QuestionJson,LayoutJson;public bool Resolved;
 }
 public class SessionRecord {
  public string Date, Topic,Category;
  public int Correct, Total, Score, Difficulty;
  public double Seconds;
  public bool Completed, Endless;
 }
 public class Profile {
  public Dictionary<string,MiniProgress> MiniGames=new Dictionary<string,MiniProgress>();
  public int StackBest;
  public int Version = 1;
  public string Name = "", Sticker = "leaf", LastPlayed = "";
  public int Character = 0, Coins = 40, BestStreak = 0, StudyCards = 0;
  public double PlaySeconds = 0;
  public Settings Settings = new Settings();
  public List<string> Owned = new List<string> { "leaf" };
  public List<Attempt> Attempts = new List<Attempt>();
  public List<SessionRecord> Sessions = new List<SessionRecord>();
  public string Pet="",PetColor="ginger";
  public List<NotebookPage> Notebook = new List<NotebookPage>{new NotebookPage()};
  public int NotebookIndex=0;
 }
 public class SaveStore {
  public string Path {get;private set;}
  readonly string fallback;
  public string Warning = "";
  public SaveStore(string path,string fallbackPath=null) { Path = path;fallback=fallbackPath; }
  public Profile Load() {
   if(fallback!=null&&File.Exists(fallback))Path=fallback;
   if (!File.Exists(Path)) return new Profile();
   try { return Read(Path); } catch {
    Warning = "Your save could not be read. A backup was checked; the original file has been kept.";
    try { return Read(Path + ".bak"); } catch { return new Profile(); }
   }
  }
  Profile Read(string p) {
   var v = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Profile>(File.ReadAllText(p));
   if(v == null || v.Version != 1 || v.Settings == null || v.Attempts == null || v.Sessions == null || v.Owned == null) throw new InvalidDataException();
   v.Character = Math.Max(0,Math.Min(9,v.Character));
   v.Coins = Math.Max(0,v.Coins);
   v.Settings.Volume = Math.Max(0,Math.Min(1,v.Settings.Volume));
   v.Settings.TextScale = Math.Max(.85,Math.Min(1.15,v.Settings.TextScale));
   v.Settings.ButtonScale = Math.Max(.75,Math.Min(1,v.Settings.ButtonScale));
   v.Settings.Track = Math.Max(0,v.Settings.Track);
   if(v.Settings.CustomMusic==null)v.Settings.CustomMusic=new List<string>();
   if(v.Settings.MusicTitles==null)v.Settings.MusicTitles=new Dictionary<string,string>();
   if(v.Settings.QuestionCategories==null)v.Settings.QuestionCategories=new Dictionary<string,string>();
   if(v.Settings.DisabledMusic==null)v.Settings.DisabledMusic=new List<string>();if(v.Settings.RecentColors==null)v.Settings.RecentColors=new List<string>();if(v.Settings.Paper==null)v.Settings.Paper=new PaperDesign();
   ProfileFeatures.Normalize(v);
   v.Settings.Difficulty = Math.Max(0,Math.Min(3,v.Settings.Difficulty));
   if(v.Settings.Language != "ar") v.Settings.Language = "en";
   if(!new[]{"matcha","rose","sky","lavender","night"}.Contains(v.Settings.Theme)) v.Settings.Theme = "matcha";
   if(v.Settings.RoundLength != 0 && v.Settings.RoundLength != 5 && v.Settings.RoundLength != 10 && v.Settings.RoundLength != 15) v.Settings.RoundLength = 10;
   return v;
  }
  public bool Save(Profile v) {
   if(Write(v))return true;
   if(fallback!=null&&Path!=fallback){Path=fallback;return Write(v);}
   return false;
  }
  public bool Reset(Profile fresh){if(!Save(fresh))return false;return Save(fresh);}
  bool Write(Profile v){
   try {
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
    var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(v);
    File.WriteAllText(Path + ".tmp", json);
    if(File.Exists(Path)) File.Replace(Path + ".tmp",Path,Path + ".bak");
    else File.Move(Path + ".tmp",Path);
    Warning="";return true;
   } catch(Exception ex) { Warning = "Could not save progress: " + ex.Message; return false; }
  }
 }
 public class Question {
  public string Id, Topic, Kind, PromptEn, PromptAr, Stem, Answer, HintEn, HintAr, ExplainEn, ExplainAr, Image, Speech;
  public string[] Options,Pairs,PairArabic,PairMeanings;
  public string Translation;
  public string Category;
  public int Difficulty;
  public string Prompt(bool ar) { return ar ? PromptAr : PromptEn; }
  public string Hint(bool ar) { return ar ? HintAr : HintEn; }
  public string Explain(bool ar) { return ar ? ExplainAr : ExplainEn; }
 }
 public class QuestionBank {
  public List<Question> Questions;
  public Random Random;
  public QuestionBank(string path, int? seed = null) {
   Random = seed.HasValue ? new Random(seed.Value) : new Random();
   Questions = new JavaScriptSerializer().Deserialize<List<Question>>(File.ReadAllText(path));
   Questions.AddRange(LearnerDictionary.Questions());Validate();
  }
  public void Validate() {
   var ids = new HashSet<string>();
   foreach(var q in Questions) {
    if(!ids.Add(q.Id) || string.IsNullOrWhiteSpace(q.Answer) || string.IsNullOrWhiteSpace(q.PromptAr) || string.IsNullOrWhiteSpace(q.ExplainAr)) throw new InvalidDataException("Invalid question " + q.Id);
    if(q.Options != null && q.Kind != "order" && (q.Options.Distinct().Count() != q.Options.Length || !q.Options.Contains(q.Answer))) throw new InvalidDataException("Invalid choices " + q.Id);
    if(q.Kind=="pairs"&&(q.Pairs==null||q.Pairs.Length<4||q.Pairs.Any(x=>x.Split('|').Length!=2)||q.Pairs.Select(x=>x.Split('|')[0]).Distinct().Count()!=q.Pairs.Length||q.Pairs.Select(x=>x.Split('|')[1]).Distinct().Count()!=q.Pairs.Length))throw new InvalidDataException("Invalid word pairs "+q.Id);
   }
  }
  public List<Question> Round(string topic, int difficulty, int count, IEnumerable<string> recent) {
   var stream=new QuestionStream(this,topic,difficulty,recent);
   return Enumerable.Range(0,count).Select(i=>stream.Next()).ToList();
  }
  public Question MathQuestion(int level,string category="all") {
   int max = new[]{12,40,100}[level]; int a=Random.Next(1,max+1), b=Random.Next(1,max+1), answer=0;
   int op=Random.Next(level==2 ? 5 : 4); string stem="", hintEn="",hintAr="",exEn="",exAr="";
   if(category=="addition")op=0;if(category=="subtraction")op=1;if(category=="multiplication")op=2;if(category=="division")op=3;if(category=="mixed-operations")op=4;if(category=="word-problems")op=Random.Next(4);
   if(op==0) { answer=a+b; stem=a+" + "+b; hintEn="Add the tens first, then the ones."; hintAr="اجمع العشرات أولاً ثم الآحاد."; }
   if(op==1) { if((level<2||category=="word-problems") && a<b) { int t=a;a=b;b=t; } answer=a-b; stem=a+" − "+b;hintEn="Count back, or find the distance between the numbers.";hintAr="عدّ إلى الخلف أو احسب الفرق بين العددين."; }
   if(op==2) { a=Random.Next(2,new[]{6,13,21}[level]); b=Random.Next(2,new[]{6,13,16}[level]);answer=a*b;stem=a+" × "+b;hintEn="Think of equal groups. Multiplication is repeated addition.";hintAr="فكّر في مجموعات متساوية. الضرب هو جمع متكرر."; }
   if(op==3) { b=Random.Next(2,new[]{6,13,21}[level]);answer=Random.Next(2,new[]{6,13,21}[level]);a=b*answer;stem=a+" ÷ "+b;hintEn="Which number multiplied by the divisor gives the first number?";hintAr="ما العدد الذي إذا ضربته في المقسوم عليه أعطاك العدد الأول؟"; }
   if(op==4) { a=Random.Next(2,21);b=Random.Next(2,13);int c=Random.Next(2,10);answer=a+b*c;stem=a+" + "+b+" × "+c;hintEn="Multiply before adding.";hintAr="احسب الضرب قبل الجمع.";exEn="Do "+b+" × "+c+" first, then add "+a+".";exAr="احسب عملية الضرب أولاً ثم أضف العدد المتبقي."; }
   var opts = new HashSet<int>{answer};
   while(opts.Count<4) { int d=answer+Random.Next(-Math.Max(4,level*8),Math.Max(5,level*8+1));if(level>0 || d>=0)opts.Add(d); }
   string promptEn="Solve the equation",promptAr="حلّ المسألة";
   if(category=="word-problems"){
    if(op==0){promptEn="You have "+a+" books and get "+b+" more. How many books?";promptAr="لديك "+a+" كتب وحصلت على "+b+" أخرى. كم كتاباً لديك؟";}
    if(op==1){promptEn="You have "+a+" stickers and give away "+b+". How many remain?";promptAr="لديك "+a+" ملصقاً وأعطيت "+b+" منها. كم ملصقاً بقي؟";}
    if(op==2){promptEn=a+" boxes hold "+b+" pencils each. How many pencils altogether?";promptAr="في كل صندوق "+b+" أقلام، ولدينا "+a+" صناديق. كم قلماً في المجموع؟";}
    if(op==3){promptEn=a+" cards are shared equally between "+b+" people. How many each?";promptAr="نوزّع "+a+" بطاقة بالتساوي على "+b+" أشخاص. كم بطاقة لكل شخص؟";}
   }
   return new Question { Id="math:"+(category=="word-problems"?"word:":"")+stem,Category=category=="word-problems"?category:new[]{"addition","subtraction","multiplication","division","mixed-operations"}[op],Topic="math",Kind="choice",Difficulty=level,PromptEn=promptEn,PromptAr=promptAr,Stem=stem+" = ?",Answer=answer.ToString(),Options=opts.Select(x=>x.ToString()).OrderBy(x=>Random.Next()).ToArray(),HintEn=hintEn,HintAr=hintAr,ExplainEn=stem+" = "+answer+". "+exEn,ExplainAr=stem+" = "+answer+". "+exAr };
  }
  public static bool Matches(string given,string answer) { return Normalize(given)==Normalize(answer); }
  static string Normalize(string s) { return string.Join(" ",(s??"").Trim().ToLowerInvariant().TrimEnd('.','!','?').Split(new[]{' ','\t','\r','\n'},StringSplitOptions.RemoveEmptyEntries)); }
 }
 public class GameSession {
  readonly bool singleLife;readonly bool? lifeOverride;
  public GameSession(int difficulty=0,bool singleLife=false,bool endless=false,bool? lifeOverride=null){Difficulty=difficulty;this.singleLife=singleLife;this.lifeOverride=lifeOverride;Endless=endless;Lives=MaxLives;}
  public int MaxLives {get{return (lifeOverride??(Difficulty==2||singleLife))?1:3;}}
  public List<Question> Questions;
  public int Index=0, Lives=3, Hints=3, Score=0, Correct=0, Streak=0, Earned=0;
  public bool Answered=false, UsedHint=false, Recorded=false,Endless=false;
  public double Seconds=0, QuestionSeconds=0;
  public string Topic,Category="all";
  public int Difficulty;
  public Question Current { get { return Questions[Index]; } }
  public int Level {get{return Difficulty==3?Current.Difficulty:Difficulty;}}
  public int Limit { get { return (Current.Kind=="memory" || Current.Kind=="search" || Current.Kind=="order"||Current.Kind=="pairs") ? new[]{100,80,60}[Level] : new[]{55,40,30}[Level]; } }
  public bool IsOver { get { return Lives<=0 || (!Endless&&Index>=Questions.Count-1); } }
  public Attempt Answer(string given, bool timeout) {
   if(Answered) return null;
   Answered=true;
   bool correct=!timeout && QuestionBank.Matches(given,Current.Answer);
   if(correct) { Correct++; Streak++; Score+=(UsedHint?6:10)*(Level+1)+Math.Min(Streak-1,5)*2;Earned+=UsedHint?1:3; } else { Lives--; Streak=0; }
   return new Attempt {Id=Current.Id,Topic=Current.Topic,Kind=Current.Kind,Category=QuestionCatalog.Category(Current),Prompt=string.IsNullOrWhiteSpace(Current.Stem)?Current.PromptEn:Current.Stem,Answer=Current.Answer,Given=given,Explanation=Current.ExplainEn,ExplanationAr=Current.ExplainAr,Date=DateTime.Now.ToString("o"),Difficulty=Level,Correct=correct,Hint=UsedHint,TimedOut=timeout,Seconds=QuestionSeconds};
  }
  public bool Hint() { if(Answered || UsedHint || Hints<=0) return false; Hints--;UsedHint=true;return true; }
  public void Next(Question next=null) { if(!Answered || IsOver) return;if(Endless&&Index==Questions.Count-1){if(next==null)throw new InvalidOperationException("An endless round needs its next question.");Questions.Add(next);}Index++;Answered=false;UsedHint=false;QuestionSeconds=0; }
 }
}
