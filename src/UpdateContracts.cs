using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace LittleNotebook {
 public class ReleaseFile {public string path,sha256,pack;public long size;}
 public class ReleasePack {public string name,url,sha256;public long size;}
 public class ReleaseManifest {public string version,repository,notes;public List<ReleaseFile> files;public List<ReleasePack> packs;}
 public class ApplyUpdateRequest {public string root,stage,version,product,data;public int process;public bool restart=true;public List<ReleaseFile> files;public List<string> removed;}
 public static class UpdateFiles {
  public static string Hash(string path){using(var hash=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
  public static string SafePath(string root,string relative){if(string.IsNullOrWhiteSpace(relative)||relative.Contains(":")||relative.StartsWith("/")||relative.StartsWith("\\"))throw new InvalidDataException("Invalid update path.");string[] parts=relative.Replace('\\','/').Split('/');if(parts.Any(x=>x==".."||x=="."||x.Length==0)||(parts[0].Equals("data",StringComparison.OrdinalIgnoreCase)||parts[0].Equals("user-data-test",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Update attempted to change user data.");string top=parts[0];if(!new[]{"assets","content","speech-runtime"}.Contains(top)&&parts.Length!=1)throw new InvalidDataException("Unexpected update directory.");if(parts.Length==1&&!new[]{"LittleNotebook.exe","LittleNotebook.exe.config","LittleNotebook.Update.exe","README.md","PLAN.md","ASSETS.md","TESTING.md","CONTENT.md","STRESS_REPORT.md","UPDATES.md","payload-manifest.json","radio-palette-prompts.json"}.Contains(top))throw new InvalidDataException("Unexpected update file.");string resolved=Path.GetFullPath(Path.Combine(root,relative));if(!resolved.StartsWith(Path.GetFullPath(root).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update path escaped installation.");for(string at=Path.GetDirectoryName(resolved);at!=null&&at.StartsWith(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase);at=Path.GetDirectoryName(at))if(Directory.Exists(at)&&(File.GetAttributes(at)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Update cannot follow folder links.");return resolved;}
  public static bool TrustedUrl(string value){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Host=="github.com"&&uri.AbsolutePath.StartsWith("/"+AppInfo.Repository+"/releases/download/",StringComparison.Ordinal);}
  public static List<ReleaseFile> Changed(string root,ReleaseManifest manifest){if(manifest.repository!=AppInfo.Repository||manifest.files==null||manifest.packs==null)throw new InvalidDataException("Invalid release manifest.");var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var changed=new List<ReleaseFile>();foreach(var file in manifest.files){string local=SafePath(root,file.path);if(!names.Add(file.path)||file.sha256==null||file.sha256.Length!=64||file.size<0)throw new InvalidDataException("Invalid release file.");if(!File.Exists(local)||new FileInfo(local).Length!=file.size||Hash(local)!=file.sha256)changed.Add(file);}return changed;}
 }
}
