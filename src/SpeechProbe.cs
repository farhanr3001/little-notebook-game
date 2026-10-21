using System;
using System.IO;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace LittleNotebook {
 public static class SpeechProbe {
  public static int Run(){
   string output=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output/speech.txt"));Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,"Speech lifecycle probe starting. UI thread "+Thread.CurrentThread.ManagedThreadId+Environment.NewLine);
   object gate=new object();Action<string> log=message=>{lock(gate)File.AppendAllText(output,message+Environment.NewLine);};int result=0,steps=0,completed=0;AudioSystem audio=null;bool finished=false;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
   AppDomain.CurrentDomain.UnhandledException+=(s,e)=>log("FATAL: "+e.ExceptionObject);
   app.DispatcherUnhandledException+=(s,e)=>{log("FAIL dispatcher: "+e.Exception);result=1;e.Handled=true;app.Shutdown();};
   try{
    audio=new AudioSystem(new Settings{Music=false,Sound=false,Volume=0});
    if(!audio.SystemVoiceAvailable){log("SKIP: No installed English speech voice available.");audio.Dispose();return 2;}
    var native=(SpeechSynthesizer)typeof(AudioSystem).GetField("speech",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio);native.Volume=0;native.Rate=8;
    native.SpeakStarted+=(s,e)=>log("Speech started on thread "+Thread.CurrentThread.ManagedThreadId);
    native.SpeakCompleted+=(s,e)=>{Interlocked.Increment(ref completed);log("Speech completed/cancelled on thread "+Thread.CurrentThread.ManagedThreadId);};
    var clock=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
    clock.Tick+=(s,e)=>{
     try{
      steps++;
      if(steps<=50){audio.Speak(steps%2==0?"Read the book.":"This is a longer sentence to cancel while answering the listening question.");audio.StopSpeech();audio.Speak("Take a chair.");}
      if(steps>50&&steps<=80){audio.Speak("Drink some water.");audio.StopSpeech();audio.ApplyVolume();}
      if(steps==81){audio.Speak("A final question is being answered while closing.");audio.Dispose();audio.Dispose();}
      if(steps>=100){clock.Stop();finished=true;log(completed>0?"PASS: rapid replay, cancel, completion, volume updates and repeated dispose; "+completed+" speech completion events.":"FAIL: No completion events observed.");if(completed==0)result=1;app.Shutdown();}
     }catch(Exception ex){clock.Stop();result=1;log("FAIL: "+ex);app.Shutdown();}
    };clock.Start();app.Run();
   }catch(Exception ex){result=1;log("FAIL startup: "+ex);}finally{if(audio!=null&&!finished)try{audio.Dispose();}catch(Exception ex){result=1;log("FAIL cleanup: "+ex);}}
   return result;
  }
 }
}
