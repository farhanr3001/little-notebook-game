using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public static class PetArt {
  static BitmapSource atlas;static Dictionary<int,BitmapSource> cached=new Dictionary<int,BitmapSource>();
  public static BitmapSource Get(string id,string color){
   if(atlas==null)atlas=Sketch.Asset("pets.png");int index=id=="pet:cat"||id=="cat"?(color=="tuxedo"?1:color=="cream"?2:0):id.EndsWith("dog")?3:id.EndsWith("rabbit")?4:5;
   if(!cached.ContainsKey(index)){int[] edges={0,374,731,1097,1494,1838,2172};int left=edges[index],right=edges[index+1];cached[index]=CharacterArt.Trim(new CroppedBitmap(atlas,new Int32Rect(left,0,right-left,atlas.PixelHeight)));}return cached[index];
  }
 }
 public partial class MainWindow {
  double shopOffset=0;Dictionary<string,string> petColors=new Dictionary<string,string>();
  Image PetImage(string id,string color,double width,double height){return new Image{Source=PetArt.Get(id,color),Width=width,Height=height,Stretch=Stretch.Uniform};}
  string PetColor(Reward reward){string color;if(petColors.TryGetValue(reward.Id,out color))return color;return Profile.Pet==reward.Id?Profile.PetColor:reward.Colors[0];}
  string ColorName(string color){switch(color){case "ginger":return T("Ginger","برتقالي");case "tuxedo":return T("Black & white","أسود وأبيض");case "cream":return T("Cream","كريمي");case "tan":return T("Tan","بني فاتح");case "blue":return T("Blue","أزرق");default:return T("White","أبيض");}}
  void Shop(){
   Header(T("REWARDS","المكافآت"),T("Reward shop","متجر المكافآت"),T("Use earned stars for stickers, pets and notebook stamps.","استخدم نجومك لشراء ملصقات وحيوانات وطوابع للدفتر."));
   Label(body,"✦ "+Profile.Coins+T(" stars"," نجمة"),15,132,25,650);Label(body,T("Purchases are cosmetic. Earn stars by answering questions.","المشتريات للزينة. اكسب النجوم بالإجابة عن الأسئلة."),16,180,15,890);
   var content=new StackPanel{Width=912};
   foreach(string category in new[]{"stickers","pets","stamps"}){
    var title=Text(category=="stickers"?T("Stickers","الملصقات"):category=="pets"?T("Pets","الحيوانات الأليفة"):T("Notebook stamps","طوابع الدفتر"),25,890);title.Margin=new Thickness(0,5,0,15);content.Children.Add(title);
    var entries=ProfileFeatures.Rewards.Where(x=>x.Category==category).ToArray();
    for(int row=0;row<(entries.Length+3)/4;row++){
     var line=new Canvas{Width=912,Height=category=="pets"?330:258};
     for(int col=0;col<4;col++){int index=row*4+col;if(index>=entries.Length)break;var reward=entries[index];bool owned=Profile.Owned.Contains(reward.Id),equipped=category=="pets"?Profile.Pet==reward.Id&&Profile.PetColor==PetColor(reward):category=="stickers"?Profile.Sticker==reward.Id:owned;
      var card=new Canvas{Width=180,Height=category=="pets"?282:210};
      if(category=="pets")Put(card,PetImage(reward.Id,PetColor(reward),95,89),42,0);else Put(card,new Sketch(reward.Art,p,80,80),49,0);
      Label(card,Ar?reward.NameAr:reward.Name,0,94,18,180).TextAlignment=TextAlignment.Center;
      if(category=="pets"&&reward.Colors.Length>1){
       for(int n=0;n<reward.Colors.Length;n++){string color=reward.Colors[n];var choice=Button("",()=>{petColors[reward.Id]=color;Build();},31,28,PetColor(reward)==color);choice.ToolTip=ColorName(color);System.Windows.Automation.AutomationProperties.SetName(choice,ColorName(color)+" "+reward.Name);choice.Content=new System.Windows.Shapes.Ellipse{Width=15,Height=15,Stroke=B(p.Ink),StrokeThickness=1,Fill=color=="ginger"?Brushes.SandyBrown:color=="tuxedo"?Brushes.DimGray:Brushes.OldLace};Put(card,choice,37+n*38,129);}
      }
      Label(card,owned?T("Owned","تم الشراء"):"✦ "+reward.Price,0,category=="pets"?167:136,17,180).TextAlignment=TextAlignment.Center;
      if(category=="pets")Btn(card,T("Preview","معاينة"),3,203,()=>PreviewPet(reward,PetColor(reward)),174,37);
      string label=category=="stamps"?(owned?T("Ready in notebook","متاح في الدفتر"):T("Buy stamps","شراء الطوابع")):equipped?T("Equipped","مُستخدم"):owned?T("Equip","استخدام"):T("Buy & equip","شراء واستخدام");
      var buy=Button(label,()=>{if(!owned&&!ProfileFeatures.Purchase(Profile,reward.Id))return;ProfileFeatures.Equip(Profile,reward.Id,category=="pets"?PetColor(reward):null);Save();Build();},177,42,!equipped);buy.FontSize=15*Profile.Settings.TextScale;buy.IsEnabled=(!equipped||category=="pets"&&PetColor(reward)!=Profile.PetColor)&&(owned||Profile.Coins>=reward.Price);Put(card,buy,1,category=="pets"?254:179);
      Put(line,Box(card,211,category=="pets"?313:241,owned?p.Light:p.Paper),col*228,0);
     }content.Children.Add(line);
    }
   }
   if(!string.IsNullOrEmpty(Profile.Pet)){var remove=Button(T("Put pet away","إخفاء الحيوان"),()=>{Profile.Pet="";Save();Build();},280,45);remove.Margin=new Thickness(0,0,0,20);content.Children.Add(remove);}
   double restore=shopOffset;var scroll=new ScrollViewer{Width=947,Height=523,Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Visible,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly};StylePaperScroll(scroll);scroll.Loaded+=(s,e)=>scroll.ScrollToVerticalOffset(restore);scroll.ScrollChanged+=(s,e)=>{if(scroll.IsLoaded)shopOffset=scroll.VerticalOffset;};Put(body,scroll,8,218);
  }
  void PreviewPet(Reward reward,string color){Modal(T("Pet preview","معاينة الحيوان"),c=>{
   Put(c,Avatar(Profile.Character,267,240),249,108);Put(c,PetImage(reward.Id,color,106,96),422,280);
   Label(c,Profile.Name,157,397,24,473).TextAlignment=TextAlignment.Center;Label(c,(Ar?reward.NameAr:reward.Name)+" · "+ColorName(color),117,447,20,550).TextAlignment=TextAlignment.Center;
   Btn(c,T("Back","رجوع"),102,519,CloseModal,264,51);bool owned=Profile.Owned.Contains(reward.Id);var buy=Button(owned?T("Equip pet","استخدام الحيوان"):T("Buy & equip","شراء واستخدام")+(owned?"":"  ✦ "+reward.Price),()=>{if(!owned&&!ProfileFeatures.Purchase(Profile,reward.Id))return;ProfileFeatures.Equip(Profile,reward.Id,color);Save();Build();paused=false;},310,51,true);buy.IsEnabled=owned||Profile.Coins>=reward.Price;Put(c,buy,390,519);
  },632);}
 }
}
