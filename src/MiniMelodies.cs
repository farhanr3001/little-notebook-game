using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace LittleNotebook {
 public sealed class MiniMelody {
  public string Id,Title,Arabic;public int[] Notes;public double[] Beats;public double BeatSeconds;
  public double Duration{get{return Beats.Sum()*BeatSeconds+1.0;}}
 }
 public static class MiniMelodies {
  static readonly MiniMelody[] tunes=Load();
  static MiniMelody[] Load(){
   string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"content","minigame-melodies.json");
   var items=new JavaScriptSerializer().Deserialize<MiniMelody[]>(File.ReadAllText(file));
   if(items==null||items.Length==0||items.Any(t=>string.IsNullOrEmpty(t.Id)||t.Id.Any(c=>c<'a'||c>'z')||t.Notes==null||t.Beats==null||t.Notes.Length!=t.Beats.Length||t.Notes.Length<8||t.Notes.Any(n=>n<0||n>7)||t.Beats.Any(b=>b<=0||b>4)||t.BeatSeconds<.15||t.BeatSeconds>.6))throw new InvalidDataException("Invalid minigame melody data.");return items;
  }
  public static MiniMelody ForLevel(int level){return tunes[(Math.Max(1,level)-1)%tunes.Length];}
  public static MiniMelody[] All{get{return (MiniMelody[])tunes.Clone();}}
 }
}
