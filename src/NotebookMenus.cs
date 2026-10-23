using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace LittleNotebook {
 public partial class MainWindow {
  Popup notebookPopup;
  FrameworkElement NotebookIcon(string key,double size=20){
   if(key=="colors")return ColourWheelIcon(size);
   var paths=new Dictionary<string,string>{
    {"zoom-in","M19,10 A8,8 0 1 1 3,10 A8,8 0 1 1 19,10 M17,16 L24,23 M7,10 L15,10 M11,6 L11,14"},{"zoom-out","M19,10 A8,8 0 1 1 3,10 A8,8 0 1 1 19,10 M17,16 L24,23 M7,10 L15,10"},
    {"paper","M4,1 L17,1 L24,8 L24,25 L4,25 Z M17,1 L17,8 L24,8 M8,13 L20,13 M8,17 L20,17 M8,21 L17,21"},
    {"pencil","M3,20 L17,3 L23,9 L9,25 L1,27 Z M3,20 L9,25 M17,3 L20,0 L26,6 L23,9"},
    {"pen","M3,24 L8,10 L20,2 L25,7 L16,19 Z M3,24 L13,14 M11,14 A2,2 0 1 1 15,14 A2,2 0 1 1 11,14"},
    {"brush","M3,24 Q10,25 13,17 L24,3 L20,0 L9,14 Q1,13 3,24 Z M9,14 L13,17"},
    {"highlighter","M2,23 L8,17 L18,3 L25,8 L15,23 Z M8,17 L15,23 M2,23 L9,27 L15,23"},
    {"eraser","M2,16 L13,2 Q15,0 18,2 L25,9 L13,24 L9,24 Z M8,9 L19,16 M13,24 L26,24"},
    {"fill","M3,10 L13,0 L25,12 L14,23 Z M3,10 L15,11 L25,12 M18,20 Q28,7 28,22 Q25,30 18,25 Z"},
    {"colors","M13,1 A12,12 0 1 1 1,13 A12,12 0 0 1 13,1 M13,1 L13,25 M1,13 L25,13 M5,5 L21,21 M21,5 L5,21"},
    {"ungroup","M1,1 L11,1 L11,11 L1,11 Z M15,15 L25,15 L25,25 L15,25 Z M17,4 L24,4 L24,11 M24,4 L16,12 M1,17 L1,24 L8,24 M1,24 L9,16"},
    {"cursor","M4,2 L4,21 L9,16 L13,23 L17,21 L13,14 L21,14 Z"},{"selection","M2,2 L22,2 L22,22 L2,22 Z M7,2 L7,5 M17,2 L17,5 M2,7 L5,7 M22,17 L19,17"},
    {"clear","M5,1 L16,1 L22,7 L22,23 L5,23 Z M16,1 L16,7 L22,7 M9,11 L18,20 M18,11 L9,20"},
    {"photo","M2,6 L7,6 L9,2 L17,2 L19,6 L24,6 L24,22 L2,22 Z M18,14 A5,5 0 1 1 8,14 A5,5 0 1 1 18,14 M20,9 L21,9"},
    {"preview","M19,10 A8,8 0 1 1 3,10 A8,8 0 1 1 19,10 M17,16 L24,23 M7,10 L15,10 M11,6 L11,14"},
    {"eye","M1,12 Q12,-1 24,12 Q12,25 1,12 Z M17,12 A5,5 0 1 1 7,12 A5,5 0 1 1 17,12"},
    {"group","M2,2 L14,2 L14,14 L2,14 Z M10,10 L23,10 L23,23 L10,23 Z"},
    {"shape","M2,3 L14,3 L14,16 L2,16 Z M17,9 L25,23 L9,23 Z"},
    {"stamp","M12,1 L16,8 L24,9 L18,15 L20,23 L12,19 L5,23 L6,15 L1,9 L9,8 Z"},
    {"bullet","M7,5 A2,2 0 1 1 3,5 A2,2 0 1 1 7,5 M11,5 L24,5 M7,17 A2,2 0 1 1 3,17 A2,2 0 1 1 7,17 M11,17 L24,17"}
   };
   return new System.Windows.Shapes.Path{Data=Geometry.Parse(paths.ContainsKey(key)?paths[key]:paths["shape"]),Width=size,Height=size,Stretch=Stretch.Uniform,Stroke=B(p.Ink),StrokeThickness=1.55,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round};
  }
  Button NotebookIconButton(string icon,string name,Action action,double width=44,double height=37){var button=Button("",action,width,height);button.ToolTip=name;AutomationProperties.SetName(button,name);if(width>75){var panel=new StackPanel{Orientation=Orientation.Horizontal};var image=NotebookIcon(icon,18);image.Margin=new Thickness(0,0,8,0);panel.Children.Add(image);panel.Children.Add(Text(name,15));button.Content=panel;}else button.Content=NotebookIcon(icon);return button;}
  Button NotebookDropdown(string icon,string name,string category,double width){
   Button button=null;button=Button("",()=>OpenNotebookDropdown(button,category),width,39);var panel=new StackPanel{Orientation=Orientation.Horizontal};var image=NotebookIcon(icon,16);image.Margin=new Thickness(0,0,5,0);panel.Children.Add(image);panel.Children.Add(Text(name+" ▾",14));button.Content=panel;AutomationProperties.SetName(button,name+" menu");return button;
  }
  string[] AvailableNotebookStamps(){var keys=new List<string>{"star","heart"};if(Profile.Owned.Contains("stamp:flowers"))keys.AddRange(new[]{"flower","butterfly"});if(Profile.Owned.Contains("stamp:drinks"))keys.Add("cup");if(Profile.Owned.Contains("stamp:animals"))keys.AddRange(new[]{"cat","rabbit","fish","bird"});return keys.ToArray();}
  FrameworkElement NotebookPickerPicture(string category,string key){
   if(category=="shapes"){var strokes=BuildNotebookShape(key,new Point(12,8),new Point(61,54));foreach(var stroke in strokes){stroke.DrawingAttributes.Color=p.Ink;stroke.DrawingAttributes.Width=1.8;stroke.DrawingAttributes.Height=1.8;}return new InkPresenter{Width=75,Height=63,Strokes=strokes};}
   if(category=="stamps")return new Sketch(key,p,58,58);
   if(category=="selection"){Shape shape=key=="ellipse"?(Shape)new Ellipse():new Rectangle();shape.Width=54;shape.Height=43;shape.Stroke=B(p.Ink);shape.StrokeThickness=1.6;shape.StrokeDashArray=new DoubleCollection{3,2};return shape;}
   return Text(key=="dash"?"−":"•",38,60,true);
  }
  void OpenNotebookDropdown(Button anchor,string category){
   FinishNotebookText();if(notebookPopup!=null)notebookPopup.IsOpen=false;
   var panel=new StackPanel{Width=category=="selection"||category=="bullet"?194:270,Margin=new Thickness(8)};var choices=new WrapPanel{Width=panel.Width};panel.Children.Add(choices);
   string[] keys=category=="shapes"?new[]{"line","box","circle","triangle","arrow","heart"}:category=="stamps"?AvailableNotebookStamps():category=="selection"?new[]{"rectangle","ellipse"}:new[]{"dot","dash"};
   foreach(string item in keys){string key=item;var button=Button("",()=>{notebookPopup.IsOpen=false;ConfigureNotebookTool((category=="shapes"?"shape:":category=="stamps"?"stamp:":category=="selection"?"select:":"bullet:")+key);},category=="selection"||category=="bullet"?91:84,70);button.Margin=new Thickness(2);button.Content=NotebookPickerPicture(category,key);button.ToolTip=key;AutomationProperties.SetName(button,category+" "+key);choices.Children.Add(button);}
   if(category=="shapes"||category=="stamps"){var more=Button(category=="shapes"?T("More shapes…","عرض الأشكال…"):T("More stamps…","عرض الطوابع…"),()=>{notebookPopup.IsOpen=false;if(category=="shapes")NotebookShapes();else NotebookStamps();},panel.Width-4,36);more.Margin=new Thickness(2,8,2,2);panel.Children.Add(more);}
   var border=new Border{Background=Sketch.Textured(p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(8,11,8,11),Child=panel,Margin=new Thickness(4)};
   notebookPopup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,VerticalOffset=3,Child=border,AllowsTransparency=true,StaysOpen=false,PopupAnimation=Profile.Settings.ReducedMotion?PopupAnimation.None:PopupAnimation.Fade};notebookPopup.IsOpen=true;
  }
  ContextMenu NotebookSelectionMenu(){
   var menu=new ContextMenu{Background=Sketch.Textured(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),FontFamily=new FontFamily(Ar?"Segoe UI":"Segoe Print"),FontSize=15};
   var group=new MenuItem{Header=T("Group selection","تجميع التحديد"),Icon=NotebookIcon("group",17)};group.Click+=(s,e)=>ToggleNotebookGrouping();menu.Items.Add(group);
   var ungroup=new MenuItem{Header=T("Ungroup","فك التجميع")};ungroup.Click+=(s,e)=>UngroupNotebookSelection();menu.Items.Add(ungroup);
   var delete=new MenuItem{Header=T("Delete selection","حذف التحديد"),Icon=NotebookIcon("clear",17)};delete.Click+=(s,e)=>DeleteNotebookSelection();menu.Items.Add(delete);
   menu.Opened+=(s,e)=>{group.Header=NotebookSelectionGrouped?T("Ungroup selection","فك تجميع التحديد"):T("Group selection","تجميع التحديد");group.Icon=NotebookIcon(NotebookSelectionGrouped?"ungroup":"group",17);group.IsEnabled=NotebookSelectionCount>=2;ungroup.IsEnabled=notebookInk.GetSelectedStrokes().Any(x=>NotebookStrokeGroup(x)!="")||NotebookSelectedTexts.Any(x=>!string.IsNullOrEmpty(((NotebookText)x.Tag).Group));delete.IsEnabled=NotebookSelectionCount>0;};return menu;
  }
  BitmapSource RenderNotebookPage(bool mask=false,bool includeTrace=true){
   FlushNotebook();var model=CurrentNotebookPage;var canvas=new Canvas{Width=940,Height=model.Height,Background=mask?Brushes.White:B(p.Paper),ClipToBounds=true};if(!mask)for(int y=30;y<model.Height;y+=30)Put(canvas,new Rectangle{Width=940,Height=1,Fill=B(p.Line),Opacity=.7},0,y);
   if(includeTrace&&model.TraceVisible){if(!string.IsNullOrEmpty(model.TraceInk))Put(canvas,new InkPresenter{Strokes=DecodeInk(model.TraceInk),Width=model.TraceWidth,Height=model.TraceHeight,Opacity=.38},Math.Max(0,(940-model.TraceWidth)/2),35);else if(model.Template!="none")Put(canvas,new TraceArt(model.Template,p,model.TraceWidth,model.TraceHeight),Math.Max(0,(940-model.TraceWidth)/2),35);}
   foreach(var fill in model.Images){var picture=new Image{Source=DecodeNotebookImage(fill.Png),Width=fill.Width,Height=fill.Height};Put(canvas,picture,fill.X,fill.Y);}
   Put(canvas,new InkPresenter{Width=940,Height=model.Height,Strokes=DecodeInk(model.Ink)},0,0);foreach(var text in model.Texts){var label=NotebookTextBlock(text);label.Width=text.Width;Put(canvas,label,text.X,text.Y);}
   canvas.Measure(new Size(940,model.Height));canvas.Arrange(new Rect(0,0,940,model.Height));canvas.UpdateLayout();double scale=mask?1:model.Height<=6000?1.5:1;var bitmap=new RenderTargetBitmap((int)(940*scale),(int)(model.Height*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(canvas);bitmap.Freeze();return bitmap;
  }
  void WriteNotebookImage(string filename){var bitmap=RenderNotebookPage();BitmapEncoder encoder=filename.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)||filename.EndsWith(".jpeg",StringComparison.OrdinalIgnoreCase)?(BitmapEncoder)new JpegBitmapEncoder{QualityLevel=95}:new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=new FileStream(filename,FileMode.Create,FileAccess.Write))encoder.Save(file);}
  void ExportNotebookPage(){
   FinishNotebookText();var dialog=new SaveFileDialog{Title=T("Save notebook page as an image","حفظ صفحة الدفتر كصورة"),FileName="Notebook-page-"+(Profile.NotebookIndex+1)+".png",Filter="PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg",DefaultExt=".png",AddExtension=true,OverwritePrompt=true};
   if(dialog.ShowDialog(this)!=true)return;try{WriteNotebookImage(dialog.FileName);if(notebookStatus!=null)notebookStatus.Text=T("Image saved: ","حُفظت الصورة: ")+System.IO.Path.GetFileName(dialog.FileName);}catch(Exception ex){Message(T("Could not save image","تعذّر حفظ الصورة"),ex.Message);}
  }
  void PreviewNotebookPage(){
   FinishNotebookText();var bitmap=RenderNotebookPage();Modal(T("Page preview","معاينة الصفحة"),c=>{
    var image=new Image{Source=bitmap,Width=940,Height=CurrentNotebookPage.Height,Stretch=Stretch.Fill,LayoutTransform=new ScaleTransform(.64,.64)};
    var scroll=new ScrollViewer{Width=652,Height=490,Content=image,Background=B(p.Desk),HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Put(c,scroll,84,103);
    Label(c,T("Zoom","تكبير"),94,614,17,78);var zoom=PaperSlider(.25,2,.64,310);var value=Label(c,"64%",509,614,16,85,true);zoom.ValueChanged+=(s,e)=>{image.LayoutTransform=new ScaleTransform(zoom.Value,zoom.Value);value.Text=Math.Round(zoom.Value*100)+"%";};Put(c,zoom,184,610);
    Put(c,NotebookIconButton("photo",T("Save image","حفظ الصورة"),ExportNotebookPage,225,47),95,673);Btn(c,T("Done","تم"),397,673,CloseModal,300,47,true);
   },758);
  }
 }
}
