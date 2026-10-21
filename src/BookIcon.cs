using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public static class BookIcon {
  public static void Create(string directory){
   Directory.CreateDirectory(directory);int[] sizes={16,24,32,48,64,128,256};var frames=new List<byte[]>();
   foreach(int size in sizes){
    var visual=new DrawingVisual();using(var d=visual.RenderOpen()){
     d.PushTransform(new ScaleTransform(size/160.0,size/160.0));d.PushTransform(new RotateTransform(-9,80,80));
     var ink=new SolidColorBrush(Color.FromRgb(41,56,74));var cream=new SolidColorBrush(Color.FromRgb(255,248,233));var cover=new SolidColorBrush(Color.FromRgb(195,132,103));var green=new SolidColorBrush(Color.FromRgb(143,175,128));var pen=new Pen(ink,3.5);
     d.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(45,41,56,74)),null,new Rect(28,23,112,126),7,7);
     d.DrawRoundedRectangle(cream,pen,new Rect(29,19,106,124),6,6);
     d.DrawLine(new Pen(ink,1.2),new Point(45,130),new Point(132,130));d.DrawLine(new Pen(ink,1),new Point(45,136),new Point(132,136));
     d.DrawRoundedRectangle(cover,pen,new Rect(24,16,104,115),6,6);d.DrawLine(new Pen(ink,2),new Point(39,18),new Point(39,128));
     d.DrawRoundedRectangle(cream,new Pen(ink,2),new Rect(53,43,57,35),3,3);d.DrawLine(new Pen(ink,2),new Point(64,55),new Point(99,55));d.DrawLine(new Pen(ink,1.4),new Point(69,65),new Point(94,65));
     d.DrawGeometry(green,new Pen(ink,1.7),Geometry.Parse("M 92,16 L 110,16 L 110,35 L 101,29 L 92,35 Z"));
     d.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(95,255,248,233)),2.5),new Point(29,25),new Point(29,115));d.Pop();d.Pop();
    }
    var bitmap=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var buffer=new MemoryStream()){encoder.Save(buffer);frames.Add(buffer.ToArray());}
   }
   using(var output=new BinaryWriter(File.Create(Path.Combine(directory,"LittleNotebook.ico")))){
    output.Write((ushort)0);output.Write((ushort)1);output.Write((ushort)sizes.Length);int offset=6+sizes.Length*16;
    for(int i=0;i<sizes.Length;i++){output.Write((byte)(sizes[i]==256?0:sizes[i]));output.Write((byte)(sizes[i]==256?0:sizes[i]));output.Write((byte)0);output.Write((byte)0);output.Write((ushort)1);output.Write((ushort)32);output.Write(frames[i].Length);output.Write(offset);offset+=frames[i].Length;}
    foreach(var frame in frames)output.Write(frame);
   }
   File.WriteAllBytes(Path.Combine(directory,"book-icon.png"),frames[frames.Count-1]);
  }
 }
}
