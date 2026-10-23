using System;
using System.Collections.Generic;
using System.Linq;

namespace LittleNotebook {
 public class NotebookPage {
  public string Id=Guid.NewGuid().ToString("N"),Ink="",Template="none";
  public double Height=414;public bool TraceVisible=true;
  public List<NotebookText> Texts=new List<NotebookText>();
  public List<NotebookImage> Images=new List<NotebookImage>();
  public string TracePhoto="",TraceInk="";public double TraceWidth=330,TraceHeight=330;
 }
 public class NotebookImage {public string Id=Guid.NewGuid().ToString("N"),Png="",Group="";public double X,Y,Width,Height;}
 public class NotebookText {
  public string Id=Guid.NewGuid().ToString("N"),Text="",Color="#29384A",Group="";
  public double X,Y,Width=240,Height=54,FontSize=24;public bool Bold;
 }
 public class Reward {
  public string Id,Category,Name,NameAr,Art;public int Price;public string[] Colors;
 }
 public static class ProfileFeatures {
  public static List<Reward> Rewards=new List<Reward>{
   new Reward{Id="leaf",Category="stickers",Name="Leaf",NameAr="ورقة",Art="leaf",Price=0},
   new Reward{Id="star",Category="stickers",Name="Star",NameAr="نجمة",Art="star",Price=25},
   new Reward{Id="moon",Category="stickers",Name="Moon",NameAr="قمر",Art="moon",Price=40},
   new Reward{Id="paw",Category="stickers",Name="Paw",NameAr="بصمة قدم",Art="paw",Price=60},
   new Reward{Id="book",Category="stickers",Name="Book",NameAr="كتاب",Art="book",Price=65},
   new Reward{Id="sun",Category="stickers",Name="Sun",NameAr="شمس",Art="sun",Price=70},
   new Reward{Id="pet:cat",Category="pets",Name="Cat",NameAr="قطة",Art="cat",Price=120,Colors=new[]{"ginger","tuxedo","cream"}},
   new Reward{Id="pet:dog",Category="pets",Name="Dog",NameAr="كلب",Art="dog",Price=160,Colors=new[]{"tan"}},
   new Reward{Id="pet:rabbit",Category="pets",Name="Rabbit",NameAr="أرنب",Art="rabbit",Price=140,Colors=new[]{"white"}},
   new Reward{Id="pet:bird",Category="pets",Name="Bird",NameAr="عصفور",Art="bird",Price=100,Colors=new[]{"blue"}},
   new Reward{Id="stamp:flowers",Category="stamps",Name="Flowers & butterflies",NameAr="أزهار وفراشات",Art="flower",Price=85},
   new Reward{Id="stamp:drinks",Category="stamps",Name="Cups & drinks",NameAr="أكواب ومشروبات",Art="cup",Price=75},
   new Reward{Id="stamp:animals",Category="stamps",Name="Animal stamps",NameAr="طوابع الحيوانات",Art="cat",Price=110}
  };
  public static void Normalize(Profile p){
   MiniCatalog.Normalize(p);
   if(p.Notebook==null||p.Notebook.Count==0)p.Notebook=new List<NotebookPage>{new NotebookPage()};
   p.Notebook=p.Notebook.Where(x=>x!=null).ToList();if(p.Notebook.Count==0)p.Notebook.Add(new NotebookPage());
   foreach(var page in p.Notebook){if(string.IsNullOrEmpty(page.Id))page.Id=Guid.NewGuid().ToString("N");if(page.Ink==null)page.Ink="";if(page.Template==null)page.Template="none";page.Height=Math.Max(414,Math.Min(12000,double.IsNaN(page.Height)?414:page.Height));if(page.Images==null)page.Images=new List<NotebookImage>();if(page.Texts==null)page.Texts=new List<NotebookText>();page.Texts=page.Texts.Where(x=>x!=null).ToList();foreach(var text in page.Texts){if(string.IsNullOrEmpty(text.Id))text.Id=Guid.NewGuid().ToString("N");text.Text=text.Text??"";text.FontSize=Math.Max(10,Math.Min(100,text.FontSize));text.Width=Math.Max(40,Math.Min(940,text.Width));text.Height=Math.Max(30,Math.Min(12000,text.Height));text.X=Math.Max(0,Math.Min(900,text.X));text.Y=Math.Max(0,Math.Min(page.Height-30,text.Y));}}
   p.NotebookIndex=Math.Max(0,Math.Min(p.Notebook.Count-1,p.NotebookIndex));
   if(!Rewards.Any(x=>x.Category=="stickers"&&x.Id==p.Sticker))p.Sticker="leaf";
   var pet=Rewards.FirstOrDefault(x=>x.Category=="pets"&&x.Id==p.Pet);
   if(pet==null||!p.Owned.Contains(p.Pet))p.Pet="";
   else if(!pet.Colors.Contains(p.PetColor))p.PetColor=pet.Colors[0];
  }
  public static bool Purchase(Profile p,string id){var reward=Rewards.FirstOrDefault(x=>x.Id==id);if(reward==null||p.Owned.Contains(id)||p.Coins<reward.Price)return false;p.Coins-=reward.Price;p.Owned.Add(id);return true;}
  public static bool Equip(Profile p,string id,string color=null){
   var reward=Rewards.FirstOrDefault(x=>x.Id==id);if(reward==null||!p.Owned.Contains(id))return false;
   if(reward.Category=="stickers")p.Sticker=id;
   if(reward.Category=="pets"){p.Pet=id;p.PetColor=reward.Colors.Contains(color??"")?color:reward.Colors[0];}return true;
  }
  public static Profile FreshCharacter(Settings settings){return new Profile{Settings=settings};}
 }
}
