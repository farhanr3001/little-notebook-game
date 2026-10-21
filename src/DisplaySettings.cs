using System;
using System.Windows;

namespace LittleNotebook {
 public partial class MainWindow {
  Rect windowedBounds;WindowState windowedState=WindowState.Normal;bool displayApplied;
  void SetDisplayMode(bool fullscreen){Profile.Settings.BorderlessFullscreen=fullscreen;ApplyDisplayMode();Save();}
  void ApplyDisplayMode(){
   if(Profile.Settings.BorderlessFullscreen){
    if(displayApplied)return;
    windowedState=WindowState==WindowState.Maximized?WindowState.Maximized:WindowState.Normal;
    windowedBounds=WindowState==WindowState.Normal?new Rect(Left,Top,ActualWidth>0?ActualWidth:Width,ActualHeight>0?ActualHeight:Height):RestoreBounds;
    WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowState=WindowState.Maximized;displayApplied=true;
   }else if(displayApplied){
    WindowState=WindowState.Normal;WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;
    if(!windowedBounds.IsEmpty&&windowedBounds.Width>=MinWidth){Left=windowedBounds.Left;Top=windowedBounds.Top;Width=windowedBounds.Width;Height=windowedBounds.Height;}
    WindowState=windowedState;displayApplied=false;
   }
  }
  void DisplaySettings(){Modal(T("Display mode","وضع العرض"),c=>{
   Label(c,T("F11 switches between windowed and borderless fullscreen.","اضغط F11 للتبديل بين النافذة وملء الشاشة بلا حدود."),95,106,19,600).TextAlignment=TextAlignment.Center;
   Btn(c,T("Windowed","نافذة"),105,194,()=>{SetDisplayMode(false);DisplaySettings();},270,58,!Profile.Settings.BorderlessFullscreen);
   Btn(c,T("Borderless fullscreen","ملء الشاشة بلا حدود"),402,194,()=>{SetDisplayMode(true);DisplaySettings();},284,58,Profile.Settings.BorderlessFullscreen);
   Btn(c,T("Done","تم"),255,294,SettingsView,280,49,true);
  },390);}
 }
}
