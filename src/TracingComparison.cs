using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;

namespace LittleNotebook {public partial class MainWindow {
 Image[] comparisonImages;ScrollViewer[] comparisonScroll;CanvasPanning[] comparisonPan;TextBlock comparisonZoomLabel;double comparisonZoom,comparisonFit;Rect comparisonBounds;
 void ClearComparison(){if(comparisonPan!=null)foreach(var pan in comparisonPan)pan.End();comparisonImages=null;comparisonScroll=null;comparisonPan=null;comparisonZoomLabel=null;}
 FrameworkElement ComparisonReference(){var model=CurrentNotebookPage;return !string.IsNullOrEmpty(model.TracePhoto)?(FrameworkElement)new Image{Source=DecodeNotebookImage(model.TracePhoto),Width=model.TraceWidth,Height=model.TraceHeight,Stretch=Stretch.Fill}:!string.IsNullOrEmpty(model.TraceInk)?new InkPresenter{Width=model.TraceWidth,Height=model.TraceHeight,Strokes=DecodeInk(model.TraceInk)}:(FrameworkElement)new TraceArt(model.Template,p,model.TraceWidth,model.TraceHeight);}
 Rect ReferenceBounds(FrameworkElement reference){
  var model=CurrentNotebookPage;double x=Math.Max(0,(940-model.TraceWidth)/2);Rect bounds;
  if(!string.IsNullOrEmpty(model.TracePhoto))bounds=new Rect(0,0,model.TraceWidth,model.TraceHeight);
  else if(!string.IsNullOrEmpty(model.TraceInk))bounds=DecodeInk(model.TraceInk).GetBounds();
  else{
   double scale=Math.Min(1,2048/Math.Max(model.TraceWidth,model.TraceHeight));reference.Measure(new Size(model.TraceWidth,model.TraceHeight));reference.Arrange(new Rect(0,0,model.TraceWidth,model.TraceHeight));var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(model.TraceWidth*scale)),Math.Max(1,(int)Math.Ceiling(model.TraceHeight*scale)),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(reference);
   int w=bitmap.PixelWidth,h=bitmap.PixelHeight;var pixels=new byte[w*h*4];bitmap.CopyPixels(pixels,w*4,0);int minX=w,minY=h,maxX=-1,maxY=-1;
   for(int iy=0;iy<h;iy++)for(int ix=0;ix<w;ix++){int at=(iy*w+ix)*4;if(pixels[at+3]>20){minX=Math.Min(minX,ix);minY=Math.Min(minY,iy);maxX=Math.Max(maxX,ix);maxY=Math.Max(maxY,iy);}}
   bounds=maxX<0?new Rect(0,0,model.TraceWidth,model.TraceHeight):new Rect(minX/scale,minY/scale,(maxX-minX+1)/scale,(maxY-minY+1)/scale);
  }
  if(bounds.IsEmpty)bounds=new Rect(0,0,model.TraceWidth,model.TraceHeight);bounds.Offset(x,35);return bounds;
 }
 Rect ComparisonArtworkBounds(FrameworkElement reference){
  var model=CurrentNotebookPage;Rect bounds=ReferenceBounds(reference);var ink=DecodeInk(model.Ink).GetBounds();if(!ink.IsEmpty)bounds.Union(ink);
  foreach(var text in model.Texts.Where(t=>!string.IsNullOrWhiteSpace(t.Text)))bounds.Union(new Rect(text.X,text.Y,text.Width,text.Height));foreach(var image in model.Images)bounds.Union(new Rect(image.X,image.Y,image.Width,image.Height));
  double padding=Math.Max(2,Math.Min(14,Math.Max(bounds.Width,bounds.Height)*.035));bounds.Inflate(padding,padding);bounds.Intersect(new Rect(0,0,940,model.Height));return bounds.IsEmpty?new Rect(0,0,940,model.Height):bounds;
 }
 BitmapSource RenderComparisonRegion(Canvas canvas,Rect bounds){
  canvas.Measure(new Size(canvas.Width,canvas.Height));canvas.Arrange(new Rect(0,0,canvas.Width,canvas.Height));canvas.UpdateLayout();double scale=Math.Min(Math.Max(4,Math.Min(16,290/bounds.Width)),4096/Math.Max(bounds.Width,bounds.Height));var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawRectangle(new VisualBrush(canvas){Viewbox=bounds,ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill},null,new Rect(0,0,bounds.Width,bounds.Height));
  var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(bounds.Width*scale)),Math.Max(1,(int)Math.Ceiling(bounds.Height*scale)),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();return bitmap;
 }
 void ZoomComparison(double value,Point? anchor=null,int pane=0){
  if(comparisonImages==null)return;value=Math.Max(.01,Math.Min(12,value));var scroll=comparisonScroll[pane];scroll.UpdateLayout();Point at=anchor??new Point(scroll.ViewportWidth/2,scroll.ViewportHeight/2),origin=comparisonImages[pane].TranslatePoint(new Point(),scroll);double x=(at.X-origin.X)/Math.Max(.001,comparisonZoom),y=(at.Y-origin.Y)/Math.Max(.001,comparisonZoom);comparisonZoom=value;
  foreach(var image in comparisonImages)image.LayoutTransform=new ScaleTransform(value,value);foreach(var viewport in comparisonScroll){viewport.UpdateLayout();viewport.ScrollToHorizontalOffset(Math.Max(0,x*value-at.X));viewport.ScrollToVerticalOffset(Math.Max(0,y*value-at.Y));}if(comparisonPan!=null)for(int i=0;i<comparisonPan.Length;i++)if(comparisonPan[i].Active)comparisonPan[i].Rebase(System.Windows.Input.Mouse.GetPosition(comparisonScroll[i]));comparisonZoomLabel.Text=Math.Round(value*100)+"%";
 }
 void CompareNotebookTrace(){
  FinishNotebookText();FlushNotebook();var model=CurrentNotebookPage;var source=ComparisonReference();var bounds=ComparisonArtworkBounds(source);var guide=new Canvas{Width=940,Height=model.Height,Background=B(p.Paper)};Put(guide,source,Math.Max(0,(940-model.TraceWidth)/2),35);
  var reference=RenderComparisonRegion(guide,bounds);var drawingCanvas=NotebookPageCanvas(true,false);drawingCanvas.Background=B(p.Paper);var drawing=RenderComparisonRegion(drawingCanvas,bounds);
  Modal(T("Compare your tracing","قارن رسمك بالقالب"),c=>{
   comparisonBounds=bounds;comparisonImages=new Image[2];comparisonScroll=new ScrollViewer[2];comparisonPan=new CanvasPanning[2];comparisonFit=Math.Min(270/bounds.Width,424/bounds.Height);comparisonZoom=comparisonFit;
   Label(c,T("Reference","المرجع"),95,101,20,282).TextAlignment=TextAlignment.Center;Label(c,T("Your drawing","رسمك"),409,101,20,282).TextAlignment=TextAlignment.Center;
   for(int i=0;i<2;i++){int index=i;var image=new Image{Source=i==0?reference:drawing,Width=bounds.Width,Height=bounds.Height,Stretch=Stretch.Fill,LayoutTransform=new ScaleTransform(comparisonFit,comparisonFit)};comparisonImages[i]=image;
    var viewport=new ScrollViewer{Width=292,Height=446,Content=image,Background=B(p.Paper),HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};StyleCanvasScroll(viewport);comparisonScroll[i]=viewport;
    Put(c,new Border{Width=294,Height=448,Child=viewport,BorderBrush=B(p.Line),BorderThickness=new Thickness(1)},88+i*320,142);
    comparisonPan[i]=new CanvasPanning(viewport);viewport.PreviewMouseWheel+=(s,e)=>{ZoomComparison(comparisonZoom*(e.Delta>0?1.15:1/1.15),e.GetPosition(viewport),index);e.Handled=true;};
    viewport.ScrollChanged+=(s,e)=>{if(comparisonScroll==null||e.HorizontalChange==0&&e.VerticalChange==0)return;var other=comparisonScroll[1-index];if(Math.Abs(other.HorizontalOffset-viewport.HorizontalOffset)>.1)other.ScrollToHorizontalOffset(viewport.HorizontalOffset);if(Math.Abs(other.VerticalOffset-viewport.VerticalOffset)>.1)other.ScrollToVerticalOffset(viewport.VerticalOffset);};
   }
   Put(c,NotebookIconButton("zoom-out",T("Zoom out","تصغير"),()=>ZoomComparison(comparisonZoom/1.2),40,35),219,612);comparisonZoomLabel=Label(c,Math.Round(comparisonFit*100)+"%",271,620,15,75,true);Put(c,NotebookIconButton("zoom-in",T("Zoom in","تكبير"),()=>ZoomComparison(comparisonZoom*1.2),40,35),352,612);Btn(c,T("Fit","ملاءمة"),407,612,()=>ZoomComparison(comparisonFit),87,35);Btn(c,"100%",509,612,()=>ZoomComparison(1),88,35);
   Label(c,T("Wheel: zoom · middle-drag: pan · both views stay aligned","العجلة: تكبير · سحب الزر الأوسط: تحريك · المعاينتان متطابقتان"),92,663,14,610).TextAlignment=TextAlignment.Center;Btn(c,T("Continue tracing","متابعة التتبّع"),230,714,CloseModal,340,46,true);
  },790);
 }
 // The ordinary paper scrollbar template is vertical. A horizontal template is
 // needed for wide zoomed canvases as well as the linked comparison viewports.
 void StyleCanvasScroll(ScrollViewer scroll){
  StylePaperScroll(scroll);var style=(Style)scroll.Resources[typeof(ScrollBar)];var horizontal=new Trigger{Property=ScrollBar.OrientationProperty,Value=Orientation.Horizontal};horizontal.Setters.Add(new Setter(FrameworkElement.WidthProperty,double.NaN));horizontal.Setters.Add(new Setter(FrameworkElement.HeightProperty,14.0));
  string template=@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'><Grid Background='Transparent'><Border Height='5' VerticalAlignment='Center' Background='#D7D6D2' CornerRadius='3'/><Track x:Name='PART_Track' Orientation='Horizontal' IsDirectionReversed='False'><Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageLeftCommand' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton><Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Margin='0,2' MinWidth='26' CornerRadius='5' Background='#8FAF80' BorderBrush='#29384A' BorderThickness='1'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageRightCommand' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton></Track></Grid></ControlTemplate>";
  horizontal.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Parse(template.Replace("#8FAF80",p.Accent.ToString()))));style.Triggers.Add(horizontal);
 }
}}
