using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace LittleNotebook {
 public partial class MainWindow {
  public async void RunUpdateTests(){
   string output=Path.Combine(baseDir,"..","test-output","update.txt");int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
   try{
    timer.Stop();Profile=new Profile{Name="Update test"};Profile.Settings.Music=false;Profile.Settings.Sound=false;Profile.Settings.Volume=0;audio.ApplyVolume();page="home";Build();
    double originalWidth=Width;SetDisplayMode(true);await Task.Delay(150);check(WindowStyle==WindowStyle.None&&WindowState==WindowState.Maximized&&ResizeMode==ResizeMode.NoResize,"Borderless mode failed");check(store.Load().Settings.BorderlessFullscreen,"Display mode did not save");
    var f11=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this),Environment.TickCount,Key.F11){RoutedEvent=Keyboard.PreviewKeyDownEvent};RaiseEvent(f11);await Task.Delay(150);check(f11.Handled&&!Profile.Settings.BorderlessFullscreen&&WindowStyle==WindowStyle.SingleBorderWindow&&WindowState==WindowState.Normal&&Math.Abs(Width-originalWidth)<2,"F11 did not restore windowed bounds");
    SettingsView();ClickTestButton("Display: Windowed  ▾");Capture("v334-display-settings");ClickTestButton("Borderless fullscreen");check(Profile.Settings.BorderlessFullscreen,"Display settings button failed");ClickTestButton("Windowed");ClickTestButton("Done");check(overlay.Children.Count>0,"Display Done did not return to settings");CloseModal();
    var edit=Descendants<Button>(stage).First(x=>AutomationProperties.GetName(x)=="Edit character");check(Canvas.GetLeft(edit)<100,"Edit icon is not to avatar left");
    // Test gameplay without competing with the owner's foreground window/focus.
    WindowState=WindowState.Minimized;ShowActivated=false;await Task.Delay(200);
    foreach(string language in new[]{"en","ar"}){
     Profile.Settings.Language=language;
     foreach(var q in bank.Questions.Where(x=>x.Kind=="listen")){
      game=new GameSession{Topic="english",Questions=new List<Question>{q}};page="game";paused=false;interactionId="";Build();audio.StopSpeech();
      check(answerButtons.Count==0&&Descendants<TextBox>(interaction).Count()==1,"Listening retained choices / missing input");check(q.PromptEn=="Listen, then write the word"&&q.PromptAr=="استمع ثم اكتب الكلمة","Wrong listening instruction");
      int count=listeningAutoCount;AdvanceListeningAutoplay(.9);check(listeningAutoCount==count,"Listening started too early");PauseMenu();AdvanceListeningAutoplay(3);check(listeningAutoCount==count,"Listening started while paused");CloseModal();AdvanceListeningAutoplay(.11);check(listeningAutoCount==count+1&&audio.IsSpeaking,"Delayed listening did not play");audio.StopSpeech();Build();AdvanceListeningAutoplay(2);check(listeningAutoCount==count+1,"Redraw replayed listening");
      var input=Descendants<TextBox>(interaction).Single();input.Text="  "+q.Answer.ToUpperInvariant()+"  ";ClickBody(T("Check spelling","تحقق من الكتابة"));check(game.Answered&&game.Correct==1&&input.IsReadOnly==false,"Typed listening failed");check(Descendants<TextBox>(interaction).Single().IsReadOnly,"Answered input remained editable");AdvanceFeedback(1.36);check(page=="results","Listening results failed: "+q.Id+" "+language+" page="+page+" paused="+paused+" feedback="+answerFeedbackElapsed+" overlays="+overlay.Children.Count);
     }
    }
    Profile.Settings.Language="en";var listen=bank.Questions.First(x=>x.Kind=="listen");game=new GameSession{Topic="english",Questions=new List<Question>{listen,listen}};page="game";paused=false;interactionId="";Build();int before=listeningAutoCount;PlayListeningWord();audio.StopSpeech();AdvanceListeningAutoplay(2);check(listeningAutoCount==before,"Manual replay before delay caused duplicate auto playback");Submit("misspelled",false);check(game.Lives==2&&game.Correct==0,"Incorrect listening spelling did not cost a life");AdvanceFeedback(1.36);check(game.Index==1&&listeningAutoPending,"Next listening question did not reset auto play");Capture("v334-listening-english");
    Profile.Settings.Language="ar";Build();Capture("v334-listening-arabic");Profile.Settings.Language="en";Build();PauseMenu();ClickTestButton("Exit game");ClickTestButton("Keep playing");check(game!=null&&page=="game"&&!paused,"Cancel Exit game did not resume");PauseMenu();ClickTestButton("Exit game");ClickTestButton("Leave");check(game==null&&page=="home"&&IsVisible&&!closing&&Profile.Sessions.Last().Completed==false,"Exit game closed app / lost partial round");
    PauseMenu();ClickTestButton("Exit game");check(page=="home"&&overlay.Children.Count==0&&!paused&&IsVisible,"Exit menu outside round did not return home");SettingsView();Capture("v334-settings");CloseModal();check(Diagnostics.ErrorCount==0,"Async errors logged");
    File.WriteAllText(output,"PASS: "+checks+" checks. Borderless/windowed settings, F11/bounds/persistence, left edit icon; all listening records in both languages use typed answers, one-second auto playback once, pause/redraw/manual replay, normalization and auto results; incorrect spelling/lives, exit/cancel/partial saves without closing app. Native renders captured.");game=null;closing=true;Close();
   }catch(Exception ex){Diagnostics.Log("update test",ex);File.WriteAllText(output,"FAIL: "+ex);game=null;closing=true;Close();Environment.ExitCode=1;}
  }
 }
}
