using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LittleNotebook {
 public partial class MainWindow {
  Grid notebookBoard;Canvas notebookRules,notebookGhost,notebookActions,notebookSnapGuides;ScrollViewer notebookScroll;Button traceEye,notebookRemoveLength;TextBlock notebookHelp;
  bool notebookGesture;Point notebookPointer;Rect notebookDragBounds;
  void NotebookView(){
   ProfileFeatures.Normalize(Profile);Header(T("NOTEBOOK","دفتر الرسم"),paperDesignDraft!=null?T("Edit background paper","تعديل ورقة الخلفية"):T("My notebook","دفتري"),T("Draw, write and arrange your notes.","ارسم واكتب ورتّب ملاحظاتك."));
   if(notebookPageId!=CurrentNotebookPage.Id){notebookUndo.Clear();notebookRedo.Clear();}notebookPageId=CurrentNotebookPage.Id;
   notebookEditing=null;notebookTools.Clear();
   string[] tools={"pencil","pen","brush","highlighter","fill","eraser"};string[] names={T("Pencil","رصاص"),T("Pen","قلم"),T("Brush","فرشاة"),T("Highlight","تظليل"),T("Fill","تعبئة"),T("Eraser","ممحاة")};
   for(int i=0;i<tools.Length;i++){string tool=tools[i];var b=NotebookIconButton(tool,names[i],()=>ConfigureNotebookTool(tool),79,39);notebookTools[tool]=b;Put(body,b,138+i*83,126);}
   var select=NotebookIconButton("cursor",T("Select","تحديد"),()=>ConfigureNotebookTool("select"),44,39);notebookTools["select"]=select;Put(body,select,8,126);
   Put(body,NotebookDropdown("selection",T("Area","منطقة"),"selection",78),56,126);
   var text=Button(T("T  Text","T نص"),()=>ConfigureNotebookTool("text"),65,39);notebookTools["text"]=text;Put(body,text,636,126);
   Put(body,NotebookDropdown("bullet",T("Bullets","تعداد"),"bullet",73),705,126);
   Put(body,NotebookDropdown("shape",T("Shapes","أشكال"),"shapes",84),782,126);
   Put(body,NotebookDropdown("stamp",T("Stamps","طوابع"),"stamps",84),870,126);
   Color[] colors={Color.FromRgb(41,56,74),Color.FromRgb(119,161,112),Color.FromRgb(83,132,177),Color.FromRgb(184,108,105),Color.FromRgb(215,170,65),Color.FromRgb(150,122,179)};
   for(int i=0;i<colors.Length;i++){Color color=colors[i];var b=Button("",()=>{notebookColor=color;ConfigureNotebookTool(notebookTool);},27,29);b.Content=new Ellipse{Width=18,Height=18,Fill=B(color),Stroke=B(p.Ink),StrokeThickness=1};AutomationProperties.SetName(b,T("Drawing colour ","لون الرسم ")+(i+1));Put(body,b,9+i*31,179);}
   Put(body,NotebookIconButton("colors",T("Custom colour wheel","عجلة الألوان"),NotebookColorWheel,28,29),195,179);
   Label(body,T("Size","الحجم"),231,182,14,45);var size=PaperSlider(1,30,notebookSize,99);AutomationProperties.SetName(size,T("Drawing size","حجم الرسم"));size.ValueChanged+=(s,e)=>{notebookSize=size.Value;ConfigureNotebookTool(notebookTool);};Put(body,size,279,176);
   Btn(body,T("↶ Undo","↶ تراجع"),389,175,()=>NotebookHistory(false),86,37);Btn(body,T("Redo ↷","إعادة ↷"),482,175,()=>NotebookHistory(true),86,37);
   Put(body,NotebookIconButton("clear",T("Clear paper","مسح الصفحة"),ClearNotebookPage,137,37),576,175);
   Btn(body,T("Tracing…","تتبّع…"),721,175,NotebookTemplates,225,37);
   notebookTextTools=new Canvas{Width=310,Height=37};Put(body,notebookTextTools,8,223);
   notebookBoldButton=Button("B",ToggleNotebookBold,38,34);notebookBoldButton.FontWeight=FontWeights.Bold;notebookBoldButton.ToolTip=T("Bold text","نص عريض");Put(notebookTextTools,notebookBoldButton,0,0);
   Label(notebookTextTools,T("Text size","حجم النص"),49,6,13,80);notebookFontSlider=PaperSlider(10,72,notebookFontSize,123);AutomationProperties.SetName(notebookFontSlider,T("Text font size","حجم خط النص"));notebookFontSlider.ValueChanged+=(s,e)=>ChangeNotebookFontSize(notebookFontSlider.Value);Put(notebookTextTools,notebookFontSlider,133,-1);notebookFontLabel=Label(notebookTextTools,((int)notebookFontSize).ToString(),268,6,13,45,true);
   notebookHelp=Label(body,"",330,229,12,215);
   Btn(body,T("+ Add paper length","+ إطالة الورقة"),572,221,ExtendNotebookPage,202,37);
   notebookRemoveLength=Button("−",RemoveNotebookLength,40,37);notebookRemoveLength.ToolTip=T("Remove paper length","تقليل طول الورقة");AutomationProperties.SetName(notebookRemoveLength,T("Remove paper length","تقليل طول الورقة"));Put(body,notebookRemoveLength,779,221);
   Put(body,NotebookIconButton("paper",T("Full paper preview","معاينة الورقة كاملة"),PreviewNotebookPage,44,37),834,221);
   Put(body,NotebookIconButton("photo",T("Save page as image","حفظ الصفحة كصورة"),ExportNotebookPage,44,37),892,221);
   notebookBoard=new Grid{Width=940,Height=CurrentNotebookPage.Height,Background=B(p.Paper),ClipToBounds=true};
   notebookRules=new Canvas{Width=940,Height=CurrentNotebookPage.Height,IsHitTestVisible=false};notebookBoard.Children.Add(notebookRules);
   notebookGuide=new Canvas{Width=940,Height=CurrentNotebookPage.Height,IsHitTestVisible=false};notebookBoard.Children.Add(notebookGuide);
   notebookInk=new SmoothInkCanvas{Width=940,Height=CurrentNotebookPage.Height,Background=Brushes.Transparent,ClipToBounds=true,Strokes=DecodeInk(CurrentNotebookPage.Ink)};notebookInk.Starting+=()=>{FinishNotebookText();BeginNotebookChange();};notebookInk.Committed+=CommitNotebookChange;notebookBoard.Children.Add(notebookInk);
   notebookGhost=new Canvas{Width=940,Height=CurrentNotebookPage.Height,IsHitTestVisible=false};notebookBoard.Children.Add(notebookGhost);
   notebookSnapGuides=new Canvas{Width=940,Height=CurrentNotebookPage.Height,IsHitTestVisible=false};notebookBoard.Children.Add(notebookSnapGuides);
   notebookActions=new Canvas{Width=940,Height=CurrentNotebookPage.Height};notebookBoard.Children.Add(notebookActions);
   notebookInk.PointerStarting=NotebookPointerDown;notebookInk.PointerMoving=NotebookPointerMove;notebookInk.PointerEnding=NotebookPointerUp;
   notebookInk.MouseLeave+=(s,e)=>{if(!notebookGesture)notebookGhost.Children.Clear();};
   notebookInk.LostMouseCapture+=(s,e)=>{if(notebookGesture){notebookGesture=false;notebookPending=null;notebookGhost.Children.Clear();}};
   notebookInk.SelectionChanged+=(s,e)=>NotebookSelectionChanged();
   notebookInk.SelectionMoving+=(s,e)=>{if(notebookPending==null)BeginNotebookChange();e.NewRectangle=SnapNotebookMove(e.NewRectangle);};notebookInk.SelectionMoved+=(s,e)=>{ClearNotebookSnap();SyncNotebookTexts();CommitNotebookChange();RefreshNotebookSelectionActions();};
   notebookInk.SelectionResizing+=(s,e)=>ResizeNotebookSelection(e);notebookInk.SelectionResized+=(s,e)=>FinishNotebookSelectionResize();
   notebookInk.ContextMenu=NotebookSelectionMenu();
   notebookInk.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Delete&&notebookEditing==null){DeleteNotebookSelection();e.Handled=true;}if(e.Key==Key.Escape&&notebookEditing==null){notebookInk.Select(new StrokeCollection());e.Handled=true;}};
   RenderNotebookTexts();
   notebookScroll=new ScrollViewer{Width=958,Height=365,Content=notebookBoard,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly};StylePaperScroll(notebookScroll);
   var sheet=new Grid{Width=960,Height=367,ClipToBounds=true};sheet.Children.Add(notebookScroll);traceEye=NotebookIconButton("eye",T("Show or hide tracing","إظهار أو إخفاء القالب"),ToggleNotebookTrace,38,34);traceEye.HorizontalAlignment=HorizontalAlignment.Right;traceEye.VerticalAlignment=VerticalAlignment.Top;traceEye.Margin=new Thickness(0,10,30,0);sheet.Children.Add(traceEye);
   Put(body,new Border{Width=962,Height=369,Child=sheet,BorderBrush=B(p.Line),BorderThickness=new Thickness(1)},8,267);
   ResizeNotebookSurface();RefreshNotebookGuide();ConfigureNotebookTool(notebookTool);
   notebookStatus=Label(body,T("Saved locally","محفوظ محلياً"),13,646,12,490);
   Label(body,T("Drag to place · Shift+Enter: new text line","اسحب للوضع · Shift+Enter: سطر جديد"),505,646,12,440).TextAlignment=TextAlignment.Right;
   if(paperDesignDraft!=null){Btn(body,T("Cancel paper changes","إلغاء تغييرات الورقة"),35,695,()=>FinishPaperEditor(false),338,45);Btn(body,T("Save background paper","حفظ ورقة الخلفية"),519,695,()=>FinishPaperEditor(true),420,45,true);}else{
   var previous=Button(T("‹ Previous","‹ السابقة"),()=>TurnNotebookPage(-1),162,45);previous.IsEnabled=Profile.NotebookIndex>0;Put(body,previous,9,695);
   Label(body,T("Page ","صفحة ")+(Profile.NotebookIndex+1)+" / "+Profile.Notebook.Count,192,704,18,195).TextAlignment=TextAlignment.Center;
   Put(body,DangerButton(T("Reset notebook…","إعادة ضبط الدفتر…"),ConfirmResetNotebook,235,45),421,695);
   Btn(body,Profile.NotebookIndex==Profile.Notebook.Count-1?T("New page ›","صفحة جديدة ›"):T("Next page ›","الصفحة التالية ›"),722,695,()=>TurnNotebookPage(1),222,45,true);
   }
   notebookSaveTimer.Stop();notebookSaveTimer.Tick-=SaveNotebookTimer;notebookSaveTimer.Tick+=SaveNotebookTimer;
  }
  void ResizeNotebookSurface(){if(notebookBoard==null||notebookInk==null)return;double height=CurrentNotebookPage.Height;notebookBoard.Height=height;foreach(var element in new FrameworkElement[]{notebookInk,notebookRules,notebookGuide,notebookGhost,notebookActions,notebookSnapGuides})if(element!=null)element.Height=height;notebookRules.Children.Clear();for(int y=30;y<height;y+=30)Put(notebookRules,new Rectangle{Width=940,Height=1,Fill=B(p.Line),Opacity=.70},0,y);if(notebookRemoveLength!=null)notebookRemoveLength.IsEnabled=height>414;}
  void ExtendNotebookPage(){FinishNotebookText();BeginNotebookChange();double old=CurrentNotebookPage.Height;CurrentNotebookPage.Height=Math.Min(12000,old+600);ResizeNotebookSurface();CommitNotebookChange();notebookScroll.ScrollToVerticalOffset(Math.Max(0,old-90));}
  void ClearNotebookPage(){string tool=notebookTool;FinishNotebookText();BeginNotebookChange();notebookInk.Select(new StrokeCollection());notebookInk.Strokes.Clear();CurrentNotebookPage.Texts.Clear();CurrentNotebookPage.Images.Clear();RenderNotebookTexts();CurrentNotebookPage.Template="none";CurrentNotebookPage.TraceInk="";RefreshNotebookGuide();CommitNotebookChange();ConfigureNotebookTool(tool);}
  void ToggleNotebookTrace(){BeginNotebookChange();CurrentNotebookPage.TraceVisible=!CurrentNotebookPage.TraceVisible;RefreshNotebookGuide();CommitNotebookChange();}
  void RefreshTraceEye(){if(traceEye==null)return;traceEye.Visibility=CurrentNotebookPage.Template=="none"&&string.IsNullOrEmpty(CurrentNotebookPage.TraceInk)?Visibility.Collapsed:Visibility.Visible;traceEye.Opacity=CurrentNotebookPage.TraceVisible?1:.55;traceEye.ToolTip=CurrentNotebookPage.TraceVisible?T("Hide tracing outline","إخفاء خطوط التتبّع"):T("Show tracing outline","إظهار خطوط التتبّع");}
  Point ClampNotebookPoint(Point pt){return new Point(Math.Max(1,Math.Min(939,pt.X)),Math.Max(1,Math.Min(CurrentNotebookPage.Height-1,pt.Y)));}
  bool NotebookPointerDown(Point point){
   point=ClampNotebookPoint(point);notebookPointer=point;
   if(notebookTool=="fill"){FillNotebookAt(point);return true;}
   if(notebookTool=="text"&&NotebookTextFrames.Any(frame=>TextBounds(frame).Contains(point)))return false;
   if(notebookTool=="select"||notebookTool.StartsWith("select:")){
    var bounds=notebookInk.GetSelectionBounds();if(!bounds.IsEmpty){bounds.Inflate(13,13);if(bounds.Contains(point))return false;}
    if(notebookTool=="select")return false;
   }else if(!notebookTool.StartsWith("shape:")&&!notebookTool.StartsWith("stamp:")&&!notebookTool.StartsWith("bullet:")&&notebookTool!="text")return false;
   FinishNotebookText();notebookInk.Select(new StrokeCollection());BeginNotebookChange();notebookShapeStart=point;notebookGesture=true;NotebookPointerMove(point);return true;
  }
  void NotebookPointerMove(Point point){
   if(notebookGhost==null)return;point=ClampNotebookPoint(point);notebookPointer=point;notebookGhost.Children.Clear();
   if(new[]{"eraser","pencil","pen","brush","highlighter"}.Contains(notebookTool)){double size=notebookTool=="eraser"?notebookSize*3:notebookInk.DefaultDrawingAttributes.Width;double height=notebookTool=="eraser"?size:notebookInk.DefaultDrawingAttributes.Height;Shape tip=notebookTool=="highlighter"?(Shape)new Rectangle():new Ellipse();tip.Width=Math.Max(2,size);tip.Height=Math.Max(2,height);tip.Stroke=Brushes.Gray;tip.StrokeThickness=1;tip.Fill=new SolidColorBrush(Color.FromArgb(30,255,255,255));Put(notebookGhost,tip,point.X-tip.Width/2,point.Y-tip.Height/2);return;}
   if(notebookTool=="fill"){Put(notebookGhost,NotebookIcon("fill",24),point.X-12,point.Y-12);return;}
   if((notebookTool.StartsWith("shape:")||notebookTool.StartsWith("stamp:"))&&!notebookGesture){var instruction=Text(notebookTool.StartsWith("shape:")?T("Drag input to create shape","اسحب لإنشاء شكل"):T("Drag input to create stamp","اسحب لإنشاء طابع"),12,245);instruction.Background=B(p.Paper);Put(notebookGhost,instruction,Math.Max(4,Math.Min(690,point.X-112)),Math.Max(2,point.Y-36));return;}
   if(notebookTool.StartsWith("select:")||notebookTool=="text"){
    if(!notebookGesture)return;var rect=new Rect(notebookShapeStart,point);notebookDragBounds=rect;Shape outline=notebookTool=="select:ellipse"?(Shape)new Ellipse():new Rectangle();outline.Width=Math.Max(1,rect.Width);outline.Height=Math.Max(1,rect.Height);outline.Stroke=Brushes.Gray;outline.StrokeThickness=1.4;outline.StrokeDashArray=new DoubleCollection{4,3};outline.Fill=new SolidColorBrush(Color.FromArgb(22,105,125,113));Put(notebookGhost,outline,rect.X,rect.Y);return;
   }
   StrokeCollection preview=null;
   if(notebookTool.StartsWith("shape:")){var start=notebookGesture?notebookShapeStart:new Point(Math.Max(0,point.X-40),Math.Max(0,point.Y-40));var end=notebookGesture?point:new Point(Math.Min(940,point.X+40),Math.Min(CurrentNotebookPage.Height,point.Y+40));if((start-end).Length>.5)preview=BuildNotebookShape(notebookTool.Substring(6),start,end);}
   else if(notebookTool.StartsWith("stamp:")){var rect=NotebookStampRect(point);preview=BuildNotebookGeometry(Doodles.Paths[notebookTool.Substring(6)],rect);}
   else if(notebookTool.StartsWith("bullet:"))preview=BuildNotebookBullet(point,notebookTool=="bullet:dash");
   if(preview!=null){foreach(var stroke in preview){stroke.DrawingAttributes.Color=Color.FromArgb(145,90,96,98);stroke.DrawingAttributes.IsHighlighter=false;}notebookGhost.Children.Add(new InkPresenter{Strokes=preview,IsHitTestVisible=false});}
  }
  Rect NotebookStampRect(Point point){if(notebookGesture&&(point-notebookShapeStart).Length>6){var area=new Rect(notebookShapeStart,point);return new Rect(area.X,area.Y,Math.Max(12,area.Width),Math.Max(12,area.Height));}return new Rect(Math.Max(0,Math.Min(844,point.X-48)),Math.Max(0,Math.Min(CurrentNotebookPage.Height-96,point.Y-48)),96,96);}
  void NotebookPointerUp(Point point){
   point=ClampNotebookPoint(point);var kind=notebookTool;var stamp=NotebookStampRect(point);notebookGesture=false;notebookGhost.Children.Clear();
   if(kind.StartsWith("select:")){notebookPending=null;SelectNotebookArea(new Rect(notebookShapeStart,point),kind=="select:ellipse");return;}
   if(kind=="text"){var area=new Rect(notebookShapeStart,point);if(area.Width<40)area=new Rect(point.X,point.Y,Math.Min(240,940-point.X),54);AddNotebookText(area);return;}
   if(kind.StartsWith("shape:")&&(point-notebookShapeStart).Length>2)AddNotebookShape(kind.Substring(6),notebookShapeStart,point);
   else if(kind.StartsWith("stamp:"))AddNotebookGeometry(Doodles.Paths[kind.Substring(6)],stamp);
   else if(kind.StartsWith("bullet:"))notebookInk.Strokes.Add(BuildNotebookBullet(point,kind=="bullet:dash"));
   CommitNotebookChange();NotebookPointerMove(point);
  }
  StrokeCollection BuildNotebookBullet(Point point,bool dash){double size=Math.Max(4,notebookSize*2);var pts=new StylusPointCollection();pts.Add(new StylusPoint(point.X,point.Y));if(dash)pts.Add(new StylusPoint(Math.Min(938,point.X+size*3),point.Y));return new StrokeCollection(new[]{new Stroke(pts,new DrawingAttributes{Color=notebookColor,Width=size,Height=size,IgnorePressure=true})});}
 }
}
