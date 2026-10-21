using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace LittleNotebook {
 public static class SpeechClips {
  static Dictionary<string,string> files;static readonly string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","speech");
  static Dictionary<string,string> Files{get{if(files==null){files=new Dictionary<string,string>();try{string path=Path.Combine(directory,"manifest.json");if(File.Exists(path))files=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(path))??files;}catch{}}return files;}}
  public static bool Available{get{return Files.Count>0;}}
  public static string Find(string text){string file;if(string.IsNullOrWhiteSpace(text)||!Files.TryGetValue(text.Trim().ToLowerInvariant(),out file)||string.IsNullOrWhiteSpace(file)||Path.GetFileName(file)!=file)return null;string path=Path.Combine(directory,file);return File.Exists(path)?path:null;}
 }
}
