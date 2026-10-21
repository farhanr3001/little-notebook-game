using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace LittleNotebook {
 public partial class MainWindow {
  int stressChecks,stressQuestions;long stressPeak;string stressPhase="initializing";System.Diagnostics.Stopwatch stressClock;
  string StressOutput{get{return Path.Combine(baseDir,"..","test-output","stress.txt");}}
  void StressCheck(bool value,string message){stressChecks++;if(!value)throw new Exception(message);if(Diagnostics.ErrorCount>0)throw new Exception("An asynchronous exception was logged during "+stressPhase);}
  void StressLog(string message){long memory=System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;stressPeak=Math.Max(stressPeak,memory);File.AppendAllText(StressOutput,stressClock.Elapsed.ToString(@"mm\:ss")+" "+message+" [private memory "+(memory/1048576)+" MB]"+Environment.NewLine);}
  void ClickBody(string name){UpdateLayout();var b=Descendants<Button>(body).First(x=>x.IsEnabled&&AutomationProperties.GetName(x)==name);b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));}
  void MuteStressSpeech(){var narration=typeof(AudioSystem).GetField("narration",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio) as MediaPlayer;if(narration!=null)narration.IsMuted=true;}
  void AnswerActivity(Question q){
   if(q.Kind=="order"){
    foreach(string word in q.Answer.Split(' ')){
     var button=Descendants<Button>(interaction).First(x=>x.IsEnabled&&Convert.ToString(x.Content)==word);button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    }ClickBody(T("Check sentence","تحقق من الجملة"));
   }else if(q.Kind=="spell"||q.Kind=="listen"){
    Descendants<TextBox>(interaction).Single().Text=q.Answer;ClickBody(T("Check spelling","تحقق من الكتابة"));
   }else if(q.Kind=="search"){
    bool found=false;for(int start=0;start<64&&!found;start++)foreach(int step in new[]{1,8}){var cells=Enumerable.Range(0,q.Answer.Length).Select(i=>start+i*step).ToArray();if(cells.Last()<64&&(step==8||cells[0]/8==cells.Last()/8)&&new string(cells.Select(i=>searchLetters[i]).ToArray())==q.Answer.ToUpperInvariant()){foreach(int cell in cells)SelectSearch(cell);found=true;break;}}
    StressCheck(found,"Generated search has no solution: "+q.Id);ClickBody(T("Check word","تحقق من الكلمة"));
   }else if(q.Kind=="memory"){
    foreach(int value in memoryCards.Distinct().ToArray()){var cells=Enumerable.Range(0,memoryCards.Length).Where(i=>memoryCards[i]==value).ToArray();FlipCard(cells[0]);FlipCard(cells[1]);}
   }else if(q.Kind=="pairs"){
    for(int i=0;i<q.Pairs.Length;i++){wordPairLeft=i;MatchWordPair(i);}
   }else {
    answerButtons.First(b=>Convert.ToString(b.Content)==DisplayAnswer(q.Answer)).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
   }
  }
  public async void RunStress(){
   stressClock=System.Diagnostics.Stopwatch.StartNew();Directory.CreateDirectory(Path.GetDirectoryName(StressOutput));File.WriteAllText(StressOutput,"RUNNING: isolated native feature stress suite."+Environment.NewLine);timer.Stop();
   try{
    Profile.Settings.Sound=false;Profile.Settings.Volume=0;audio.ApplyVolume();Profile=new Profile{Name="Stress test",Coins=100000};Profile.Settings.Music=false;Profile.Settings.Sound=false;Profile.Settings.Timer=false;Profile.Settings.ReducedMotion=true;
    var nativeSpeech=(SpeechSynthesizer)typeof(AudioSystem).GetField("speech",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);if(audio.SystemVoiceAvailable){nativeSpeech.Volume=0;nativeSpeech.Rate=8;}StressLog("English playback available: "+audio.VoiceAvailable+"; installed Windows fallback: "+audio.SystemVoiceAvailable);
    stressPhase="all question records / both languages";
    foreach(string language in new[]{"en","ar"}){
     Profile.Settings.Language=language;
     for(int i=0;i<bank.Questions.Count;i++){
      var q=bank.Questions[i];Profile.Settings.Difficulty=q.Difficulty;game=new GameSession(q.Difficulty){Topic=q.Topic,Questions=new List<Question>{q}};page="game";paused=false;interactionId="";Build();UpdateLayout();
      bool hint=i%13==0;if(hint)UseHint();bool correct=i%5>1;bool timeout=i%5==0;int coins=Profile.Coins,attempts=Profile.Attempts.Count;int lives=game.Lives;
      if(q.Kind=="listen"&&audio.VoiceAvailable){StressCheck(audio.Speak(q.Speech),"Listening failed to start: "+q.Id);MuteStressSpeech();await Task.Delay(15);audio.Speak(q.Speech);MuteStressSpeech();}
      if(correct)AnswerActivity(q);else Submit(timeout?"":"incorrect stress answer",timeout);
      StressCheck(game.Answered&&game.Correct==(correct?1:0),"Answer result incorrect: "+q.Id);StressCheck(Profile.Attempts.Count==attempts+1,"Attempt count: "+q.Id);StressCheck(game.Lives==lives-(correct?0:1),"Life count: "+q.Id);StressCheck(Profile.Coins==coins+(correct?(hint?1:3):0),"Reward count: "+q.Id);
      Submit(q.Answer,false);StressCheck(Profile.Attempts.Count==attempts+1,"Duplicate submit awarded twice");AdvanceFeedback(.6);StressCheck(page=="game","Feedback advanced early");PauseMenu();AdvanceFeedback(10);StressCheck(page=="game","Paused feedback advanced");CloseModal();AdvanceFeedback(.76);StressCheck(page=="results"&&game.Recorded,"Automatic results transition failed: "+language+" "+i+" "+q.Id+" "+q.Kind+" page="+page+" paused="+paused+" feedback="+answerFeedbackElapsed+" overlay="+overlay.Children.Count);
      stressQuestions++;if(i%25==0){StressLog(language+" questions "+(i+1)+" / "+bank.Questions.Count);await Task.Delay(1);}if(i%75==74){game=null;Profile.Attempts.Clear();Profile.Sessions.Clear();GC.Collect();await Task.Delay(1);}
     }
    }
    StressLog("PASS all "+stressQuestions+" record/language flows, including answer/cancel while speech is running");
    game=null;page="home";stressPhase="navigation, palettes, sizes and modals";
    foreach(string language in new[]{"en","ar"})foreach(string theme in new[]{"matcha","rose","sky","lavender","night"})foreach(bool hidden in new[]{false,true}){
     Profile.Settings.Language=language;Profile.Settings.Theme=theme;Profile.Settings.SidebarHidden=hidden;Profile.Settings.TextScale=hidden?1.15:.85;Profile.Settings.ButtonScale=hidden?1:.75;
     foreach(string screen in new[]{"home","study","profile","shop","notebook"}){page=screen;Build();UpdateLayout();StressCheck(overlay.Children.Count==0,"Unexpected modal on navigation");}
     SettingsView();ToggleSidebar();StressCheck(overlay.Children.Count>0&&paused&&Profile.Settings.SidebarHidden==hidden,"Sidebar shortcut dismissed a modal / left invisible pause");CloseModal();SizingSettings();CloseModal();EditCharacter();ClickTestButton(T("Cancel","إلغاء"));ConfirmResetCharacter();ClickTestButton(T("Cancel","إلغاء"));CloseModal();RadioSelection();CloseModal();await Task.Delay(1);
    }
    StressLog("PASS navigation across language/theme/sidebar/size matrix, modal cancellation and sidebar shortcut");
    Profile.Settings.Language="en";Profile.Settings.Theme="matcha";Profile.Settings.TextScale=.9;Profile.Settings.ButtonScale=.92;Profile.Settings.SidebarHidden=false;page="home";Build();stressPhase="round options and category replenishment";
    foreach(string topic in new[]{"mixed","english","math","puzzles","general"})for(int level=0;level<3;level++)foreach(int length in new[]{0,5,10,15})foreach(bool single in new[]{false,true}){
     selectedTopic=topic;Profile.Settings.Difficulty=level;Profile.Settings.RoundLength=length;Profile.Settings.SingleLife=single;Profile.Settings.Timer=length!=0;StartGame();StressCheck(game.MaxLives==(level==2||single?1:3)&&game.Endless==(length==0),"Round settings changed unexpectedly");StressCheck(game.Questions.Count==(length==0?1:length),"Round length incorrect");
     Submit(game.Current.Answer,false);AdvanceFeedback(1.36);StressCheck(page=="game"&&game.Index==1,"Next question transition failed");int count=Profile.Sessions.Count;Navigate("study");ClickTestButton(T("Keep playing","متابعة اللعب"));StressCheck(page=="game","Cancelled leaving navigated away");Navigate("study");ClickTestButton(T("Main menu","العودة إلى مكتبي"));StressCheck(page=="study"&&Profile.Sessions.Count==count+1&&!Profile.Sessions.Last().Completed,"Partial round leave was not saved");await Task.Delay(1);
    }
    StressLog("PASS 120 round/life/timer setups with next question, leave/cancel and partial-session saves");
    game=null;stressPhase="rewards and character changes";Profile.Coins=100000;
    foreach(var reward in ProfileFeatures.Rewards){int before=Profile.Coins;bool owned=Profile.Owned.Contains(reward.Id);bool bought=ProfileFeatures.Purchase(Profile,reward.Id);StressCheck(bought!=owned&&Profile.Coins==before-(owned?0:reward.Price),"Purchase price / owned guard "+reward.Id);StressCheck(!ProfileFeatures.Purchase(Profile,reward.Id),"Duplicate purchase "+reward.Id);if(reward.Category=="pets")foreach(string color in reward.Colors){StressCheck(ProfileFeatures.Equip(Profile,reward.Id,color),"Pet equip "+reward.Id);page="shop";Build();PreviewPet(reward,color);UpdateLayout();CloseModal();}else ProfileFeatures.Equip(Profile,reward.Id);}
    for(int character=0;character<10;character++){Profile.Character=character;Build();EditCharacter();ClickTestButton("Cancel");StressCheck(Profile.Character==character,"Character editor cancel changed selection");}Save();var reloaded=store.Load();StressCheck(reloaded.Owned.Count==ProfileFeatures.Rewards.Count&&reloaded.Pet==Profile.Pet,"Purchased rewards did not persist");StressLog("PASS every reward/pet colour and all character edit cancellations");
    stressPhase="notebook pages, dense ink and history";Profile.Notebook=new List<NotebookPage>{new NotebookPage()};Profile.NotebookIndex=0;notebookPageId="";page="notebook";Build();
    for(int n=0;n<24;n++){
     foreach(string tool in new[]{"pencil","pen","brush","highlighter"}){ConfigureNotebookTool(tool);BeginNotebookChange();for(int s=0;s<10;s++){var points=new StylusPointCollection();for(int k=0;k<120;k++)points.Add(new StylusPoint(20+k*7,30+s*32+Math.Sin(k*.17)*10));notebookInk.Strokes.Add(new Stroke(points,notebookInk.DefaultDrawingAttributes.Clone()));}CommitNotebookChange();}
     foreach(string shape in new[]{"line","box","circle","triangle","arrow","heart"}){BeginNotebookChange();AddNotebookShape(shape,new Point(10,10),new Point(125,125));CommitNotebookChange();}foreach(string stamp in new[]{"star","heart","flower","butterfly","cup","cat","rabbit","fish","bird"}){BeginNotebookChange();StampNotebook(stamp,new Point(330,220));CommitNotebookChange();}
     CurrentNotebookPage.Template=new[]{"cat","rabbit","fish","bird","flower","cup","eye","butterfly"}[n%8];RefreshNotebookGuide();int strokes=notebookInk.Strokes.Count;BeginNotebookChange();notebookInk.Strokes.Clear();CommitNotebookChange();NotebookHistory(false);StressCheck(notebookInk.Strokes.Count==strokes,"Dense ink clear/undo lost ink");NotebookHistory(true);StressCheck(notebookInk.Strokes.Count==0,"Dense ink redo failed");NotebookHistory(false);Save();StressCheck(DecodeInk(store.Load().Notebook[n].Ink).Count==strokes,"Dense ink round trip failed");TurnNotebookPage(1);await Task.Delay(1);
    }
    for(int n=23;n>=0;n--){TurnNotebookPage(-1);StressCheck(notebookInk.Strokes.Count>40,"Page turning lost dense ink");}ConfirmResetNotebook();ClickTestButton("Cancel");StressCheck(Profile.Notebook.Count==25,"Reset cancellation lost pages");int beforeStars=Profile.Coins;ResetNotebook();StressCheck(Profile.Notebook.Count==1&&Profile.Coins==beforeStars,"Notebook reset changed stars");StressLog("PASS 25 pages, dense smooth curves, every tool/shape/stamp/tracing, clear/undo/redo and reset");
    stressPhase="save failures, migration and recovery";string denied=Path.Combine(baseDir,"..","test-output","locked-save.json");File.WriteAllText(denied,"keep this original");using(var held=new FileStream(denied,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){var fallback=new SaveStore(denied,denied+".fallback");StressCheck(fallback.Save(Profile)&&fallback.Path.EndsWith(".fallback"),"Locked save did not fall back");}StressCheck(File.ReadAllText(denied)=="keep this original","Locked original was overwritten");StressCheck(store.Save(Profile),"Final stress save failed");StressLog("PASS locked save fallback; existing core suite covers corrupt JSON, legacy migrations and backups");
    stressPhase="delayed memory callback and close during speech";var memory=bank.Questions.First(q=>q.Kind=="memory");game=new GameSession{Topic="puzzles",Questions=new List<Question>{memory}};page="game";interactionId="";Build();int other=Enumerable.Range(1,memoryCards.Length-1).First(i=>memoryCards[i]!=memoryCards[0]);FlipCard(0);FlipCard(other);RecordSession(false);game=null;page="home";Build();audio.Speak("This is interrupted when the game closes.");await Task.Delay(1000);StressCheck(page=="home","Old memory callback changed the page");audio.Speak("Goodbye");StressCheck(Diagnostics.ErrorCount==0,"Asynchronous errors were logged");StressLog("PASS delayed callbacks after navigation; close during speech follows");
    StressLog("PASS: "+stressChecks+" checks; "+stressQuestions+" complete question flows; peak observed private memory "+(stressPeak/1048576)+" MB; elapsed "+stressClock.Elapsed.ToString(@"mm\:ss")+".");closing=true;Close();
   }catch(Exception ex){Diagnostics.Log("stress failure: "+stressPhase,ex);File.AppendAllText(StressOutput,"FAIL in "+stressPhase+": "+ex+Environment.NewLine);closing=true;game=null;Close();Environment.ExitCode=1;}
  }
 }
}
