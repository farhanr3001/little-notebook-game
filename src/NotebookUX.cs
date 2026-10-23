using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
namespace LittleNotebook {public partial class MainWindow {
 double notebookZoom=1;TextBlock notebookZoomLabel;Button traceStop,traceCompare;
 bool NotebookShortcutsAvailable(DependencyObject source){return page=="notebook"&&overlay.Children.Count==0&&notebookInk!=null&&notebookEditing==null&&!(source is TextBoxBase)&&!(source is PasswordBox)&&!(source is ComboBox)&&!(Keyboard.FocusedElement is TextBoxBase)&&!(Keyboard.FocusedElement is PasswordBox)&&!(Keyboard.FocusedElement is ComboBox)&&(notebookInk.ContextMenu==null||!notebookInk.ContextMenu.IsOpen)&&(notebookPopup==null||!notebookPopup.IsOpen);}
 bool NotebookKey(KeyEventArgs e){return NotebookShortcutsAvailable(e.OriginalSource as DependencyObject)&&UseNotebookShortcut(e.Key,Keyboard.Modifiers,e.IsRepeat);}
 bool UseNotebookShortcut(Key key,ModifierKeys modifiers,bool repeat){
  if(key==Key.Z&&(modifiers==ModifierKeys.Control||modifiers==(ModifierKeys.Control|ModifierKeys.Shift))){if(!repeat)NotebookHistory((modifiers&ModifierKeys.Shift)!=0);return true;}
  if(modifiers!=ModifierKeys.None)return false;
  string tool=key==Key.A?"select":key==Key.S?"select:"+Profile.Settings.NotebookArea:key==Key.D?"pencil":key==Key.F?"pen":key==Key.G?"brush":key==Key.Z?"highlighter":key==Key.X?"fill":key==Key.C?"eraser":null;
  if(tool==null)return false;if(!repeat){ConfigureNotebookTool(tool);notebookInk.Focus();}return true;
 }
 void SetNotebookZoom(double value,Point? anchor=null){
  if(notebookBoard==null||notebookScroll==null)return;value=Math.Max(.025,Math.Min(3,value));double old=notebookZoom;Point at=anchor??new Point(notebookScroll.ViewportWidth/2,notebookScroll.ViewportHeight/2);double x=(notebookScroll.HorizontalOffset+at.X)*value/old-at.X,y=(notebookScroll.VerticalOffset+at.Y)*value/old-at.Y;
  notebookZoom=value;notebookBoard.LayoutTransform=new ScaleTransform(value,value);notebookScroll.UpdateLayout();notebookScroll.ScrollToHorizontalOffset(Math.Max(0,x));notebookScroll.ScrollToVerticalOffset(Math.Max(0,y));if(notebookPan!=null&&notebookPan.Active)notebookPan.Rebase(Mouse.GetPosition(notebookScroll));if(notebookZoomLabel!=null)notebookZoomLabel.Text=Math.Round(value*100)+"%";
 }
 void FitNotebook(){SetNotebookZoom(Math.Min(938.0/940,347.0/CurrentNotebookPage.Height));notebookScroll.ScrollToHorizontalOffset(0);notebookScroll.ScrollToVerticalOffset(0);}
 void StopNotebookTrace(){FinishNotebookText();BeginNotebookChange();CurrentNotebookPage.Template="none";CurrentNotebookPage.TraceInk="";CurrentNotebookPage.TracePhoto="";RefreshNotebookGuide();CommitNotebookChange();}
 FrameworkElement ColourWheelIcon(double size){var canvas=new Canvas{Width=size,Height=size};Color[] colours={Colors.Coral,Colors.Gold,Colors.YellowGreen,Colors.Turquoise,Colors.CornflowerBlue,Colors.MediumPurple,Colors.HotPink,Colors.OrangeRed};
  for(int i=0;i<colours.Length;i++){double a=i*Math.PI/4,b=(i+1)*Math.PI/4,r=size*.46;Point center=new Point(size/2,size/2),from=center+new Vector(Math.Cos(a)*r,Math.Sin(a)*r),to=center+new Vector(Math.Cos(b)*r,Math.Sin(b)*r);var geometry=new StreamGeometry();using(var g=geometry.Open()){g.BeginFigure(center,true,true);g.LineTo(from,true,false);g.ArcTo(to,new Size(r,r),0,false,SweepDirection.Clockwise,true,false);}canvas.Children.Add(new Path{Data=geometry,Fill=B(colours[i]),IsHitTestVisible=false});}
  Put(canvas,new Ellipse{Width=size*.3,Height=size*.3,Fill=B(p.Paper),Stroke=B(p.Ink),StrokeThickness=.7},size*.35,size*.35);canvas.Children.Add(new Ellipse{Width=size,Height=size,Stroke=B(p.Ink),StrokeThickness=1});return canvas;
 }
 void OpenPenStyles(Button anchor){
  FinishNotebookText();if(notebookPopup!=null)notebookPopup.IsOpen=false;var panel=new StackPanel{Width=220,Margin=new Thickness(9)};
  string[] keys={"round","fine","chisel","soft"},names={T("Round pen","قلم دائري"),T("Fine liner","قلم دقيق"),T("Chisel nib","سن مشطوف"),T("Soft ink","حبر ناعم")};
  for(int i=0;i<keys.Length;i++){string key=keys[i];var button=Button("",()=>{Profile.Settings.NotebookPen=key;notebookPopup.IsOpen=false;ConfigureNotebookTool("pen");Save();},216,58,Profile.Settings.NotebookPen==key);var preview=new Canvas{Width=196,Height=48};Label(preview,names[i],5,1,14,184);var points=new StylusPointCollection();for(int n=0;n<30;n++)points.Add(new StylusPoint(10+n*6,34+Math.Sin(n*.34)*5));Put(preview,new InkPresenter{Strokes=new StrokeCollection{new Stroke(points,PenAttributes(key,3))}},0,0);button.Content=preview;button.Margin=new Thickness(0,2,0,2);panel.Children.Add(button);}
  notebookPopup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,AllowsTransparency=true,StaysOpen=false,Child=new Border{Background=Sketch.Textured(p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Child=panel}};notebookPopup.IsOpen=true;
 }
 DrawingAttributes PenAttributes(string key,double size){var a=new DrawingAttributes{Color=notebookColor,Width=Math.Max(.5,size*(key=="fine"?.55:key=="chisel"?2:1)),Height=Math.Max(.5,size*(key=="fine"?.55:key=="chisel"?.4:1)),IgnorePressure=true,FitToCurve=true};if(key=="chisel"){a.StylusTip=StylusTip.Rectangle;var matrix=Matrix.Identity;matrix.Rotate(-35);a.StylusTipTransform=matrix;}if(key=="soft"){var c=a.Color;c.A=150;a.Color=c;}return a;}
}}
