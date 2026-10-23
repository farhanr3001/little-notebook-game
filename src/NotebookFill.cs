using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public partial class MainWindow {
  static BitmapSource DecodeNotebookImage(string encoded){using(var stream=new MemoryStream(Convert.FromBase64String(encoded))){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.StreamSource=stream;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();bitmap.Freeze();return bitmap;}}
  static string EncodeNotebookImage(BitmapSource bitmap){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=new MemoryStream()){encoder.Save(stream);return Convert.ToBase64String(stream.ToArray());}}
  Border AddNotebookImageFrame(NotebookImage model){var frame=new Border{Tag=model,Width=model.Width,Height=model.Height,Child=new Image{Source=DecodeNotebookImage(model.Png),Stretch=Stretch.Fill},Background=Brushes.Transparent};InkCanvas.SetLeft(frame,model.X);InkCanvas.SetTop(frame,model.Y);notebookInk.Children.Add(frame);return frame;}
  void FillNotebookAt(Point point){
   FinishNotebookText();var image=NotebookFloodImage(RenderNotebookPage(true),point,notebookColor);BeginNotebookChange();CurrentNotebookPage.Images.Add(image);AddNotebookImageFrame(image);CommitNotebookChange();ConfigureNotebookTool("fill");
  }
  static NotebookImage NotebookFloodImage(BitmapSource mask,Point point,Color colour){int width=mask.PixelWidth,height=mask.PixelHeight,stride=width*4;var source=new byte[stride*height];mask.CopyPixels(source,stride,0);int sx=Math.Max(0,Math.Min(width-1,(int)point.X)),sy=Math.Max(0,Math.Min(height-1,(int)point.Y)),seed=sy*width+sx;int b=source[seed*4],g=source[seed*4+1],r=source[seed*4+2];
   var visited=new bool[width*height];var queue=new int[width*height];int head=0,tail=0;queue[tail++]=seed;visited[seed]=true;int minX=sx,maxX=sx,minY=sy,maxY=sy;
   Action<int> add=index=>{if(visited[index])return;visited[index]=true;int offset=index*4;if(Math.Abs(source[offset]-b)+Math.Abs(source[offset+1]-g)+Math.Abs(source[offset+2]-r)<=55)queue[tail++]=index;};
   while(head<tail){int index=queue[head++],x=index%width,y=index/width;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);if(x>0)add(index-1);if(x<width-1)add(index+1);if(y>0)add(index-width);if(y<height-1)add(index+width);}
   int w=maxX-minX+1,h=maxY-minY+1;var pixels=new byte[w*h*4];for(int i=0;i<tail;i++){int x=queue[i]%width-minX,y=queue[i]/width-minY,offset=(y*w+x)*4;pixels[offset]=colour.B;pixels[offset+1]=colour.G;pixels[offset+2]=colour.R;pixels[offset+3]=255;}
   var bitmap=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,pixels,w*4);return new NotebookImage{X=minX,Y=minY,Width=w,Height=h,Png=EncodeNotebookImage(bitmap)};
  }
 }
}
