using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace LittleNotebook {
 public partial class MainWindow {
  bool showRemovedMusic;
  void RadioSelection(){
   Modal(T("Choose music","اختيار الموسيقى"),c=>{
    Label(c,T("Shuffled playback · choose a track, or add your own music.","تشغيل عشوائي · اختر مقطعاً أو أضف موسيقاك."),94,102,17,605);
    Btn(c,T("Music library","المكتبة"),94,141,()=>{showRemovedMusic=false;RadioSelection();},273,38,!showRemovedMusic);Btn(c,T("Removed music","الموسيقى المُزالة")+" ("+Profile.Settings.DisabledMusic.Count+")",385,141,()=>{showRemovedMusic=true;RadioSelection();},305,38,showRemovedMusic);
    var list=new StackPanel{Margin=new Thickness(3)};
    foreach(var track in audio.AllTracks.Where(x=>Profile.Settings.DisabledMusic.Contains(x.File)==showRemovedMusic)){
     var row=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,11)};int index=audio.Tracks.FindIndex(x=>x.File==track.File);var button=Button((!showRemovedMusic&&index==Profile.Settings.Track?"♫  ":"")+track.Title+(track.Artist.Length>0?" · "+track.Artist:""),()=>{if(showRemovedMusic){audio.SetTrackEnabled(track.File,true);Save();RadioSelection();}else{audio.Select(index);Save();Build();RadioSelection();}},track.Custom?477:526,49,!showRemovedMusic&&index==Profile.Settings.Track);row.Children.Add(button);if(track.Custom){var rename=Button("✎",()=>RenameMusic(track),43,49);rename.Margin=new Thickness(6,0,0,0);rename.ToolTip=T("Rename music","إعادة تسمية الموسيقى");row.Children.Add(rename);}
     var remove=Button(showRemovedMusic?"↶":"×",()=>{audio.SetTrackEnabled(track.File,showRemovedMusic);Save();Build();RadioSelection();},48,49);remove.Margin=new Thickness(8,0,0,0);remove.Foreground=showRemovedMusic?B(p.Ink):Brushes.IndianRed;remove.ToolTip=showRemovedMusic?T("Restore music","استعادة الموسيقى"):T("Remove from radio","إزالة من الراديو");row.Children.Add(remove);list.Children.Add(row);
    }
    if(list.Children.Count==0)list.Children.Add(Text(showRemovedMusic?T("No removed music.","لا توجد موسيقى مُزالة."):T("No active music. Restore a track or add your own.","لا توجد موسيقى نشطة. استعد مقطعاً أو أضف موسيقاك."),18,580));
    var scroll=new ScrollViewer{Width=619,Height=224,Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(c,scroll,90,191);
    Btn(c,Profile.Settings.Music?T("Pause music","إيقاف الموسيقى مؤقتاً"):T("Play music","تشغيل الموسيقى"),94,427,()=>{audio.Toggle();Save();Build();RadioSelection();},260,47);
    Button addMusic=null;addMusic=Button(T("Import music  ▾","استيراد موسيقى  ▾"),()=>OpenMusicImportMenu(addMusic),322,47);Put(c,addMusic,374,427);
    Label(c,T("MP3, WAV or M4A · files are copied into your local game folder.","MP3 أو WAV أو M4A · تُنسخ الملفات إلى مجلد اللعبة المحلي."),94,494,13,604);
    Label(c,"Music: Purrple Cat · purrplecat.itch.io/just-relax",94,528,12,604,true);
    if(audio.Error.Length>0)Label(c,audio.Error,94,562,13,604);
   },620);
  }
  void ImportMusic(){
   var dialog=new OpenFileDialog{Title=T("Choose music files","اختر ملفات الموسيقى"),Filter="Music files|*.mp3;*.wav;*.m4a",Multiselect=true};
   if(dialog.ShowDialog(this)!=true)return;ImportMusicFiles(dialog.FileNames);
  }
  void ImportMusicFiles(string[] sources){
   if(sources.Length==0)return;try{
    foreach(string source in sources){
     string relative=Path.Combine("data","music",Guid.NewGuid().ToString("N"),Path.GetFileName(source));string dest=AudioSystem.Resolve(relative);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(source,dest);audio.AddTrack(relative);
    }
    audio.Select(audio.Tracks.Count-sources.Length);Save();Build();RadioSelection();
   }catch(Exception ex){Message(T("Could not add music","تعذّرت إضافة الموسيقى"),ex.Message);}
  }
 }
}
