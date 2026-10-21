using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;

namespace LittleNotebook {
 public partial class MainWindow {
  public async void RunNarrationTests(){
   string output=Path.Combine(baseDir,"..","test-output","narration.txt");File.WriteAllText(output,"RUNNING: English/Arabic question narration."+Environment.NewLine);timer.Stop();int checks=0;
   Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
   try{
    Profile.Settings.Sound=false;Profile.Settings.Volume=0;audio.ApplyVolume();((MediaPlayer)typeof(AudioSystem).GetField("neuralPlayer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio)).IsMuted=true;Profile=new Profile{Name="Narration test"};Profile.Settings.Music=false;Profile.Settings.Sound=false;Profile.Settings.Timer=false;Profile.Settings.ReducedMotion=true;
    check(!Profile.Settings.AutoReadQuestions,"Automatic question speech must default off");check(audio.QuestionVoiceAvailable("en")&&audio.QuestionVoiceAvailable("ar"),"Both offline voices must be bundled");
    foreach(bool arabic in new[]{false,true})foreach(var item in bank.Questions){var segments=QuestionSpeech.Make(item,arabic);check(segments.Count>0&&segments.All(x=>!string.IsNullOrWhiteSpace(x.Text)),"Empty question narration "+item.Id);if(item.Kind=="listen")check(segments.Count==1&&segments[0].Text==item.Prompt(arabic),"Listening answer leaked into narration "+item.Id);if(item.Kind=="pairs")check(segments.Count==1,"Pair solutions leaked into narration");}
    var question=bank.Questions.First(x=>x.Topic=="general");game=new GameSession{Topic=question.Topic,Questions=new System.Collections.Generic.List<Question>{question}};page="game";interactionId="";Build();await Task.Delay(50);check(questionReadTask==null,"Speech autoplayed with preference off");ClickBody("Read question aloud");check(questionReadTask!=null&&await questionReadTask,"Manual speaker failed: "+audio.SpeechError);audio.StopSpeech();Capture("tts-01-english-question");
    int utterances=0;foreach(string language in new[]{"en","ar"})foreach(var sample in bank.Questions.GroupBy(x=>x.Topic+":"+x.Kind).Select(g=>g.First()).Concat(new[]{bank.MathQuestion(0),bank.MathQuestion(2,"word-problems")})){
     Profile.Settings.Language=language;bool ok=await audio.ReadQuestionAsync(QuestionSpeech.Make(sample,Ar));check(ok,"Offline synthesis failed: "+language+" "+sample.Id+" "+audio.SpeechError);audio.StopSpeech();utterances++;
    }
    File.AppendAllText(output,"PASS: "+utterances+" question examples synthesized across both languages, every format and generated maths; all 675 records checked for complete instructions and no listening/pair answer leakage."+Environment.NewLine);
    Profile.Settings.Language="ar";Build();Capture("tts-02-arabic-question");Profile.Settings.AutoReadQuestions=true;lastAutoReadKey="";questionReadTask=null;Build();await Task.Delay(100);check(questionReadTask!=null&&await questionReadTask,"Arabic automatic narration failed: "+audio.SpeechError);int first=questionReadSerial;Build();await Task.Delay(50);check(first==questionReadSerial,"A redraw replayed the question");UseHint();await Task.Delay(50);check(first==questionReadSerial,"A hint replayed the question");PauseMenu();check(!audio.IsSpeaking&&!audio.IsPreparingQuestion,"Pause did not stop narration");CloseModal();await Task.Delay(50);check(first==questionReadSerial,"Resume repeated completed question narration");
    Profile.Settings.Language="en";Build();await Task.Delay(100);check(first!=questionReadSerial&&await questionReadTask,"Language change did not select the new voice");audio.StopSpeech();
    var pending=audio.ReadQuestionAsync(new[]{new SpeechSegment("What number comes after "+DateTime.UtcNow.Ticks.ToString()+"?","en")});audio.StopSpeech();check(!await pending&&!audio.IsSpeaking,"Cancelled preparation played late");
    var old=audio.ReadQuestionAsync(new[]{new SpeechSegment("Read the English question carefully.","en")});var latest=audio.ReadQuestionAsync(new[]{new SpeechSegment("اقرأ السؤال بعناية ثم اختر الإجابة الصحيحة.","ar")});check(!await old&&await latest,"Rapid language replacement failed: "+audio.SpeechError);audio.StopSpeech();
    lastAutoReadKey="";questionReadTask=null;Build();await Task.Delay(30);var duringAnswer=questionReadTask;Submit(game.Current.Answer,false);if(duringAnswer!=null)await duringAnswer;check(!audio.IsSpeaking&&!audio.IsPreparingQuestion,"Answering did not cancel question narration");Profile.Settings.AutoReadQuestions=false;AdvanceFeedback(1.36);game=null;page="home";Build();Profile.Settings.AutoReadQuestions=true;Save();check(store.Load().Settings.AutoReadQuestions,"Auto narration preference did not persist");Profile.Settings.AutoReadQuestions=false;
    SettingsView();Capture("tts-03-settings-english");CloseModal();Profile.Settings.Language="ar";Profile.Settings.TextScale=1.15;Build();SettingsView();Capture("tts-04-settings-arabic");CloseModal();check(Diagnostics.ErrorCount==0,"An asynchronous exception was logged");
    File.AppendAllText(output,"PASS: "+checks+" checks; manual/auto off-default, no repeat on redraw/hint/resume, language changes, preparation cancellation, rapid replacement, answer cancellation, saved preference and native settings layouts."+Environment.NewLine);
    var closingSpeech=audio.ReadQuestionAsync(new[]{new SpeechSegment("The next number is "+DateTime.UtcNow.Ticks.ToString(),"en")});closing=true;game=null;Close();await closingSpeech;
   }catch(Exception ex){File.AppendAllText(output,"FAIL: "+ex+Environment.NewLine);Environment.ExitCode=1;closing=true;game=null;Close();}
  }
 }
}
