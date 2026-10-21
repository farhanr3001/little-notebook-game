using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace LittleNotebook {
 public partial class MainWindow {
  bool updateChecking,updateInstalling,deliveryUpdateTest;
  static async Task<string> DownloadText(string url){ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;using(var web=new WebClient()){web.Headers[HttpRequestHeader.UserAgent]="LittleNotebook/"+AppInfo.Version;return await web.DownloadStringTaskAsync(url);}}
  async void CheckForUpdates(bool automatic){await CheckForUpdatesAsync(automatic);}
  async Task CheckForUpdatesAsync(bool automatic){
   if((Diagnostics.Testing&&!deliveryUpdateTest)||updateChecking)return;updateChecking=true;
   try{
    var fetch=DownloadText("https://api.github.com/repos/"+AppInfo.Repository+"/releases/latest");if(await Task.WhenAny(fetch,Task.Delay(12000))!=fetch)throw new TimeoutException(T("The update server did not respond. Try again later.","لم يستجب خادم التحديثات. حاول لاحقاً."));var release=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(await fetch);string tag=Convert.ToString(release["tag_name"]).TrimStart('v');Version latest,current;if(!Version.TryParse(tag,out latest)||!Version.TryParse(AppInfo.Version,out current))throw new InvalidDataException("Invalid release version.");
    if(latest<=current){if(!automatic)Message(T("You’re up to date","لديك أحدث إصدار"),T("Little Notebook ","دفتر صغير ")+AppInfo.Version+T(" is the latest version."," هو أحدث إصدار."));return;}
    var assets=((System.Collections.IEnumerable)release["assets"]).Cast<Dictionary<string,object>>();var asset=assets.FirstOrDefault(x=>Convert.ToString(x["name"])=="update-manifest.json");if(asset==null)throw new InvalidDataException("This release has no update manifest.");string manifestUrl=Convert.ToString(asset["browser_download_url"]);if(!UpdateFiles.TrustedUrl(manifestUrl))throw new InvalidDataException("Untrusted update location.");var manifest=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<ReleaseManifest>(await DownloadText(manifestUrl));if(manifest.version!=tag||manifest.repository!=AppInfo.Repository)throw new InvalidDataException("Release metadata does not match.");
    Modal(T("Update available","يتوفر تحديث"),c=>{Label(c,"Little Notebook "+tag,98,108,27,590);Label(c,T("Your character, progress, music and notebook will be kept.","سيُحتفظ بشخصيتك وتقدّمك وموسيقاك ودفترك."),98,166,19,590);var notes=Text(manifest.notes??"",16,570);var scroll=new ScrollViewer{Width=596,Height=133,Content=notes,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};StylePaperScroll(scroll);Put(c,scroll,98,235);Btn(c,T("Later","لاحقاً"),103,409,CloseModal,265,50);Btn(c,T("Update now","التحديث الآن"),399,409,()=>InstallUpdate(manifest),296,50,true);},507);
   }catch(Exception ex){if(!automatic)Message(T("Could not check for updates","تعذّر البحث عن تحديثات"),ex is WebException&&((WebException)ex).Response is HttpWebResponse&&((HttpWebResponse)((WebException)ex).Response).StatusCode==HttpStatusCode.NotFound?T("No published release was found. Please try again later.","لم يُعثر على إصدار منشور. حاول لاحقاً."):ex.Message);}finally{updateChecking=false;}
  }
  async void InstallUpdate(ReleaseManifest manifest){await InstallUpdateAsync(manifest);}
  async Task InstallUpdateAsync(ReleaseManifest manifest){
   if(updateInstalling)return;if(!AppPaths.Installed){Message(T("Install Little Notebook first","ثبّت اللعبة أولاً"),T("Automatic updates are available for the installed game. Run the setup installer from the release page first.","تتوفر التحديثات التلقائية للنسخة المثبّتة. شغّل برنامج التثبيت من صفحة الإصدار أولاً."));return;}updateInstalling=true;
   TextBlock status=null;Modal(T("Preparing update","جارٍ تحضير التحديث"),c=>{status=Label(c,T("Checking changed files…","فحص الملفات المتغيّرة…"),100,123,20,590);Label(c,T("Please keep this window open. Your saved data is separate.","اترك النافذة مفتوحة. بياناتك المحفوظة منفصلة."),100,202,17,590);},345);
   try{
    string root=AppPaths.InstallDirectory,stage=Path.Combine(AppPaths.DataDirectory,"updates",manifest.version+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);var changed=await Task.Run(()=>UpdateFiles.Changed(root,manifest));int index=0;var needed=new HashSet<string>(changed.Select(x=>x.pack));
    foreach(var pack in manifest.packs.Where(x=>needed.Contains(x.name))){if(!UpdateFiles.TrustedUrl(pack.url))throw new InvalidDataException("Untrusted update package.");status.Text=T("Downloading changed files ","تنزيل الملفات المتغيّرة ")+(++index)+" / "+needed.Count;string archive=Path.Combine(stage,pack.name);using(var web=new WebClient()){web.Headers[HttpRequestHeader.UserAgent]="LittleNotebook/"+AppInfo.Version;await web.DownloadFileTaskAsync(pack.url,archive);}if(new FileInfo(archive).Length!=pack.size||UpdateFiles.Hash(archive)!=pack.sha256)throw new InvalidDataException("Downloaded update did not pass verification.");
     using(var zip=ZipFile.OpenRead(archive))foreach(var file in changed.Where(x=>x.pack==pack.name)){var entry=zip.GetEntry(file.path.Replace('\\','/'));if(entry==null||entry.Length!=file.size)throw new InvalidDataException("Update file is missing.");string target=UpdateFiles.SafePath(Path.Combine(stage,"files"),file.path);Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target,true);if(UpdateFiles.Hash(target)!=file.sha256)throw new InvalidDataException("Update file hash mismatch.");}
    }
    var removed=new List<string>();string oldManifest=Path.Combine(root,"payload-manifest.json");if(File.Exists(oldManifest)){var previous=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<ReleaseManifest>(File.ReadAllText(oldManifest));var all=new HashSet<string>(manifest.files.Select(x=>x.path),StringComparer.OrdinalIgnoreCase);removed=previous.files.Where(x=>!all.Contains(x.path)).Select(x=>x.path).ToList();foreach(string path in removed)UpdateFiles.SafePath(root,path);}
    File.WriteAllText(Path.Combine(stage,"next-manifest.json"),new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(manifest));var request=new ApplyUpdateRequest{root=root,stage=stage,version=manifest.version,product=AppInfo.ProductId,data=AppPaths.DataDirectory,process=Process.GetCurrentProcess().Id,restart=!deliveryUpdateTest,files=changed,removed=removed};string requestFile=Path.Combine(stage,"request.json");File.WriteAllText(requestFile,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(request));string helper=Path.Combine(stage,"LittleNotebook.Update.exe");File.Copy(Path.Combine(root,"LittleNotebook.Update.exe"),helper,true);if(game!=null&&!game.Recorded)RecordSession(false);if(!Save())throw new IOException(store.Warning);status.Text=T("Installing update…","تثبيت التحديث…");Process.Start(new ProcessStartInfo{FileName=helper,Arguments="--apply \""+requestFile+"\"",UseShellExecute=false,CreateNoWindow=true});closing=true;Close();
   }catch(Exception ex){Message(T("Update could not be installed","تعذّر تثبيت التحديث"),ex.Message);}finally{updateInstalling=false;}
  }
 }
}
