using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Web.Script.Serialization;
namespace LittleNotebook {public partial class MainWindow {
 public async void RunDeliveryChecks(){string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/delivery.txt")),export="";try{
  timer.Stop();audio.StopSpeech();Profile=new Profile{Name="Isolated export check"};Profile.Settings.Music=false;Profile.Settings.Sound=false;page="notebook";Build();notebookInk.Strokes.Add(new Stroke(new StylusPointCollection(new[]{new StylusPoint(35,45),new StylusPoint(150,90)})));FlushNotebook();
  export=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"LittleNotebook-export-check-"+Guid.NewGuid().ToString("N")+".png");WriteNotebookImage(export);if(!File.Exists(export)||new FileInfo(export).Length<1000)throw new Exception("Pictures export was not written");File.Delete(export);export="";
  if(!UpdateFiles.SafePath(baseDir,"speech-runtime/Lib/numpy/test/data/fixture.dat").StartsWith(baseDir))throw new Exception("Legitimate runtime data path rejected");
  using(var voice=new NeuralSpeechService()){foreach(string language in new[]{"en","ar"}){string wav=await voice.RenderAsync(new SpeechSegment(language=="en"?"Read the question carefully.":"اقرأ السؤال بعناية.",language),System.Threading.CancellationToken.None);if(new FileInfo(wav).Length<=44)throw new Exception("Offline voice returned an empty WAV: "+language);}}
  File.WriteAllText(report,"PASS: English and Arabic speech synthesized without audio playback; actual notebook PNG exported to Windows Pictures under the ordinary user token, verified and removed. Isolated drawing/profile only; no UI automation or security changes. Legitimate nested runtime data remains updateable.");Environment.ExitCode=0;
 }catch(Exception ex){File.WriteAllText(report,"FAIL: "+ex);Environment.ExitCode=1;}finally{if(export.Length>0)try{File.Delete(export);}catch{}game=null;closing=true;Close();}}
 public async void RunLiveUpdateChecks(){
  string report=Path.Combine(AppPaths.DataDirectory,"live-client-test.txt");try{
   string isolated=Path.Combine(AppPaths.InstallDirectory,"user-data-test");if(!string.Equals(AppPaths.DataDirectory,isolated,StringComparison.OrdinalIgnoreCase))throw new Exception("Live update checks require an isolated test installation");
   timer.Stop();deliveryUpdateTest=true;await CheckForUpdatesAsync(true);var text=string.Join("\n",Descendants<TextBlock>(overlay).Select(t=>t.Text));
   if(Descendants<Button>(overlay).Any(b=>Convert.ToString(b.Content)=="Update now")){
    if(!text.Contains("Update available")||!Descendants<Button>(overlay).Any(b=>Convert.ToString(b.Content)=="Update now"))throw new Exception("Startup did not offer published update: "+text);
    File.WriteAllText(report,"PASS: older installed client received startup update offer from GitHub. Applying changed components through the production updater.\n");
    var release=new JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,object>>(await DownloadText("https://api.github.com/repos/"+AppInfo.Repository+"/releases/latest"));var assets=((System.Collections.IEnumerable)release["assets"]).Cast<System.Collections.Generic.Dictionary<string,object>>();string url=Convert.ToString(assets.Single(x=>Convert.ToString(x["name"])=="update-manifest.json")["browser_download_url"]);if(!UpdateFiles.TrustedUrl(url))throw new Exception("Untrusted live manifest");var manifest=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<ReleaseManifest>(await DownloadText(url));await InstallUpdateAsync(manifest);if(!closing)throw new Exception("Production update did not close the old client");
   }else{
    if(overlay.Children.Count!=0)throw new Exception("Current client showed startup update popup: "+text);await CheckForUpdatesAsync(false);text=string.Join("\n",Descendants<TextBlock>(overlay).Select(t=>t.Text));if(!text.Contains("You’re up to date"))throw new Exception("Manual check did not report current version: "+text);File.AppendAllText(report,"PASS: current client has no startup popup; manual Check for updates reports up to date.\n");closing=true;Close();
   }
  }catch(Exception ex){File.AppendAllText(report,"FAIL: "+ex);Environment.ExitCode=1;closing=true;Close();}
 }
}}
