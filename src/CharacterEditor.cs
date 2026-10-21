using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;

namespace LittleNotebook {
 public partial class MainWindow {
  Button DangerButton(string label,Action action,double width=300,double height=48){var b=Button(label,action,width,height);b.Background=Sketch.Textured(Color.FromRgb(177,83,74));b.Foreground=Brushes.WhiteSmoke;return b;}
  void EditCharacter(){
   int draftCharacter=Profile.Character;string draftName=Profile.Name,draftPet=Profile.Pet,draftPetColor=Profile.PetColor;Action draw=null;
   draw=()=>Modal(T("Edit character","تعديل الشخصية"),c=>{
    bool boy=new[]{3,5,6,7}.Contains(draftCharacter);int[] choices=boy?new[]{3,5,6,7}:new[]{0,1,2,4,8,9};
    Btn(c,T("Boy","ولد"),219,105,()=>{draftCharacter=3;draw();},170,43,boy);Btn(c,T("Girl","بنت"),391,105,()=>{draftCharacter=0;draw();},170,43,!boy);
    Put(c,Avatar(draftCharacter,238,240),275,160);if(draftPet!="")Put(c,PetImage(draftPet,draftPetColor,85,83),456,315);
    Btn(c,"‹",175,248,()=>{draftCharacter=choices[(Array.IndexOf(choices,draftCharacter)+choices.Length-1)%choices.Length];draw();},63,61);
    Btn(c,"›",559,248,()=>{draftCharacter=choices[(Array.IndexOf(choices,draftCharacter)+1)%choices.Length];draw();},63,61);
    for(int i=0;i<choices.Length;i++){int index=choices[i];var b=Button("",()=>{draftCharacter=index;draw();},89,103,index==draftCharacter);b.Content=Avatar(index,92,72);AutomationProperties.SetName(b,T("Character ","الشخصية ")+(index+1));Put(c,b,(790-choices.Length*101)/2.0+i*101,414);}
    Label(c,T("Your name","اسمك"),94,556,19,126);
    var input=new TextBox{Text=draftName,Width=452,Height=45,MaxLength=24,FontFamily=new FontFamily(Ar?"Segoe UI":"Segoe Print"),FontSize=24*Profile.Settings.TextScale,Background=Brushes.Transparent,Foreground=B(p.Ink),BorderThickness=new Thickness(0),VerticalContentAlignment=VerticalAlignment.Center,FlowDirection=Ar?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
    AutomationProperties.SetName(input,T("Edit your name","تعديل الاسم"));var frame=Box(input,478,54,p.Paper);frame.Padding=new Thickness(12,2,12,2);Put(c,frame,218,545);
    Button pets=null;pets=Button(T("Pet: ","الحيوان: ")+(draftPet==""?T("None","بلا حيوان"):ProfileFeatures.Rewards.First(x=>x.Id==draftPet).Name)+" ▾",()=>EditPetDropdown(pets,draftPet,draftPetColor,(id,color)=>{draftPet=id;draftPetColor=color;draw();}),420,42);Put(c,pets,185,619);
    Btn(c,T("Cancel","إلغاء"),101,680,CloseModal,272,51);
    var confirm=Button(T("Confirm","تأكيد"),()=>{Profile.Character=draftCharacter;Profile.Name=draftName.Trim();Profile.Pet=draftPet;Profile.PetColor=draftPetColor;Save();Build();paused=false;lastTick=DateTime.UtcNow;},292,51,true);confirm.IsEnabled=!string.IsNullOrWhiteSpace(draftName);Put(c,confirm,403,680);
    input.TextChanged+=(s,e)=>{draftName=input.Text;confirm.IsEnabled=!string.IsNullOrWhiteSpace(draftName);};
   },790);draw();
  }
  void ConfirmResetCharacter(){
   Modal(T("Reset character?","إعادة ضبط الشخصية؟"),c=>{
    Label(c,T("This deletes this character's name, statistics, stars, purchases, pets and all notebook pages. You will return to character creation. Your language, theme and audio settings are kept.","سيُحذف اسم هذه الشخصية وإحصاءاتها ونجومها ومشترياتها وحيواناتها وكل صفحات دفترها. ستعود إلى إنشاء الشخصية. تُحفظ إعدادات اللغة والألوان والصوت."),96,107,20,598);
    Btn(c,T("Cancel","إلغاء"),103,308,SettingsView,266,52);
    Put(c,DangerButton(T("Delete progress & reset","حذف التقدّم وإعادة الضبط"),ResetCharacter,312,52),391,308);
   },424);
  }
  void ResetCharacter(){
   var fresh=ProfileFeatures.FreshCharacter(Profile.Settings);
   if(!store.Reset(fresh)){Message(T("Reset could not be saved","تعذّر حفظ إعادة الضبط"),store.Warning);return;}
   notebookInk=null;notebookPageId="";notebookSaveTimer.Stop();notebookUndo.Clear();notebookRedo.Clear();Profile=fresh;game=null;editingCharacter=false;shopOffset=0;audio.StopSpeech();page="welcome";paused=false;Build();
  }
 }
}
