using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace LittleNotebook {
 public static class AudioProbe {
  public static int Run(){
   var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var player=new MediaPlayer();
   var files=new Queue<string>(new[]{"beach-resort.mp3","road-trip.mp3","look-on-the-bright-side.mp3","typing.wav","click.wav","correct.wav","wrong.wav","finish.wav"});var results=new List<string>();string current="";int status=0;
   string output=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output/audio.txt"));
   var timeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};
   AudioSystem radio=null;bool finished=false;
   Action finish=()=>{if(finished)return;finished=true;timeout.Stop();player.Close();if(radio!=null)radio.Dispose();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,results);app.Shutdown();};
   Action checkAutoAdvance=()=>{
    current="automatic radio shuffle";var settings=new Settings{Music=true,Volume=0,Sound=false};radio=new AudioSystem(settings);var visited=new List<int>{settings.Track};
    var native=(MediaPlayer)typeof(AudioSystem).GetField("music",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(radio);
    native.MediaOpened+=(s,e)=>{if(!finished&&native.NaturalDuration.HasTimeSpan)native.Position=native.NaturalDuration.TimeSpan-TimeSpan.FromMilliseconds(180);};
    native.MediaFailed+=(s,e)=>{status=1;results.Add("FAIL: radio playback "+e.ErrorException.Message);finish();};
    radio.TrackChanged+=()=>{
     if(finished)return;int previous=visited.Last();visited.Add(settings.Track);
     if(previous==settings.Track||visited.Count==3&&visited.Distinct().Count()!=3){status=1;results.Add("FAIL: automatic shuffle repeated a track early");finish();return;}
     if(visited.Count==4){results.Add("PASS: three real MediaEnded events automatically advanced the shuffled radio, without adjacent repeats; order "+string.Join(" → ",visited));finish();}
    };timeout.Start();
   };
   Action next=null;next=()=>{timeout.Stop();if(files.Count==0){results.Insert(0,"PASS: Windows MediaPlayer decoded all three full tracks and five piano cues.");checkAutoAdvance();return;}current=files.Dequeue();player.Open(new Uri(AudioSystem.Resolve(current)));timeout.Start();};
   player.MediaOpened+=(s,e)=>{if(!player.NaturalDuration.HasTimeSpan||player.NaturalDuration.TimeSpan.TotalSeconds<=0){status=1;results.Add("FAIL: invalid duration "+current);finish();return;}results.Add(current+": "+player.NaturalDuration.TimeSpan.TotalSeconds.ToString("F2")+" seconds");player.Close();next();};
   player.MediaFailed+=(s,e)=>{status=1;results.Add("FAIL: "+current+" "+e.ErrorException.Message);finish();};timeout.Tick+=(s,e)=>{status=1;results.Add("FAIL: timed out opening "+current);finish();};
   app.Dispatcher.BeginInvoke(next);app.Run();return status;
  }
 }
}
