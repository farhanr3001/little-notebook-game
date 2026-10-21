using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace LittleNotebook {
 public partial class MainWindow {
  static readonly Guid NotebookGroupProperty=new Guid("248D2C63-B7F9-4A09-864F-476D93C69E2D");
  Border notebookEditing;bool notebookSelecting,notebookRefreshingTextTools,notebookBold;double notebookFontSize=24;
  Canvas notebookTextTools;Button notebookBoldButton;Slider notebookFontSlider;TextBlock notebookFontLabel;
  Dictionary<Border,double> notebookResizeFonts;Rect notebookResizeBounds;
  IEnumerable<Border> NotebookObjectFrames{get{return notebookInk==null?Enumerable.Empty<Border>():notebookInk.Children.OfType<Border>().Where(x=>x.Tag is NotebookText||x.Tag is NotebookImage);}}
  IEnumerable<Border> NotebookSelectedObjects{get{return notebookInk.GetSelectedElements().OfType<Border>().Where(x=>x.Tag is NotebookText||x.Tag is NotebookImage);}}
  IEnumerable<Border> NotebookTextFrames{get{return notebookInk==null?Enumerable.Empty<Border>():notebookInk.Children.OfType<Border>().Where(x=>x.Tag is NotebookText);}}
  IEnumerable<Border> NotebookSelectedTexts{get{return notebookInk.GetSelectedElements().OfType<Border>().Where(x=>x.Tag is NotebookText);}}
  Rect TextBounds(Border frame){return new Rect(double.IsNaN(InkCanvas.GetLeft(frame))?0:InkCanvas.GetLeft(frame),double.IsNaN(InkCanvas.GetTop(frame))?0:InkCanvas.GetTop(frame),frame.Width,frame.Height);}
  void SyncNotebookTexts(){foreach(var frame in NotebookObjectFrames){var r=TextBounds(frame);var text=frame.Tag as NotebookText;if(text!=null){text.X=r.X;text.Y=r.Y;text.Width=r.Width;text.Height=r.Height;}var image=frame.Tag as NotebookImage;if(image!=null){image.X=r.X;image.Y=r.Y;image.Width=r.Width;image.Height=r.Height;}}}
  string NotebookFrameGroup(Border frame){var text=frame.Tag as NotebookText;if(text!=null)return text.Group;var image=frame.Tag as NotebookImage;return image==null?"":image.Group;}
  void SetNotebookFrameGroup(Border frame,string group){var text=frame.Tag as NotebookText;if(text!=null)text.Group=group;var image=frame.Tag as NotebookImage;if(image!=null)image.Group=group;}
  Brush NotebookTextBrush(NotebookText text){try{return (Brush)new BrushConverter().ConvertFromString(text.Color);}catch{return B(p.Ink);}}
  TextBlock NotebookTextBlock(NotebookText text){return new TextBlock{Text=text.Text,FontFamily=new FontFamily(Regex.IsMatch(text.Text??"","[\u0600-\u06ff]")?"Segoe UI":"Segoe Print"),FontSize=text.FontSize,FontWeight=text.Bold?FontWeights.Bold:FontWeights.Normal,Foreground=NotebookTextBrush(text),TextWrapping=TextWrapping.Wrap,Padding=new Thickness(6),FlowDirection=Regex.IsMatch(text.Text??"","[\u0600-\u06ff]")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};}
  void RenderNotebookTexts(){notebookEditing=null;notebookInk.Children.Clear();foreach(var image in CurrentNotebookPage.Images)AddNotebookImageFrame(image);foreach(var text in CurrentNotebookPage.Texts)AddNotebookTextFrame(text);RefreshNotebookTextHitTesting();}
  Border AddNotebookTextFrame(NotebookText text){
   var frame=new Border{Tag=text,Width=text.Width,Height=text.Height,Background=Brushes.Transparent,Child=NotebookTextBlock(text),Focusable=true};InkCanvas.SetLeft(frame,text.X);InkCanvas.SetTop(frame,text.Y);notebookInk.Children.Add(frame);
   AutomationProperties.SetName(frame,T("Notebook text: ","نص الدفتر: ")+text.Text);
   frame.AddHandler(Mouse.PreviewMouseDownEvent,new MouseButtonEventHandler((s,e)=>{
    if(e.ChangedButton!=MouseButton.Left||notebookEditing==frame)return;
    if(e.ClickCount>=2){notebookInk.ReleaseMouseCapture();BeginNotebookTextEdit(frame);e.Handled=true;}
    else if(notebookTool=="text"){notebookInk.EditingMode=InkCanvasEditingMode.Select;notebookInk.Select(new StrokeCollection(),new UIElement[]{frame});e.Handled=true;}
   }),true);
   return frame;
  }
  void RefreshNotebookTextHitTesting(){foreach(var frame in NotebookObjectFrames)frame.IsHitTestVisible=notebookTool=="text"||notebookTool=="select"||notebookTool.StartsWith("select:")||frame==notebookEditing;}
  void AddNotebookText(Rect area){
   var text=new NotebookText{X=Math.Min(area.X,900),Y=area.Y,Width=Math.Max(40,Math.Min(940-area.X,area.Width)),Height=Math.Max(48,area.Height),FontSize=notebookFontSize,Bold=notebookBold,Color=notebookColor.ToString()};CurrentNotebookPage.Texts.Add(text);var frame=AddNotebookTextFrame(text);BeginNotebookTextEdit(frame,true);
  }
  void BeginNotebookTextEdit(Border frame,bool added=false){
   FinishNotebookText();if(!added)BeginNotebookChange();notebookInk.Select(new StrokeCollection());notebookInk.EditingMode=InkCanvasEditingMode.None;notebookTool="text";notebookEditing=frame;var text=(NotebookText)frame.Tag;
   var input=new TextBox{Text=text.Text,FontFamily=new FontFamily(Regex.IsMatch(text.Text,"[\u0600-\u06ff]")?"Segoe UI":"Segoe Print"),FontSize=text.FontSize,FontWeight=text.Bold?FontWeights.Bold:FontWeights.Normal,Foreground=NotebookTextBrush(text),Background=B(p.Paper),BorderThickness=new Thickness(0),Padding=new Thickness(6),TextWrapping=TextWrapping.Wrap,AcceptsReturn=true,AcceptsTab=false,MaxLength=10000,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,FlowDirection=Regex.IsMatch(text.Text,"[\u0600-\u06ff]")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};frame.Child=input;frame.BorderBrush=B(p.Accent);frame.BorderThickness=new Thickness(1);
   input.TextChanged+=(s,e)=>{text.Text=input.Text;var measure=NotebookTextBlock(text);measure.Measure(new Size(Math.Max(30,frame.Width-2),double.PositiveInfinity));frame.Height=Math.Max(48,measure.DesiredSize.Height+4);text.Height=frame.Height;if(text.Y+text.Height>CurrentNotebookPage.Height){CurrentNotebookPage.Height=Math.Min(12000,text.Y+text.Height+60);ResizeNotebookSurface();}notebookSaveTimer.Stop();notebookSaveTimer.Start();};
   input.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Enter&&(Keyboard.Modifiers&ModifierKeys.Shift)==0){FinishNotebookText();e.Handled=true;}else if(e.Key==Key.Escape){var snapshot=notebookPending;notebookEditing=null;notebookPending=null;if(snapshot!=null)RestoreNotebook(snapshot);e.Handled=true;}};
   input.LostKeyboardFocus+=(s,e)=>Dispatcher.BeginInvoke(new Action(()=>{if(notebookEditing==frame&&!input.IsKeyboardFocusWithin&&notebookInk!=null&&notebookInk.Children.Contains(frame))FinishNotebookText();}),System.Windows.Threading.DispatcherPriority.Background);
   RefreshNotebookTextHitTesting();RefreshTextTools();input.Focus();input.CaretIndex=input.Text.Length;
  }
  void FinishNotebookText(){
   if(notebookEditing==null)return;var frame=notebookEditing;notebookEditing=null;var text=(NotebookText)frame.Tag;var input=frame.Child as TextBox;if(input!=null)text.Text=input.Text;frame.BorderThickness=new Thickness(0);frame.Child=NotebookTextBlock(text);
   if(string.IsNullOrWhiteSpace(text.Text)){CurrentNotebookPage.Texts.Remove(text);notebookInk.Children.Remove(frame);}else{notebookTool="select";notebookInk.EditingMode=InkCanvasEditingMode.Select;notebookInk.Select(new StrokeCollection(),new UIElement[]{frame});}
   CommitNotebookChange();ConfigureNotebookTool(notebookTool);
  }
  IEnumerable<Border> FormattingNotebookTexts(){return notebookEditing!=null?new[]{notebookEditing}:NotebookSelectedTexts.ToArray();}
  void RefreshTextTools(){
   if(notebookTextTools==null)return;notebookRefreshingTextTools=true;var first=FormattingNotebookTexts().FirstOrDefault();if(first!=null){var model=(NotebookText)first.Tag;notebookFontSize=model.FontSize;notebookBold=model.Bold;}
   notebookBoldButton.Background=Sketch.Textured(notebookBold?p.Accent:p.Paper);notebookBoldButton.Foreground=B(notebookBold?p.AccentInk:p.Ink);notebookFontSlider.Value=Math.Min(72,notebookFontSize);notebookFontLabel.Text=((int)notebookFontSize).ToString();notebookRefreshingTextTools=false;
   if(notebookHelp!=null)notebookHelp.Text=notebookTool.StartsWith("shape:")||notebookTool.StartsWith("stamp:")?"":notebookTool.StartsWith("select")?T("Drag handles · double-click text to edit","اسحب المقابض · انقر النص مرتين للتعديل"):notebookTool=="eraser"?T("Erase inside the circle","امسح داخل الدائرة"):notebookTool=="text"?T("Drag a text box · Enter to finish","اسحب مربع نص · Enter للإنهاء"):T("Scroll down · add length when needed","مرّر للأسفل · أطِل الصفحة عند الحاجة");
  }
  void UpdateNotebookTextStyle(Border frame){var text=(NotebookText)frame.Tag;var input=frame.Child as TextBox;if(input!=null){input.FontSize=text.FontSize;input.FontWeight=text.Bold?FontWeights.Bold:FontWeights.Normal;}else frame.Child=NotebookTextBlock(text);var measure=NotebookTextBlock(text);measure.Measure(new Size(Math.Max(30,frame.Width-2),double.PositiveInfinity));frame.Height=Math.Max(frame.Height,measure.DesiredSize.Height+4);text.Height=frame.Height;if(text.Y+frame.Height>CurrentNotebookPage.Height){CurrentNotebookPage.Height=Math.Min(12000,text.Y+frame.Height+60);ResizeNotebookSurface();}}
  void ChangeNotebookFontSize(double size){if(notebookRefreshingTextTools)return;notebookFontSize=size;if(notebookFontLabel!=null)notebookFontLabel.Text=((int)size).ToString();if(notebookInk==null)return;var frames=FormattingNotebookTexts().ToArray();if(frames.Length==0)return;if(notebookPending==null)BeginNotebookChange();foreach(var frame in frames){((NotebookText)frame.Tag).FontSize=size;UpdateNotebookTextStyle(frame);}CommitNotebookChange();}
  void ToggleNotebookBold(){var frames=FormattingNotebookTexts().ToArray();notebookBold=frames.Length>0?!frames.All(x=>((NotebookText)x.Tag).Bold):!notebookBold;if(frames.Length>0){if(notebookPending==null)BeginNotebookChange();foreach(var frame in frames){((NotebookText)frame.Tag).Bold=notebookBold;UpdateNotebookTextStyle(frame);}CommitNotebookChange();}RefreshTextTools();}
  void SelectNotebookArea(Rect rect,bool ellipse){
   if(rect.Width<2||rect.Height<2){notebookInk.Select(new StrokeCollection());return;}
   StrokeCollection strokes;if(ellipse){var outline=Enumerable.Range(0,72).Select(i=>new Point(rect.X+rect.Width/2+Math.Cos(i*Math.PI/36)*rect.Width/2,rect.Y+rect.Height/2+Math.Sin(i*Math.PI/36)*rect.Height/2));strokes=notebookInk.Strokes.HitTest(outline,60);}else strokes=notebookInk.Strokes.HitTest(rect,60);
   var elements=NotebookObjectFrames.Where(frame=>{var r=TextBounds(frame);var center=new Point(r.X+r.Width/2,r.Y+r.Height/2);if(!ellipse)return rect.IntersectsWith(r);return Math.Pow((center.X-rect.X-rect.Width/2)/(rect.Width/2),2)+Math.Pow((center.Y-rect.Y-rect.Height/2)/(rect.Height/2),2)<=1;}).Cast<UIElement>().ToArray();
   notebookInk.EditingMode=InkCanvasEditingMode.Select;notebookInk.Select(strokes,elements);RefreshNotebookSelectionActions();
  }
  string NotebookStrokeGroup(Stroke stroke){return stroke.ContainsPropertyData(NotebookGroupProperty)?Convert.ToString(stroke.GetPropertyData(NotebookGroupProperty)):"";}
  void NotebookSelectionChanged(){
   if(notebookSelecting)return;notebookSelecting=true;
   try{var selected=notebookInk.GetSelectedStrokes();var elements=notebookInk.GetSelectedElements().ToList();var groups=new HashSet<string>(selected.Select(NotebookStrokeGroup).Concat(elements.OfType<Border>().Select(NotebookFrameGroup)).Where(x=>!string.IsNullOrEmpty(x)));
    foreach(var stroke in notebookInk.Strokes)if(groups.Contains(NotebookStrokeGroup(stroke))&&!selected.Contains(stroke))selected.Add(stroke);foreach(var frame in NotebookObjectFrames)if(groups.Contains(NotebookFrameGroup(frame))&&!elements.Contains(frame))elements.Add(frame);notebookInk.Select(selected,elements);
   }finally{notebookSelecting=false;}
   RefreshNotebookSelectionActions();RefreshTextTools();
  }
  int NotebookSelectionCount{get{return notebookInk.GetSelectedStrokes().Count+notebookInk.GetSelectedElements().Count;}}
  void GroupNotebookSelection(){
   if(NotebookSelectionCount<2){if(notebookStatus!=null)notebookStatus.Text=T("Select two or more items to group.","حدّد عنصرين أو أكثر لتجميعهما.");return;}
   BeginNotebookChange();string id=Guid.NewGuid().ToString("N");foreach(var stroke in notebookInk.GetSelectedStrokes()){if(stroke.ContainsPropertyData(NotebookGroupProperty))stroke.RemovePropertyData(NotebookGroupProperty);stroke.AddPropertyData(NotebookGroupProperty,id);}foreach(var frame in NotebookSelectedObjects)SetNotebookFrameGroup(frame,id);CommitNotebookChange();RefreshNotebookSelectionActions();
  }
  void UngroupNotebookSelection(){BeginNotebookChange();foreach(var stroke in notebookInk.GetSelectedStrokes())if(stroke.ContainsPropertyData(NotebookGroupProperty))stroke.RemovePropertyData(NotebookGroupProperty);foreach(var frame in NotebookSelectedObjects)SetNotebookFrameGroup(frame,"");CommitNotebookChange();RefreshNotebookSelectionActions();}
  void DeleteNotebookSelection(){if(NotebookSelectionCount==0)return;BeginNotebookChange();var selected=notebookInk.GetSelectedStrokes();var texts=NotebookSelectedObjects.ToArray();notebookInk.Select(new StrokeCollection());notebookInk.Strokes.Remove(selected);foreach(var frame in texts){if(frame.Tag is NotebookText)CurrentNotebookPage.Texts.Remove((NotebookText)frame.Tag);else CurrentNotebookPage.Images.Remove((NotebookImage)frame.Tag);notebookInk.Children.Remove(frame);}CommitNotebookChange();}
  void RefreshNotebookSelectionActions(){if(notebookActions==null)return;notebookActions.Children.Clear();if(NotebookSelectionCount<2||notebookEditing!=null)return;var rect=notebookInk.GetSelectionBounds();bool grouped=NotebookSelectionGrouped;var button=NotebookIconButton(grouped?"ungroup":"group",grouped?T("Ungroup selection","فك تجميع التحديد"):T("Group selection","تجميع التحديد"),ToggleNotebookGrouping,32,30);Put(notebookActions,button,Math.Max(4,Math.Min(904,rect.Right+9)),Math.Max(4,rect.Top-34));}
  void ResizeNotebookSelection(InkCanvasSelectionEditingEventArgs args){
   ResizeNotebookSelectionFonts(args.OldRectangle,args.NewRectangle);
  }
  void ResizeNotebookSelectionFonts(Rect oldRect,Rect newRect){
   if(notebookResizeFonts==null){BeginNotebookChange();notebookResizeBounds=oldRect;notebookResizeFonts=NotebookSelectedTexts.ToDictionary(x=>x,x=>((NotebookText)x.Tag).FontSize);}
   double factor=notebookResizeBounds.Width>1?newRect.Width/notebookResizeBounds.Width:1;foreach(var item in notebookResizeFonts){((NotebookText)item.Key.Tag).FontSize=Math.Max(10,Math.Min(100,item.Value*factor));UpdateNotebookTextStyle(item.Key);}
  }
  void FinishNotebookSelectionResize(){notebookResizeFonts=null;SyncNotebookTexts();CommitNotebookChange();RefreshNotebookSelectionActions();RefreshTextTools();}
 }
}
