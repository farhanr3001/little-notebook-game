using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LittleNotebook {
 public partial class MainWindow {
  string lastAutoReadKey="";Task<bool> questionReadTask;Button questionSpeaker;int questionReadSerial;
  string NarrationKey{get{return game==null?"":game.Index+":"+game.Current.Id+":"+Profile.Settings.Language;}}
  void AddQuestionSpeaker(Canvas note){
   questionSpeaker=null;if(game.Current.Kind=="search"||!QuestionSpeech.Make(game.Current,Ar).All(segment=>audio.QuestionVoiceAvailable(segment.Language)))return;
   questionSpeaker=Button("",ReadQuestion,43,42);questionSpeaker.Content=new System.Windows.Shapes.Path{Data=Geometry.Parse("M2,10 L7,10 L14,4 L14,24 L7,18 L2,18 Z M18,8 Q25,14 18,20 M22,4 Q34,14 22,24"),Width=29,Height=27,Stretch=Stretch.Uniform,Stroke=B(p.Ink),StrokeThickness=1.7,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};questionSpeaker.ToolTip=T("Read question aloud","قراءة السؤال بصوت عالٍ");AutomationProperties.SetName(questionSpeaker,T("Read question aloud","قراءة السؤال بصوت عالٍ"));questionSpeaker.IsEnabled=!game.Answered&&audio.QuestionVoiceAvailable(Ar?"ar":"en");Put(note,questionSpeaker,837,5);
  }
  async void ReadQuestion(){
   if(game==null||game.Answered||page!="game"||paused)return;int serial=++questionReadSerial;string key=NarrationKey;var current=game;var button=questionSpeaker;if(button!=null)button.ToolTip=T("Preparing speech…","جارٍ تحضير الصوت…");questionReadTask=audio.ReadQuestionAsync(QuestionSpeech.Make(game.Current,Ar));bool success=await questionReadTask;if(serial!=questionReadSerial)return;if(button!=null)button.ToolTip=T("Read question aloud","قراءة السؤال بصوت عالٍ");if(!success&&game==current&&key==NarrationKey&&!game.Answered&&!paused&&audio.SpeechError.Length>0)Message(T("Speech unavailable","الصوت غير متاح"),audio.SpeechError);
  }
  void MaybeAutoReadQuestion(){
   if(game==null||page!="game"||paused||game.Answered||questionSpeaker==null||!Profile.Settings.AutoReadQuestions||lastAutoReadKey==NarrationKey)return;string key=NarrationKey;var current=game;
   Dispatcher.BeginInvoke(new Action(()=>{if(game!=current||key!=NarrationKey||page!="game"||paused||game.Answered||questionSpeaker==null||!Profile.Settings.AutoReadQuestions||lastAutoReadKey==key)return;lastAutoReadKey=key;ReadQuestion();}),DispatcherPriority.Background);
  }
 }
}
