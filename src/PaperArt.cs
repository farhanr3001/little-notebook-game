using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public partial class Sketch {
  public static PaperDesign PaperSettings;
  public PaperDesign PaperOverride;PaperDesign ActivePaper{get{return PaperOverride??PaperSettings;}}
  static Dictionary<string,BitmapSource> paperIcons=new Dictionary<string,BitmapSource>();
  static BitmapSource PaperIcon(string kind){if(!paperIcons.ContainsKey(kind)){if(kind=="bow")paperIcons[kind]=Asset("bow-emoji.png");else using(var stream=new MemoryStream(Convert.FromBase64String(kind.Substring(6)))){var image=new BitmapImage();image.BeginInit();image.StreamSource=stream;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();paperIcons[kind]=image;}}return paperIcons[kind];}
  void DrawPaperBindings(DrawingContext d,double width,double height){int index=0;for(int y=38;y<height-28;y+=76){string kind=ActivePaper!=null&&ActivePaper.Holes!=null&&index<ActivePaper.Holes.Count?ActivePaper.Holes[index]:"holes";index++;if(kind=="none")continue;if(kind=="holes"){d.DrawEllipse(B(P.Desk),null,new Point(24,y),8,10);Line(d,13,y-5,33,y-10,P.Ink,4);Line(d,13,y-4,32,y-8,P.Muted,1);}else if(kind=="bow"||kind.StartsWith("image:")){try{d.DrawImage(PaperIcon(kind),new Rect(10,y-13,28,26));}catch{}}else{d.PushTransform(new TranslateTransform(9,y-15));Doodles.Draw(d,kind,30,30,P);d.Pop();}}}
  void DrawPaperDecorations(DrawingContext d,double width,double height){
   var page=ActivePaper==null?null:ActivePaper.Decorations;if(page==null)return;d.PushClip(new RectangleGeometry(new Rect(0,0,width-12,height-14)));d.PushTransform(new ScaleTransform((width-12)/940,(height-14)/Math.Max(414,page.Height)));
   try{foreach(var image in page.Images??new List<NotebookImage>())using(var stream=new MemoryStream(Convert.FromBase64String(image.Png))){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.StreamSource=stream;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();d.DrawImage(bitmap,new Rect(image.X,image.Y,image.Width,image.Height));}if(!string.IsNullOrEmpty(page.Ink))using(var buffer=new MemoryStream(Convert.FromBase64String(page.Ink)))new StrokeCollection(buffer).Draw(d);
    foreach(var text in page.Texts??new List<NotebookText>()){Color color;try{color=(Color)ColorConverter.ConvertFromString(text.Color);}catch{color=P.Ink;}var formatted=new FormattedText(text.Text??"",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe Print"),FontStyles.Normal,text.Bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),text.FontSize,B(color),1){MaxTextWidth=Math.Max(40,text.Width)};d.DrawText(formatted,new Point(text.X,text.Y));}
   }catch{}d.Pop();d.Pop();
  }
 }
}
