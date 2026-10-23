using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;
namespace LittleNotebook {public partial class MainWindow {
 FrameworkElement notebookFillPointer;
 bool MoveNotebookBrushTip(Point point){
  if(!new[]{"eraser","pencil","pen","brush","highlighter"}.Contains(notebookTool))return false;
  var attributes=notebookInk.DefaultDrawingAttributes;bool rectangle=notebookTool!="eraser"&&attributes.StylusTip==StylusTip.Rectangle;string key=notebookTool+(rectangle?":rectangle":":ellipse");
  if(notebookTip==null||notebookTipTool!=key){notebookTip=rectangle?(Shape)new Rectangle():new Ellipse();notebookTip.Stroke=Brushes.Gray;notebookTip.StrokeThickness=1;notebookTip.Fill=new SolidColorBrush(Color.FromArgb(30,255,255,255));notebookTip.RenderTransformOrigin=new Point(.5,.5);notebookTipTool=key;}
  if(!notebookGhost.Children.Contains(notebookTip)){notebookGhost.Children.Clear();notebookGhost.Children.Add(notebookTip);}
  notebookTip.Width=Math.Max(2,notebookTool=="eraser"?notebookSize*3:attributes.Width);notebookTip.Height=Math.Max(2,notebookTool=="eraser"?notebookSize*3:attributes.Height);notebookTip.RenderTransform=rectangle?new MatrixTransform(attributes.StylusTipTransform):Transform.Identity;Canvas.SetLeft(notebookTip,point.X-notebookTip.Width/2);Canvas.SetTop(notebookTip,point.Y-notebookTip.Height/2);return true;
 }
 void MoveNotebookFillPointer(Point point){
  if(notebookFillPointer==null){var cursor=new Canvas{Width=34,Height=34,IsHitTestVisible=false};cursor.Children.Add(new Path{Data=Geometry.Parse("M0,0 L0,18 L5,13 L9,23 L13,21 L9,12 L19,12 Z"),Fill=Brushes.White,Stroke=B(p.Ink),StrokeThickness=1.2,StrokeLineJoin=PenLineJoin.Round});Put(cursor,NotebookIcon("fill",17),16,15);notebookFillPointer=cursor;}
  if(!notebookGhost.Children.Contains(notebookFillPointer)){notebookGhost.Children.Clear();notebookGhost.Children.Add(notebookFillPointer);}Canvas.SetLeft(notebookFillPointer,point.X);Canvas.SetTop(notebookFillPointer,point.Y);
 }
}}
