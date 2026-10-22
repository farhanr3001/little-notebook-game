using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LittleNotebook {public partial class MainWindow {
 public async void RunMiniAudioChecks(){
  string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/audio-transitions.txt"));int checks=0;
  Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  Func<Func<bool>,Task> waitNative=async ready=>{var deadline=DateTime.UtcNow.AddSeconds(4);while(!ready()&&DateTime.UtcNow<deadline)await Task.Delay(20);};
  try{
   timer.Stop();audio.Dispose();Profile=new Profile{Name="Maya"};Profile.Settings.Sound=true;Profile.Settings.Music=true;Profile.Settings.Volume=.4;MiniCatalog.Normalize(Profile);audio=new AudioSystem(Profile.Settings,true);
   var notes=(MediaPlayer[])typeof(AudioSystem).GetField("miniNotes",flags).GetValue(audio);var cues=(MediaPlayer[])typeof(AudioSystem).GetField("miniPlayers",flags).GetValue(audio);var replay=(MediaPlayer)typeof(AudioSystem).GetField("miniReplay",flags).GetValue(audio);var players=notes.Concat(cues).Concat(new[]{replay}).ToArray();
   Func<MediaPlayer,bool> isPaused=player=>{var state=typeof(MediaPlayer).GetField("_mediaPlayerState",flags).GetValue(player);return state!=null&&(bool)state.GetType().GetField("_paused",flags).GetValue(state);};
   var requests=new List<string>();var gains=new List<double>();audio.PlaybackRequested+=requests.Add;audio.MusicVolumeApplied+=gains.Add;
   Action quiet=()=>{foreach(var player in players){check(player.Source==null||player.IsMuted&&player.Volume==0&&isPaused(player),"An inactive minigame voice is not silent and paused");check(player.Source==null||player.Position==TimeSpan.Zero,"A preloaded voice advanced without a game action");}};
   foreach(string kind in MiniCatalog.Keys){
    requests.Clear();StartMiniGame(kind);await Task.Delay(100);quiet();check(requests.Count==0,"Starting "+kind+" requested audio: "+string.Join(",",requests));bool piano=kind=="arrows"||kind=="echo"||kind=="stack";check(notes.Count(p=>p.Source!=null)==(piano?16:0),"Non-piano game loaded the piano cache: "+kind);BackToMiniGames();
   }
   Profile.Settings.SelectedMiniGame="mousedash";page="minigames";Build();requests.Clear();Descendants<Button>(body).Single(b=>Convert.ToString(b.Content).StartsWith("Start game")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(100);quiet();check(requests.Count==0,"The minigame start button plays a piano click");PauseMiniGame();requests.Clear();ClickTestButton("Keep playing");check(requests.Count==0,"The minigame resume button plays a piano click");BackToMiniGames();
   StartMiniGame("mousedash");requests.Clear();gains.Clear();for(int i=0;i<10;i++){MiniDashDown(new Point(450,204));MiniDashUp();await Task.Delay(20);MiniDashDown(new Point(450,204));MiniDashUp();}
   await Task.Delay(150);quiet();check(requests.Count==0,"Mouse Dash hold/release requested sound");check(notes.All(p=>p.Source==null),"Mouse Dash hold/release opened a piano voice");check(gains.Count==0,"Mouse Dash hold/release changed radio gain");BackToMiniGames();
   StartMiniGame("arrows");await Task.Delay(100);quiet();requests.Clear();audio.MiniNote(3);await waitNative(()=>notes[3].Position.TotalMilliseconds>20);check(requests.SequenceEqual(new[]{"note-3"})&&notes[3].Position.TotalMilliseconds>20,"An intentional piano note did not play");check(notes.Where((p,i)=>i!=3).All(p=>p.Volume==0&&isPaused(p)),"Playing one note woke the rest of the cache");
   audio.PauseMiniSounds();await Task.Delay(40);double position=notes[3].Position.TotalMilliseconds;await Task.Delay(100);check(Math.Abs(notes[3].Position.TotalMilliseconds-position)<20,"Paused piano voice continues playing");audio.ResumeMiniSounds();await Task.Delay(120);check(notes[3].Position.TotalMilliseconds>position+20,"Intended note does not resume");await Task.Delay(1050);check(notes[3].Volume==0&&notes[3].IsMuted&&isPaused(notes[3]),"Completed piano voice remains armed");audio.PauseMiniSounds();audio.ResumeMiniSounds();await Task.Delay(80);check(notes[3].Volume==0&&isPaused(notes[3]),"Resume replays a completed note");
   audio.MiniTune(mini.Melody);await waitNative(()=>replay.Position.TotalMilliseconds>20);check(replay.Position.TotalMilliseconds>20,"Intentional full melody did not start");requests.Clear();gains.Clear();Profile.MiniGames[mini.Kind].Level=mini.Level+1;mini.Phase="complete";mini.Wait=.01;mini.Tick(.02);await Task.Delay(150);quiet();check(requests.Count==0&&mini.Level==2,"Next arrow round requests a stray sound");check(gains.Count>0&&gains.All(g=>g<=Profile.Settings.Volume*.24+.00001),"Next arrow round briefly raises radio gain");
   foreach(string kind in new[]{"signal","tiles","maze","pulse"}){StartMiniGame(kind);audio.MiniWin();await Task.Delay(60);requests.Clear();mini.Phase="complete";mini.Wait=.01;mini.Tick(.02);await Task.Delay(100);quiet();check(requests.Count==0&&notes.All(p=>p.Source==null),"Next "+kind+" round plays/preloads piano");}
   for(int i=0;i<12;i++){StartMiniGame("arrows");StartMiniGame("mousedash");}requests.Clear();await Task.Delay(200);quiet();check(requests.Count==0&&notes.All(p=>p.Source==null),"Late media events revive a stopped piano voice");
   Profile.Settings.Sound=false;StartMiniGame("arrows");requests.Clear();audio.MiniNote(2);audio.MiniWin();audio.MiniTune(mini.Melody);await Task.Delay(100);quiet();check(requests.Count==0&&notes.All(p=>p.Source==null),"Sound-off still loads or plays minigame audio");
   BackToMiniGames();audio.Dispose();foreach(string file in Directory.GetFiles(Path.Combine(baseDir,"assets/minigames"),"*.wav")){
    Func<bool> unlocked=()=>{try{using(var stream=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))return true;}catch(IOException){return false;}};await waitNative(unlocked);check(unlocked(),"Native audio handle survived disposal: "+Path.GetFileName(file));
   }
   check(Diagnostics.ErrorCount==0,"Native audio transitions logged an unexpected exception");
   File.WriteAllText(report,"PASS: "+checks+" real Windows MediaPlayer transition checks, with probe output muted. Eight game starts, ten Mouse Dash hold/release cycles, note and full-song playback, pause/resume/end, next rounds, stable radio gain, late media events, sound-off and unlocked files. No desktop interaction.\n");
  }catch(Exception error){File.WriteAllText(report,"FAIL after "+checks+" checks: "+error);Environment.ExitCode=1;}finally{closing=true;Close();}
 }
}}
