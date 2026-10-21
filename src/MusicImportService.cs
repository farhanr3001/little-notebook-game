using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace LittleNotebook {
 public class MusicImportProgress {public string Stage;public double? Percent;}
 public class MusicImportResult {public string File,Title;}
 public sealed class MusicImportService:IDisposable {
  readonly object sync=new object();Process worker;OwnedProcessJob job;string folder;bool disposed,completed;
  public int WorkerId{get{lock(sync)return worker==null?0:worker.Id;}}
  public static bool ValidLink(string text){Uri uri;return Uri.TryCreate(text,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Port==443&&uri.UserInfo.Length==0&&Array.IndexOf(new[]{"youtube.com","www.youtube.com","m.youtube.com","music.youtube.com","youtu.be","www.youtu.be","tiktok.com","www.tiktok.com","vm.tiktok.com","vt.tiktok.com"},uri.Host.ToLowerInvariant())>=0;}
  public async Task<MusicImportResult> DownloadAsync(string url,IProgress<MusicImportProgress> progress,CancellationToken token){
   if(!ValidLink(url))throw new ArgumentException("Paste a public HTTPS YouTube or TikTok link.");string runtime=Path.Combine(AppPaths.InstallDirectory,"speech-runtime");folder=Path.Combine(AppPaths.DataDirectory,"music",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var json=new JavaScriptSerializer();Process child;
   try{
    lock(sync){if(disposed)throw new ObjectDisposedException("MusicImportService");var start=new ProcessStartInfo{FileName=Path.Combine(runtime,"python.exe"),Arguments="-u \""+Path.Combine(runtime,"media_worker.py")+"\"",WorkingDirectory=runtime,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};start.EnvironmentVariables["DENO_DIR"]=Path.Combine(AppPaths.DataDirectory,"media-cache");start.EnvironmentVariables["TEMP"]=folder;start.EnvironmentVariables["TMP"]=folder;worker=child=new Process{StartInfo=start};job=new OwnedProcessJob();child.Start();job.Attach(child);child.ErrorDataReceived+=(s,e)=>{};child.BeginErrorReadLine();}
    using(token.Register(Stop)){using(var input=new StreamWriter(child.StandardInput.BaseStream,new UTF8Encoding(false))){await input.WriteLineAsync(json.Serialize(new{url=url,output=folder})).ConfigureAwait(false);await input.FlushAsync().ConfigureAwait(false);}MusicImportResult result=null;string error="";var timeout=Task.Delay(TimeSpan.FromMinutes(15));
     while(true){token.ThrowIfCancellationRequested();var read=child.StandardOutput.ReadLineAsync();if(await Task.WhenAny(read,timeout).ConfigureAwait(false)==timeout)throw new TimeoutException("The music download took too long. Try again.");string line=await read.ConfigureAwait(false);if(line==null)break;if(line.Length>65536)throw new InvalidDataException("Invalid downloader response.");var value=json.Deserialize<Dictionary<string,object>>(line);string kind=Convert.ToString(value["kind"]);if(kind=="progress"){object number;double? percent=value.TryGetValue("percent",out number)&&number!=null?(double?)Convert.ToDouble(number):null;if(progress!=null)progress.Report(new MusicImportProgress{Stage=Convert.ToString(value["stage"]),Percent=percent});}else if(kind=="done")result=new MusicImportResult{File=Convert.ToString(value["path"]),Title=Convert.ToString(value["title"])};else if(kind=="error")error=Convert.ToString(value["message"]);}
     await Task.Run(()=>child.WaitForExit(5000)).ConfigureAwait(false);token.ThrowIfCancellationRequested();if(child.ExitCode!=0||result==null)throw new IOException(error.Length>0?error:"The link could not be imported.");string path=Path.GetFullPath(result.File);if(!path.StartsWith(folder+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!path.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase)||!File.Exists(path))throw new InvalidDataException("Invalid imported audio path.");completed=true;return result;
    }
   }catch(Exception){if(token.IsCancellationRequested)throw new OperationCanceledException(token);throw;}finally{Stop();CleanPartial();}
  }
  void Stop(){lock(sync){if(job!=null){job.Dispose();job=null;}var child=worker;worker=null;if(child!=null)try{if(!child.HasExited){child.Kill();child.WaitForExit(3000);}}catch(InvalidOperationException){}finally{child.Dispose();}}}
  void CleanPartial(){if(completed||string.IsNullOrEmpty(folder))return;string owned=Path.Combine(AppPaths.DataDirectory,"music")+Path.DirectorySeparatorChar;if(Path.GetFullPath(folder).StartsWith(owned,StringComparison.OrdinalIgnoreCase))try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch{}}
  public void Dispose(){lock(sync){if(disposed)return;disposed=true;}Stop();CleanPartial();}
 }
}
