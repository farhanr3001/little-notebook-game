using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LittleNotebook {
 public class SmoothInkCanvas:InkCanvas {
  public event Action Starting,Committed;Stroke mouseStroke;Point filtered;
  public Func<Point,bool> PointerStarting;public Action<Point> PointerMoving,PointerEnding;bool externalGesture;
  protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e){
   if(PointerStarting!=null&&PointerStarting(e.GetPosition(this))){externalGesture=true;CaptureMouse();e.Handled=true;return;}
   if(EditingMode==InkCanvasEditingMode.Ink||EditingMode==InkCanvasEditingMode.EraseByPoint){if(Starting!=null)Starting();}
   if(EditingMode!=InkCanvasEditingMode.Ink||e.StylusDevice!=null){base.OnPreviewMouseLeftButtonDown(e);return;}
   filtered=e.GetPosition(this);mouseStroke=new Stroke(new StylusPointCollection(new[]{new StylusPoint(filtered.X,filtered.Y)}),DefaultDrawingAttributes.Clone());mouseStroke.DrawingAttributes.FitToCurve=false;Strokes.Add(mouseStroke);CaptureMouse();e.Handled=true;
  }
  void Append(Point raw,bool final){
   if(mouseStroke==null)return;raw.X=Math.Max(0,Math.Min(ActualWidth,raw.X));raw.Y=Math.Max(0,Math.Min(ActualHeight,raw.Y));double distance=(raw-filtered).Length;double alpha=final?1:distance>8?.82:.60;Point next=new Point(filtered.X+(raw.X-filtered.X)*alpha,filtered.Y+(raw.Y-filtered.Y)*alpha);if(distance<.7&&!final)return;int steps=1;
   for(int n=1;n<=steps;n++){double t=n/(double)steps;mouseStroke.StylusPoints.Add(new StylusPoint(filtered.X+(next.X-filtered.X)*t,filtered.Y+(next.Y-filtered.Y)*t));}filtered=next;if(mouseStroke.StylusPoints.Count>=96){var attr=mouseStroke.DrawingAttributes.Clone();mouseStroke=new Stroke(new StylusPointCollection(new[]{new StylusPoint(filtered.X,filtered.Y)}),attr);Strokes.Add(mouseStroke);}
  }
  protected override void OnPreviewMouseMove(MouseEventArgs e){if(PointerMoving!=null)PointerMoving(e.GetPosition(this));if(externalGesture){e.Handled=true;return;}if(mouseStroke!=null&&e.LeftButton==MouseButtonState.Pressed){Append(e.GetPosition(this),false);e.Handled=true;}else base.OnPreviewMouseMove(e);}
  protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e){if(externalGesture){externalGesture=false;if(PointerEnding!=null)PointerEnding(e.GetPosition(this));ReleaseMouseCapture();e.Handled=true;return;}if(mouseStroke!=null){Append(e.GetPosition(this),true);mouseStroke=null;ReleaseMouseCapture();e.Handled=true;if(Committed!=null)Committed();}else{base.OnPreviewMouseLeftButtonUp(e);if(EditingMode==InkCanvasEditingMode.EraseByPoint&&Committed!=null)Committed();}}
  protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e){base.OnStrokeCollected(e);if(Committed!=null)Committed();}
 }
 public partial class MainWindow {
  SmoothInkCanvas notebookInk;string notebookPageId="",notebookTool="pencil",notebookPending=null;Color notebookColor=Color.FromRgb(41,56,74);double notebookSize=3;
  DispatcherTimer notebookSaveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(550)};
  List<string> notebookUndo=new List<string>(),notebookRedo=new List<string>();Dictionary<string,Button> notebookTools=new Dictionary<string,Button>();
  Canvas notebookGuide;TextBlock notebookStatus;Point notebookShapeStart;
  static string EncodeInk(StrokeCollection strokes){if(strokes.Count==0)return "";using(var buffer=new MemoryStream()){strokes.Save(buffer,true);return Convert.ToBase64String(buffer.ToArray());}}
  static StrokeCollection DecodeInk(string encoded){if(string.IsNullOrEmpty(encoded))return new StrokeCollection();using(var buffer=new MemoryStream(Convert.FromBase64String(encoded)))return new StrokeCollection(buffer);}
  NotebookPage CurrentNotebookPage{get{return paperDesignDraft??Profile.Notebook[Profile.NotebookIndex];}}
  void FlushNotebook(){if(notebookInk==null||Profile==null)return;var model=paperDesignDraft!=null&&paperDesignDraft.Id==notebookPageId?paperDesignDraft:Profile.Notebook.FirstOrDefault(x=>x.Id==notebookPageId);if(model!=null){SyncNotebookTexts();model.Ink=EncodeInk(notebookInk.Strokes);}}
  string NotebookSnapshot(){FlushNotebook();return new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(CurrentNotebookPage);}
  void BeginNotebookChange(){notebookSaveTimer.Stop();notebookPending=NotebookSnapshot();}
  void CommitNotebookChange(){
   if(notebookPending!=null&&notebookPending!=NotebookSnapshot()){notebookUndo.Add(notebookPending);if(notebookUndo.Count>40)notebookUndo.RemoveAt(0);notebookRedo.Clear();}notebookPending=null;FlushNotebook();if(notebookStatus!=null)notebookStatus.Text=T("Saving…","جارٍ الحفظ…");notebookSaveTimer.Stop();notebookSaveTimer.Start();
  }
  void RestoreNotebook(string snapshot){string tool=notebookTool;var saved=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<NotebookPage>(snapshot);CurrentNotebookPage.Template=saved.Template;CurrentNotebookPage.TraceVisible=saved.TraceVisible;CurrentNotebookPage.Height=saved.Height;CurrentNotebookPage.Texts=saved.Texts??new List<NotebookText>();CurrentNotebookPage.Images=saved.Images??new List<NotebookImage>();CurrentNotebookPage.TraceInk=saved.TraceInk;CurrentNotebookPage.TraceWidth=saved.TraceWidth;CurrentNotebookPage.TraceHeight=saved.TraceHeight;notebookInk.Select(new StrokeCollection());notebookInk.Strokes=DecodeInk(saved.Ink);RenderNotebookTexts();ResizeNotebookSurface();RefreshNotebookGuide();FlushNotebook();ConfigureNotebookTool(tool);Save();}
  void NotebookHistory(bool redo){var source=redo?notebookRedo:notebookUndo;var destination=redo?notebookUndo:notebookRedo;if(source.Count==0)return;destination.Add(NotebookSnapshot());string state=source.Last();source.RemoveAt(source.Count-1);RestoreNotebook(state);}
  void ConfigureNotebookTool(string tool){
   if(notebookTool!=tool)FinishNotebookText();notebookTool=tool;if(notebookInk==null)return;
   if(tool!="select"&&!tool.StartsWith("select:"))notebookInk.Select(new StrokeCollection());
   notebookInk.EditingMode=tool=="eraser"?InkCanvasEditingMode.EraseByPoint:tool=="select"||tool.StartsWith("select:")?InkCanvasEditingMode.Select:tool.StartsWith("shape:")||tool.StartsWith("stamp:")||tool.StartsWith("bullet:")||tool=="text"||tool=="fill"?InkCanvasEditingMode.None:InkCanvasEditingMode.Ink;
   Color color=notebookColor;if(tool=="pencil")color.A=205;var width=tool=="brush"?notebookSize*2.5:tool=="highlighter"?notebookSize*5:notebookSize;
   notebookInk.DefaultDrawingAttributes=new DrawingAttributes{Color=color,Width=width,Height=tool=="highlighter"?width*.45:width,FitToCurve=true,IgnorePressure=true,IsHighlighter=tool=="highlighter",StylusTip=tool=="highlighter"?StylusTip.Rectangle:StylusTip.Ellipse};
   notebookInk.EraserShape=new EllipseStylusShape(notebookSize*3,notebookSize*3);notebookInk.UseCustomCursor=true;notebookInk.Cursor=new[]{"eraser","pencil","pen","brush","highlighter","fill"}.Contains(tool)||tool.StartsWith("bullet:")?Cursors.None:tool=="select"||tool.StartsWith("select:")?Cursors.Arrow:tool=="text"?Cursors.IBeam:Cursors.Cross;
   if(notebookGhost!=null)notebookGhost.Children.Clear();foreach(var entry in notebookTools){entry.Value.Background=Sketch.Textured(entry.Key==tool?p.Accent:p.Paper);entry.Value.Foreground=B(entry.Key==tool?p.AccentInk:p.Ink);}
   RefreshTextTools();RefreshNotebookTextHitTesting();
  }
  void SaveNotebookTimer(object sender,EventArgs e){notebookSaveTimer.Stop();bool saved=Save();if(notebookStatus!=null)notebookStatus.Text=saved?T("Saved locally","محفوظ محلياً"):T("Could not save. Please check the window title.","تعذّر الحفظ. راجع عنوان النافذة.");}
  void TurnNotebookPage(int direction){FlushNotebook();if(direction>0&&Profile.NotebookIndex==Profile.Notebook.Count-1)Profile.Notebook.Add(new NotebookPage());Profile.NotebookIndex=Math.Max(0,Math.Min(Profile.Notebook.Count-1,Profile.NotebookIndex+direction));Save();Build();if(!Profile.Settings.ReducedMotion)body.BeginAnimation(OpacityProperty,new DoubleAnimation(.45,1,TimeSpan.FromMilliseconds(160)));}
  void RefreshNotebookGuide(){if(notebookGuide==null)return;notebookGuide.Children.Clear();if(CurrentNotebookPage.TraceVisible){if(!string.IsNullOrEmpty(CurrentNotebookPage.TraceInk))Put(notebookGuide,new InkPresenter{Strokes=DecodeInk(CurrentNotebookPage.TraceInk),Width=CurrentNotebookPage.TraceWidth,Height=CurrentNotebookPage.TraceHeight,Opacity=.38},Math.Max(0,(940-CurrentNotebookPage.TraceWidth)/2),35);else if(CurrentNotebookPage.Template!="none")Put(notebookGuide,new TraceArt(CurrentNotebookPage.Template,p,CurrentNotebookPage.TraceWidth,CurrentNotebookPage.TraceHeight),Math.Max(0,(940-CurrentNotebookPage.TraceWidth)/2),35);}RefreshTraceEye();}
  void ConfirmResetNotebook(){Modal(T("Reset the whole notebook?","إعادة ضبط الدفتر كله؟"),c=>{Label(c,T("This deletes every notebook page, drawing, stamp and tracing template. Game progress, stars and rewards stay the same.","سيُحذف كل صفحات الدفتر ورسومه وطوابعه وقوالبه. لن يتغيّر تقدّم اللعبة أو النجوم أو المكافآت."),96,109,21,600);Btn(c,T("Cancel","إلغاء"),103,279,CloseModal,264,52);Put(c,DangerButton(T("Delete all pages","حذف كل الصفحات"),ResetNotebook,309,52),393,279);},394);}
  void ResetNotebook(){notebookSaveTimer.Stop();FlushNotebook();var previous=Profile.Notebook;int previousIndex=Profile.NotebookIndex;notebookInk=null;Profile.Notebook=new List<NotebookPage>{new NotebookPage()};Profile.NotebookIndex=0;if(!store.Reset(Profile)){Profile.Notebook=previous;Profile.NotebookIndex=previousIndex;Build();Message(T("Notebook reset could not be saved","تعذّر حفظ إعادة ضبط الدفتر"),store.Warning);return;}notebookPageId="";notebookUndo.Clear();notebookRedo.Clear();page="notebook";Build();paused=false;}
  void NotebookShapes(){Modal(T("Shapes","الأشكال"),c=>{string[] keys={"line","box","circle","triangle","arrow","heart"};string[] names={T("Line","خط"),T("Rectangle","مستطيل"),T("Circle","دائرة"),T("Triangle","مثلث"),T("Arrow","سهم"),T("Heart","قلب")};Label(c,T("Choose a shape, then drag on the page.","اختر شكلاً ثم اسحب على الصفحة."),96,102,18,595);for(int i=0;i<keys.Length;i++){string key=keys[i];Btn(c,names[i],102+(i%3)*204,166+(i/3)*77,()=>{CloseModal();ConfigureNotebookTool("shape:"+key);},186,55);}},370);}
  void NotebookStamps(){
   var keys=new List<string>{"star","heart"};if(Profile.Owned.Contains("stamp:flowers"))keys.AddRange(new[]{"flower","butterfly"});if(Profile.Owned.Contains("stamp:drinks"))keys.Add("cup");if(Profile.Owned.Contains("stamp:animals"))keys.AddRange(new[]{"cat","rabbit","fish","bird"});
   Modal(T("Notebook stamps","طوابع الدفتر"),c=>{Label(c,T("Choose a stamp, then click anywhere on the page. More packs are in the shop.","اختر طابعاً ثم اضغط على الصفحة. تتوفر مجموعات أخرى في المتجر."),96,103,17,596);for(int i=0;i<keys.Count;i++){string key=keys[i];var b=Button("",()=>{CloseModal();ConfigureNotebookTool("stamp:"+key);},115,105);b.Content=new Sketch(key,p,76,76);AutomationProperties.SetName(b,key+" stamp");Put(c,b,100+(i%5)*121,179+(i/5)*117);}},478);
  }
  void NotebookTemplates(){TracePicker();}
  void AddNotebookShape(string kind,Point start,Point end){notebookInk.Strokes.Add(BuildNotebookShape(kind,start,end));}
  StrokeCollection BuildNotebookShape(string kind,Point start,Point end){
   double left=Math.Min(start.X,end.X),top=Math.Min(start.Y,end.Y),w=Math.Abs(start.X-end.X),h=Math.Abs(start.Y-end.Y);var points=new StylusPointCollection();
   if(kind=="line"||kind=="arrow"){points.Add(new StylusPoint(start.X,start.Y));points.Add(new StylusPoint(end.X,end.Y));if(kind=="arrow"){double angle=Math.Atan2(end.Y-start.Y,end.X-start.X);points.Add(new StylusPoint(end.X-18*Math.Cos(angle-.5),end.Y-18*Math.Sin(angle-.5)));points.Add(new StylusPoint(end.X,end.Y));points.Add(new StylusPoint(end.X-18*Math.Cos(angle+.5),end.Y-18*Math.Sin(angle+.5)));}}
   else if(kind=="circle")for(int n=0;n<=64;n++)points.Add(new StylusPoint(left+w/2+Math.Cos(n*Math.PI/32)*w/2,top+h/2+Math.Sin(n*Math.PI/32)*h/2));
   else if(kind=="heart"){return BuildNotebookGeometry(Doodles.Paths["heart"],new Rect(left,top,w,h));}
   else if(kind=="triangle"){points.Add(new StylusPoint(left+w/2,top));points.Add(new StylusPoint(left+w,top+h));points.Add(new StylusPoint(left,top+h));points.Add(new StylusPoint(left+w/2,top));}
   else{points.Add(new StylusPoint(left,top));points.Add(new StylusPoint(left+w,top));points.Add(new StylusPoint(left+w,top+h));points.Add(new StylusPoint(left,top+h));points.Add(new StylusPoint(left,top));}
   var attributes=notebookInk.DefaultDrawingAttributes.Clone();attributes.IsHighlighter=false;attributes.FitToCurve=kind=="circle";return new StrokeCollection(new[]{new Stroke(points,attributes)});
  }
  void StampNotebook(string key,Point point){AddNotebookGeometry(Doodles.Paths[key],new Rect(Math.Max(0,Math.Min(844,point.X-48)),Math.Max(0,Math.Min(CurrentNotebookPage.Height-96,point.Y-48)),96,96));}
  void AddNotebookGeometry(string[] paths,Rect target){notebookInk.Strokes.Add(BuildNotebookGeometry(paths,target));}
  StrokeCollection BuildNotebookGeometry(string[] paths,Rect target){var strokes=new StrokeCollection();foreach(string path in paths){var geometry=Geometry.Parse(path).GetFlattenedPathGeometry(.5,ToleranceType.Absolute);foreach(var figure in geometry.Figures){var points=new StylusPointCollection();Func<Point,StylusPoint> convert=pt=>new StylusPoint(target.X+pt.X/160*target.Width,target.Y+pt.Y/160*target.Height);points.Add(convert(figure.StartPoint));foreach(var segment in figure.Segments){var poly=segment as PolyLineSegment;if(poly!=null)foreach(Point pt in poly.Points)points.Add(convert(pt));var line=segment as LineSegment;if(line!=null)points.Add(convert(line.Point));}if(figure.IsClosed)points.Add(convert(figure.StartPoint));if(points.Count>1){var attr=new DrawingAttributes{Color=notebookColor,Width=Math.Max(1.5,notebookSize),Height=Math.Max(1.5,notebookSize),FitToCurve=false,IgnorePressure=true};strokes.Add(new Stroke(points,attr));}}}return strokes;}
 }
}
