using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LittleNotebook {
 public class HueWheel:FrameworkElement {
  public double Hue,Saturation=1,Value=1;public event Action<Color> Changed;BitmapSource wheel;
  public HueWheel(){Width=218;Height=218;Cursor=Cursors.Cross;}
  public static Color Hsv(double hue,double saturation,double value){double c=value*saturation,x=c*(1-Math.Abs(hue/60%2-1)),m=value-c;double r=0,g=0,b=0;if(hue<60){r=c;g=x;}else if(hue<120){r=x;g=c;}else if(hue<180){g=c;b=x;}else if(hue<240){g=x;b=c;}else if(hue<300){r=x;b=c;}else{r=c;b=x;}return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));}
  public Color Selected{get{return Hsv(Hue,Saturation,Value);}}
  public void SetColor(Color color){double max=Math.Max(color.R,Math.Max(color.G,color.B))/255.0,min=Math.Min(color.R,Math.Min(color.G,color.B))/255.0,d=max-min;Value=max;Saturation=max==0?0:d/max;Hue=d==0?0:max==color.R/255.0?60*(((color.G-color.B)/255.0/d)%6):max==color.G/255.0?60*((color.B-color.R)/255.0/d+2):60*((color.R-color.G)/255.0/d+4);if(Hue<0)Hue+=360;InvalidateVisual();}
  protected override void OnRender(DrawingContext dc){if(wheel==null){int n=218;byte[] pixels=new byte[n*n*4];for(int y=0;y<n;y++)for(int x=0;x<n;x++){double dx=x-109,dy=y-109,r=Math.Sqrt(dx*dx+dy*dy)/103;if(r>1)continue;double hue=Math.Atan2(dy,dx)*180/Math.PI;if(hue<0)hue+=360;Color color=Hsv(hue,r,1);int i=(y*n+x)*4;pixels[i]=color.B;pixels[i+1]=color.G;pixels[i+2]=color.R;pixels[i+3]=255;}wheel=BitmapSource.Create(n,n,96,96,PixelFormats.Bgra32,null,pixels,n*4);wheel.Freeze();}dc.DrawImage(wheel,new Rect(0,0,218,218));double a=Hue*Math.PI/180;dc.DrawEllipse(null,new Pen(Brushes.White,4),new Point(109+Math.Cos(a)*Saturation*103,109+Math.Sin(a)*Saturation*103),6,6);dc.DrawEllipse(null,new Pen(Brushes.DarkSlateGray,1.2),new Point(109+Math.Cos(a)*Saturation*103,109+Math.Sin(a)*Saturation*103),7,7);}
  void Pick(Point point){double dx=point.X-109,dy=point.Y-109;Hue=Math.Atan2(dy,dx)*180/Math.PI;if(Hue<0)Hue+=360;Saturation=Math.Min(1,Math.Sqrt(dx*dx+dy*dy)/103);InvalidateVisual();if(Changed!=null)Changed(Selected);}
  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){CaptureMouse();Pick(e.GetPosition(this));e.Handled=true;}
  protected override void OnMouseMove(MouseEventArgs e){if(IsMouseCaptured)Pick(e.GetPosition(this));}
  protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(IsMouseCaptured){Pick(e.GetPosition(this));ReleaseMouseCapture();e.Handled=true;}}
 }
 public partial class MainWindow {
  double? notebookSnapX,notebookSnapY;
  Rect SnapNotebookMove(Rect moved){
   if((Keyboard.Modifiers&ModifierKeys.Alt)!=0){ClearNotebookSnap();return moved;}
   var selected=notebookInk.GetSelectedStrokes();var elements=notebookInk.GetSelectedElements();var targets=notebookInk.Strokes.Where(x=>!selected.Contains(x)).Select(x=>x.GetBounds()).Concat(NotebookObjectFrames.Where(x=>!elements.Contains(x)).Select(TextBounds)).ToList();
   var xs=new List<double>{12,470,928};var ys=new List<double>{12,CurrentNotebookPage.Height/2,CurrentNotebookPage.Height-12};foreach(var r in targets){xs.AddRange(new[]{r.Left,r.Left+r.Width/2,r.Right});ys.AddRange(new[]{r.Top,r.Top+r.Height/2,r.Bottom});}
   double xdelta=FindNotebookSnap(new[]{moved.Left,moved.Left+moved.Width/2,moved.Right},xs,ref notebookSnapX),ydelta=FindNotebookSnap(new[]{moved.Top,moved.Top+moved.Height/2,moved.Bottom},ys,ref notebookSnapY);moved.X+=xdelta;moved.Y+=ydelta;
   moved.X=Math.Max(0,Math.Min(Math.Max(0,940-moved.Width),moved.X));moved.Y=Math.Max(0,Math.Min(Math.Max(0,CurrentNotebookPage.Height-moved.Height),moved.Y));
   if(notebookSnapGuides!=null){notebookSnapGuides.Children.Clear();if(notebookSnapX.HasValue)notebookSnapGuides.Children.Add(new Line{X1=notebookSnapX.Value,X2=notebookSnapX.Value,Y1=0,Y2=CurrentNotebookPage.Height,Stroke=B(p.Accent),StrokeThickness=1,StrokeDashArray=new DoubleCollection{4,3}});if(notebookSnapY.HasValue)notebookSnapGuides.Children.Add(new Line{X1=0,X2=940,Y1=notebookSnapY.Value,Y2=notebookSnapY.Value,Stroke=B(p.Accent),StrokeThickness=1,StrokeDashArray=new DoubleCollection{4,3}});}return moved;
  }
  double FindNotebookSnap(double[] edges,List<double> targets,ref double? sticky){if(sticky.HasValue){double saved=sticky.Value;double nearest=edges.OrderBy(x=>Math.Abs(saved-x)).First();if(Math.Abs(sticky.Value-nearest)<=11)return sticky.Value-nearest;sticky=null;}double best=7,delta=0;foreach(double target in targets)foreach(double edge in edges){double distance=Math.Abs(target-edge);if(distance<best){best=distance;delta=target-edge;sticky=target;}}return delta;}
  void ClearNotebookSnap(){notebookSnapX=null;notebookSnapY=null;if(notebookSnapGuides!=null)notebookSnapGuides.Children.Clear();}
  bool NotebookSelectionGrouped{get{return NotebookSelectionCount>0&&notebookInk.GetSelectedStrokes().All(x=>NotebookStrokeGroup(x)!="")&&notebookInk.GetSelectedElements().OfType<Border>().All(x=>NotebookFrameGroup(x)!="");}}
  void ToggleNotebookGrouping(){if(NotebookSelectionGrouped)UngroupNotebookSelection();else GroupNotebookSelection();}
  void RemoveNotebookLength(){
   if(CurrentNotebookPage.Height<=414)return;double target=Math.Max(414,CurrentNotebookPage.Height-600);bool crop=notebookInk.Strokes.Any(x=>x.GetBounds().Bottom>target)||NotebookObjectFrames.Any(x=>TextBounds(x).Bottom>target);
   Action apply=()=>{string tool=notebookTool;FinishNotebookText();BeginNotebookChange();CurrentNotebookPage.Height=target;notebookInk.Strokes.Clip(new Rect(0,0,940,target));CurrentNotebookPage.Texts.RemoveAll(x=>x.Y>=target);CurrentNotebookPage.Images.RemoveAll(x=>x.Y>=target);RenderNotebookTexts();ResizeNotebookSurface();CommitNotebookChange();ConfigureNotebookTool(tool);if(overlay.Children.Count>0)CloseModal();};
   if(!crop){apply();return;}Modal(T("Shorten this paper?","تقليل طول الورقة؟"),c=>{Label(c,T("Notes below the new bottom edge will be cropped. You can undo this change.","سيُقصّ المحتوى أسفل الحافة الجديدة. يمكنك التراجع عن التغيير."),96,108,21,590);Btn(c,T("Keep length","إبقاء الطول"),103,262,CloseModal,267,49);Put(c,DangerButton(T("Shorten paper","تقصير الورقة"),apply,289,49),396,262);},368);
  }
  void NotebookColorWheel(){NotebookColorWheel(notebookColor,color=>{notebookColor=color;ConfigureNotebookTool(notebookTool);CloseModal();});}
  void NotebookColorWheel(Color initial,Action<Color> confirm){
   Color pending=initial;Modal(T("Choose a colour","اختر لوناً"),c=>{
    var wheel=new HueWheel();wheel.SetColor(pending);Put(c,wheel,105,110);var swatch=new Border{Width=175,Height=86,CornerRadius=new CornerRadius(10),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.3),Background=B(pending)};Put(c,swatch,415,119);
    Label(c,T("Hex colour","رمز اللون"),416,224,17,230);var hex=new TextBox{Text="#"+pending.R.ToString("X2")+pending.G.ToString("X2")+pending.B.ToString("X2"),Width=190,Height=42,FontFamily=new FontFamily("Segoe Print"),FontSize=20,Background=B(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink),Padding=new Thickness(7,3,7,3)};Put(c,hex,415,259);
    Label(c,T("Brightness","السطوع"),99,354,17,200);var brightness=PaperSlider(0,1,wheel.Value,464);Put(c,brightness,228,348);bool syncing=false;Action refresh=()=>{pending=wheel.Selected;swatch.Background=B(pending);syncing=true;hex.Text="#"+pending.R.ToString("X2")+pending.G.ToString("X2")+pending.B.ToString("X2");syncing=false;};wheel.Changed+=color=>refresh();brightness.ValueChanged+=(s,e)=>{wheel.Value=brightness.Value;refresh();};hex.TextChanged+=(s,e)=>{if(syncing||hex.Text.Length!=7)return;try{pending=(Color)ColorConverter.ConvertFromString(hex.Text);wheel.SetColor(pending);swatch.Background=B(pending);syncing=true;brightness.Value=wheel.Value;syncing=false;}catch{}};
    Label(c,T("Recent colours","الألوان الأخيرة"),100,403,18,590);for(int i=0;i<Profile.Settings.RecentColors.Count&&i<12;i++){string value=Profile.Settings.RecentColors[i];var button=Button("",()=>{try{wheel.SetColor((Color)ColorConverter.ConvertFromString(value));brightness.Value=wheel.Value;refresh();}catch{}},37,34);button.Background=(Brush)new BrushConverter().ConvertFromString(value);button.ToolTip=value;Put(c,button,100+i*46,447);}
    Btn(c,T("Cancel","إلغاء"),104,528,CloseModal,265,49);Btn(c,T("Use colour","استخدام اللون"),399,528,()=>{string value=pending.ToString();Profile.Settings.RecentColors.Remove(value);Profile.Settings.RecentColors.Insert(0,value);Profile.Settings.RecentColors=Profile.Settings.RecentColors.Take(12).ToList();Save();confirm(pending);},293,49,true);
   },628);
  }
 }
}
