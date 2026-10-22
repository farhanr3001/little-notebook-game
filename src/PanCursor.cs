using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public static class PanCursor {
  static readonly MemoryStream data=Create();
  public static readonly Cursor Closed=new Cursor(data);
  static MemoryStream Create(){
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){
    dc.DrawGeometry(Brushes.White,new Pen(Brushes.Black,1.4){LineJoin=PenLineJoin.Round},Geometry.Parse("M8,15 L7,13 Q4,11 3,14 Q2,16 5,19 L10,24 L11,29 L24,29 L25,23 Q28,21 28,17 L28,12 Q28,9 25,10 L24,12 L24,9 Q24,6 21,7 L20,10 L20,7 Q20,4 17,5 L16,9 L16,8 Q16,5 13,6 Q11,6 11,10 L11,16 Q9,17 8,15 Z"));
    foreach(int x in new[]{14,18,22})dc.DrawLine(new Pen(Brushes.Black,1),new Point(x,14),new Point(x,18));
   }
   var image=new RenderTargetBitmap(32,32,96,96,PixelFormats.Pbgra32);image.Render(visual);var pixels=new byte[32*32*4];image.CopyPixels(pixels,128,0);
   var stream=new MemoryStream();var writer=new BinaryWriter(stream);writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)1);writer.Write((byte)32);writer.Write((byte)32);writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)16);writer.Write((ushort)16);writer.Write(40+4096+128);writer.Write(22);
   writer.Write(40);writer.Write(32);writer.Write(64);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);writer.Write(4096+128);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
   for(int y=31;y>=0;y--)for(int x=0;x<32;x++){int at=(y*32+x)*4,a=pixels[at+3];for(int c=0;c<3;c++)writer.Write(a==0?(byte)0:(byte)Math.Min(255,pixels[at+c]*255/a));writer.Write((byte)a);}
   for(int y=31;y>=0;y--)for(int group=0;group<4;group++){byte mask=0;for(int bit=0;bit<8;bit++)if(pixels[(y*32+group*8+bit)*4+3]==0)mask|=(byte)(128>>bit);writer.Write(mask);}
   stream.Position=0;return stream;
  }
 }
}
