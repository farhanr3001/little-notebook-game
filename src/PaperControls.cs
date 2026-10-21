using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace LittleNotebook {
 public partial class MainWindow {
  void StylePaperScroll(ScrollViewer scroll){
   string style=@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'>
    <Setter Property='Width' Value='14'/><Setter Property='Background' Value='Transparent'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='Transparent'>
    <Border Width='5' HorizontalAlignment='Center' Background='#D7D6D2' CornerRadius='3'/><Track x:Name='PART_Track' Orientation='Vertical' IsDirectionReversed='True'>
    <Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
    <Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Margin='2,0' MinHeight='26' CornerRadius='5' Background='#8FAF80' BorderBrush='#29384A' BorderThickness='1'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
    <Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Focusable='False'><RepeatButton.Template><ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
    </Track></Grid></ControlTemplate></Setter.Value></Setter></Style>";
   scroll.Resources[typeof(ScrollBar)]=(Style)XamlReader.Parse(style.Replace("#8FAF80",p.Accent.ToString()));
  }
 }
}
