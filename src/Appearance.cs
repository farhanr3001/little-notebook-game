using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace LittleNotebook {
 public partial class MainWindow {
  Button TimerToggle(){
   return PaperToggle(Profile.Settings.Timer?T("Timer on","المؤقت مفعّل"):T("Timer off","المؤقت متوقف"),Profile.Settings.Timer,()=>{Profile.Settings.Timer=!Profile.Settings.Timer;Save();Build();});
  }
  Button PaperToggle(string label,bool value,Action change,bool enabled=true){
   var button=new Button{Width=190,Height=43,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand,IsEnabled=enabled,Opacity=enabled?1:.65};
   var panel=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,FlowDirection=Ar?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
   var track=new Canvas{Width=48,Height=26,Margin=new Thickness(0,0,13,0)};
   track.Children.Add(new Border{Width=47,Height=25,CornerRadius=new CornerRadius(12),Background=Sketch.Textured(value?p.Accent:p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.4)});
   var knob=new Ellipse{Width=18,Height=19,Fill=B(p.Paper),Stroke=B(p.Ink),StrokeThickness=1.1};Canvas.SetLeft(knob,value?25:4);Canvas.SetTop(knob,3);track.Children.Add(knob);panel.Children.Add(track);
   panel.Children.Add(Text(label,17));button.Content=panel;
   var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);button.Template=new ControlTemplate(typeof(Button)){VisualTree=content};
   AutomationProperties.SetName(button,label+": "+(value?T("On","مفعّل"):T("Off","متوقف")));button.ToolTip=enabled?label:T("Hard always has one life.","المستوى الصعب له محاولة واحدة دائماً.");button.Click+=(s,e)=>{audio.Fx("click");change();};return button;
  }
  Slider PaperSlider(double min,double max,double value,double width){
   var slider=new Slider{Minimum=min,Maximum=max,Value=value,Width=width,Height=28,IsMoveToPointEnabled=true,SmallChange=(max-min)/20,LargeChange=(max-min)/5};
   string template=@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>
    <Grid Background='Transparent'><Border Height='7' VerticalAlignment='Center' Background='#F9F0D9' BorderBrush='#29384A' BorderThickness='1.3' CornerRadius='4'/>
     <Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' IsDirectionReversed='False'>
      <Track.DecreaseRepeatButton><RepeatButton Command='Slider.DecreaseLarge' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Height='5' Margin='2,0,0,0' VerticalAlignment='Center' CornerRadius='3' Background='#8FAF80'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
      <Track.Thumb><Thumb Width='17' Height='22'><Thumb.Template><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Ellipse Width='17' Height='19' Fill='#E9BC60' Stroke='#29384A' StrokeThickness='1.4'/><Ellipse Width='5' Height='5' Margin='-4,-6,0,0' Fill='#FFF3C9' Opacity='.65'/></Grid></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
      <Track.IncreaseRepeatButton><RepeatButton Command='Slider.IncreaseLarge' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
     </Track>
    </Grid></ControlTemplate>";
   slider.Template=(ControlTemplate)XamlReader.Parse(template.Replace("#8FAF80",p.Accent.ToString()));return slider;
  }
  void SizingSettings(){
   double textScale=Profile.Settings.TextScale,buttonScale=Profile.Settings.ButtonScale;
   Modal(T("Text & button sizes","حجم النص والأزرار"),c=>{
    Label(c,T("Adjust the sliders. See how it feels before applying.","حرّك الأشرطة وشاهد النتيجة قبل تطبيق التغيير."),96,96,18,596);
    Label(c,T("Text size","حجم النص"),95,157,20,300);var textValue=Label(c,Math.Round(textScale*100)+"%",598,157,19,95,true);
    var textSlider=PaperSlider(.85,1.15,textScale,580);AutomationProperties.SetName(textSlider,"Text size preview");Put(c,textSlider,98,196);
    Label(c,T("Button size","حجم الأزرار"),95,242,20,300);var buttonValue=Label(c,Math.Round(buttonScale*100)+"%",598,242,19,95,true);
    var buttonSlider=PaperSlider(.75,1,buttonScale,580);AutomationProperties.SetName(buttonSlider,"Button size preview");Put(c,buttonSlider,98,281);
    var preview=new Canvas{Width=589,Height=163};var card=Box(preview,622,198,p.Light);Put(c,card,85,340);
    Action draw=()=>{
     preview.Children.Clear();var t=Text(T("Choose the correct answer.","اختر الإجابة الصحيحة."),23,580);t.FontSize=23*textScale;Put(preview,t,0,7);
     var sample=Button(T("Choose this answer","اختر هذه الإجابة"),()=>{},285,61,true);sample.Tag=null;sample.Width=285*buttonScale;sample.Height=61*buttonScale;sample.FontSize=20*textScale;Put(preview,sample,(589-sample.Width)/2,77);
    };draw();
    textSlider.ValueChanged+=(s,e)=>{textScale=textSlider.Value;textValue.Text=Math.Round(textScale*100)+"%";draw();};
    buttonSlider.ValueChanged+=(s,e)=>{buttonScale=buttonSlider.Value;buttonValue.Text=Math.Round(buttonScale*100)+"%";draw();};
    Btn(c,T("Reset preview","إعادة ضبط المعاينة"),91,579,()=>{textSlider.Value=.9;buttonSlider.Value=.92;},218,49);
    Btn(c,T("Apply sizes","تطبيق الأحجام"),370,579,()=>{Profile.Settings.TextScale=textScale;Profile.Settings.ButtonScale=buttonScale;Save();Build();SettingsView();},330,49,true);
   },684);
  }
 }
}
