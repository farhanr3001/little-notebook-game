using System;
using System.IO;

namespace LittleNotebook {
 public static class Diagnostics {
  static readonly object gate=new object();public static bool Testing;public static int ErrorCount;
  public static void Log(string context,Exception exception){
   lock(gate){
    ErrorCount++;
    string entry=DateTime.UtcNow.ToString("o")+" ["+context+"] "+exception+Environment.NewLine;
    string directory=Testing?Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output")):Path.Combine(AppPaths.DataDirectory??Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"),"logs");
    try{Directory.CreateDirectory(directory);string path=Path.Combine(directory,"errors.log");if(File.Exists(path)&&new FileInfo(path).Length>2097152)File.WriteAllText(path,"Previous log exceeded 2 MB."+Environment.NewLine);File.AppendAllText(path,entry);return;}catch{}
    try{File.AppendAllText(Path.Combine(Path.GetTempPath(),"LittleNotebook-error.txt"),entry);}catch{}
   }
  }
 }
}
