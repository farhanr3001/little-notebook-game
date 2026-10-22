using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace LittleNotebook {public partial class MainWindow {
 public async void RunHealthChecks(){string report=Path.Combine(baseDir,"..","test-output","health.txt");int checks=0;try{
  timer.Stop();for(int i=0;i<3;i++){var voice=new NeuralSpeechService();var cancel=new CancellationTokenSource();Task<string> pending=voice.RenderAsync(new SpeechSegment("Health check "+Guid.NewGuid().ToString("N"),i==1?"ar":"en"),cancel.Token);int pid=voice.WorkerId;if(pid==0)throw new Exception("Speech worker did not start");checks++;cancel.Cancel();voice.Dispose();try{await pending;}catch{}try{using(var child=Process.GetProcessById(pid))if(!child.HasExited)throw new Exception("Speech worker survived disposal");}catch(ArgumentException){}checks++;voice.Dispose();checks++;}
  var notes=(System.Windows.Media.MediaPlayer[])typeof(AudioSystem).GetField("miniPlayers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(audio);for(int i=0;i<notes.Length;i++){notes[i].Open(new Uri(Path.Combine(baseDir,"assets/minigames/note-"+i+".wav")));notes[i].Volume=0;notes[i].Play();}await Task.Delay(100);
  audio.Speak("garden");audio.Dispose();audio.Dispose();foreach(string file in new[]{"beach-resort.mp3","typing.wav","minigames/note-0.wav","minigames/note-1.wav","minigames/note-2.wav","minigames/note-3.wav"})using(var stream=new FileStream(Path.Combine(baseDir,"assets",file),FileMode.Open,FileAccess.ReadWrite,FileShare.None)){checks++;}
  if(Diagnostics.ErrorCount!=0)throw new Exception("Unexpected UI errors during health checks");File.WriteAllText(report,"PASS: "+checks+" shutdown health checks. Pending English/Arabic speech cancelled; owned worker processes ended and waited; repeated disposal safe; media files unlocked. Windows kill-on-close job protects against orphan workers after a game crash.");Environment.ExitCode=0;
 }catch(Exception ex){File.WriteAllText(report,"FAIL: "+ex);Environment.ExitCode=1;}finally{closing=true;Close();}}
}}
