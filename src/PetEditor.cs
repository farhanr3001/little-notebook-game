using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LittleNotebook {
 public partial class MainWindow {
  Popup petEditorPopup;
  void EditPetDropdown(Button anchor,string current,string currentColor,Action<string,string> select){var panel=new StackPanel{Width=340,Margin=new Thickness(7)};var popup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Top,StaysOpen=false,AllowsTransparency=true};var none=Button(T("No pet","بلا حيوان"),()=>{popup.IsOpen=false;select("",currentColor);},327,40,current=="");panel.Children.Add(none);
   foreach(var reward in ProfileFeatures.Rewards.Where(x=>x.Category=="pets"&&Profile.Owned.Contains(x.Id)))foreach(string color in reward.Colors){var button=Button("",()=>{popup.IsOpen=false;select(reward.Id,color);},327,57,current==reward.Id&&currentColor==color);button.Margin=new Thickness(0,5,0,0);var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(PetImage(reward.Id,color,45,45));row.Children.Add(Text((Ar?reward.NameAr:reward.Name)+" · "+ColorName(color),15,235));button.Content=row;panel.Children.Add(button);}var scroll=new ScrollViewer{Content=panel,MaxHeight=315,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};StylePaperScroll(scroll);popup.Child=new Border{Child=scroll,Background=Sketch.Textured(p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9)};petEditorPopup=popup;popup.IsOpen=true;
  }
 }
}
