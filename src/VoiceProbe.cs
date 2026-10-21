using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LittleNotebook {
 public static class VoiceProbe {
  static MediaPlayer Player(AudioSystem audio,string field){return (MediaPlayer)typeof(AudioSystem).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio);}
  static async Task Wait(Func<bool> condition,string label){for(int i=0;i<300;i++){if(condition())return;await Task.Delay(10);}throw new Exception("Timed out: "+label);}
  public static int Run(){
   string output=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output/voice.txt"));File.WriteAllText(output,"RUNNING: neural voice playback probe."+Environment.NewLine);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var settings=new Settings{Music=true,Volume=.4,Sound=false};var audio=new AudioSystem(settings);Player(audio,"music").IsMuted=true;int result=0;
   app.DispatcherUnhandledException+=(s,e)=>{result=1;File.AppendAllText(output,"FAIL asynchronous: "+e.Exception+Environment.NewLine);e.Handled=true;app.Shutdown();};
   app.Dispatcher.BeginInvoke(new Action(async()=>{
    try{
     var bank=new QuestionBank(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"content/questions.json"));var words=bank.Questions.Where(q=>q.Kind=="listen").Select(q=>q.Speech).Distinct().ToArray();
     typeof(AudioSystem).GetField("systemVoiceAvailable",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(audio,false);
     foreach(string word in words){
      if(SpeechClips.Find(word)==null||!audio.CanSpeak(word)||!audio.Speak(word)||audio.LastVoiceBackend!="Kokoro af_heart")throw new Exception("Missing neural playback: "+word);
      var player=Player(audio,"narration");player.IsMuted=true;bool opened=false,ended=false;Exception failure=null;player.MediaOpened+=(s,e)=>opened=true;player.MediaEnded+=(s,e)=>ended=true;player.MediaFailed+=(s,e)=>failure=e.ErrorException;
      await Wait(()=>opened||failure!=null,"open "+word);if(failure!=null)throw failure;if(!player.NaturalDuration.HasTimeSpan||player.NaturalDuration.TimeSpan.TotalSeconds<.2)throw new Exception("Invalid clip duration "+word);
      if(Math.Abs(Player(audio,"music").Volume-.088)>.001)throw new Exception("Music did not duck during speech");player.Position=player.NaturalDuration.TimeSpan-TimeSpan.FromMilliseconds(75);await Wait(()=>ended,"end "+word);if(Math.Abs(Player(audio,"music").Volume-.28)>.001)throw new Exception("Music did not restore after speech");
     }
     File.AppendAllText(output,"PASS: all "+words.Length+" distinct listening clips opened and reached actual MediaEnded, without any installed system voice; music ducked/restored correctly."+Environment.NewLine);
     for(int i=0;i<180;i++){audio.Speak(words[i%words.Length]);Player(audio,"narration").IsMuted=true;await Task.Delay(3);if(i%3==0)audio.StopSpeech();}
     audio.Speak(words[0]);Player(audio,"narration").IsMuted=true;await Task.Delay(100);if(Player(audio,"narration")==null||Math.Abs(Player(audio,"music").Volume-.088)>.001)throw new Exception("A stale completion cancelled newer speech");audio.StopSpeech();
     if(audio.CanSpeak("unknown unbundled phrase"))throw new Exception("Unavailable speech was not detected without a system voice");
     string invalid=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output/invalid-audio.mp3"));File.WriteAllText(invalid,"This deliberately is not an MP3.");audio.AddTrack(invalid);audio.Select(audio.Tracks.Count-1);await Wait(()=>audio.Error.Length>0,"invalid imported music error");audio.Select(0);await Wait(()=>audio.Error.Length==0,"valid track recovery");
     File.AppendAllText(output,"PASS: 180 rapid neural replays/cancellations, stale-callback protection, unavailable-voice detection and corrupt imported-music recovery."+Environment.NewLine);audio.Speak(words[0]);Player(audio,"narration").IsMuted=true;audio.Dispose();audio.Dispose();audio.StopSpeech();if(audio.Speak(words[0]))throw new Exception("Disposed audio still started speech");await Task.Delay(150);if(Diagnostics.ErrorCount!=0)throw new Exception("Asynchronous exception logged");File.AppendAllText(output,"PASS: close during neural playback and repeated disposal."+Environment.NewLine);
    }catch(Exception ex){result=1;File.AppendAllText(output,"FAIL: "+ex+Environment.NewLine);}finally{audio.Dispose();app.Shutdown();}
   }));app.Run();return result;
  }
 }
}
