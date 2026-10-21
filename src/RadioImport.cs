using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace LittleNotebook {public partial class MainWindow {
 Popup musicImportMenu;MusicImportService musicImporter;CancellationTokenSource musicImportCancel;int musicImportSerial;
 void OpenMusicImportMenu(Button anchor){
  var content=new StackPanel{Width=290,Margin=new Thickness(8)};var files=Button(T("Choose from File Explorer…","اختيار من مستكشف الملفات…"),()=>{musicImportMenu.IsOpen=false;ImportMusic();},286,43);var link=Button(T("Choose from YouTube / TikTok…","اختيار من يوتيوب / تيك توك…"),()=>{musicImportMenu.IsOpen=false;ImportMusicLink();},286,43);files.Margin=new Thickness(0,0,0,6);content.Children.Add(files);content.Children.Add(link);musicImportMenu=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Top,AllowsTransparency=true,StaysOpen=false,Child=new Border{Child=content,Background=Sketch.Textured(p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(10)}};musicImportMenu.IsOpen=true;
 }
 void CancelMusicDownload(){musicImportSerial++;var cancel=musicImportCancel;musicImportCancel=null;if(cancel!=null)cancel.Cancel();var service=musicImporter;musicImporter=null;if(service!=null)service.Dispose();}
 void ImportMusicLink(){Modal(T("Import music from a link","استيراد موسيقى من رابط"),c=>{
  Label(c,T("Paste a public YouTube video or TikTok video/sound link.","ألصق رابط فيديو عام من يوتيوب أو فيديو/صوت من تيك توك."),94,100,17,605);
  var input=new TextBox{Width=469,Height=46,FontSize=17,Padding=new Thickness(9),Background=B(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink),MaxLength=2048,FlowDirection=FlowDirection.LeftToRight};Put(c,input,96,153);var paste=Button(T("Paste","لصق"),()=>{try{if(Clipboard.ContainsText())input.Text=Clipboard.GetText().Trim();}catch{statusForPaste(input);}},112,46);Put(c,paste,581,153);
  Label(c,T("TikTok links may use ssstik.io or tikwm.com to retrieve audio.","قد تُستخدم ssstik.io أو tikwm.com لجلب أصوات تيك توك."),96,211,12,604);var status=Label(c,"",96,248,16,596);status.MaxHeight=74;var bar=new ProgressBar{Width=594,Height=13,Minimum=0,Maximum=100,Foreground=B(p.Accent),Background=B(p.Light),Visibility=Visibility.Collapsed};Put(c,bar,97,327);
  Button import=null;import=Button(T("Import","استيراد"),async ()=>{
   string url=input.Text.Trim();if(!MusicImportService.ValidLink(url)){status.Text=T("Paste a valid HTTPS YouTube or TikTok link.","ألصق رابط HTTPS صحيحاً من يوتيوب أو تيك توك.");return;}
   input.IsEnabled=paste.IsEnabled=import.IsEnabled=false;bar.Visibility=Visibility.Visible;bar.IsIndeterminate=true;status.Text=T("Connecting…","جارٍ الاتصال…");var service=musicImporter=new MusicImportService();var cancel=musicImportCancel=new CancellationTokenSource();int serial=++musicImportSerial;
   try{var progress=new Progress<MusicImportProgress>(value=>{if(serial!=musicImportSerial||closing)return;bar.IsIndeterminate=!value.Percent.HasValue;if(value.Percent.HasValue)bar.Value=value.Percent.Value;status.Text=(value.Stage=="converting"?T("Converting to MP3…","جارٍ التحويل إلى MP3…"):value.Stage=="connecting"?T("Connecting…","جارٍ الاتصال…"):T("Downloading…","جارٍ التنزيل…"))+(value.Percent.HasValue?" "+Math.Round(value.Percent.Value)+"%":"");});var result=await service.DownloadAsync(url,progress,cancel.Token);if(serial!=musicImportSerial||closing)return;string relative="data"+Path.DirectorySeparatorChar+result.File.Substring(AppPaths.DataDirectory.TrimEnd(Path.DirectorySeparatorChar).Length+1);audio.AddTrack(relative);audio.RenameTrack(relative,result.Title);audio.Select(audio.Tracks.Count-1);Save();musicImporter=null;musicImportCancel=null;showRemovedMusic=false;Build();RadioSelection();}
   catch(OperationCanceledException){}catch(Exception error){if(serial==musicImportSerial&&!closing){status.Text=T("Could not import: ","تعذّر الاستيراد: ")+error.Message;input.IsEnabled=paste.IsEnabled=import.IsEnabled=true;bar.Visibility=Visibility.Collapsed;}}
   finally{if(musicImporter==service){musicImporter=null;musicImportCancel=null;}service.Dispose();cancel.Dispose();}
  },289,49,true);Put(c,import,400,366);Btn(c,T("Cancel","إلغاء"),97,366,()=>{CancelMusicDownload();RadioSelection();},271,49);
 },460);}
 void statusForPaste(TextBox input){input.Focus();}
 void RenameMusic(MusicTrack track){Modal(T("Rename music","إعادة تسمية الموسيقى"),c=>{Label(c,T("Change its name in your radio. The file stays unchanged.","غيّر الاسم في الراديو فقط. يبقى الملف كما هو."),95,104,18,599);var input=new TextBox{Text=track.Title,Width=590,Height=48,FontFamily=new FontFamily(Ar?"Segoe UI":"Segoe Print"),FontSize=21,Padding=new Thickness(8),Background=B(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink),MaxLength=120};Put(c,input,98,167);Btn(c,T("Cancel","إلغاء"),98,254,RadioSelection,275,49);Btn(c,T("Save name","حفظ الاسم"),400,254,()=>{if(string.IsNullOrWhiteSpace(input.Text))return;audio.RenameTrack(track.File,input.Text);Save();Build();RadioSelection();},291,49,true);},349);}
}}
