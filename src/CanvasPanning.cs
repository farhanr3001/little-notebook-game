using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LittleNotebook {
 // Capture the viewport rather than the ink surface: middle-drag never starts
 // or edits a stroke, and the closed hand exists only for the captured gesture.
 public sealed class CanvasPanning {
  readonly ScrollViewer scroll;readonly Action<bool> changed;Point start;double x,y;Cursor savedCursor;bool savedForce;Window window;
  public bool Active{get;private set;}
  public CanvasPanning(ScrollViewer viewport,Action<bool> onChanged=null){
   scroll=viewport;changed=onChanged;
   scroll.PreviewMouseDown+=(s,e)=>{if(e.ChangedButton==MouseButton.Middle&&e.LeftButton!=MouseButtonState.Pressed){Begin(e.GetPosition(scroll));e.Handled=true;}};
   scroll.PreviewMouseMove+=(s,e)=>{if(!Active)return;if(e.MiddleButton!=MouseButtonState.Pressed){End();return;}Move(e.GetPosition(scroll));e.Handled=true;};
   scroll.PreviewMouseUp+=(s,e)=>{if(Active&&e.ChangedButton==MouseButton.Middle){End();e.Handled=true;}};
   scroll.LostMouseCapture+=(s,e)=>End();scroll.Loaded+=(s,e)=>{window=Window.GetWindow(scroll);if(window!=null)window.Deactivated+=Deactivate;};
   scroll.Unloaded+=(s,e)=>{End();if(window!=null)window.Deactivated-=Deactivate;window=null;};
  }
  void Deactivate(object sender,EventArgs e){End();}
  public void Begin(Point point,bool capture=true){if(Active)return;start=point;x=scroll.HorizontalOffset;y=scroll.VerticalOffset;savedCursor=scroll.Cursor;savedForce=scroll.ForceCursor;Active=true;scroll.Cursor=PanCursor.Closed;scroll.ForceCursor=true;if(capture&&!scroll.CaptureMouse()){End();return;}if(changed!=null)changed(true);}
  public void Move(Point point){if(!Active)return;scroll.ScrollToHorizontalOffset(x+start.X-point.X);scroll.ScrollToVerticalOffset(y+start.Y-point.Y);}
  public void Rebase(Point point){if(!Active)return;scroll.UpdateLayout();start=point;x=scroll.HorizontalOffset;y=scroll.VerticalOffset;}
  public void End(){if(!Active)return;Active=false;scroll.Cursor=savedCursor;scroll.ForceCursor=savedForce;if(scroll.IsMouseCaptured)scroll.ReleaseMouseCapture();if(changed!=null)changed(false);}
 }
 public partial class MainWindow {
  CanvasPanning notebookPan;
  bool NotebookAltKey(Key key,DependencyObject source){return page=="notebook"&&(key==Key.LeftAlt||key==Key.RightAlt)&&!(source is System.Windows.Controls.Primitives.TextBoxBase)&&!(Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase)&&!(Keyboard.FocusedElement is PasswordBox);}
  static bool NotebookWheelZoom(ModifierKeys modifiers){return (modifiers&(ModifierKeys.Control|ModifierKeys.Alt))!=0;}
  void AttachNotebookNavigation(){
   notebookPan=new CanvasPanning(notebookScroll,active=>{if(notebookGhost!=null)notebookGhost.Children.Clear();if(!active&&notebookInk!=null&&notebookInk.IsMouseOver)NotebookPointerMove(Mouse.GetPosition(notebookInk));});
   notebookScroll.PreviewMouseWheel+=(s,e)=>{if(NotebookWheelZoom(Keyboard.Modifiers)){SetNotebookZoom(notebookZoom*(e.Delta>0?1.12:1/1.12),e.GetPosition(notebookScroll));e.Handled=true;}};
  }
 }
}
