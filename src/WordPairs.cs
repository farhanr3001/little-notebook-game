using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LittleNotebook {
 public partial class MainWindow {
  int wordPairLeft=-1,wordPairWrong=-1;int[] wordPairRight;HashSet<int> wordPairsMatched=new HashSet<int>();
  void RenderWordPairs(){
   var q=game.Current;bool repair=q.Category=="spelling-repair",dictionary=q.Category=="dictionary";var pairs=q.Pairs;int count=pairs.Length;double step=count==4?52:count==5?43:36,height=count==4?43:count==5?37:32;if(repair){step=56;height=32;}if(dictionary){step=56;height=53;}
   Label(interaction,repair?T("Sound spelling","كتابة حسب الصوت"):"English",45,0,14,365,true).TextAlignment=TextAlignment.Center;Label(interaction,repair?T("Correct spelling & meaning","الإملاء الصحيح والمعنى"):dictionary?T("Meaning","المعنى"):"العربية",524,0,14,365).TextAlignment=TextAlignment.Center;
   for(int i=0;i<count;i++){
    int left=i,right=wordPairRight[i];string[] l=pairs[left].Split('|'),r=pairs[right].Split('|');
    var english=Button((wordPairsMatched.Contains(left)?"✓  ":"")+l[0],()=>{wordPairLeft=left;RenderInteraction();},386,height,wordPairsMatched.Contains(left)||wordPairLeft==left);english.IsEnabled=!game.Answered&&!wordPairsMatched.Contains(left);english.FlowDirection=FlowDirection.LeftToRight;if(wordPairsMatched.Contains(left))english.Opacity=1;english.FontFamily=new System.Windows.Media.FontFamily("Segoe Print");english.FontSize=18*Profile.Settings.TextScale;Put(interaction,english,28,31+i*step);
    var arabic=Button((wordPairsMatched.Contains(right)?"✓  ":"")+(dictionary&&Ar?q.PairArabic[right]:r[1]),()=>MatchWordPair(right),386,height,wordPairsMatched.Contains(right));arabic.FlowDirection=Ar&&!repair?FlowDirection.RightToLeft:FlowDirection.LeftToRight;arabic.IsEnabled=!game.Answered&&!wordPairsMatched.Contains(right);if(wordPairsMatched.Contains(right))arabic.Opacity=1;arabic.FontSize=(dictionary?15:19)*Profile.Settings.TextScale;Put(interaction,arabic,520,31+i*step);
    if(repair&&wordPairsMatched.Contains(right)){var meaning=Label(interaction,Ar?q.PairArabic[right]:q.PairMeanings[right],520,65+i*step,12,386);meaning.MaxHeight=22;}
    if(game.Answered&&wordPairWrong>=0){if(left==wordPairLeft)english.Background=B(System.Windows.Media.Color.FromRgb(218,160,136));if(right==wordPairWrong)arabic.Background=B(System.Windows.Media.Color.FromRgb(218,160,136));else if(right==wordPairLeft){arabic.Background=B(p.Accent);arabic.Foreground=B(p.AccentInk);}}
   }
  }
  void MatchWordPair(int right){
   if(wordPairLeft<0||game.Answered||wordPairsMatched.Contains(right))return;
   if(wordPairLeft!=right){wordPairWrong=right;Submit(game.Current.Pairs[wordPairLeft].Split('|')[0]+" = "+game.Current.Pairs[right].Split('|')[1],false);return;}
   wordPairsMatched.Add(right);wordPairLeft=-1;audio.Fx("correct");if(wordPairsMatched.Count==game.Current.Pairs.Length)Submit("paired",false);else RenderInteraction();
  }
 }
}
