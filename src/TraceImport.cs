using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace LittleNotebook {
 public static class PhotoTracing {
  public static StrokeCollection Extract(BitmapSource image,double threshold,Color color){
   var source=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);int w=source.PixelWidth,h=source.PixelHeight;var rgba=new byte[w*h*4];source.CopyPixels(rgba,w*4,0);var gray=new double[w*h];for(int i=0;i<gray.Length;i++)gray[i]=(.2126*rgba[i*4+2]+.7152*rgba[i*4+1]+.0722*rgba[i*4])*(rgba[i*4+3]/255.0)+255*(1-rgba[i*4+3]/255.0);
   var smooth=new double[w*h];for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++){int i=y*w+x;smooth[i]=(gray[i-w-1]+2*gray[i-w]+gray[i-w+1]+2*gray[i-1]+4*gray[i]+2*gray[i+1]+gray[i+w-1]+2*gray[i+w]+gray[i+w+1])/16;}
   var magnitude=new double[w*h];var directions=new byte[w*h];for(int y=2;y<h-2;y++)for(int x=2;x<w-2;x++){int i=y*w+x;double gx=-smooth[i-w-1]+smooth[i-w+1]-2*smooth[i-1]+2*smooth[i+1]-smooth[i+w-1]+smooth[i+w+1],gy=-smooth[i-w-1]-2*smooth[i-w]-smooth[i-w+1]+smooth[i+w-1]+2*smooth[i+w]+smooth[i+w+1];magnitude[i]=Math.Sqrt(gx*gx+gy*gy)/4;double angle=(Math.Atan2(gy,gx)*180/Math.PI+180)%180;directions[i]=(byte)(angle<22.5||angle>=157.5?0:angle<67.5?1:angle<112.5?2:3);}
   var candidates=new byte[w*h];var queue=new Queue<int>();for(int y=3;y<h-3;y++)for(int x=3;x<w-3;x++){int i=y*w+x;int offset=directions[i]==0?1:directions[i]==1?w+1:directions[i]==2?w:w-1;double v=magnitude[i];if(v<magnitude[i-offset]||v<magnitude[i+offset]||v<threshold*.4)continue;candidates[i]=(byte)(v>=threshold?2:1);if(candidates[i]==2)queue.Enqueue(i);}
   int[] neighbors={-w-1,-w,-w+1,-1,1,w-1,w,w+1};while(queue.Count>0){int i=queue.Dequeue();foreach(int offset in neighbors){int n=i+offset;if(n>0&&n<candidates.Length&&candidates[n]==1){candidates[n]=2;queue.Enqueue(n);}}}
   var used=new bool[w*h];var strokes=new StrokeCollection();for(int start=0;start<candidates.Length;start++){if(candidates[start]!=2||used[start])continue;var points=new StylusPointCollection();int current=start;while(current>=0){used[current]=true;points.Add(new StylusPoint(current%w,current/w));int next=-1;foreach(int offset in neighbors){int n=current+offset;if(n>=0&&n<candidates.Length&&candidates[n]==2&&!used[n]&&Math.Abs(n%w-current%w)<=1){next=n;break;}}current=next;}if(points.Count>=5)strokes.Add(new Stroke(points,new DrawingAttributes{Color=color,Width=1.6,Height=1.6,FitToCurve=true,IgnorePressure=true}));}
   return strokes;
  }
 }
 public partial class MainWindow {
  void ImportTracing(){var dialog=new OpenFileDialog{Title=T("Import a photo for tracing","استيراد صورة للتتبّع"),Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp"};if(dialog.ShowDialog(this)!=true)return;try{
   var decoder=BitmapDecoder.Create(new Uri(dialog.FileName),BitmapCreateOptions.DelayCreation,BitmapCacheOption.OnLoad);var original=decoder.Frames[0];double factor=Math.Min(1,Math.Min(880.0/original.PixelWidth,1700.0/original.PixelHeight));var image=new TransformedBitmap(original,new ScaleTransform(factor,factor));EditImportedTracing(image);
  }catch(Exception ex){Message(T("Could not import tracing","تعذّر استيراد القالب"),ex.Message);}}
  void EditImportedTracing(BitmapSource image){
   double width=Math.Min(820,image.PixelWidth),threshold=45;double height=width*image.PixelHeight/image.PixelWidth;var originals=PhotoTracing.Extract(image,threshold,p.Ink);var strokes=originals.Clone();strokes.Transform(new Matrix(width/image.PixelWidth,0,0,width/image.PixelWidth,0,0),false);SmoothInkCanvas ink=null;var undo=new Stack<string>();bool changing=false;
   Modal(T("Adjust imported tracing","تعديل القالب المستورد"),c=>{
    Label(c,T("Adjust detail and size. Use the pen or eraser before placing.","اضبط التفاصيل والحجم. عدّل بالقلم والممحاة قبل الوضع."),94,102,16,610);
    var surface=new Grid{Width=width,Height=height,Background=B(p.Paper)};ink=new SmoothInkCanvas{Width=width,Height=height,Strokes=strokes,Background=Brushes.Transparent,DefaultDrawingAttributes=new DrawingAttributes{Color=p.Ink,Width=1.6,Height=1.6,FitToCurve=true,IgnorePressure=true},Opacity=.60,EraserShape=new EllipseStylusShape(18,18)};surface.Children.Add(ink);var scroll=new ScrollViewer{Width=650,Height=357,Content=surface,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};StylePaperScroll(scroll);Put(c,scroll,82,150);
    ink.Starting+=()=>undo.Push(EncodeInk(ink.Strokes));Btn(c,T("Pen","قلم"),94,523,()=>ink.EditingMode=InkCanvasEditingMode.Ink,107,37,true);Btn(c,T("Eraser","ممحاة"),214,523,()=>ink.EditingMode=InkCanvasEditingMode.EraseByPoint,119,37);Btn(c,T("Undo","تراجع"),347,523,()=>{if(undo.Count>0)ink.Strokes=DecodeInk(undo.Pop());},108,37);
    Label(c,T("Detail","التفاصيل"),94,580,15,100);var detail=PaperSlider(10,130,threshold,203);Put(c,detail,175,573);Label(c,T("Width","العرض"),405,580,15,88);var scale=PaperSlider(120,880,width,194);Put(c,scale,492,573);
    var regenerate=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(300)};regenerate.Tick+=(s,e)=>{regenerate.Stop();if(overlay.Children.Count==0)return;undo.Push(EncodeInk(ink.Strokes));var outlines=PhotoTracing.Extract(image,detail.Value,p.Ink);outlines.Transform(new Matrix(width/image.PixelWidth,0,0,width/image.PixelWidth,0,0),false);ink.Strokes=outlines;};detail.ValueChanged+=(s,e)=>{regenerate.Stop();regenerate.Start();};scale.ValueChanged+=(s,e)=>{if(changing)return;double next=scale.Value;ink.Strokes.Transform(new Matrix(next/width,0,0,next/width,0,0),false);width=next;height=width*image.PixelHeight/image.PixelWidth;surface.Width=ink.Width=width;surface.Height=ink.Height=height;};
    Label(c,T("Changing detail regenerates the outline; make pen edits afterwards.","تغيير التفاصيل يعيد إنشاء القالب؛ أضف تعديلات القلم بعد ذلك."),94,626,13,595);
    Btn(c,T("Cancel","إلغاء"),100,682,()=>{regenerate.Stop();NotebookTemplates();},266,47);Btn(c,T("Place tracing","وضع القالب"),399,682,()=>{regenerate.Stop();CloseModal();BeginNotebookChange();CurrentNotebookPage.Template="imported";CurrentNotebookPage.TraceInk=EncodeInk(ink.Strokes);CurrentNotebookPage.TraceWidth=width;CurrentNotebookPage.TraceHeight=height;CurrentNotebookPage.TraceVisible=true;CurrentNotebookPage.Height=Math.Min(12000,Math.Max(CurrentNotebookPage.Height,height+70));ResizeNotebookSurface();RefreshNotebookGuide();CommitNotebookChange();},296,47,true);
   },779);
  }
 }
}
