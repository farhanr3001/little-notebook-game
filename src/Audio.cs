using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace LittleNotebook {
 public class MusicTrack {
  public string Title,Artist,File;public bool Custom;
 }
 public class ShufflePlaylist {
  readonly Random random;readonly int size;Queue<int> upcoming=new Queue<int>();List<int> history=new List<int>();int position;
  public int Current{get{return history[position];}}
  public ShufflePlaylist(int size,int current,int? seed=null){this.size=Math.Max(1,size);random=seed.HasValue?new Random(seed.Value):new Random();Select(current);}
  public void Select(int index){history.Clear();history.Add(Math.Max(0,Math.Min(size-1,index)));position=0;upcoming=new Queue<int>(Enumerable.Range(0,size).Where(i=>i!=Current).OrderBy(i=>random.Next()));}
  public int Next(){
   if(position<history.Count-1)return history[++position];
   if(upcoming.Count==0){var cycle=Enumerable.Range(0,size).OrderBy(i=>random.Next()).ToList();if(size>1&&cycle[0]==Current){int next=cycle[1];cycle[1]=cycle[0];cycle[0]=next;}upcoming=new Queue<int>(cycle);}
   history.Add(upcoming.Dequeue());position++;if(history.Count>256){history.RemoveAt(0);position--;}return Current;
  }
  public int Previous(){if(position>0)position--;return Current;}
 }
 public class AudioSystem : IDisposable {
  readonly MediaPlayer[] miniPlayers=Enumerable.Range(0,4).Select(i=>new MediaPlayer()).ToArray();int miniVoice;
  readonly MediaPlayer[] miniNotes=Enumerable.Range(0,16).Select(i=>new MediaPlayer()).ToArray();readonly MediaPlayer miniReplay=new MediaPlayer();
  readonly HashSet<MediaPlayer> miniRunning=new HashSet<MediaPlayer>();readonly int[] miniNoteVoice=new int[8];bool miniReady,miniPaused,miniFocus,miniSong;readonly bool silentProbe;
  internal event Action<string> PlaybackRequested;internal event Action<double> MusicVolumeApplied;
  public bool MiniPaused{get{return miniPaused;}}
  MediaPlayer music=new MediaPlayer(),effect=new MediaPlayer();SpeechSynthesizer speech;Settings settings;
  readonly Dispatcher owner=Dispatcher.CurrentDispatcher;Prompt activePrompt;bool disposed,systemVoiceAvailable;readonly MediaPlayer neuralPlayer=new MediaPlayer();MediaPlayer narration;string clipWord;
  readonly NeuralSpeechService questionVoice=new NeuralSpeechService();CancellationTokenSource questionRequest;Queue<string> questionClips=new Queue<string>();public string SpeechError="";public bool IsSpeaking{get{return speaking;}}public bool IsPreparingQuestion{get{return questionRequest!=null;}}public bool QuestionVoiceAvailable(string language){return !disposed&&questionVoice.Available(language);}
  public bool VoiceAvailable{get{return !disposed&&(systemVoiceAvailable||SpeechClips.Available);}}public bool SystemVoiceAvailable{get{return !disposed&&systemVoiceAvailable;}}public bool CanSpeak(string text){return !disposed&&(systemVoiceAvailable||SpeechClips.Find(text)!=null);}
  public string LastVoiceBackend{get;private set;}bool speaking=false;public string Error="";DateTime lastTyping=DateTime.MinValue;
  public List<MusicTrack> Tracks=new List<MusicTrack>(),AllTracks=new List<MusicTrack>();ShufflePlaylist playlist;public event Action TrackChanged;
  public string CurrentTitle {get{return Tracks.Count==0?"No music selected":Tracks[settings.Track%Tracks.Count].Title;}}
  public AudioSystem(Settings s,bool silentNativeProbe=false){
   settings=s;silentProbe=silentNativeProbe;music.IsMuted=effect.IsMuted=neuralPlayer.IsMuted=silentProbe;
   foreach(var voice in miniPlayers.Concat(miniNotes).Concat(new[]{miniReplay})){var player=voice;player.IsMuted=true;player.Volume=0;player.Pause();
    player.MediaOpened+=(sender,e)=>{if(disposed||player.Source==null)return;if(!miniRunning.Contains(player)){player.Pause();player.IsMuted=true;player.Volume=0;}else if(miniPaused)player.Pause();};
    player.MediaEnded+=(sender,e)=>{if(!player.NaturalDuration.HasTimeSpan||player.Position<player.NaturalDuration.TimeSpan-TimeSpan.FromMilliseconds(50))return;FinishMiniPlayer(player);};
    player.MediaFailed+=(sender,e)=>FinishMiniPlayer(player);
   }
   Tracks.Add(new MusicTrack{Title="Beach Resort",Artist="Purrple Cat",File="beach-resort.mp3"});
   Tracks.Add(new MusicTrack{Title="Road Trip",Artist="Purrple Cat",File="road-trip.mp3"});
   Tracks.Add(new MusicTrack{Title="Look on the Bright Side",Artist="Purrple Cat",File="look-on-the-bright-side.mp3"});
   foreach(string path in settings.CustomMusic.Where(x=>File.Exists(Resolve(x))))Tracks.Add(new MusicTrack{Title=settings.MusicTitles.ContainsKey(path)?settings.MusicTitles[path]:Path.GetFileNameWithoutExtension(path),Artist="",File=path,Custom=true});
   AllTracks=Tracks.ToList();Tracks.RemoveAll(x=>settings.DisabledMusic.Contains(x.File));settings.Track=Tracks.Count==0?0:settings.Track%Tracks.Count;
   playlist=new ShufflePlaylist(Tracks.Count,settings.Track);
   music.MediaEnded+=(a,b)=>Track(1);
   music.MediaFailed+=(a,b)=>Error="Music could not be played: "+b.ErrorException.Message;
   music.MediaOpened+=(a,b)=>Error="";
   neuralPlayer.MediaEnded+=(sender,e)=>{if(disposed||narration!=neuralPlayer||!neuralPlayer.NaturalDuration.HasTimeSpan||neuralPlayer.Position<neuralPlayer.NaturalDuration.TimeSpan-TimeSpan.FromMilliseconds(50))return;if(questionClips.Count>0){PlayQuestionClip();return;}narration=null;clipWord=null;neuralPlayer.Stop();speaking=false;ApplyVolume();};
   neuralPlayer.MediaFailed+=(sender,e)=>{if(disposed||narration!=neuralPlayer)return;string word=clipWord;narration=null;clipWord=null;neuralPlayer.Stop();speaking=false;ApplyVolume();if(word!=null)SpeakSystem(word);};
   try{speech=new SpeechSynthesizer();var v=speech.GetInstalledVoices().FirstOrDefault(x=>x.Enabled&&x.VoiceInfo.Culture.TwoLetterISOLanguageName=="en");if(v!=null){speech.SelectVoice(v.VoiceInfo.Name);systemVoiceAvailable=true;}speech.Rate=0;speech.SpeakCompleted+=SpeechCompleted;}catch{systemVoiceAvailable=false;}
   LoadTrack();
  }
  public static string Resolve(string file){return Path.IsPathRooted(file)?file:(file.StartsWith("data/",StringComparison.OrdinalIgnoreCase)||file.StartsWith("data\\",StringComparison.OrdinalIgnoreCase))?Path.Combine(AppPaths.DataDirectory,file.Substring(5)):Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",file);}
  void OnOwner(Action action){if(disposed||owner.HasShutdownStarted)return;if(owner.CheckAccess()){if(!disposed)action();}else try{owner.BeginInvoke(new Action(()=>{if(!disposed)action();}));}catch(InvalidOperationException){}}
  void SpeechCompleted(object sender,SpeakCompletedEventArgs e){OnOwner(()=>{if(activePrompt!=e.Prompt)return;activePrompt=null;speaking=false;ApplyVolume();});}
  public void ApplyVolume(){if(disposed)return;if(!owner.CheckAccess()){OnOwner(ApplyVolume);return;}music.Volume=settings.Music?settings.Volume*(speaking||miniSong?.12:miniFocus?.24:.70):0;if(MusicVolumeApplied!=null)MusicVolumeApplied(music.Volume);}
  public void LoadTrack(){if(disposed||Tracks.Count==0){settings.Music=false;music.Pause();return;}music.Open(new Uri(Resolve(Tracks[settings.Track%Tracks.Count].File)));ApplyVolume();if(settings.Music)music.Play();}
  public void Toggle(){if(disposed||Tracks.Count==0)return;settings.Music=!settings.Music;if(settings.Music)music.Play();else music.Pause();ApplyVolume();}
  public void Select(int index){if(disposed||Tracks.Count==0)return;playlist.Select(Math.Max(0,index)%Tracks.Count);settings.Track=playlist.Current;settings.Music=true;LoadTrack();if(TrackChanged!=null)TrackChanged();}
  public void Track(int delta){if(disposed||Tracks.Count==0)return;settings.Track=delta<0?playlist.Previous():playlist.Next();LoadTrack();if(TrackChanged!=null)TrackChanged();}
  public void AddTrack(string path){settings.CustomMusic.Add(path);var track=new MusicTrack{Title=settings.MusicTitles.ContainsKey(path)?settings.MusicTitles[path]:Path.GetFileNameWithoutExtension(path),Artist="",File=path,Custom=true};AllTracks.Add(track);Tracks.Add(track);playlist=new ShufflePlaylist(Tracks.Count,settings.Track);}
  public void RenameTrack(string file,string title){title=(title??"").Trim().Replace("\r"," ").Replace("\n"," ");if(title.Length==0)return;if(title.Length>120)title=title.Substring(0,120);var track=AllTracks.FirstOrDefault(t=>t.File==file&&t.Custom);if(track==null)return;settings.MusicTitles[file]=title;track.Title=title;}
  public void SetTrackEnabled(string file,bool enabled){string current=Tracks.Count==0?null:Tracks[settings.Track].File;if(enabled)settings.DisabledMusic.Remove(file);else if(!settings.DisabledMusic.Contains(file))settings.DisabledMusic.Add(file);Tracks=AllTracks.Where(x=>!settings.DisabledMusic.Contains(x.File)).ToList();settings.Track=Math.Max(0,Tracks.FindIndex(x=>x.File==current));playlist=new ShufflePlaylist(Tracks.Count,settings.Track);LoadTrack();if(TrackChanged!=null)TrackChanged();}
  public void Fx(string type){if(disposed||!settings.Sound)return;effect.Open(new Uri(Resolve(type+".wav")));effect.Volume=type=="click"?.18:.32;if(PlaybackRequested!=null)PlaybackRequested("fx-"+type);effect.Play();}
  bool MiniPlaybackEnabled{get{return !disposed&&settings.Sound&&(!Diagnostics.Testing||silentProbe);}}
  void OpenMiniPaused(MediaPlayer player,string file){player.IsMuted=true;player.Volume=0;player.Pause();player.Open(new Uri(file));player.Pause();}
  void PlayMini(MediaPlayer player,double volume,string cue){miniRunning.Add(player);player.Volume=volume;player.Position=TimeSpan.Zero;if(PlaybackRequested!=null)PlaybackRequested(cue);if(!miniPaused){player.Play();player.IsMuted=silentProbe;}}
  void FinishMiniPlayer(MediaPlayer player){if(disposed||player.Source==null)return;miniRunning.Remove(player);player.Pause();player.IsMuted=true;player.Volume=0;if(player==miniReplay){miniSong=false;ApplyVolume();}}
  public void PrepareMiniSounds(bool focus=false){if(disposed)return;miniFocus=focus;ApplyVolume();if(!focus||miniReady||!MiniPlaybackEnabled)return;for(int i=0;i<miniNotes.Length;i++)if(miniNotes[i].Source==null)OpenMiniPaused(miniNotes[i],Resolve("minigames/note-"+(i%8)+".wav"));miniReady=true;}
  void MiniClip(string name,double volume){if(!MiniPlaybackEnabled)return;string file=Resolve("minigames/"+name+".wav");if(!File.Exists(file))return;var player=miniPlayers[miniVoice++%miniPlayers.Length];OpenMiniPaused(player,file);PlayMini(player,volume,"cue-"+name);}
  public void MiniNote(int note){if(note<0){MiniClip("low",.22);return;}if(!MiniPlaybackEnabled)return;int key=note%8;var player=miniNotes[key+8*(miniNoteVoice[key]++%2)];if(player.Source==null)OpenMiniPaused(player,Resolve("minigames/note-"+key+".wav"));PlayMini(player,.34,"note-"+key);}
  public void MiniTune(MiniMelody tune){if(!MiniPlaybackEnabled||tune==null)return;OpenMiniPaused(miniReplay,Resolve("minigames/tune-"+tune.Id+".wav"));miniSong=true;ApplyVolume();PlayMini(miniReplay,.40,"song-"+tune.Id);}
  public void InterruptMiniNotes(){foreach(var player in miniNotes.Concat(miniPlayers)){miniRunning.Remove(player);player.Pause();player.IsMuted=true;player.Volume=0;}}
  public void MiniWin(){MiniClip("clear",.28);}
  public void MiniPulseWin(){MiniClip("pulse",.28);}
  public void PauseMiniSounds(){if(disposed||miniPaused)return;miniPaused=true;foreach(var player in miniRunning.ToArray())player.Pause();}
  public void ResumeMiniSounds(){if(disposed||!miniPaused)return;miniPaused=false;if(settings.Sound)foreach(var player in miniRunning.ToArray()){player.Play();player.IsMuted=silentProbe;}else StopMiniSounds();}
  public void StopMiniSounds(bool restoreMusic=true){if(disposed)return;miniRunning.Clear();foreach(var player in miniPlayers.Concat(miniNotes).Concat(new[]{miniReplay})){player.IsMuted=true;player.Volume=0;player.Stop();player.Close();}miniReady=miniPaused=miniSong=false;if(restoreMusic)miniFocus=false;ApplyVolume();}
  public void Typing(){if(disposed||!settings.Sound||(DateTime.UtcNow-lastTyping).TotalMilliseconds<75)return;lastTyping=DateTime.UtcNow;effect.Open(new Uri(Resolve("typing.wav")));effect.Volume=.095;effect.Play();}
  bool SpeakSystem(string word){if(!systemVoiceAvailable)return false;try{activePrompt=new Prompt(word);speaking=true;LastVoiceBackend="Windows fallback";ApplyVolume();speech.SpeakAsync(activePrompt);return true;}catch{activePrompt=null;speaking=false;ApplyVolume();return false;}}
  void PlayQuestionClip(){string path=questionClips.Dequeue();narration=neuralPlayer;clipWord=null;neuralPlayer.Volume=.85;neuralPlayer.Open(new Uri(path));neuralPlayer.Play();}
  public async Task<bool> ReadQuestionAsync(IEnumerable<SpeechSegment> segments){
   if(disposed)return false;StopSpeech();SpeechError="";var request=new CancellationTokenSource();questionRequest=request;
   try{var files=new List<string>();foreach(var segment in segments){request.Token.ThrowIfCancellationRequested();files.Add(await questionVoice.RenderAsync(segment,request.Token));}request.Token.ThrowIfCancellationRequested();if(disposed||files.Count==0)return false;questionClips=new Queue<string>(files);questionRequest=null;speaking=true;LastVoiceBackend="Offline question narration";ApplyVolume();PlayQuestionClip();return true;
   }catch(OperationCanceledException){return false;}catch(Exception ex){if(!disposed&&!request.IsCancellationRequested){SpeechError=ex.Message;speaking=false;ApplyVolume();}return false;}finally{if(questionRequest==request)questionRequest=null;request.Dispose();}
  }
  public bool Speak(string word){
   if(disposed||string.IsNullOrWhiteSpace(word))return false;StopSpeech();string clip=SpeechClips.Find(word);if(clip==null)return SpeakSystem(word);
   var player=neuralPlayer;narration=player;clipWord=word;player.Volume=.85;speaking=true;LastVoiceBackend="Kokoro af_heart";ApplyVolume();
   try{player.Open(new Uri(clip));player.Play();return true;}catch{narration=null;player.Stop();speaking=false;ApplyVolume();return SpeakSystem(word);}
  }
  public void StopSpeech(){if(disposed)return;var request=questionRequest;questionRequest=null;if(request!=null)request.Cancel();questionClips.Clear();activePrompt=null;clipWord=null;speaking=false;var player=narration;narration=null;if(player!=null)player.Stop();if(speech!=null)try{speech.SpeakAsyncCancelAll();}catch(ObjectDisposedException){}ApplyVolume();}
  public void Dispose(){if(disposed)return;music.IsMuted=effect.IsMuted=neuralPlayer.IsMuted=true;StopSpeech();StopMiniSounds();disposed=true;questionVoice.Dispose();activePrompt=null;speaking=false;systemVoiceAvailable=false;var player=narration;narration=null;if(player!=null)player.Close();if(speech!=null){speech.SpeakCompleted-=SpeechCompleted;speech.SpeakAsyncCancelAll();speech.Dispose();}neuralPlayer.Close();music.Close();effect.Close();TrackChanged=null;PlaybackRequested=null;MusicVolumeApplied=null;}
 }
}
