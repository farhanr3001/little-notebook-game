using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace LittleNotebook {public partial class MainWindow {
 void RenderTracingSheet(){var canvas=new Canvas{Width=1140,Height=630,Background=B(p.Paper)};for(int i=0;i<TraceLibrary.Keys.Length;i++){string key=TraceLibrary.Keys[i];double x=i%6*190,y=i/6*210;Put(canvas,new TraceArt(key,p,172,172),x+9,y+5);Label(canvas,key,x+5,y+181,15,181).TextAlignment=TextAlignment.Center;}canvas.Measure(new Size(1140,630));canvas.Arrange(new Rect(0,0,1140,630));canvas.UpdateLayout();var bitmap=new RenderTargetBitmap(1140,630,96,96,PixelFormats.Pbgra32);bitmap.Render(canvas);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create(Path.GetFullPath(Path.Combine(baseDir,"../test-output/tracing-sheet.png"))))png.Save(output);}
}}
