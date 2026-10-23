using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace LittleNotebook {
 public static class Program {
  static System.Threading.Mutex instance;
  [STAThread] public static int Main(string[] args) {
   AppInfo.SetIdentity();
   Diagnostics.Testing=Array.Exists(args,x=>x.EndsWith("-test")||x=="--smoke"||x=="--stress"||x=="--review");
   if(!Diagnostics.Testing&&AppPaths.Installed){bool created;instance=new System.Threading.Mutex(true,"Local\\LittleNotebook-"+AppInfo.ProductId,out created);if(!created){MessageBox.Show("Little Notebook is already open.","Little Notebook");return 0;}}
   AppDomain.CurrentDomain.UnhandledException+=(s,e)=>Diagnostics.Log("fatal background exception",e.ExceptionObject as Exception??new Exception(Convert.ToString(e.ExceptionObject)));
   bool tests=Array.IndexOf(args,"--self-test")>=0;
   if(tests)return SelfTests.Run();
   if(Array.IndexOf(args,"--make-icon")>=0){BookIcon.Create(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../assets")));return 0;}
   if(Array.IndexOf(args,"--audio-test")>=0)return AudioProbe.Run();
   if(Array.IndexOf(args,"--speech-test")>=0)return SpeechProbe.Run();
   if(Array.IndexOf(args,"--voice-test")>=0)return VoiceProbe.Run();
   try {
    var app=new Application();app.DispatcherUnhandledException+=(s,e)=>{Diagnostics.Log("UI exception",e.Exception);if(!Diagnostics.Testing)MessageBox.Show("Little Notebook had a problem. Your previously saved progress is safe.\n\n"+e.Exception.Message,"Little Notebook");e.Handled=true;};
    bool smoke=Array.IndexOf(args,"--smoke")>=0;
    bool stress=Array.IndexOf(args,"--stress")>=0;
    bool narration=Array.IndexOf(args,"--narration-test")>=0;
    bool update=Array.IndexOf(args,"--update-test")>=0;
    bool health=Array.IndexOf(args,"--health-test")>=0;bool delivery=Array.IndexOf(args,"--delivery-test")>=0;bool liveUpdate=Array.IndexOf(args,"--live-update-test")>=0;bool mediaLive=Array.IndexOf(args,"--media-live-test")>=0;bool media=Array.IndexOf(args,"--media-test")>=0||mediaLive;
    bool notebook=Array.IndexOf(args,"--notebook-test")>=0;bool revision=Array.IndexOf(args,"--revision-test")>=0;
    bool notebookReview=Array.IndexOf(args,"--notebook-review")>=0;
    bool refinements=Array.IndexOf(args,"--refinement-test")>=0;bool extraPuzzles=Array.IndexOf(args,"--extra-puzzles-test")>=0;bool learningAdditions=Array.IndexOf(args,"--learning-additions-test")>=0;bool minigames=Array.IndexOf(args,"--minigame-test")>=0;bool miniAudio=Array.IndexOf(args,"--audio-transition-test")>=0;
    var window=new MainWindow(refinements||extraPuzzles||learningAdditions||miniAudio||minigames||media||delivery||health||revision||notebookReview||notebook||update||narration||stress||smoke||Array.IndexOf(args,"--review")>=0,refinements?"refinements-profile.json":extraPuzzles?"extra-puzzles-profile.json":learningAdditions?"learning-additions-profile.json":miniAudio?"audio-transitions-profile.json":minigames?"minigames-profile.json":revision?"revision-profile.json":notebookReview?"notebook-preview-profile.json":notebook?"notebook-profile.json":update?"update-profile.json":narration?"narration-profile.json":stress?"stress-profile.json":smoke?"smoke-profile.json":"ui-profile.json");
    if(Diagnostics.Testing&&!notebookReview&&Array.IndexOf(args,"--review")<0){window.ShowActivated=false;window.WindowState=WindowState.Minimized;}
    if(minigames){window.ShowActivated=false;window.WindowState=WindowState.Minimized;window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunPlayPolishChecks()),DispatcherPriority.ApplicationIdle);}
    if(refinements)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunRefinementChecks()),DispatcherPriority.ApplicationIdle);
    if(extraPuzzles)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunExtraPuzzleChecks()),DispatcherPriority.ApplicationIdle);
    if(learningAdditions)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunLearningAdditionChecks()),DispatcherPriority.ApplicationIdle);
    if(miniAudio)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunMiniAudioChecks()),DispatcherPriority.ApplicationIdle);
    if(notebook||revision||health||delivery||liveUpdate)window.ShowActivated=false;
    if(delivery||liveUpdate||media)window.WindowState=WindowState.Minimized;
    if(stress||narration){window.ShowActivated=false;window.WindowState=WindowState.Minimized;}
    if(smoke)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunSmoke()),DispatcherPriority.ApplicationIdle);
    if(stress)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunStress()),DispatcherPriority.ApplicationIdle);
    if(narration)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunNarrationTests()),DispatcherPriority.ApplicationIdle);
    if(update)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunUpdateTests()),DispatcherPriority.ApplicationIdle);
    if(media)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunMediaChecks(mediaLive)),DispatcherPriority.ApplicationIdle);
    if(delivery)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunDeliveryChecks()),DispatcherPriority.ApplicationIdle);
    if(liveUpdate)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunLiveUpdateChecks()),DispatcherPriority.ApplicationIdle);
    if(health)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunHealthChecks()),DispatcherPriority.ApplicationIdle);
    if(revision)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunRevisionTests()),DispatcherPriority.ApplicationIdle);
    if(notebook)window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>window.RunNotebookTests()),DispatcherPriority.ApplicationIdle);
    if(notebookReview)window.Loaded+=(s,e)=>window.OpenNotebookReview();
    app.Run(window);return Environment.ExitCode;
   }catch(Exception ex){Diagnostics.Log("startup",ex);MessageBox.Show(ex.Message,"Little Notebook could not start");return 1;}
  }
 }
}
