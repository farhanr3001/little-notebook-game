using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace LittleNotebook {
 public class NeuralSpeechService:IDisposable {
  readonly string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"speech-runtime");readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);Process worker;StreamWriter input;bool disposed;readonly object workerLock=new object();OwnedProcessJob workerJob;
  public int WorkerId{get{lock(workerLock)return worker==null?0:worker.Id;}}
  public bool Available(string language){return !disposed&&File.Exists(Path.Combine(directory,"python.exe"))&&File.Exists(Path.Combine(directory,"worker.py"))&&File.Exists(Path.Combine(directory,"models",language=="ar"?"ar_JO-kareem-medium.onnx":"kokoro-v1.0.int8.onnx"));}
  public string CacheDirectory{get{return Path.Combine(AppPaths.DataDirectory,"speech-cache");}}
  void Start(){lock(workerLock){if(worker!=null&&!worker.HasExited)return;if(disposed)throw new ObjectDisposedException("NeuralSpeechService");var start=new ProcessStartInfo{FileName=Path.Combine(directory,"python.exe"),Arguments="-u \""+Path.Combine(directory,"worker.py")+"\"",WorkingDirectory=directory,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};worker=new Process{StartInfo=start};worker.ErrorDataReceived+=(s,e)=>{};workerJob=new OwnedProcessJob();worker.Start();try{workerJob.Attach(worker);}catch{try{worker.Kill();worker.WaitForExit(2000);}catch{}worker.Dispose();worker=null;workerJob.Dispose();workerJob=null;throw;}input=new StreamWriter(worker.StandardInput.BaseStream,new UTF8Encoding(false)){AutoFlush=true};worker.BeginErrorReadLine();}}
  public async Task<string> RenderAsync(SpeechSegment segment,CancellationToken cancellation){
   if(!Available(segment.Language))throw new InvalidOperationException("The offline "+segment.Language+" voice files are missing.");string key;using(var hash=SHA256.Create())key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("v1|"+segment.Language+"|"+segment.Text))).Replace("-","");string path=Path.Combine(CacheDirectory,key+".wav");if(File.Exists(path))return path;
   await gate.WaitAsync(cancellation).ConfigureAwait(false);
   try{
    cancellation.ThrowIfCancellationRequested();if(File.Exists(path))return path;Directory.CreateDirectory(CacheDirectory);Start();var json=new JavaScriptSerializer();string request=json.Serialize(new{language=segment.Language,text=segment.Text,output=path});await input.WriteLineAsync(request).ConfigureAwait(false);await input.FlushAsync().ConfigureAwait(false);
    var response=worker.StandardOutput.ReadLineAsync();if(await Task.WhenAny(response,Task.Delay(45000)).ConfigureAwait(false)!=response){StopWorker();throw new TimeoutException("The speech voice took too long to respond.");}string line=await response.ConfigureAwait(false);cancellation.ThrowIfCancellationRequested();if(line==null)throw new IOException("The speech helper stopped unexpectedly.");var value=json.Deserialize<Dictionary<string,object>>(line);if(!value.ContainsKey("ok")||!(bool)value["ok"]||!File.Exists(path))throw new IOException(value.ContainsKey("error")?Convert.ToString(value["error"]):"Speech generation failed.");return path;
   }finally{gate.Release();}
  }
  void StopWorker(){lock(workerLock){var current=worker;worker=null;input=null;try{if(workerJob!=null){workerJob.Dispose();workerJob=null;}if(current!=null&&!current.HasExited){current.Kill();current.WaitForExit(3000);}}catch(InvalidOperationException){}finally{if(current!=null)current.Dispose();}}}
  public void Dispose(){lock(workerLock){if(disposed)return;disposed=true;StopWorker();}}
 }
}
