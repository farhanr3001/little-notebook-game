using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;

namespace LittleNotebook {
 public static class UpdateEngine {
  public static string Ini(string file,string key){if(!File.Exists(file))return "";foreach(string line in File.ReadAllLines(file)){int i=line.IndexOf('=');if(i>0&&line.Substring(0,i).Trim()==key)return line.Substring(i+1).Trim();}return "";}
  public static void AtomicCopy(string from,string target){Directory.CreateDirectory(Path.GetDirectoryName(target));string temp=target+".update-new";using(var source=File.OpenRead(from))using(var dest=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){source.CopyTo(dest);dest.Flush(true);}try{if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{if(File.Exists(temp))try{File.Delete(temp);}catch{}}}
  public static void Apply(ApplyUpdateRequest request,bool test=false){
   string root=Path.GetFullPath(request.root).TrimEnd('\\'),stage=Path.GetFullPath(request.stage).TrimEnd('\\'),data=Path.GetFullPath(request.data).TrimEnd('\\');string marker=Path.Combine(root,"installed.ini");
   if(request.product!=AppInfo.ProductId||Ini(marker,"ProductId")!=AppInfo.ProductId)throw new InvalidDataException("This is not a Little Notebook installation.");string normalData=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LittleNotebook"),testData=Path.Combine(root,"user-data-test");if(!string.Equals(data,normalData,StringComparison.OrdinalIgnoreCase)&&!string.Equals(data,testData,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unexpected data location.");if(!stage.StartsWith(Path.Combine(data,"updates")+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unexpected staging location.");if(test&&!string.Equals(data,testData,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Test updates require an isolated installation.");
   if(!test){try{var parent=Process.GetProcessById(request.process);if(!string.Equals(parent.MainModule.FileName,Path.Combine(root,"LittleNotebook.exe"),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unexpected game process.");if(!parent.WaitForExit(60000))throw new IOException("The game is still open. Close it and try again.");}catch(ArgumentException){}}
   foreach(var file in request.files){UpdateFiles.SafePath(root,file.path);string source=UpdateFiles.SafePath(Path.Combine(stage,"files"),file.path);if(!File.Exists(source)||new FileInfo(source).Length!=file.size||UpdateFiles.Hash(source)!=file.sha256)throw new InvalidDataException("Staged file verification failed: "+file.path);}
   foreach(string relative in request.removed)UpdateFiles.SafePath(root,relative);
   string rollback=Path.Combine(stage,"rollback");Directory.CreateDirectory(rollback);var touched=new List<string>();var existed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);string oldIni=File.ReadAllText(marker);string manifest=Path.Combine(root,"payload-manifest.json");bool oldManifest=File.Exists(manifest);if(oldManifest)File.Copy(manifest,Path.Combine(rollback,"previous-manifest.json"),true);
   try{
    foreach(string relative in request.files.Select(x=>x.path).Concat(request.removed).Distinct(StringComparer.OrdinalIgnoreCase)){
     string target=UpdateFiles.SafePath(root,relative);if(File.Exists(target)){if((File.GetAttributes(target)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Linked files cannot be updated.");string backup=UpdateFiles.SafePath(rollback,relative);Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(target,backup,true);existed.Add(relative);}touched.Add(relative);
     var file=request.files.FirstOrDefault(x=>x.path==relative);if(file==null){if(File.Exists(target))File.Delete(target);}else AtomicCopy(UpdateFiles.SafePath(Path.Combine(stage,"files"),relative),target);
    }
    if(File.Exists(Path.Combine(stage,"next-manifest.json")))AtomicCopy(Path.Combine(stage,"next-manifest.json"),manifest);string[] ini=File.ReadAllLines(marker);bool found=false;for(int i=0;i<ini.Length;i++)if(ini[i].StartsWith("Version=")){ini[i]="Version="+request.version;found=true;}File.WriteAllText(marker,string.Join(Environment.NewLine,ini)+(found?"":Environment.NewLine+"Version="+request.version));File.AppendAllText(Path.Combine(data,"update.log"),DateTime.UtcNow.ToString("o")+" Updated to "+request.version+"; changed "+request.files.Count+" files; data preserved."+Environment.NewLine);
    try{string id=AppInfo.ProductId+(string.Equals(data,testData,StringComparison.OrdinalIgnoreCase)?"-Test":"")+"_is1";using(var registry=Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser,Microsoft.Win32.RegistryView.Registry64))using(var key=registry.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\"+id,true))if(key!=null){key.SetValue("DisplayVersion",request.version);key.SetValue("DisplayName","Little Notebook");}}catch{}
   }catch{
    foreach(string relative in touched.AsEnumerable().Reverse()){string target=UpdateFiles.SafePath(root,relative),backup=UpdateFiles.SafePath(rollback,relative);if(existed.Contains(relative)){try{AtomicCopy(backup,target);}catch{}}else if(File.Exists(target))File.Delete(target);}
    File.WriteAllText(marker,oldIni);if(oldManifest)AtomicCopy(Path.Combine(rollback,"previous-manifest.json"),manifest);else if(File.Exists(manifest))File.Delete(manifest);throw;
   }
  }
  public static void CleanManagedInstall(string root){root=Path.GetFullPath(root);if(Ini(Path.Combine(root,"installed.ini"),"ProductId")!=AppInfo.ProductId)return;string manifest=Path.Combine(root,"payload-manifest.json");if(!File.Exists(manifest))return;var data=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<ReleaseManifest>(File.ReadAllText(manifest));foreach(var file in data.files){string target=UpdateFiles.SafePath(root,file.path);if(Path.GetFileName(target)=="LittleNotebook.Update.exe")continue;try{if(File.Exists(target))File.Delete(target);}catch{}}}
 }
 public static class UpdateProgram {
  [STAThread]public static int Main(string[] args){
   AppInfo.SetIdentity(true);
   try{
    if(args.Length==2&&args[0]=="--uninstall-clean"){UpdateEngine.CleanManagedInstall(args[1]);return 0;}
    if(args.Length<2||args[0]!="--apply"&&args[0]!="--apply-test")return 2;var request=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<ApplyUpdateRequest>(File.ReadAllText(args[1]));bool test=args[0]=="--apply-test";UpdateEngine.Apply(request,test);if(!test&&request.restart)Process.Start(new ProcessStartInfo{FileName=Path.Combine(request.root,"LittleNotebook.exe"),WorkingDirectory=request.root,UseShellExecute=false});return 0;
   }catch(Exception ex){if(args.Contains("--apply-test")||args.Contains("--uninstall-clean")){Console.Error.WriteLine(ex);return 1;}MessageBox.Show("The update could not finish. Existing files were restored where possible. Your saved game data has not been replaced.\n\n"+ex.Message,"Little Notebook update");return 1;}
  }
 }
}
