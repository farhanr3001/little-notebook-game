using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly:AssemblyVersion("0.5.2.0")]
[assembly:AssemblyFileVersion("0.5.2.0")]
namespace LittleNotebook {
 public static class AppInfo {public const string Version="0.5.2",ProductId="5C35749C-14DE-49BC-AED7-E462359A73A4",Repository="farhanr3001/little-notebook-game";public static void SetIdentity(bool updater=false){try{SetCurrentProcessExplicitAppUserModelID(updater?"Farhan.LittleNotebook.Update":"Farhan.LittleNotebook");}catch{}}[DllImport("shell32.dll")]static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)]string appId);}
 public static class AppPaths {
  public static string DataDirectory{get;private set;}
  public static string InstallDirectory{get{return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);}}
  public static bool Installed{get{return ReadInstallValue("ProductId")==AppInfo.ProductId;}}
  public static string ReadInstallValue(string key){string file=Path.Combine(InstallDirectory,"installed.ini");if(!File.Exists(file))return "";foreach(string line in File.ReadAllLines(file)){int split=line.IndexOf('=');if(split>0&&line.Substring(0,split).Trim()==key)return line.Substring(split+1).Trim();}return "";}
  public static void Initialize(bool testing){string local=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LittleNotebook"),portable=Path.Combine(InstallDirectory,"data");if(Installed&&!testing){string saved=ReadInstallValue("DataDirectory");string test=Path.Combine(InstallDirectory,"user-data-test");DataDirectory=string.Equals(saved,test,StringComparison.OrdinalIgnoreCase)?test:local;}else{DataDirectory=File.Exists(Path.Combine(portable,"profile.json"))?portable:local;if(testing||!Writable(DataDirectory))DataDirectory=portable;}Directory.CreateDirectory(DataDirectory);}
  static bool Writable(string directory){try{Directory.CreateDirectory(directory);string probe=Path.Combine(directory,".write-check-"+Guid.NewGuid().ToString("N"));File.WriteAllText(probe,"");File.Delete(probe);return true;}catch{return false;}}
  public static void FollowSave(string filename){DataDirectory=Path.GetDirectoryName(Path.GetFullPath(filename));}
  public static bool RestrictedToken(){try{int value,length;return GetTokenInformation(System.Security.Principal.WindowsIdentity.GetCurrent().Token,29,out value,4,out length)&&value!=0;}catch{return false;}}
  [DllImport("advapi32.dll",SetLastError=true)]static extern bool GetTokenInformation(IntPtr token,int informationClass,out int information,int length,out int returnedLength);
 }
}
