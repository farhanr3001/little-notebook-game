using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace LittleNotebook {
 public class PaperDesign {
  public string PaperColor="#FFF8E9",LineColor="#CDD9D6",MarginColor="#C9736E";public double Spacing=37;public bool Lines=true,Margin=true;
  public List<string> Holes=Enumerable.Repeat("holes",11).ToList();
  public static PaperDesign ForTheme(string theme){var palette=new Palette(theme);return new PaperDesign{PaperColor=palette.Paper.ToString(),LineColor=palette.Line.ToString()};}
  public NotebookPage Decorations=new NotebookPage{Height=841};
 }
 public partial class MainWindow {
  PaperDesign paperDesignEditing;NotebookPage paperDesignDraft;
  void ApplyPaperDesign(){var design=Profile.Settings.Paper??new PaperDesign();Sketch.PaperSettings=design;try{p.Paper=(Color)ColorConverter.ConvertFromString(design.PaperColor);p.Line=(Color)ColorConverter.ConvertFromString(design.LineColor);}catch{}}
  void PaperDesignView(){PaperDesignView(new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<PaperDesign>(new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(Profile.Settings.Paper??new PaperDesign())));}
  void PaperDesignView(PaperDesign draft){
   if(draft.Holes==null)draft.Holes=Enumerable.Repeat("holes",11).ToList();while(draft.Holes.Count<11)draft.Holes.Add("holes");
   Modal(T("Background paper","تصميم ورقة الخلفية"),c=>{
    RefreshPaperPreview(c,draft);var reset=Button("↻",()=>PaperDesignView(PaperDesign.ForTheme(Profile.Settings.Theme)),40,39);reset.ToolTip=T("Reset to this theme’s default paper","استعادة ورقة المظهر الافتراضية");Put(c,reset,663,25);
    Label(c,T("Choose your paper, rules and each binding decoration.","اختر الورقة والخطوط والزينة عند كل ثقب."),96,102,17,600);
    string[] labels={T("Paper colour","لون الورقة"),T("Line colour","لون الخطوط"),T("Margin colour","لون الهامش")};string[] values={draft.PaperColor,draft.LineColor,draft.MarginColor};
    for(int i=0;i<3;i++){int index=i;Label(c,labels[i],99+i*201,151,16,194);var color=Button("",()=>NotebookColorWheel((Color)ColorConverter.ConvertFromString(values[index]),selected=>{if(index==0)draft.PaperColor=selected.ToString();if(index==1)draft.LineColor=selected.ToString();if(index==2)draft.MarginColor=selected.ToString();PaperDesignView(draft);}),184,43);color.Background=(Brush)new BrushConverter().ConvertFromString(values[i]);Put(c,color,98+i*201,185);}
    Btn(c,draft.Lines?T("Lines: on","الخطوط: نعم"):T("Lines: off","الخطوط: لا"),100,247,()=>{draft.Lines=!draft.Lines;PaperDesignView(draft);},202,42,draft.Lines);Btn(c,draft.Margin?T("Margin: on","الهامش: نعم"):T("Margin: off","الهامش: لا"),317,247,()=>{draft.Margin=!draft.Margin;PaperDesignView(draft);},202,42,draft.Margin);
    Label(c,T("Line spacing","المسافة بين الخطوط"),100,313,16,210);var space=PaperSlider(20,70,draft.Spacing,358);space.ValueChanged+=(s,e)=>{draft.Spacing=space.Value;RefreshPaperPreview(c,draft);};Put(c,space,328,306);
    Label(c,T("Binding decorations","زينة حافة الورقة"),99,366,19,590);Btn(c,T("All bows","أشرطة وردية"),100,405,()=>{draft.Holes=Enumerable.Repeat("bow",11).ToList();PaperDesignView(draft);},185,43);Btn(c,T("All holes","ثقوب عادية"),299,405,()=>{draft.Holes=Enumerable.Repeat("holes",11).ToList();PaperDesignView(draft);},185,43);Btn(c,T("None","بلا زينة"),500,405,()=>{draft.Holes=Enumerable.Repeat("none",11).ToList();PaperDesignView(draft);},185,43);
    for(int i=0;i<11;i++){int index=i;Button hole=null;hole=Button("",()=>OpenHolePicker(hole,draft,index),47,47);string kind=draft.Holes[i];hole.Content=kind=="bow"?(object)new Image{Source=Sketch.Asset("bow-emoji.png"),Width=30,Height=30}:kind=="none"?(object)Text("—",20,28,true):kind=="holes"?Text("○",25,28,true):kind.StartsWith("image:")?(object)new Image{Source=DecodeNotebookImage(kind.Substring(6)),Width=31,Height=31}:new Sketch(kind,p,31,31);hole.ToolTip=T("Binding position ","الموضع ")+(i+1);Put(c,hole,99+i*54,470);}
    Btn(c,T("Draw & decorate the paper…","ارسم وزيّن الورقة…"),99,552,()=>OpenPaperDecorationEditor(draft),590,53,true);
    Label(c,T("Drawing tools, text, stamps and colours are available in the editor.","تتوفر أدوات الرسم والنص والطوابع والألوان في المحرّر."),99,625,15,590);
    Btn(c,T("Cancel","إلغاء"),100,695,SettingsView,266,49);Btn(c,T("Save paper design","حفظ تصميم الورقة"),398,695,()=>{Profile.Settings.Paper=draft;Save();Build();SettingsView();},298,49,true);
   },790);
  }
  void OpenHolePicker(Button anchor,PaperDesign draft,int index){
   var panel=new WrapPanel{Width=250,Margin=new Thickness(7)};var popup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true};
   foreach(string kind in new[]{"holes","bow","star","heart","flower","none"}){string selected=kind;var button=Button("",()=>{draft.Holes[index]=selected;popup.IsOpen=false;PaperDesignView(draft);},75,58);button.Margin=new Thickness(3);button.Content=kind=="bow"?(object)new Image{Source=Sketch.Asset("bow-emoji.png"),Width=37,Height=37}:kind=="holes"?Text("○",28,42,true):kind=="none"?(object)Text("—",22,42,true):new Sketch(kind,p,37,37);button.ToolTip=kind;panel.Children.Add(button);}
   var import=Button(T("Import icon…","استيراد أيقونة…"),()=>{popup.IsOpen=false;var dialog=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.bmp"};if(dialog.ShowDialog(this)!=true)return;try{var source=new System.Windows.Media.Imaging.BitmapImage();source.BeginInit();source.UriSource=new Uri(dialog.FileName);source.DecodePixelWidth=128;source.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;source.EndInit();draft.Holes[index]="image:"+EncodeNotebookImage(source);PaperDesignView(draft);}catch(Exception ex){Message(T("Could not import icon","تعذّر استيراد الأيقونة"),ex.Message);}},239,39);panel.Children.Add(import);popup.Child=new Border{Child=panel,Background=Sketch.Textured(p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8)};popup.IsOpen=true;
  }
  void FinishPaperEditor(bool save){FlushNotebook();if(save){paperDesignEditing.Decorations=paperDesignDraft;Profile.Settings.Paper=paperDesignEditing;}paperDesignDraft=null;paperDesignEditing=null;notebookInk=null;notebookPageId="";Save();page="home";Build();SettingsView();}
 }
}
